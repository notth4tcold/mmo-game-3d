using UnityEngine;

public class Inputs {
    public bool up;
    public bool down;
    public bool left;
    public bool right;
    public bool jump;

    public Inputs() {
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);
        jump = Input.GetKey(KeyCode.Space);
    }
}
