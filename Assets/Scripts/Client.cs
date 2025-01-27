using UnityEngine;
using System.Collections.Generic;

public class Client : MonoBehaviour {
    public static Client Instance;

    public GameObject playerPrefab;
    private Rigidbody rb;
    private PlayerMoviment playerMoviment;
    private float moveSpeed = 1;
    private float latency = 2f;

    private float timer;
    private uint currentTick;
    private float timeBetweenTicks;
    //private const float serverTickRate = 60f;
    private const int bufferSize = 1024;

    private StatePayload[] stateBuffer;
    private InputPayload[] inputBuffer;

    private Queue<InputPayload> sendToServerInputQueue;
    private Queue<StatePayload> receiveFromServerStateQueue;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    void Start() {
        var playerGO = Instantiate(playerPrefab, gameObject.scene) as GameObject;
        playerGO.GetComponent<Renderer>().material.color = Color.blue;
        rb = playerGO.GetComponent<Rigidbody>();
        playerMoviment = playerGO.GetComponent<PlayerMoviment>();

        timer = 0.0f;
        currentTick = 0;

        stateBuffer = new StatePayload[bufferSize];
        inputBuffer = new InputPayload[bufferSize];

        sendToServerInputQueue = new Queue<InputPayload>();
        receiveFromServerStateQueue = new Queue<StatePayload>();
        //timeBetweenTicks = 1f / serverTickRate;
    }

    void Update() {
        timeBetweenTicks = Time.fixedDeltaTime;
        timer += Time.deltaTime;

        while (timer >= timeBetweenTicks) {
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

        StatePayload statePayload = new StatePayload(currentTick, rb);
        stateBuffer[bufferIndex] = statePayload;

        playerMoviment.movePlayer(rb, inputPayload.inputs, moveSpeed);
        InstanciateSceneController.Instance.clientPhysicsScene.Simulate(timeBetweenTicks);

        sendToServerInputQueue.Enqueue(inputPayload);
    }

    void sendInputsToServer() {
        if (sendToServerInputQueue.Count > 0 && Time.time >= sendToServerInputQueue.Peek().deliveryTime) {
            Server.Instance.OnClientInput(sendToServerInputQueue.Dequeue());
        }
    }

    public void OnServerMovementState(StatePayload serverState) {
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

            rb.position = statePayload.position;
            rb.rotation = statePayload.rotation;
            rb.velocity = statePayload.velocity;
            rb.angularVelocity = statePayload.angularVelocity;

            uint rewind_tick = statePayload.tick;
            while (rewind_tick < currentTick) {
                bufferIndex = rewind_tick % bufferSize;

                stateBuffer[bufferIndex].position = rb.position;
                stateBuffer[bufferIndex].rotation = rb.rotation;

                playerMoviment.movePlayer(rb, inputBuffer[bufferIndex].inputs, moveSpeed);
                InstanciateSceneController.Instance.clientPhysicsScene.Simulate(timeBetweenTicks);

                ++rewind_tick;
            }
        }
    }
}
