namespace frar.lobbyserver;

// Model for storing the current state of the Lobby.
// Any exceptions throws should be forwarded as ActionRejected events.
public class LobbyModel {
    private readonly Dictionary<String, Player> players = new Dictionary<String, Player>();
    private readonly Dictionary<String, Game> games = new Dictionary<String, Game>();

    public Dictionary<String, Player> Players {
        get {
            return new Dictionary<String, Player>(players);
        }
    }

    public Dictionary<String, Game> Games {
        get {
            return new Dictionary<String, Game>(games);
        }
    }

    public Player AddPlayer(string name) {
        if (players.ContainsKey(name)) throw new PlayerNameException(name);
        players[name] = new Player(name);
        return players[name];
    }

    public Player GetPlayer(string name) {

        try {
            return this.players[name];
        }
        catch (KeyNotFoundException ex) {
            throw new UnknownPlayerException(name, ex);
        }
    }

    /// <summary>
    /// Remove a player from the lobby.
    /// </summary>
    /// <param name="name">The name of the player to remove</param>
    /// <exception cref="UnknownPlayerException">Thrown when the player does not exist.</exception>
    public void RemovePlayer(string name) {
        if (!players.ContainsKey(name)) throw new UnknownPlayerException(name);
        players.Remove(name);
    }

    public Game CreateGame(string gameName, string ownerName, string password, int maxplayers) {
        if (!this.HasPlayer(ownerName)) throw new UnknownPlayerException(ownerName);
        if (this.players[ownerName].HasGame) throw new PlayerInGameException(ownerName);
        if (games.ContainsKey(gameName)) throw new GameNameException(gameName);

        Game game = new Game(gameName, Players[ownerName], password, maxplayers);
        this.games.Add(gameName, game);
        this.players.Remove(ownerName);
        this.players[ownerName] = new Player(ownerName, game);
        return game;
    }

    /// <summary>
    /// Retrieve a game by name.
    /// </summary>
    /// <param name="gameName">The name of the game.</param>
    /// <exception cref="UnknownGameException">The game has not been created.</exception> 
    /// 
    public Game GetGame(string gameName) {
        if (!this.games.ContainsKey(gameName)) throw new UnknownGameException(gameName);
        return this.games[gameName];
    }

    /// <summary>
    /// Determine whether a player is in the lobby model.
    /// </summary>
    /// <param name="name">The player to locate in the model.</param>
    /// <returns>true if name is found, otherwise false</returns>
    public bool HasPlayer(string pName) {
        return this.players.ContainsKey(pName);
    }

    /// <summary>
    /// Determine whether a game is in the lobby model.
    /// </summary>
    /// <param name="name">The game to locate in the model</param>
    /// <returns>true if name is found, otherwise false</returns>
    public bool HasGame(string name) {
        return this.games.ContainsKey(name);
    }
}