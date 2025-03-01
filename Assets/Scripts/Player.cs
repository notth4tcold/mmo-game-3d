using UnityEngine;

public class Player {
    public string id { get; private set; }
    public string username { get; set; }

    public Rigidbody rb { get; set; }
    public PlayerController playerController { get; set; }

    public Player(string id) {
        this.id = id;
    }
}