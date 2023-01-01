using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace frar.lobbyserver;

public class Game {
    [JsonProperty] public readonly string Name;
    [JsonProperty] public readonly string Owner;    
    [JsonProperty] public readonly int MaxPlayers;
    [JsonProperty] public readonly bool PasswordRequired;
    [JsonIgnore] public readonly string Password;

    private readonly List<string> _invited = new List<string>();
    public List<string> Invited {
        get {
            return new List<string>(_invited);
        }
    }

    private List<string> _players = new List<string>();
    public List<string> Players {
        get {
            return new List<string>(_players);
        }
    }

    public Game(string name, string owner, int maxplayers, string password) {
        if (maxplayers < 2 || maxplayers > 5) throw new MaxPlayersException(maxplayers);

        this.Name = name;
        this.Owner = owner;
        this.Password = password;
        this.MaxPlayers = maxplayers;
        this._players.Add(owner);
        this.PasswordRequired = (password != "" && password != null);
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
    public bool AddPlayer(string player, string password = "") {
        if (this._players.Contains(player)) throw new RepeatedPlayerException(player);
        if (!this._invited.Contains(player) && this.Password != password) return false;
        if (_players.Count >= MaxPlayers) throw new GameFullException();
        this._players.Add(player);        
        return true;
    }

    /// <summary>
    /// Remove a player from this game.
    /// Will remove the game from the player (set to null).
    /// </summary>
    /// <param name="player">The player to add</param>
    /// <exception cref="RemoveOwnerException">Can not remove the owning player</exception>
    /// <exception cref="UnknownPlayerException">Game must contain the player</exception>
    public void RemovePlayer(string player) {
        if (player == this.Owner) throw new RemoveOwnerException(player);
        if (!_players.Contains(player)) throw new UnknownPlayerException(player);
        _players.Remove(player);
    }

    /// <summary>
    /// Invite a player to this game.
    /// </summary>
    /// <param name="player">The player to invite</param>
    /// <exception cref="RepeatedPlayerException">Player was previously invited</exception>
    public void AddInvite(string player) {
        if (this._invited.Contains(player)) throw new RepeatedPlayerException(player);
        this._invited.Add(player);
    }

    /// <summary>
    /// Uninvite a player
    /// </summary>
    /// <param name="player">The player to uninvite</param>
    /// <exception cref="UnknownPlayerException">The player was not previously invited</exception>
    public void RemoveInvite(string player) {
        if (!_invited.Contains(player)) throw new UnknownPlayerException(player);
        _invited.Remove(player);
    }   

    /// <summary>
    /// Determine if a player is in this game.
    /// </summary>
    /// <param name="player"></param>
    /// <returns>True if the player is in the game, otherwise false</returns>
    public bool HasPlayer(string player) {
        return Players.Contains(player);
    }

    public static bool CheckName(string name) {        
        name = name.Trim();
        if (name.Length > 24) return false;
        if (name.Length < 3) return false;
        Regex rx = new Regex("^[a-zA-Z0-9 ]+$");
        if (rx.Matches(name).Count == 1) return true;
        return false;
    }

    public override string ToString() {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}