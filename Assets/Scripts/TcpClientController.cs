using UnityEngine;
using System.Net.Sockets;
using System;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Linq;

public class TcpClientController {
    private int dataBufferSize = 4096;
    private int retryCount = 0;
    private TcpClient socket;
    private NetworkStream stream;
    private byte[] recieveBuffer;
    private Packet receiveData;
    private string ip;
    private int port;

    public long totalBytesSent { get; private set; }
    public long totalBytesReceived { get; private set; }
    public double bytesUploadRate { get; private set; }
    public double bytesDownloadRate { get; private set; }

    public int totalPacketsSent { get; private set; }
    public int totalPacketsReceived { get; private set; }
    public double packetsUploadRate { get; private set; }
    public double packetsDownloadRate { get; private set; }

    //AUX VAR TO CONTROL BYTES/PACKETS DATA
    private long lastBytesSent = 0;
    private long lastBytesReceived = 0;
    public int packetsSent { get; private set; }
    public int packetsReceived { get; private set; }
    private DateTime lastCheck;

    private List<long> pingTimes = new List<long>();

    public TcpClientController(TcpClient socket, string ip, int port) {
        this.socket = socket;
        this.port = port;
        this.ip = ip;
        this.lastCheck = DateTime.Now;
    }

    public void connect() {
        socket.ReceiveBufferSize = dataBufferSize;
        socket.SendBufferSize = dataBufferSize;

        receiveData = new Packet();
        recieveBuffer = new byte[dataBufferSize];

        retryCount = 10;
        socket.BeginConnect(ip, port, connectCallback, socket);
    }

    private void connectCallback(System.IAsyncResult result) {
        try {
            socket.EndConnect(result);
            if (!socket.Connected) return;

            stream = socket.GetStream();
            stream.BeginRead(recieveBuffer, 0, dataBufferSize, receiveCallback, null);
        } catch (System.Exception e) {
            if (retryCount > 0) {
                retryCount--;
                Debug.Log(e);
                Debug.Log("Connection failed. Retrying...");
                socket.BeginConnect(ip, port, new AsyncCallback(connectCallback), socket);
            } else {
                Debug.Log("Can't connect to server.");
            }
        }
    }

    private void receiveCallback(System.IAsyncResult result) {
        try {
            int byteLenght = stream.EndRead(result);

            if (byteLenght <= 0) {
                Debug.Log("Disconnecting client tcp... \n DATA LENGHT < 4");
                Client.instance.disconnect();
                return;
            }

            byte[] data = new byte[byteLenght];
            System.Array.Copy(recieveBuffer, data, byteLenght);
            receiveData.Reset(handleData(data));

            stream.BeginRead(recieveBuffer, 0, dataBufferSize, receiveCallback, null);
        } catch (System.Exception e) {
            Debug.Log(e);
            Debug.Log("Disconnecting client tcp...");
            Client.instance.disconnect();
        }
    }

    private bool handleData(byte[] data) {
        int packetLenght = 0;

        receiveData.SetBytes(data);
        totalBytesReceived += data.Length;
        totalPacketsReceived++;
        packetsReceived++;

        if (receiveData.UnreadLength() >= 4) {
            packetLenght = receiveData.ReadInt();
            if (packetLenght <= 0) return true;
        }

        while (packetLenght > 0 && packetLenght <= receiveData.UnreadLength()) {
            byte[] packetBytes = receiveData.ReadBytes(packetLenght);
            Packet packet = new Packet(packetBytes);

            string method = packet.ReadString();

            MethodInfo theMethod = Client.instance.GetType().GetMethod(method);
            theMethod.Invoke(Client.instance, new object[] { packet });

            packetLenght = 0;

            if (receiveData.UnreadLength() >= 4) {
                packetLenght = receiveData.ReadInt();
                if (packetLenght <= 0) return true;
            }
        }

        return packetLenght <= 1;
    }


    public TcpClient getSocket() {
        return socket;
    }

    public void sendData(Packet packet) {
        try {
            if (socket == null) return;
            stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
            totalBytesSent += packet.Length();
            totalPacketsSent++;
            packetsSent++;
        } catch {
            Debug.Log("Err. sending tcp to server!");
        }
    }

    public long getPing() {
        using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping()) {
            PingReply reply = ping.Send(ip);
            if (reply.Status == IPStatus.Success) {
                pingTimes.Add(reply.RoundtripTime);
                return reply.RoundtripTime;
            } else {
                Debug.Log("Ping falhou: " + reply.Status);
                return -1;
            }
        }
    }

    public double getJitter() {
        if (pingTimes.Count < 2) return 0;
        List<long> deltas = new List<long>();
        for (int i = 1; i < pingTimes.Count; i++) {
            deltas.Add(Math.Abs(pingTimes[i] - pingTimes[i - 1]));
        }
        return deltas.Average();
    }

    public void updateThroughput() {
        var now = DateTime.Now;
        var timeDiff = (now - lastCheck).TotalSeconds;

        if (timeDiff < 1) return;

        bytesUploadRate = (totalBytesSent - lastBytesSent) / timeDiff;
        bytesDownloadRate = (totalBytesReceived - lastBytesReceived) / timeDiff;
        packetsUploadRate = packetsSent / timeDiff;
        packetsDownloadRate = packetsReceived / timeDiff;

        lastBytesSent = totalBytesSent;
        lastBytesReceived = totalBytesReceived;
        packetsSent = 0;
        packetsReceived = 0;
        lastCheck = now;
    }

    public void disconnect() {
        socket.Close();
        stream = null;
        receiveData = null;
        recieveBuffer = null;
        socket = null;
        totalBytesSent = 0;
        totalBytesReceived = 0;
        totalPacketsSent = 0;
        totalPacketsReceived = 0;
        packetsSent = 0;
        packetsReceived = 0;
    }
}
