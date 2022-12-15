namespace frar.lobbyserver;

public class LobbyModelException : Exception {
    public LobbyModelException(string message) : base(message) { }
    public LobbyModelException(string message, Exception inner) : base(message, inner) { }
}

public class MaxPlayersException : LobbyModelException {
    public MaxPlayersException(int value) : base($"max players must be 2 <= x <= 5, {value} is out of bounds") { }
}

public class GameFullException : LobbyModelException {
    public GameFullException() : base("Game can not accept more players.") { }
}

public class RepeatedPlayerException : LobbyModelException {
    public RepeatedPlayerException(string name) : base($"Player {name} already added.") { }
}

public class RemoveOwnerException : LobbyModelException {
    public RemoveOwnerException(string name) : base($"Can not remove owner, {name}, from game.") { }
}

public class PlayerNameException : LobbyModelException {
    public PlayerNameException(string name) : base($"Player name already in use: {name}.") { }
}

public class PlayerInGameException : LobbyModelException {
    public PlayerInGameException(string name) : base($"Player already in game: {name}.") { }
}

public class UnknownPlayerException : LobbyModelException {
    public UnknownPlayerException(string name) : base($"Unknown player name: {name}.") { }   
    public UnknownPlayerException(string name, Exception inner) : base($"Unknown player name: {name}.", inner) { }   
}

public class UnknownGameException : LobbyModelException {
    public UnknownGameException(string name) : base($"Unknown game name: {name}.") { }   
}

public class GameNameException : LobbyModelException {
    public GameNameException(string name) : base($"Game name already in use: {name}.") { }
}