using UnityEngine;

public class StatePayload {
    public uint tick;
    public float deliveryTime; // Only used by the server to simulate latency
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public Quaternion rotation;

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