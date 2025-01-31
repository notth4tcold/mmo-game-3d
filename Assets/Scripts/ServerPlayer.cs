using System.Net;
using System.Collections.Generic;
using UnityEngine;

public class ServerPlayer {
    public string id { get; private set; }
    public string username { get; set; }
    public TcpServerController tcp { get; set; }
    public IPEndPoint endPointUdp { get; set; }

    public Rigidbody rb { get; set; }
    public PlayerController playerController { get; set; }
    public uint currentTick { get; set; }
    public bool hasStateToSend { get; set; }

    public Queue<StatePayload> sendToClientStateQueue { get; set; }
    public Queue<InputPayload> receiveFromClientInputQueue { get; set; }

    public ServerPlayer(string id) {
        this.id = id;
        this.sendToClientStateQueue = new Queue<StatePayload>();
        this.receiveFromClientInputQueue = new Queue<InputPayload>();
        currentTick = 0;
        hasStateToSend = false;
    }

    public void disconnect() {
        tcp.disconnect();
        tcp = null;
        endPointUdp = null;
    }
}