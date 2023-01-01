namespace frar.lobbyserver;

public class InvalidSessionException : Exception {
    public InvalidSessionException(string msg) : base(msg) { }
}