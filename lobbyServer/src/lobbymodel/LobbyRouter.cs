
using frar.clientserver;
namespace frar.lobbyserver;

public class LobbyRouter : ThreadedRouter {
    private LobbyModel model = new LobbyModel();
    private DatabaseInterface dbi = new DatabaseInterface();
    private Player player;

    [Route]
    public void RegisterPlayer(string name, string password, string email) {
        if (dbi.HasUsername(name)) {
            var packet = new Packet("RegisterRejected");
            packet["reason"] = "username already in use";
            this.Connection.Write(packet);
        }
        else {
            dbi.RegisterPlayer(name, password, email);
            var packet = new Packet("RegisterAccepted");
            this.Connection.Write(packet);
        }
    }

    [Route]
    public void Login(string name, string password) {
        if (dbi.Verify(name, password)) {
            var clientPacket = new Packet("LoginAccepted");
            clientPacket["hash"] = dbi.AssignSession(name);
            this.Connection.Write(clientPacket);

            var globalPacket = new Packet("PlayerLogin");
            globalPacket["playername"] = name;
            this.Connection.Write(globalPacket);

            this.player = model.AddPlayer(name);
        }
        else {
            var packet = new Packet("LoginRejected");
            this.Connection.Write(packet);
        }
    }

    [Route]
    public void LoginSession(string hash) {
        var name = dbi.VerifySession(hash);

        if (name != "") {
            var clientPacket = new Packet("LoginAccepted");
            clientPacket["hash"] = dbi.AssignSession(name);
            this.Connection.Write(clientPacket);

            var globalPacket = new Packet("PlayerLogin");
            globalPacket["playername"] = name;
            this.Connection.Write(globalPacket);

            this.player = model.AddPlayer(name);
        }
        else {
            var packet = new Packet("LoginRejected");
            this.Connection.Write(packet);
        }
    }

    [Route]
    public void Logout() {
        if (player != null && this.model.HasPlayer(player.Name)) {
            var clientPacket = new Packet("LogoutAccepted");
            this.Connection.Write(clientPacket);

            var globalPacket = new Packet("LeaveLobby");
            globalPacket["playername"] = player.Name;
            this.Connection.Write(globalPacket);

            model.RemovePlayer(player.Name);
        }
        else {
            var packet = new Packet("LogoutRejected");
            packet["reason"] = $"client not logged in";
            this.Connection.Write(packet);
        }
    }

    [Route]
    public void CreateGame(string gamename, int maxplayers, string password = "") {
        if (player == null) {
            var packet = new Packet("CreateRejected");
            packet["reason"] = $"client not logged in";
            this.Connection.Write(packet);
        }
        else if (!Game.CheckName(gamename)) {
            var packet = new Packet("CreateRejected");
            packet["reason"] = $"invalid name";
            this.Connection.Write(packet);
        }
        else if (player.HasGame) {
            var packet = new Packet("CreateRejected");
            packet["reason"] = $"player already in game";
            this.Connection.Write(packet);
        }
        else if (this.model.Games.Keys.Contains("name")) {
            var packet = new Packet("CreateRejected");
            packet["reason"] = $"game name already in use";
            this.Connection.Write(packet);
        }
        else {
            Game game = new Game(gamename, player, maxplayers, password);

            var clientPacket = new Packet("CreateAccepted");
            this.Connection.Write(clientPacket);

            var globalPacket1 = new Packet("NewGame");
            this.Connection.Write(globalPacket1);

        }
    }
}