using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System;

public class UdpClientController {
    private IPEndPoint endPoint;
    private UdpClient socket;
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

    public UdpClientController(UdpClient socket, string ip, int port) {
        this.socket = socket;
        this.port = port;
        this.ip = ip;
        this.endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        this.lastCheck = DateTime.Now;
    }

    public void connect() {
        socket.Connect(endPoint);
        socket.BeginReceive(receiveCallback, null);
    }

    private void receiveCallback(System.IAsyncResult result) {
        try {
            byte[] data = socket.EndReceive(result, ref endPoint);
            socket.BeginReceive(receiveCallback, null);
            if (data.Length < 4) {
                Debug.Log("Err. receiving udp data! Disconnecting client udp... \n DATA LENGHT < 4");
                Client.instance.disconnect();
                return;
            }

            handleData(data);
        } catch (System.Exception e) {
            Debug.Log(e);
            Debug.Log("Err. receiving udp data!");
            Client.instance.disconnect();
        }
    }

    private void handleData(byte[] data) {
        Packet packet = new Packet(data);
        totalBytesReceived += data.Length;
        totalPacketsReceived++;
        packetsReceived++;

        int id = packet.ReadInt(); //só para remover o id do pacote
        string method = packet.ReadString();

        MethodInfo theMethod = Client.instance.GetType().GetMethod(method);
        theMethod.Invoke(Client.instance, new object[] { packet });
    }

    public void sendData(Packet packet) {
        try {
            if (socket == null) return;
            socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
            totalBytesSent += packet.Length();
            totalPacketsSent++;
            packetsSent++;
        } catch {
            Debug.Log("Err. sending udp to server!");
        }
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

    public double GetPacketLossRate() {
        // TODO CORRIGIR PARA CALCULAR CORRETAMENTE OS PACOTES QUE NAO VIERAM USANDO O TICK DO PACOTE
        if (totalPacketsReceived <= 0 || totalPacketsSent <= 0) return 0;
        return (double)(totalPacketsSent - totalPacketsReceived) / totalPacketsSent * 100;
    }

    public void disconnect() {
        socket.Close();
        endPoint = null;
        socket = null;
        totalBytesSent = 0;
        totalBytesReceived = 0;
        totalPacketsSent = 0;
        totalPacketsReceived = 0;
        packetsSent = 0;
        packetsReceived = 0;
    }
}
