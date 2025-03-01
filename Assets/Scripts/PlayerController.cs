using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Rigidbody rb;
    private float maxSpeed = 5f;
    private float moveSpeed = 2.5f;
    private float jumpThreshold = -2;

    public bool isCollidingWithOtherPlayer = false;

    void Start() {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    public void movePlayer(Inputs inputs) {
        if (rb == null) return;
        if (rb.velocity.z <= maxSpeed && inputs.up) rb.AddForce(Vector3.forward * moveSpeed, ForceMode.VelocityChange);
        if (rb.velocity.z >= -maxSpeed && inputs.down) rb.AddForce(Vector3.back * moveSpeed, ForceMode.VelocityChange);
        if (rb.velocity.x >= -maxSpeed && inputs.left) rb.AddForce(Vector3.left * moveSpeed, ForceMode.VelocityChange);
        if (rb.velocity.x <= maxSpeed && inputs.right) rb.AddForce(Vector3.right * moveSpeed, ForceMode.VelocityChange);
        if (rb.velocity.y <= maxSpeed && rb.velocity.y >= -maxSpeed && rb.position.y <= jumpThreshold && inputs.jump) rb.AddForce(Vector3.up * moveSpeed, ForceMode.VelocityChange);
    }
}