
using frar.clientserver;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver;

public class LobbyRouter : ThreadedRouter {
    public static LobbyModel sharedModel = new LobbyModel();
    public static Dictionary<string, IConnection> liveConnections = new Dictionary<string, IConnection>();

    private DatabaseInterface dbi;
    public Player? player = null;

    public LobbyRouter() {
        this.dbi = new DatabaseInterface();
    }

    public LobbyRouter(DatabaseInterface dbi) {
        this.dbi = dbi;
    }

    public void Broadcast(Packet packet) {
        foreach (IConnection connection in liveConnections.Values) {
            connection.Write(packet);
        }
    }

    [Route(Rule = "(?i)^(?!(login)|(register)).*$", Index = -1)]
    public void CheckForLogin([Ctrl] RouterController ctrl) {
        if (this.player == null) {
            var packet = new Packet("AuthError");
            packet["reason"] = $"client not logged in";
            this.Connection.Write(packet);
            ctrl.TerminateRoute = true;
        }
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

            this.player = sharedModel.AddPlayer(name);
            lock (liveConnections) {
                liveConnections.Add(name, this.Connection);
            }

            var globalPacket = new Packet("PlayerLogin");
            globalPacket["playername"] = name;
            this.Broadcast(globalPacket);
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

            this.player = sharedModel.AddPlayer(name);
            lock (liveConnections) {
                liveConnections.Add(name, this.Connection);
            }

            var globalPacket = new Packet("PlayerLogin");
            globalPacket["playername"] = name;
            this.Broadcast(globalPacket);
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
            this.Broadcast(globalPacket);

            sharedModel.RemovePlayer(player.Name);
            lock (liveConnections) {
                liveConnections.Remove(player.Name);
            }
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

                var clientPacket = new Packet("CreateAccepted");
                clientPacket["gamename"] = gamename;
                this.Connection.Write(clientPacket);

                var globalPacket1 = new Packet("NewGame");
                globalPacket1["game"] = game;
                this.Broadcast(globalPacket1); // TODO CHANGE TO BROADCAST
            }
            catch (LobbyModelException ex) {
                this.Connection.Write(ex.Packet("CreateRejected"));
            }
    }

    [Route]
    public void JoinGame(string gamename, string password = "") {
        if (sharedModel.HasGame(gamename) == false) {
            var clientPacket = new Packet("JoinRejected");
            clientPacket["reason"] = $"unknown game {gamename}";
            this.Connection.Write(clientPacket);
            return;
        }

        var game = sharedModel.GetGame(gamename);

        if (this.player!.HasGame) {
            var clientPacket = new Packet("JoinRejected");
            clientPacket["reason"] = "player already in game";
            this.Connection.Write(clientPacket);
        }else if (!game.Invited.Contains(player.Name) && game.Password != password) {
                var clientPacket = new Packet("JoinRejected");
                clientPacket["reason"] = "passwords do not match";
                this.Connection.Write(clientPacket);
        }
        else try {
                sharedModel.GetGame(gamename).AddPlayer(this.player.Name);
                this.player.Game = gamename;

                var clientPacket = new Packet("JoinAccepted");
                clientPacket["gamename"] = gamename;
                this.Connection.Write(clientPacket);

                var globalPacket = new Packet("PlayerJoined");
                globalPacket["gamename"] = gamename;
                globalPacket["playername"] = this.player.Name;
                this.Broadcast(globalPacket);
            }
            catch (LobbyModelException ex) {
                this.Connection.Write(ex.Packet("JoinRejected"));
            }
    }

    [Route]
    public void InvitePlayer(string playername) {
        if (!this.player!.HasGame) {
            var clientPacket = new Packet("InviteRejected");
            clientPacket["playername"] = playername;
            clientPacket["reason"] = "player not in game";
            this.Connection.Write(clientPacket);
        }
        else if (!sharedModel.Players.ContainsKey(playername)) {
            var clientPacket = new Packet("InviteRejected");
            clientPacket["playername"] = playername;
            clientPacket["reason"] = "unknown invitee";
            this.Connection.Write(clientPacket);
        }
        else {
            sharedModel.Games[player.Game].AddInvite(playername);

            var clientPacket = new Packet("InviteAccepted");
            clientPacket["playername"] = playername;
            this.Connection.Write(clientPacket);

            var invitedPacket = new Packet("Invite");
            invitedPacket["gamename"] = this.player.Game;
            liveConnections[playername].Write(invitedPacket);
        }
    }
}