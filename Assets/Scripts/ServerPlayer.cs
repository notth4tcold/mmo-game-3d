using System.Net;
using UnityEngine;
using UnityEngine.AI;

public class ServerPlayer {
    public string id { get; private set; }
    public string username { get; set; }
    public bool isReady { get; set; }

    public TcpServerController tcp { get; set; }
    public IPEndPoint endPointUdp { get; set; }

    public Rigidbody rb { get; set; }
    public PlayerController playerController { get; set; }

    public Inputs[] inputBuffer { get; set; }
    private int inputBufferSize;
    private const int bufferSize = 1024;

    public uint lastInputTickRecieved;

    public ServerPlayer(string id) {
        this.id = id;
        this.inputBuffer = new Inputs[bufferSize];
    }

    public void addInputs(Inputs inputs) {
        uint bufferIndex = inputs.tick % bufferSize;
        inputBuffer[bufferIndex] = inputs;
        inputBufferSize++;
    }

    public Inputs getNextInputs(uint tick) {
        uint bufferIndex = tick % bufferSize;

        if (Client.instance != null && Client.instance.id == id) {
            //Debug.LogWarning("" + id);
        }

        if (inputBuffer[bufferIndex] == null) {
            Debug.Log("Lost tick at: " + tick + " using last one");
            uint index = (tick - 1) % bufferSize;
            return inputBuffer[index];
        }

        inputBufferSize--;
        return inputBuffer[bufferIndex];
    }

    public int getInputsSize() {
        return inputBufferSize;
    }

    public void disconnect() {
        tcp.disconnect();
        tcp = null;
        endPointUdp = null;
        isReady = false;
    }
}