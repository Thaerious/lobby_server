using System.Text.RegularExpressions;

namespace frar.lobbyserver;

public class Game {
    public readonly string Name;
    public readonly Player Owner;
    public readonly string Password;
    public readonly int MaxPlayers;

    private readonly List<Player> _invited = new List<Player>();
    public List<Player> Invited {
        get {
            return new List<Player>(_invited);
        }
    }

    private List<Player> _players = new List<Player>();
    public List<Player> Players {
        get {
            return new List<Player>(_players);
        }
    }

    public Game(string name, Player owner, int maxplayers, string password) {
        if (maxplayers < 2 || maxplayers > 5) throw new MaxPlayersException(maxplayers);

        this.Name = name;
        this.Owner = owner;
        this.Password = password;
        this.MaxPlayers = maxplayers;
        this._players.Add(owner);
    }

    /// <summary>
    /// Add a new player to the the game.
    /// Can not add a player that has already been added.
    /// Will add the game to the player.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="password">Plaintext PW must match PW game was made with</param>
    /// <returns>true if the player was added to the game, else false.</returns>
    /// <exception cref="RepeatedPlayerException">The player is already in the game.</exception>
    /// <exception cref="GameFullException">The game already has max players.</exception>
    /// 
    public bool AddPlayer(Player player, string password = "") {
        if (!this._invited.Contains(player) && this.Password != password) return false;
        if (player.HasGame) throw new RepeatedPlayerException(player.Name);
        if (_players.Count >= MaxPlayers) throw new GameFullException();
        this._players.Add(player);
        player.Game = this;
        return true;
    }

    /// <summary>
    /// Remove a player from this game.
    /// Will remove the game from the player (set to null).
    /// </summary>
    /// <param name="player">The player to add</param>
    /// <exception cref="RemoveOwnerException">Can not remove the owning player</exception>
    /// <exception cref="UnknownPlayerException">Game must contain the player</exception>
    public void RemovePlayer(Player player) {
        if (player == this.Owner) throw new RemoveOwnerException(player.Name);
        if (!_players.Contains(player)) throw new UnknownPlayerException(player.Name);
        _players.Remove(player);
        player.Game = null;
    }

    /// <summary>
    /// Invite a player to this game.
    /// </summary>
    /// <param name="player">The player to invite</param>
    /// <exception cref="RepeatedPlayerException">Player was previously invited</exception>
    public void AddInvite(Player player) {
        if (this._invited.Contains(player)) throw new RepeatedPlayerException(player.Name);
        this._invited.Add(player);
    }

    /// <summary>
    /// Uninvite a player
    /// </summary>
    /// <param name="player">The player to uninvite</param>
    /// <exception cref="UnknownPlayerException">The player was not previously invited</exception>
    public void RemoveInvite(Player player) {
        if (!_invited.Contains(player)) throw new UnknownPlayerException(player.Name);
        _invited.Remove(player);
    }   

    /// <summary>
    /// Determine if a player is in this game.
    /// </summary>
    /// <param name="player"></param>
    /// <returns>True if the player is in the game, otherwise false</returns>
    public bool HasPlayer(Player player) {
        return Players.Contains(player);
    }

    public static bool CheckName(string name) {        
        name = name.Trim();
        if (name.Length > 24) return false;
        if (name.Length < 3) return false;
        Regex rx = new Regex("^[a-zA-Z0-9 ._/-]+$");
        if (rx.Matches(name).Count == 1) return true;
        return false;
    }
}