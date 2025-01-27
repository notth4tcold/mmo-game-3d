using System.Collections.Generic;
using UnityEngine;

public class Server : MonoBehaviour {
    public static Server Instance;

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

    private Queue<StatePayload> sendToClientStateQueue;
    private Queue<InputPayload> receiveFromClientInputQueue;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    void Start() {
        var playerSimulatorGo = Instantiate(playerPrefab, gameObject.scene) as GameObject;
        playerSimulatorGo.GetComponent<Renderer>().material.color = Color.red;
        rb = playerSimulatorGo.GetComponent<Rigidbody>();
        playerMoviment = playerSimulatorGo.GetComponent<PlayerMoviment>();

        timer = 0.0f;
        currentTick = 0;

        sendToClientStateQueue = new Queue<StatePayload>();
        receiveFromClientInputQueue = new Queue<InputPayload>();
        //timeBetweenTicks = 1f / serverTickRate;
    }

    void Update() {
        timeBetweenTicks = Time.fixedDeltaTime;
        timer += Time.deltaTime;

        while (timer >= timeBetweenTicks) {
            timer -= timeBetweenTicks;
            handleTick(timeBetweenTicks);
        }

        sendStateToClient();
    }

    void handleTick(float timeBetweenTicks) {
        if (receiveFromClientInputQueue.Count == 0) return;

        InputPayload inputPayload = receiveFromClientInputQueue.Dequeue();

        if (inputPayload.tick >= currentTick) {
            playerMoviment.movePlayer(rb, inputPayload.inputs, moveSpeed);
            InstanciateSceneController.Instance.serverPhysicsScene.Simulate(timeBetweenTicks);

            currentTick++; //TICK DE CADA CLIENT

            StatePayload statePayload = new StatePayload(currentTick, rb, latency); // PLAYER ID NO PAYLOAD
            sendToClientStateQueue.Enqueue(statePayload);
        }
    }

    void sendStateToClient() {// ENVIAR PARA O CLIENT CORRETO
        if (sendToClientStateQueue.Count > 0 && Time.time >= sendToClientStateQueue.Peek().deliveryTime) {
            Client.Instance.OnServerMovementState(sendToClientStateQueue.Dequeue());
        }
    }

    public void OnClientInput(InputPayload inputPayload) {// PLAYER ID NO PAYLOAD
        receiveFromClientInputQueue.Enqueue(inputPayload);
    }
}
