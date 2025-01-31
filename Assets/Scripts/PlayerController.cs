using UnityEngine;

public class PlayerController : MonoBehaviour {
    private Rigidbody rb;
    private float moveSpeed = 1;

    void Start() {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    public void movePlayer(Inputs inputs) {
        if (inputs.up) rb.AddForce(Vector3.forward * moveSpeed, ForceMode.Impulse);
        if (inputs.down) rb.AddForce(Vector3.back * moveSpeed, ForceMode.Impulse);
        if (inputs.left) rb.AddForce(Vector3.left * moveSpeed, ForceMode.Impulse);
        if (inputs.right) rb.AddForce(Vector3.right * moveSpeed, ForceMode.Impulse);
        if (inputs.jump) rb.AddForce(Vector3.up * moveSpeed, ForceMode.Impulse);
    }
}