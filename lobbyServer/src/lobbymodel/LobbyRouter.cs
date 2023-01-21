
using frar.clientserver;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver;

public class LobbyRouter : ThreadedRouter {
    public static LobbyModel sharedModel = new LobbyModel();
    public static Dictionary<string, LobbyRouter> liveRouters = new Dictionary<string, LobbyRouter>();

    private DatabaseInterface dbi;
    Player? player = null;

    public LobbyRouter() {
        this.dbi = new DatabaseInterface();        
    }

    public LobbyRouter(DatabaseInterface dbi) {
        this.dbi = dbi;
    }

    public void Broadcast(Packet packet) {
        foreach (LobbyRouter router in liveRouters.Values) {
            router.Connection.Write(packet);
        }
    }

    /// <summary>
    /// Retrieve the game object for specified player.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public Game GetPlayersGame(string playername) {
        var gamename = sharedModel.GetPlayer(playername).Game;
        return sharedModel.GetGame(gamename);
    }

    public Game GetPlayersGame() {
        ArgumentNullException.ThrowIfNull(this.player);
        return GetPlayersGame(this.player.Name);
    }

    [OnDisconnect]
    public void OnDisconnect(DISCONNECT_REASON reason) {
        this.Logout();
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
            packet["reason"] = "Player alrady exists in lobby";
            this.Connection.Write(packet);
        }
        else if (dbi.Verify(name, password)) {
            var clientPacket = new Packet("LoginAccepted");
            clientPacket["hash"] = dbi.AssignSession(name);
            this.Connection.Write(clientPacket);

            this.player = sharedModel.AddPlayer(name);
            lock (liveRouters) {
                liveRouters.Add(name, this);
            }

            var globalPacket = new Packet("PlayerLogin");
            globalPacket["playername"] = name;
            this.Broadcast(globalPacket);
        }
        else {
            var packet = new Packet("LoginRejected");
            packet["reason"] = "Login verification failed";
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
            lock (liveRouters) {
                liveRouters.Add(name, this);
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
    public void LeaveGame() {
        if (!player!.HasGame) {
            var packet = new Packet("LeaveRejected");
            packet["reason"] = "player not in game";
            this.Connection.Write(packet);
        }
        else if (GetPlayersGame().Owner == this.player.Name) {
            foreach (string playername in GetPlayersGame().Players) {
                if (playername == this.player.Name) continue;
                var targetPacket = new Packet("KickedFromGame");
                targetPacket["reason"] = "The owner terminated the game";
                liveRouters[playername].Connection.Write(targetPacket);
                sharedModel.Players[playername].ClearGame();
            }

            this.Connection.Write(new Packet("LeaveAccepted"));

            var packet = new Packet("RemoveGame");
            packet["gamename"] = this.player.Game;
            this.Broadcast(packet);

            this.player.ClearGame();
        }
        else {
            sharedModel.Games[player.Game].RemovePlayer(player.Name);
            this.Connection.Write(new Packet("LeaveAccepted"));

            var packet = new Packet("PlayerLeave");
            packet["gamename"] = player.Game;
            packet["playername"] = player.Name;
            this.Broadcast(packet);

            player.Game = "";
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
            lock (liveRouters) {
                liveRouters.Remove(player.Name);
                this.player = null;
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
        }
        else if (!game.Invited.Contains(player.Name) && game.Password != password) {
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
            liveRouters[playername].Connection.Write(invitedPacket);
        }
    }

    [Route]
    public void RequestPlayers() {
        Dictionary<string, Player> players = sharedModel.Players;
        var packet = new Packet("PlayerList");
        packet["players"] = players;
        this.Connection.Write(packet);
    }

    [Route]
    public void RequestGames() {
        Dictionary<string, Game> games = sharedModel.Games;
        var packet = new Packet("GameList");
        packet["games"] = games;
        this.Connection.Write(packet);
    }

    [Route]
    public void KickPlayer(string playername) {
        if (!this.player!.HasGame) {
            var packet = new Packet("KickRejected");
            packet["reason"] = "Player is not in a game";
            this.Connection.Write(packet);
        }
        else if (this.player.Name == playername) {
            var packet = new Packet("KickRejected");
            packet["reason"] = "Can not kick owner";
            this.Connection.Write(packet);
        }
        else if (!GetPlayersGame(this.player.Name).Players.Contains(playername)) {
            var packet = new Packet("KickRejected");
            packet["reason"] = "Target player is not in the game";
            this.Connection.Write(packet);
        }
        else {
            sharedModel.GetGame(this.player!.Game).RemovePlayer(playername);
            sharedModel.GetPlayer(playername).ClearGame();

            var ownerPacket = new Packet("KickAccepted");
            ownerPacket["playername"] = playername;
            this.Connection.Write(ownerPacket);

            var targetPacket = new Packet("KickedFromGame");
            targetPacket["reason"] = "The owner has removed you from the game";
            liveRouters[playername].Connection.Write(targetPacket);

            var globalPacket = new Packet("PlayerLeave");
            globalPacket["playername"] = playername;
            globalPacket["gamename"] = this.player.Game;
            this.Broadcast(globalPacket);
        }
    }

    [Route]
    public void StartGame() {
        if (!this.player!.HasGame || GetPlayersGame().Owner != this.player.Name) {
            var packet = new Packet("StartRejected");
            packet["reason"] = "Player is not game owner";
            this.Connection.Write(packet);
        }
        else {
            var removeGamePacket = new Packet("RemoveGame");
            removeGamePacket["gamename"] = this.player.Game;
            this.Broadcast(removeGamePacket);

            var startGamePacket = new Packet("StartGame");
            startGamePacket["ip"] = "127.0.0.1";
            startGamePacket["port"] = "9999";

            var players = sharedModel.StartGame(this.player.Game);
            foreach (String playername in players) {
                liveRouters[playername].Connection.Write(startGamePacket);

                var globalPacket = new Packet("LeaveLobby");
                globalPacket["playername"] = playername;
                this.Broadcast(globalPacket);

                lock (liveRouters) {
                    liveRouters[playername].player = null;
                    liveRouters.Remove(playername);
                }
            }
        }
    }
}