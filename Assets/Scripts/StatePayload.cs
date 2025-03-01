using System.Collections.Generic;

public class StatePayload {
    public uint tick { get; private set; }
    public Dictionary<string, PlayerState> playerstates = new Dictionary<string, PlayerState>();

    public StatePayload() { }

    public StatePayload(uint tick) {
        this.tick = tick;
    }

    public StatePayload(uint tick, Dictionary<string, Player> players) {
        this.tick = tick;

        foreach (var player in players.Values) {
            PlayerState playerState = new PlayerState(player.id, player.rb);
            playerstates.Add(playerState.id, playerState);
        }
    }
}