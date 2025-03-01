
using System.Collections.Generic;

public class InputPayload {
    public uint tick { get; private set; }
    public List<Inputs> inputList { get; set; }

    public InputPayload() { }

    public InputPayload(uint tick) {
        this.tick = tick;
        this.inputList = new List<Inputs>();
    }
}