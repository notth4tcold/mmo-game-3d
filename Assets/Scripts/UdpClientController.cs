using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

public class UdpClientController {
    private IPEndPoint endPoint;
    private UdpClient socket;
    private string ip;
    private int port;

    public UdpClientController(UdpClient socket, string ip, int port) {
        this.socket = socket;
        this.port = port;
        this.ip = ip;
        this.endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
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
            Debug.Log("Err. receiving udp data! Disconnecting client udp...");
            Client.instance.disconnect();
        }
    }

    private void handleData(byte[] data) {
        Packet packet = new Packet(data);

        int id = packet.ReadInt(); //só para remover o id do pacote
        string method = packet.ReadString();

        MethodInfo theMethod = Client.instance.GetType().GetMethod(method);
        theMethod.Invoke(Client.instance, new object[] { packet });
    }

    public void sendData(Packet packet) {
        try {
            if (socket == null) return;
            socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
        } catch {
            Debug.Log("Err. sending udp to server!");
        }
    }

    public void disconnect() {
        socket.Close();
        endPoint = null;
        socket = null;
    }
}
