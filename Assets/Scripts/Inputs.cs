using UnityEngine;

public class Inputs {
    public uint tick { get; private set; }

    public bool up { get; set; }
    public bool down { get; set; }
    public bool left { get; set; }
    public bool right { get; set; }
    public bool jump { get; set; }

    public Inputs() { }

    public Inputs(uint tick) {
        this.tick = tick;
    }

    public void SetInputs() {
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);
        jump = Input.GetKey(KeyCode.Space);
    }
}
