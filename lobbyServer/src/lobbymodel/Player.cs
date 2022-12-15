namespace frar.lobbyserver;

// Data class for storing a player in the LobbyModel
public class Player {
    public readonly string Name;
    public readonly Game? Game;

    public Player(string name) => this.Name = name;

    public Player(string name, Game game) {
        this.Name = name; 
        this.Game = game;
    }

    public bool HasGame() => this.Game != null;
}
