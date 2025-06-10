using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class Server : MonoBehaviour {
    public static Server instance;
    public GameObject playerPrefab;

    private TcpListener tcpListener;
    private UdpServerController udpListener;
    private int totalPlayers = 0;
    private int maxPlayers = 50;
    private int port = 8080;

    private uint currentTick;
    private bool sendState;

    public Dictionary<string, ServerPlayer> players = new Dictionary<string, ServerPlayer>();

    private float timer;
    private float timeBetweenTicks;
    private const float tickRate = 120f;
    private float latency = 0.2f;
    private float packetLossChance = 0.05f;

    private float minInputBufferSizeToStart = 8;
    private float inputBufferIsLow = 4;
    private float inputBufferIsHigh = 15;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    void Start() {
        this.timeBetweenTicks = 1f / tickRate;
        startServer();
    }

    void Update() {
        this.timer += Time.deltaTime;

        while (this.timer >= this.timeBetweenTicks) {
            this.timer -= timeBetweenTicks;
            this.currentTick++;
            handleTick(this.timeBetweenTicks);
        }
    }

    void handleTick(float tickTime) {
        foreach (var player in this.players.Values) {
            if (!player.isReady || player.getInputsSize() < 0) continue;
            Inputs inputs = player.getNextInputs(currentTick);

            if (inputs != null) {
                player.playerController.movePlayer(inputs);
                this.sendState = true;
            }
        }

        InstanciateSceneController.Instance.serverPhysicsScene.Simulate(tickTime);

        if (this.sendState) {
            StatePayload state = new StatePayload(this.currentTick);
            foreach (ServerPlayer player in this.players.Values) {
                if (!player.isReady) continue;
                state.playerstates.Add(player.id, new PlayerState(player.id, player.rb));
            }

            Packet packet = new Packet();
            packet.Write("OnPlayerMovementState");
            packet.Write(state.tick);
            packet.Write(state.playerstates.Count);
            foreach (PlayerState playerState in state.playerstates.Values) {
                packet.Write(playerState);
            }

            foreach (PlayerState playerState in state.playerstates.Values) {
                int inputBufferSize = players[playerState.id].getInputsSize();
                packet.Write(playerState.id);
                if (inputBufferSize <= inputBufferIsLow) {
                    packet.Write("inputBufferIsLow");
                } else if (inputBufferSize >= inputBufferIsHigh) {
                    packet.Write("inputBufferIsHigh");
                } else {
                    packet.Write("inputBufferIsOk");
                }
            }

            if (Random.value > this.packetLossChance) ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendUdpDataToAll(packet)); });

            this.sendState = false;
        }
    }

    void startServer() {
        this.udpListener = new UdpServerController(new UdpClient(this.port));
        this.udpListener.connect();

        this.tcpListener = new TcpListener(IPAddress.Any, this.port);
        this.tcpListener.Start();
        this.tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(tcpConnectCallback), null);

        Debug.Log("Server started!");
    }

    private void tcpConnectCallback(System.IAsyncResult result) {
        TcpClient socket = this.tcpListener.EndAcceptTcpClient(result);
        this.tcpListener.BeginAcceptTcpClient(new System.AsyncCallback(tcpConnectCallback), null);

        if (this.totalPlayers >= this.maxPlayers) {
            Debug.Log("Server is full.");
            return;
        }

        string newPlayerId = System.Guid.NewGuid().ToString();
        ServerPlayer player = new ServerPlayer(newPlayerId);
        TcpServerController tcp = new TcpServerController(socket, newPlayerId);

        player.tcp = tcp;
        tcp.connect();

        this.players.Add(player.id, player);
        sendConnectByTcp(player.id);

        this.totalPlayers++;

        Debug.Log("New player connected by tcp!");
    }

    private void sendConnectByTcp(string id) {
        Packet packet = new Packet();
        packet.Write("OnConnectByTcp");
        packet.Write(id);

        ThreadManager.ExecuteOnMainThread(() => {
            StartCoroutine(sendTcpData(id, packet));

            // SEND BACK ONLINE PLAYERS
            foreach (var player in this.players.Values) {
                if (player.id != id) {
                    packet = new Packet();
                    packet.Write("OnRequestSpawnEnemy");
                    packet.Write(player.id);
                    packet.Write(player.username);
                    packet.Write(player.rb.position);
                    packet.Write(player.rb.rotation);
                    StartCoroutine(sendTcpData(id, packet));
                }
            }
        });
    }

    public void OnConnectByUdp(string id, Packet packet) {
        Debug.Log("new connection UDP client: " + id);
        Packet packetSend = new Packet();
        packetSend.Write("OnConnectByUdp");
        ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendUdpData(id, packetSend)); });
    }

    public void OnRequestSpawnPosition(Packet packet) {
        string id = packet.ReadString();
        string username = packet.ReadString();

        this.players[id].username = username;

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        packet = new Packet();
        packet.Write("OnRequestSpawnPosition");
        packet.Write(position);
        packet.Write(rotation);
        packet.Write(this.currentTick);
        ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendTcpData(id, packet)); });
    }

    public void OnRequestSpawnPlayer(Packet packet) {
        string id = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        ThreadManager.ExecuteOnMainThread(() => {
            var playerSimulatorGo = Instantiate(this.playerPrefab, gameObject.scene) as GameObject;
            playerSimulatorGo.GetComponent<Renderer>().material.color = Color.red;

            playerSimulatorGo.transform.position = position;
            playerSimulatorGo.GetComponent<Rigidbody>().position = position;
            playerSimulatorGo.transform.rotation = rotation;
            playerSimulatorGo.GetComponent<Rigidbody>().rotation = rotation;

            this.players[id].rb = playerSimulatorGo.GetComponent<Rigidbody>();
            this.players[id].playerController = playerSimulatorGo.GetComponent<PlayerController>();

            Debug.Log("SERVER SPAWN TICK: " + this.currentTick);

            // SEND NEW PLAYER TO ALL
            packet = new Packet();
            packet.Write("OnRequestSpawnEnemy");
            packet.Write(this.players[id].id);
            packet.Write(this.players[id].username);
            packet.Write(this.players[id].rb.position);
            packet.Write(this.players[id].rb.rotation);
            StartCoroutine(sendTcpDataToAll(id, packet));
        });
    }

    public void OnClientInput(string id, Packet packet) {
        int totalInputs = packet.ReadInt();

        for (int i = 0; i < totalInputs; i++) {
            Inputs inputs = packet.ReadInputs();

            if (inputs.tick > this.players[id].lastInputTickRecieved) {
                this.players[id].lastInputTickRecieved = inputs.tick;
                this.players[id].addInputs(inputs);
            }
        }

        if (!this.players[id].isReady && this.players[id].getInputsSize() >= this.minInputBufferSizeToStart) {
            ThreadManager.ExecuteOnMainThread(() => {
                this.players[id].rb.isKinematic = false;
                this.players[id].isReady = true;

                Debug.Log("SERVER TICK: " + this.currentTick);
            });
        }
    }

    public void setEndPointUdp(string id, IPEndPoint endPoint) {
        this.players[id].endPointUdp = endPoint;
    }

    IEnumerator sendTcpData(string id, Packet packet) {
        yield return new WaitForSeconds(this.latency);

        packet.WriteLength();
        this.players[id].tcp.sendData(packet);
    }

    IEnumerator sendTcpDataToAll(Packet packet) {
        yield return new WaitForSeconds(this.latency);

        packet.WriteLength();
        foreach (ServerPlayer player in this.players.Values) {
            player.tcp.sendData(packet);
        }
    }

    IEnumerator sendTcpDataToAll(string exceptClient, Packet packet) {
        yield return new WaitForSeconds(this.latency);

        packet.WriteLength();
        foreach (ServerPlayer player in this.players.Values) {
            if (player.id != exceptClient) {
                player.tcp.sendData(packet);
            }
        }
    }

    IEnumerator sendUdpData(string id, Packet packet) {
        yield return new WaitForSeconds(this.latency);

        packet.WriteLength();
        if (this.players[id].endPointUdp != null) {
            this.udpListener.sendData(packet, this.players[id].endPointUdp);
        }
    }

    IEnumerator sendUdpDataToAll(Packet packet) {
        yield return new WaitForSeconds(this.latency);

        packet.WriteLength();
        foreach (ServerPlayer player in this.players.Values) {
            if (player.endPointUdp != null) {
                this.udpListener.sendData(packet, player.endPointUdp);
            }
        }
    }

    IEnumerator sendUdpDataToAll(string exceptClient, Packet packet) {
        yield return new WaitForSeconds(this.latency);

        packet.WriteLength();
        foreach (ServerPlayer player in this.players.Values) {
            if (player.id != exceptClient) {
                if (player.endPointUdp != null) {
                    this.udpListener.sendData(packet, player.endPointUdp);
                }
            }
        }
    }

    public void disconnectPlayer(string id) {

        ServerPlayer player = this.players[id];
        if (player == null) return;

        player.disconnect();
        this.players.Remove(id);

        ThreadManager.ExecuteOnMainThread(() => {
            Destroy(player.rb.gameObject);
        });

        Packet packet = new Packet();
        packet.Write("OnDisconnectEnemy");
        packet.Write(id);

        ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendTcpDataToAll(id, packet)); });

        Debug.Log("Player [id: " + id + " name: " + player.username + "] has disconnect!");
    }

    public void disconnectServer() {
        this.udpListener = null;

        foreach (ServerPlayer player in this.players.Values) {
            disconnectPlayer(player.id);
        }
    }

    private void OnApplicationQuit() {
        disconnectServer();
    }
}
