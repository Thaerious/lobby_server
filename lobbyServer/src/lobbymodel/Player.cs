namespace frar.lobbyserver;

// Data class for storing a player in the LobbyModel
public class Player {
    public readonly string Name;
    private Game? game;

    public Game? Game{
        get { return game; }
        set {
            if (value != null && this.game != null) throw new PlayerInGameException(this.Name);
            this.game = value;
        }
    }

    public Player(string name) => this.Name = name;

    public Player(string name, Game game) {
        this.Name = name; 
        this.game = game;
    }

    public bool HasGame { get => this.game != null; }
}
