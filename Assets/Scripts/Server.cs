using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Linq;

public class Server : MonoBehaviour {
    public static Server instance;

    public GameObject playerPrefab;

    private TcpListener tcpListener;
    private UdpServerController udpListener;
    private int totalPlayers = 0;
    private int maxPlayers = 50;
    private int port = 8080;

    Dictionary<string, ServerPlayer> players = new Dictionary<string, ServerPlayer>();

    private float timer;
    private float timeBetweenTicks;
    private float latency = 2f;
    private const int bufferSize = 1024;
    //private const float serverTickRate = 60f; //timeBetweenTicks = 1f / serverTickRate;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    void Start() {
        timer = 0.0f;
        startServer();
    }

    void Update() {
        timeBetweenTicks = Time.fixedDeltaTime;
        timer += Time.deltaTime;

        while (timer >= timeBetweenTicks && someoneNeedsToHandleTick()) {
            timer -= timeBetweenTicks;
            handleTick(timeBetweenTicks);
        }

        sendStateToClient();
    }

    private bool someoneNeedsToHandleTick() {
        return players.Count > 0;
        // foreach (var player in players.Values) {
        //     if (player.receiveFromClientInputQueue.Count > 0) return true;
        // }
        // return false;
    }

    void handleTick(float timeBetweenTicks) {
        foreach (var player in players.Values) {
            if (player.receiveFromClientInputQueue.Count == 0) return;

            InputPayload inputPayload = player.receiveFromClientInputQueue.Dequeue();

            if (inputPayload.tick >= player.currentTick) {
                player.playerController.movePlayer(inputPayload.inputs);
                player.hasStateToSend = true;
            }
        }
        InstanciateSceneController.Instance.serverPhysicsScene.Simulate(timeBetweenTicks);

        foreach (var player in players.Values) {
            if (player.hasStateToSend) {
                player.currentTick++;
                player.sendToClientStateQueue.Enqueue(new StatePayload(player.currentTick, player.rb, latency));
                player.hasStateToSend = false;
            }
        }
    }

    void startServer() {
        udpListener = new UdpServerController(new UdpClient(port));
        udpListener.connect();

        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(tcpConnectCallback), null);

        Debug.Log("Server started!");
    }

    private void tcpConnectCallback(System.IAsyncResult result) {
        TcpClient socket = tcpListener.EndAcceptTcpClient(result);
        tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(tcpConnectCallback), null);

        if (totalPlayers >= maxPlayers) {
            Debug.Log("Server is full.");
            return;
        }

        string newPlayerId = System.Guid.NewGuid().ToString();
        ServerPlayer player = new ServerPlayer(newPlayerId);
        TcpServerController tcp = new TcpServerController(socket, newPlayerId);

        player.tcp = tcp;
        tcp.connect();

        players.Add(player.id, player);
        sendConnectByTcp(player.id);

        totalPlayers++;

        Debug.Log("New player connected by tcp!");
    }

    private void sendConnectByTcp(string id) {
        Packet packet = new Packet();
        packet.Write("OnConnectByTcp");
        packet.Write(id);

        sendTcpData(id, packet);
    }

    public void OnConnectByUdp(string id, Packet packet) {
        Debug.Log("new connection UDP client: " + id);
        Packet packetSend = new Packet();
        packetSend.Write("OnConnectByUdp");
        sendUdpData(id, packetSend);
    }

    public void OnRequestSpawnPlayer(Packet packet) {
        string id = packet.ReadString();
        string username = packet.ReadString();

        ThreadManager.ExecuteOnMainThread(() => {
            var playerSimulatorGo = Instantiate(playerPrefab, gameObject.scene) as GameObject;
            playerSimulatorGo.GetComponent<Renderer>().material.color = Color.red;

            players[id].username = username;
            players[id].rb = playerSimulatorGo.GetComponent<Rigidbody>();
            players[id].playerController = playerSimulatorGo.GetComponent<PlayerController>();
        });

        packet = new Packet();
        packet.Write("OnRequestSpawnPlayer");
        sendTcpData(id, packet);

        packet = new Packet();
        packet.Write("OnRequestSpawnEnemy");
        packet.Write(id);
        packet.Write(username);
        sendTcpDataToAll(packet);
        sendTcpDataToAll(id, packet);
    }

    void sendStateToClient() {
        foreach (var player in players.Values) {
            if (player.sendToClientStateQueue.Count > 0 && Time.time >= player.sendToClientStateQueue.Peek().deliveryTime) {
                Packet packet = new Packet();
                packet.Write("OnServerMovementState");
                packet.Write(player.sendToClientStateQueue.Dequeue());
                sendUdpData(player.id, packet);
            }
        }
    }

    public void OnClientInput(string id, Packet packet) {
        InputPayload inputPayload = packet.ReadInputPayload();
        players[id].receiveFromClientInputQueue.Enqueue(inputPayload);
    }

    public void setEndPointUdp(string id, IPEndPoint endPoint) {
        players[id].endPointUdp = endPoint;
    }

    public void sendTcpData(string id, Packet packet) {
        packet.WriteLength();
        players[id].tcp.sendData(packet);
    }

    public void sendTcpDataToAll(Packet packet) {
        packet.WriteLength();

        foreach (ServerPlayer player in players.Values) {
            player.tcp.sendData(packet);
        }
    }

    public void sendTcpDataToAll(string exceptClient, Packet packet) {
        packet.WriteLength();
        foreach (ServerPlayer player in players.Values) {
            if (player.id != exceptClient) {
                player.tcp.sendData(packet);
            }
        }
    }

    public void sendUdpData(string id, Packet packet) {
        packet.WriteLength();
        udpListener.sendData(packet, players[id].endPointUdp);
    }

    public void sendUdpDataToAll(Packet packet) {
        packet.WriteLength();
        foreach (ServerPlayer player in players.Values) {
            udpListener.sendData(packet, player.endPointUdp);
        }
    }

    public void sendUdpDataToAll(string exceptClient, Packet packet) {
        packet.WriteLength();
        foreach (ServerPlayer player in players.Values) {
            if (player.id != exceptClient) {
                udpListener.sendData(packet, player.endPointUdp);
            }
        }
    }

    public void disconnectPlayer(string id) {

        ServerPlayer player = players[id];
        if (player == null) return;

        player.disconnect();
        players.Remove(player.id);

        Packet packet = new Packet();
        packet.Write("playerDisconnect");
        packet.Write(player.id);

        sendTcpDataToAll(player.id, packet);

        Debug.Log("Player [id: " + player.id + " name: " + player.username + "] has disconnect!");
    }

    public void disconnectServer() {
        udpListener = null;

        foreach (ServerPlayer player in players.Values) {
            disconnectPlayer(player.id);
        }
    }

    private void OnApplicationQuit() {
        disconnectServer();
    }
}
