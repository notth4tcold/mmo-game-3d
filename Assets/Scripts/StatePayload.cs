using UnityEngine;

public class StatePayload {
    public uint tick { get; private set; }
    public float deliveryTime { get; set; } // Only used by the server to simulate latency
    public Vector3 position { get; set; }
    public Vector3 velocity { get; set; }
    public Vector3 angularVelocity { get; set; }
    public Quaternion rotation { get; set; }

    public StatePayload() { }

    public StatePayload(uint tick, Rigidbody rb) {
        this.tick = tick;
        this.position = rb.position;
        this.velocity = rb.velocity;
        this.angularVelocity = rb.angularVelocity;
        this.rotation = rb.rotation;
    }

    public StatePayload(uint tick, Rigidbody rb, float latency) {
        this.tick = tick;
        this.position = rb.position;
        this.velocity = rb.velocity;
        this.angularVelocity = rb.angularVelocity;
        this.rotation = rb.rotation;
        this.deliveryTime = Time.time + latency;
    }
}