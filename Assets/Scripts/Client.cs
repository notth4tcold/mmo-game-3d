using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour {
    public static Client instance;

    public GameObject playerPrefab;
    private Rigidbody playerRb;
    private PlayerController playerController;

    private string id;
    private string username = "Mei";
    private string ip = "127.0.0.1";
    private int port = 8080;
    public bool isConnected = false;
    private TcpClientController tcp;
    private UdpClientController udp;

    private float timer;
    private uint currentTick;
    private float timeBetweenTicks;
    private float latency = 2f;
    private const int bufferSize = 1024;
    //private const float serverTickRate = 60f; //timeBetweenTicks = 1f / serverTickRate;

    private StatePayload[] stateBuffer;
    private InputPayload[] inputBuffer;

    private Queue<InputPayload> sendToServerInputQueue;
    private Queue<StatePayload> receiveFromServerStateQueue;

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
    }

    void Start() {
        timer = 0.0f;
        currentTick = 0;

        stateBuffer = new StatePayload[bufferSize];
        inputBuffer = new InputPayload[bufferSize];

        sendToServerInputQueue = new Queue<InputPayload>();
        receiveFromServerStateQueue = new Queue<StatePayload>();

        connectToTcp();
    }

    void Update() {
        timeBetweenTicks = Time.fixedDeltaTime;
        timer += Time.deltaTime;

        while (timer >= timeBetweenTicks && isConnected) {
            timer -= timeBetweenTicks;
            handleTick(currentTick, timeBetweenTicks);
            currentTick++;
        }

        sendInputsToServer();
        processNewStateFromServer();
    }

    void handleTick(uint currentTick, float timeBetweenTicks) {
        uint bufferIndex = currentTick % bufferSize;

        InputPayload inputPayload = new InputPayload(currentTick, latency);
        inputBuffer[bufferIndex] = inputPayload;

        StatePayload statePayload = new StatePayload(currentTick, playerRb);
        stateBuffer[bufferIndex] = statePayload;

        playerController.movePlayer(inputPayload.inputs);
        InstanciateSceneController.Instance.clientPhysicsScene.Simulate(timeBetweenTicks);

        sendToServerInputQueue.Enqueue(inputPayload);
    }

    void connectToTcp() {
        tcp = new TcpClientController(new TcpClient(), ip, port);
        tcp.connect();
    }

    public void OnConnectByTcp(Packet packet) {
        id = packet.ReadString();
        connectToUdp();
    }

    void connectToUdp() {
        UdpClient socketUdp = new UdpClient(((IPEndPoint)tcp.getSocket().Client.LocalEndPoint).Port);
        udp = new UdpClientController(socketUdp, ip, port);
        udp.connect();

        Packet packet = new Packet();
        packet.Write("OnConnectByUdp");
        packet.Write(id);
        sendUdpData(packet);
    }

    public void OnConnectByUdp(Packet packet) {
        requestSpawnPlayer();
    }

    void requestSpawnPlayer() {
        Packet packet = new Packet();
        packet.Write("OnRequestSpawnPlayer");
        packet.Write(id);
        packet.Write(username);
        sendTcpData(packet);
    }

    public void OnRequestSpawnPlayer(Packet packet) {
        ThreadManager.ExecuteOnMainThread(() => {
            var playerGO = Instantiate(playerPrefab, gameObject.scene) as GameObject;
            playerGO.GetComponent<Renderer>().material.color = Color.blue;
            playerRb = playerGO.GetComponent<Rigidbody>();
            playerController = playerGO.GetComponent<PlayerController>();

            isConnected = true;
        });
    }

    public void OnRequestSpawnEnemy(Packet packet) {
        var enemyId = packet.ReadString();
        var enemyName = packet.ReadString();

        Debug.Log("NEW ENEMY: " + enemyId + " " + enemyName);
        //ThreadManager.ExecuteOnMainThread(() => {
        //var playerGO = Instantiate(playerPrefab, gameObject.scene) as GameObject;
        //playerGO.GetComponent<Renderer>().material.color = Color.gray;
        //playerRb = playerGO.GetComponent<Rigidbody>();
        //playerController = playerGO.GetComponent<PlayerController>();
        //});
    }

    void sendInputsToServer() {
        if (sendToServerInputQueue.Count > 0 && Time.time >= sendToServerInputQueue.Peek().deliveryTime) {
            Packet packet = new Packet();
            packet.Write("OnClientInput");
            packet.Write(id);
            packet.Write(sendToServerInputQueue.Dequeue());
            sendUdpData(packet);
        }
    }

    public void OnServerMovementState(Packet packet) {
        StatePayload serverState = packet.ReadStatePayload();
        receiveFromServerStateQueue.Enqueue(serverState);
    }

    void processNewStateFromServer() {
        if (receiveFromServerStateQueue.Count == 0) return;

        StatePayload statePayload = receiveFromServerStateQueue.Dequeue();

        while (receiveFromServerStateQueue.Count > 0) {
            statePayload = receiveFromServerStateQueue.Dequeue();
        }

        uint bufferIndex = statePayload.tick % bufferSize;

        Vector3 positionError = statePayload.position - stateBuffer[bufferIndex].position;
        float rotationError = 1.0f - Quaternion.Dot(statePayload.rotation, stateBuffer[bufferIndex].rotation);

        if (positionError.sqrMagnitude > 0.0000001f || rotationError > 0.00001f) {
            Debug.Log("Reconciliate - error at tick " + statePayload.tick + " (rewinding " + (currentTick - statePayload.tick) + " ticks)");

            playerRb.position = statePayload.position;
            playerRb.rotation = statePayload.rotation;
            playerRb.velocity = statePayload.velocity;
            playerRb.angularVelocity = statePayload.angularVelocity;

            uint rewind_tick = statePayload.tick;
            while (rewind_tick < currentTick) {
                bufferIndex = rewind_tick % bufferSize;

                stateBuffer[bufferIndex].position = playerRb.position;
                stateBuffer[bufferIndex].rotation = playerRb.rotation;

                playerController.movePlayer(inputBuffer[bufferIndex].inputs);
                InstanciateSceneController.Instance.clientPhysicsScene.Simulate(timeBetweenTicks);

                ++rewind_tick;
            }
        }
    }

    public void sendTcpData(Packet packet) {
        if (tcp == null) return;
        packet.WriteLength();
        tcp.sendData(packet);
    }

    public void sendUdpData(Packet packet) {
        if (udp == null) return;
        packet.WriteLength();
        udp.sendData(packet);
    }

    public void disconnect() {
        if (!isConnected) return;
        isConnected = false;

        tcp.disconnect();
        udp.disconnect();
        tcp = null;
        udp = null;

        Debug.Log("Disconnected from server!");
    }
}
