using UnityEngine;

public class PlayerMoviment : MonoBehaviour {
    public void movePlayer(Rigidbody rb, Inputs inputs, float moveSpeed) {
        if (inputs.up) rb.AddForce(Vector3.forward * moveSpeed, ForceMode.Impulse);
        if (inputs.down) rb.AddForce(Vector3.back * moveSpeed, ForceMode.Impulse);
        if (inputs.left) rb.AddForce(Vector3.left * moveSpeed, ForceMode.Impulse);
        if (inputs.right) rb.AddForce(Vector3.right * moveSpeed, ForceMode.Impulse);
        if (inputs.jump) rb.AddForce(Vector3.up * moveSpeed, ForceMode.Impulse);
    }
}