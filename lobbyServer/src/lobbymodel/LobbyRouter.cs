
using frar.clientserver;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver;

public class LobbyRouter : ThreadedRouter {
    public static LobbyModel sharedModel = new LobbyModel();

    private DatabaseInterface dbi;
    private Player? player;

    public LobbyRouter() {
        this.dbi = new DatabaseInterface();
    }

    public LobbyRouter(DatabaseInterface dbi) {
        this.dbi = dbi;
    }

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
        if (sharedModel.HasPlayer(name)) {
            var packet = new Packet("LoginRejected");
            this.Connection.Write(packet);
        }
        else if (dbi.Verify(name, password)) {
            var clientPacket = new Packet("LoginAccepted");
            clientPacket["hash"] = dbi.AssignSession(name);
            this.Connection.Write(clientPacket);

            var globalPacket = new Packet("PlayerLogin");
            globalPacket["playername"] = name;
            this.Connection.Write(globalPacket);

            this.player = sharedModel.AddPlayer(name);
        }
        else {
            var packet = new Packet("LoginRejected");
            this.Connection.Write(packet);
        }
    }

    [Route]
    public void LoginSession(string hash) {
        try {
            var name = dbi.VerifySession(hash);
            var clientPacket = new Packet("LoginAccepted");
            clientPacket["hash"] = dbi.AssignSession(name);
            this.Connection.Write(clientPacket);

            var globalPacket = new Packet("PlayerLogin");
            globalPacket["playername"] = name;
            this.Connection.Write(globalPacket);

            this.player = sharedModel.AddPlayer(name);
        }
        catch (InvalidSessionException ex) {
            var packet = new Packet("LoginRejected");
            packet["reason"] = ex.Message;
            this.Connection.Write(packet);
            return;
        }
    }

    [Route]
    public void Logout() {
        if (player != null && sharedModel.HasPlayer(player.Name)) {
            var clientPacket = new Packet("LogoutAccepted");
            this.Connection.Write(clientPacket);

            var globalPacket = new Packet("LeaveLobby");
            globalPacket["playername"] = player.Name;
            this.Connection.Write(globalPacket);

            sharedModel.RemovePlayer(player.Name);
        }
        else {
            var packet = new Packet("LogoutRejected");
            packet["reason"] = $"client not logged in";
            this.Connection.Write(packet);
        }
    }

    [Route]
    public void CreateGame(string gamename, int maxplayers, string password = "") {
        foreach (string key in sharedModel.Games.Keys) {
            System.Console.WriteLine(key);
        }
        System.Console.WriteLine("count " + sharedModel.Games.Keys.Count);

        if (player == null) {
            var packet = new Packet("CreateRejected");
            packet["reason"] = $"client not logged in";
            this.Connection.Write(packet);
        }
        else if (player.HasGame) {
            var packet = new Packet("CreateRejected");
            packet["reason"] = $"player already in game";
            this.Connection.Write(packet);
        }
        else if (sharedModel.Games.Keys.Contains(gamename)) {
            var packet = new Packet("CreateRejected");
            packet["reason"] = $"game name already in use";
            this.Connection.Write(packet);
        }
        else try {
                Game game = sharedModel.CreateGame(gamename, player.Name, maxplayers, password);

                player.Game = gamename;

                var clientPacket = new Packet("CreateAccepted");
                clientPacket["gamename"] = gamename;
                this.Connection.Write(clientPacket);

                var globalPacket1 = new Packet("NewGame");
                globalPacket1["game"] = game;
                this.Connection.Write(globalPacket1);
            }
            catch (LobbyModelException ex) {
                this.Connection.Write(ex.Packet("CreateRejected"));
            }
    }
}