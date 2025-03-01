using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour {
    public static Client instance;
    public GameObject playerPrefab;

    public string id;
    private string ip = "192.168.0.15";
    private int port = 8080;

    public bool isReady = false;
    public uint currentTick;
    private uint lastStateReceivedTick;

    public TcpClientController tcp;
    public UdpClientController udp;

    private float timer;
    private float timeBetweenTicks;

    private const float tickRate = 120f;
    private const float tickRateFastPace = 140f;
    private const float tickRateSlowPace = 100f;
    private const int bufferSize = 1024;

    public float latency = 0.2f;
    public float packetLossChance = 0.2f;

    public double tickRateCheck;
    private int ticks;
    private float lastTickCheck;
    private uint maxRedundantInputsToSend = 20;

    public Dictionary<string, Player> players = new Dictionary<string, Player>();

    public StatePayload[] stateBuffer { get; set; }
    public Inputs[] inputBuffer { get; set; }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    void Start() {
        this.timer = 0.0f;
        this.currentTick = 0;
        this.lastStateReceivedTick = 0;
        this.ticks = 0;
        this.tickRateCheck = 0;
        this.timeBetweenTicks = 1f / tickRate;
        this.lastTickCheck = Time.time;
        this.stateBuffer = new StatePayload[bufferSize];
        this.inputBuffer = new Inputs[bufferSize];
        connectToTcp();
    }

    void Update() {
        this.timer += Time.deltaTime;

        while (this.timer >= this.timeBetweenTicks) {
            this.timer -= this.timeBetweenTicks;
            handleTick(this.timeBetweenTicks);
        }
    }

    void handleTick(float tickTime) {
        if (this.isReady) {
            uint bufferIndex = this.currentTick % bufferSize;

            var newInputs = new Inputs(this.currentTick);
            newInputs.SetInputs();
            this.inputBuffer[bufferIndex] = newInputs;

            StatePayload statePayload = new StatePayload(this.currentTick, this.players);
            this.stateBuffer[bufferIndex] = statePayload;

            this.players[id].playerController.movePlayer(newInputs);
            InstanciateSceneController.Instance.clientPhysicsScene.Simulate(tickTime);

            InputPayload inputPayload = new InputPayload(this.currentTick);

            uint lastTickCheck = this.lastStateReceivedTick;
            if (this.currentTick - lastTickCheck > this.maxRedundantInputsToSend) lastTickCheck = this.currentTick - this.maxRedundantInputsToSend;
            for (uint tick = lastTickCheck; tick <= this.currentTick; ++tick) {
                if (this.inputBuffer[tick % bufferSize] == null) {
                    continue;
                }
                inputPayload.inputList.Add(this.inputBuffer[tick % bufferSize]);
            }

            Packet packet = new Packet();
            packet.Write("OnClientInput");
            packet.Write(id);
            packet.Write(inputPayload.inputList.Count);
            foreach (Inputs inputs in inputPayload.inputList) {
                packet.Write(inputs);
            }

            if (Random.value > this.packetLossChance) ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendUdpData(packet)); });

            ++this.currentTick;
            this.ticks++;
        }
    }

    void connectToTcp() {
        this.tcp = new TcpClientController(new TcpClient(), this.ip, this.port);
        this.tcp.connect();
    }

    public void OnConnectByTcp(Packet packet) {
        this.id = packet.ReadString();
        var player = new Player(this.id);
        player.username = "Mei";
        this.players.Add(this.id, player);

        connectToUdp();
    }

    void connectToUdp() {
        UdpClient socketUdp = new UdpClient(((IPEndPoint)this.tcp.getSocket().Client.LocalEndPoint).Port);
        this.udp = new UdpClientController(socketUdp, this.ip, this.port);
        this.udp.connect();

        Packet packet = new Packet();
        packet.Write("OnConnectByUdp");
        packet.Write(this.id);
        ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendUdpData(packet)); });
    }

    public void OnConnectByUdp(Packet packet) {
        requestSpawnPosition();
    }

    void requestSpawnPosition() {
        Packet packet = new Packet();
        packet.Write("OnRequestSpawnPosition");
        packet.Write(this.id);
        packet.Write(this.players[this.id].username);
        ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendTcpData(packet)); });
    }

    public void OnRequestSpawnPosition(Packet packet) {
        uint serverTick = packet.ReadUint();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        this.currentTick = serverTick;
        this.lastStateReceivedTick = serverTick;

        Debug.Log("New player");
        ThreadManager.ExecuteOnMainThread(() => {
            var playerGO = Instantiate(this.playerPrefab, gameObject.scene) as GameObject;
            playerGO.GetComponent<Renderer>().material.color = Color.blue;

            playerGO.transform.position = position;
            playerGO.GetComponent<Rigidbody>().position = position;
            playerGO.transform.rotation = rotation;
            playerGO.GetComponent<Rigidbody>().rotation = rotation;

            this.players[id].rb = playerGO.GetComponent<Rigidbody>();
            this.players[id].playerController = playerGO.GetComponent<PlayerController>();
            this.players[id].rb.isKinematic = false;
            this.isReady = true;
        });

        requestSpawnPlayer(position, rotation);
    }

    void requestSpawnPlayer(Vector3 position, Quaternion rotation) {
        Packet packet = new Packet();
        packet.Write("OnRequestSpawnPlayer");
        packet.Write(this.id);
        packet.Write(position);
        packet.Write(rotation);
        ThreadManager.ExecuteOnMainThread(() => { StartCoroutine(sendTcpData(packet)); });
    }

    public void OnRequestSpawnEnemy(Packet packet) {
        var enemyId = packet.ReadString();
        var enemyName = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        if (this.players.ContainsKey(enemyId)) return;

        Debug.Log("New enemy: " + enemyName + " " + enemyId);
        ThreadManager.ExecuteOnMainThread(() => {
            var playerGO = Instantiate(this.playerPrefab, gameObject.scene) as GameObject;
            playerGO.GetComponent<Renderer>().material.color = Color.gray;

            playerGO.transform.position = position;
            playerGO.GetComponent<Rigidbody>().position = position;
            playerGO.transform.rotation = rotation;
            playerGO.GetComponent<Rigidbody>().rotation = rotation;

            Player enemyPlayer = new Player(enemyId);
            enemyPlayer.username = enemyName;
            enemyPlayer.rb = playerGO.GetComponent<Rigidbody>();
            enemyPlayer.playerController = playerGO.GetComponent<PlayerController>();
            enemyPlayer.rb.isKinematic = false;

            this.players.Add(enemyId, enemyPlayer);
        });
    }

    public void OnPlayerMovementState(Packet packet) {
        uint serverTick = packet.ReadUint();
        int totalPlayers = packet.ReadInt();

        StatePayload serverState = new StatePayload(serverTick);

        for (int i = 0; i < totalPlayers; i++) {
            PlayerState playerState = packet.ReadPlayerState();
            serverState.playerstates.Add(playerState.id, playerState);
        }

        for (int i = 0; i < totalPlayers; i++) {
            string clientRunPaceId = packet.ReadString();
            string clientRunPace = packet.ReadString();

            if (clientRunPaceId == this.id) {
                switch (clientRunPace) {
                    case "inputIsRunningLow":
                        ThreadManager.ExecuteOnMainThread(() => { this.timeBetweenTicks = 1f / tickRateFastPace; });
                        Debug.Log("inputIsRunningLow");
                        break;
                    case "inputIsRunningHigh":
                        ThreadManager.ExecuteOnMainThread(() => { this.timeBetweenTicks = 1f / tickRateSlowPace; });
                        Debug.Log("inputIsRunningHigh");
                        break;
                    default:
                        ThreadManager.ExecuteOnMainThread(() => { this.timeBetweenTicks = 1f / tickRate; });
                        Debug.Log("inputIsRunningOk");
                        break;
                }
            }
        }

        processNewPlayerStateFromServer(serverState);
    }

    void processNewPlayerStateFromServer(StatePayload statePayload) {
        if (!this.isReady || !statePayload.playerstates.ContainsKey(this.id)) return;

        this.lastStateReceivedTick = statePayload.tick;
        uint bufferIndex = statePayload.tick % bufferSize;

        foreach (var player in this.players.Values) {

            if (this.stateBuffer[bufferIndex] == null ||
                this.stateBuffer[bufferIndex].playerstates == null ||
                !this.stateBuffer[bufferIndex].playerstates.ContainsKey(player.id)) continue;

            Vector3 positionError = statePayload.playerstates[player.id].position - this.stateBuffer[bufferIndex].playerstates[player.id].position;
            float rotationError = 1.0f - Quaternion.Dot(statePayload.playerstates[player.id].rotation, this.stateBuffer[bufferIndex].playerstates[player.id].rotation);

            if (positionError.sqrMagnitude > 0.0000001f || rotationError > 0.00001f) {
                // if (positionError.sqrMagnitude > 0.000000001f) {
                //     Debug.Log("postion error" + positionError.sqrMagnitude);
                // }
                // if (rotationError > 0.0000001f) {
                //     Debug.Log("Rotation error" + rotationError);
                // }

                if (player.id == this.id) {
                    var msg = "Reconciliate - error at server tick " + statePayload.tick + " (rewinding " + (this.currentTick - statePayload.tick) + " ticks at tick " + this.currentTick + ")";
                    //Debug.Log(msg);
                    LogViewer.instance.Log(msg);
                } else {
                    var msg = "Enemy reconciliate - error at server tick " + statePayload.tick + " (rewinding " + (this.currentTick - statePayload.tick) + " ticks at tick " + this.currentTick + ")";
                    //Debug.Log(msg);
                    LogViewer.instance.Log(msg);
                }

                ThreadManager.ExecuteOnMainThread(() => {
                    foreach (var enemyPlayer in this.players.Values) {
                        this.players[enemyPlayer.id].rb.position = statePayload.playerstates[enemyPlayer.id].position;
                        this.players[enemyPlayer.id].rb.gameObject.transform.position = statePayload.playerstates[enemyPlayer.id].position;
                        this.players[enemyPlayer.id].rb.rotation = statePayload.playerstates[enemyPlayer.id].rotation;
                        this.players[enemyPlayer.id].rb.gameObject.transform.rotation = statePayload.playerstates[enemyPlayer.id].rotation;
                        this.players[enemyPlayer.id].rb.velocity = statePayload.playerstates[enemyPlayer.id].velocity;
                        this.players[enemyPlayer.id].rb.angularVelocity = statePayload.playerstates[enemyPlayer.id].angularVelocity;
                    }

                    uint rewind_tick = statePayload.tick;
                    while (rewind_tick < this.currentTick) {
                        bufferIndex = rewind_tick % bufferSize;

                        foreach (var playerRewind in this.players.Values) {
                            if (!this.stateBuffer[bufferIndex].playerstates.ContainsKey(playerRewind.id)) {
                                this.stateBuffer[bufferIndex].playerstates.Add(playerRewind.id, statePayload.playerstates[playerRewind.id]);
                            }
                            this.stateBuffer[bufferIndex].playerstates[playerRewind.id].position = this.players[playerRewind.id].rb.position;
                            this.stateBuffer[bufferIndex].playerstates[playerRewind.id].rotation = this.players[playerRewind.id].rb.rotation;
                        }

                        this.players[id].playerController.movePlayer(this.inputBuffer[bufferIndex]);

                        InstanciateSceneController.Instance.clientPhysicsScene.Simulate(timeBetweenTicks);

                        ++rewind_tick;
                    }
                });
            }
        }
    }

    public void updateTickRateCheck() {
        var now = Time.time;
        var timeDiff = now - this.lastTickCheck;

        if (timeDiff < 1) return;

        this.tickRateCheck = this.ticks / timeDiff;
        this.ticks = 0;
        this.lastTickCheck = now;
    }

    IEnumerator sendTcpData(Packet packet) {
        yield return new WaitForSeconds(this.latency);

        if (this.tcp != null) {
            packet.WriteLength();
            this.tcp.sendData(packet);
        }
    }

    IEnumerator sendUdpData(Packet packet) {
        yield return new WaitForSeconds(this.latency);

        if (this.udp != null) {
            packet.WriteLength();
            this.udp.sendData(packet);
        }
    }

    public void OnDisconnectEnemy(Packet packet) {
        string id = packet.ReadString();

        if (!this.players.ContainsKey(id)) return;

        ThreadManager.ExecuteOnMainThread(() => {
            Destroy(this.players[id].rb.gameObject);
        });

        this.players.Remove(id);
    }

    public void disconnect() {
        if (!this.isReady) return;
        this.isReady = false;

        this.tcp.disconnect();
        this.udp.disconnect();
        this.tcp = null;
        this.udp = null;

        ThreadManager.ExecuteOnMainThread(() => {
            Destroy(this.players[this.id].rb.gameObject);
        });

        Debug.Log("Disconnected from server!");
    }
}
