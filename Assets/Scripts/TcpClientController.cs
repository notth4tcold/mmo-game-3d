using UnityEngine;
using System.Net.Sockets;
using System;
using System.Reflection;

public class TcpClientController {
    private int dataBufferSize = 4096;
    private int retryCount = 0;
    private TcpClient socket;
    private NetworkStream stream;
    private byte[] recieveBuffer;
    private Packet receiveData;
    private string ip;
    private int port;

    public TcpClientController(TcpClient socket, string ip, int port) {
        this.socket = socket;
        this.port = port;
        this.ip = ip;
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
        } catch {
            Debug.Log("Err. sending tcp to server!");
        }
    }

    public void disconnect() {
        socket.Close();
        stream = null;
        receiveData = null;
        recieveBuffer = null;
        socket = null;
    }
}
