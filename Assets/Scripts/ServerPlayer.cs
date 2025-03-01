using System.Net;
using System.Collections.Concurrent;
using UnityEngine;

public class ServerPlayer {
    public string id { get; private set; }
    public string username { get; set; }
    public bool isReady { get; set; }

    public TcpServerController tcp { get; set; }
    public IPEndPoint endPointUdp { get; set; }

    public Rigidbody rb { get; set; }
    public PlayerController playerController { get; set; }

    private ConcurrentQueue<Inputs> queue;
    public Inputs[] inputBuffer { get; set; }
    private const int bufferSize = 1024;

    public uint lastInputTickRecieved;

    public ServerPlayer(string id) {
        this.id = id;
        this.isReady = false;
        this.lastInputTickRecieved = 0;
        this.queue = new ConcurrentQueue<Inputs>();
        this.inputBuffer = new Inputs[bufferSize];
    }

    public void addInputs(Inputs inputs) {
        uint bufferIndex = inputs.tick % bufferSize;
        inputBuffer[bufferIndex] = inputs;
        queue.Enqueue(inputs);
    }

    public Inputs getNextInputs(uint tick) {
        uint bufferIndex = tick % bufferSize;

        if (inputBuffer[bufferIndex] == null) {
            Debug.Log("Lost tick at: " + tick + " using last one");
            uint index = (tick - 1) % bufferSize;
            return inputBuffer[index];
        }

        if (queue.TryDequeue(out var inputs)) {
            return inputBuffer[bufferIndex];
        }

        Debug.Log("Error to dequeue inputs");
        return null;
    }

    public int getInputsSize() {
        return queue.Count;
    }

    public void disconnect() {
        tcp.disconnect();
        tcp = null;
        endPointUdp = null;
        isReady = false;
    }
}