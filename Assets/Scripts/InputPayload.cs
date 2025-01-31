
using UnityEngine;

public class InputPayload {
    public uint tick { get; private set; }
    public float deliveryTime { get; set; } // Only used by the client to simulate latency
    public Inputs inputs { get; set; }

    public InputPayload() { }

    public InputPayload(uint tick, float latency) {
        this.tick = tick;
        this.deliveryTime = Time.time + latency;

        var newInputs = new Inputs();
        newInputs.SetInputs();
        this.inputs = newInputs;
    }
}