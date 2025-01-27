
using UnityEngine;

public class InputPayload {
    public uint tick;
    public float deliveryTime; // Only used by the client to simulate latency
    public Inputs inputs;

    public InputPayload(uint tick, float latency) {
        this.tick = tick;
        this.deliveryTime = Time.time + latency;
        this.inputs = new Inputs();
    }
}