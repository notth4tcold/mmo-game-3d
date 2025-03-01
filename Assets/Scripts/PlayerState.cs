using UnityEngine;

public class PlayerState {
    public string id { get; private set; }

    public Vector3 position { get; set; }
    public Vector3 velocity { get; set; }
    public Vector3 angularVelocity { get; set; }
    public Quaternion rotation { get; set; }

    public PlayerState() { }

    public PlayerState(string id, Rigidbody rb) {
        this.id = id;
        this.position = rb.position;
        this.rotation = rb.rotation;
        this.velocity = rb.velocity;
        this.angularVelocity = rb.angularVelocity;
    }
}