using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.lobbyserver;
using System.Collections.Generic;
using frar.clientserver;
using System;

namespace frar.lobbyserver.test;


public class LobbyTest {
    private DatabaseInterface dbi;

    public LobbyTest() {
        dbi = new DatabaseInterface();
        dbi.CreateTables(userTable: "userTest", sessionTable: "sessionTest");
        dbi.ClearAll();
    }

    [TestInitialize]
    public void testInitialize() {
        dbi.ClearAll();
        LobbyRouter.sharedModel = new LobbyModel();
        LobbyRouter.liveConnections = new Dictionary<string, IConnection>();
    }

    public User NewUser(string name, bool login = true) {
        return new User(this, name, login);
    }

    public class User {
        public LobbyRouter router;
        public TestConnection conn;
        private LobbyTest outer;

        public User(LobbyTest outer, string name, bool login = true) {
            this.outer = outer;
            router = new LobbyRouter(outer.dbi);
            conn = new TestConnection();
            router.Connection = conn;

            if (login) {
                router.Process(new Packet("RegisterPlayer", name, "super secret", "who@ami"));
                router.Process(new Packet("Login", name, "super secret"));
            }
        }
    }
}


/// <summary>
/// Test the LobbyModel class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.LobbyRouterTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class LobbyRouterTest : LobbyTest {
    static DatabaseInterface? dbi;

    [TestMethod]
    public void register_new_player_accept() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        Assert.IsNotNull(adam.conn.Get("RegisterAccepted"));
    }

    [TestMethod]
    public void register_new_player_reject_repeat() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        Assert.IsNotNull(adam.conn.Get("RegisterRejected"));
    }

    [TestMethod]
    public void login_accept() {
        var adam = NewUser("adam");
        var clientPacket = adam.conn.Get("LoginAccepted");
        var globalPacket = adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("adam", globalPacket!["playername"]);
    }

    [TestMethod]
    public void login_rejected() {
        var adam = NewUser("adam");
        adam.router.Process(new Packet("Login", "adam", "super secret"));

        var clientPacket = adam.conn.Get("LoginRejected");
        Assert.IsNotNull(clientPacket);
    }

    [TestMethod]
    public void logout_accepted() {
        var adam = NewUser("adam");

        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        adam.router.Process(new Packet("Login", "adam", "super secret"));
        adam.router.Process(new Packet("Logout"));

        var clientPacket = adam.conn.Get("LogoutAccepted");
        Assert.IsNotNull(clientPacket);

        var globalPacket = adam.conn.Get("LeaveLobby");
        Assert.IsNotNull(globalPacket);
    }

    /// <summary>
    /// Can not log out a player that hasn't logged in.
    /// </summary>
    [TestMethod]
    public void logout_rejected() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("Logout"));

        var clientPacket = adam.conn.Get("AuthError");
        Assert.IsNotNull(clientPacket);
    }

    /// <summary>
    /// Login with credentials then logout.
    /// Relogin with the session hash.
    /// </summary>
    [TestMethod]
    public void login_session_accept() {
        var adam = NewUser("adam");

        Assert.IsNotNull(adam.conn.Peek("LoginAccepted"));
        Assert.IsNotNull(adam.conn.Get("PlayerLogin"));
        string hash = (string)(adam.conn.Peek("LoginAccepted")["hash"]);

        adam.router.Process(new Packet("Logout"));
        adam.router.Process(new Packet("LoginSession", hash!));

        var clientPacket = adam.conn.Get("LoginAccepted");
        var globalPacket = adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("adam", globalPacket!["playername"]);
    }

    /// <summary>
    /// This test passes in an incorrect hash for the session verification.
    /// </summary>
    [TestMethod]
    public void login_session_reject() {
        var adam = NewUser("adam");

        var loginPacket = adam.conn.Get("LoginAccepted");
        adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(loginPacket);
        string hash = (string)("I ain't no hash");

        adam.router.Process(new Packet("Logout"));
        adam.router.Process(new Packet("LoginSession", hash!));

        adam.conn.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
        Assert.IsNotNull(adam.conn.Get("LoginRejected"));
    }
}

/// <summary>
/// Test the CreateGame method of the LobbyRouter class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.CreateGameTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class CreateGameTest : LobbyTest {
    /// <summary>
    /// Creating a game will send packet to user.
    /// The password on the packet will be undefined.
    /// </summary> 
    [TestMethod]
    public void create_game_without_password() {
        var adam = NewUser("adam");
        adam.router.Process(new Packet("CreateGame", "my game", 4));

        Assert.AreEqual("my game", adam.conn.Get("CreateAccepted").Get<string>("gamename"));
        Game game = (Game)(adam.conn.Get("NewGame").Get<Game>("game"));
        Assert.IsNotNull(game);

        Assert.AreEqual(null, game.Password); // password is not sent in packet
        Assert.IsFalse(game.PasswordRequired);
    }

    /// <summary>
    /// Creating a game will send packet to user.
    /// Including a password.
    /// </summary> 
    [TestMethod]
    public void create_game_with_password() {
        var adam = NewUser("adam");
        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));

        if (adam.conn.Has("LoginRejected")) {
            System.Console.WriteLine(
                adam.conn.Get("LoginRejected")
            );
        }

        Assert.AreEqual("my game", adam.conn.Get("CreateAccepted")["gamename"]);
        Game game = (Game)(adam.conn.Get("NewGame").Get<Game>("game"));
        Assert.IsNotNull(game);

        Assert.AreEqual(null, game.Password); // password is not sent in packet
        Assert.IsTrue(game.PasswordRequired);
    }

    [TestMethod]
    public void create_game_not_logged_in() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        Assert.IsNotNull(adam.conn.Get("AuthError"));
    }

    [TestMethod]
    public void create_game_twice() {
        var adam = NewUser("adam");

        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        Assert.IsNotNull(adam.conn.Get("CreateRejected"));
    }

    [TestMethod]
    public void create_game_repeat_name() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        eve.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        Assert.IsNotNull(eve.conn.Get("CreateRejected"));
    }
}

/// <summary>
/// Test the JoinGame method of the LobbyRouter class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.JoinGameTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class JoinGameTest : LobbyTest {
    /// <summary>
    /// Join a game without a password.
    /// Player joins a game because they are not already in a game.
    /// - Client receives a JoinAccepted packet.
    /// </summary>
    [TestMethod]
    public void player_join_no_password_accepted() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 4));
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        // Client receives a JoinAccepted packet.
        Assert.IsTrue(eve.conn.Has("JoinAccepted"));

        // All players receive a PlayerJoined packet.
        Assert.IsTrue(adam.conn.Has("PlayerJoined"));
        Assert.IsTrue(eve.conn.Has("PlayerJoined"));

        // PlayerJoined packet has gamename field
        var packet = adam.conn.Get("PlayerJoined");
        Assert.AreEqual("adam's game", packet.Get<string>("gamename"));

        // PlayerJoined packet has playername field
        Assert.AreEqual("eve", packet.Get<string>("playername"));
    }

    /// <summary>
    /// Join a game without a password.
    /// Player joins a game because they are not already in a game.
    /// - Client receives a JoinAccepted packet.
    /// </summary>
    [TestMethod]
    public void player_join_with_password_accepted() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 4, "ima password"));
        eve.router.Process(new Packet("JoinGame", "adam's game", "ima password"));

        // Client receives a JoinAccepted packet.
        Assert.IsTrue(eve.conn.Has("JoinAccepted"));

        // All players receive a PlayerJoined packet.
        Assert.IsTrue(adam.conn.Has("PlayerJoined"));
        Assert.IsTrue(eve.conn.Has("PlayerJoined"));

        // PlayerJoined packet has gamename field
        var packet = adam.conn.Get("PlayerJoined");
        Assert.AreEqual("adam's game", packet.Get<string>("gamename"));

        // PlayerJoined packet has playername field
        Assert.AreEqual("eve", packet.Get<string>("playername"));
    }

    /// <summary>
    /// Players not logged in can not join a game
    /// </summary>
    [TestMethod]
    public void player_join_rejected_login() {
        var adam = NewUser("adam");
        var eve = NewUser("eve", login: false);

        adam.router.Process(new Packet("CreateGame", "adam's game", 4));
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        eve.conn.Assert("AuthError");
    }

    /// <summary>
    /// Players can't join a game that doesn't exist
    /// </summary>
    [TestMethod]
    public void player_join_rejected_unknown_game() {
        var eve = NewUser("eve");
        eve.router.Process(new Packet("JoinGame", "adam's game"));
        eve.conn.Assert("JoinRejected");
    }    

    /// <summary>
    /// Players can't join a full game
    /// </summary>
    [TestMethod]
    public void player_join_rejected_full_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");
        var cain = NewUser("cain");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2));
        eve.router.Process(new Packet("JoinGame", "adam's game"));
        cain.router.Process(new Packet("JoinGame", "adam's game"));

        cain.conn.Assert("JoinRejected");
    }

    /// <summary>
    /// Players can't join multiple games
    /// </summary>
    [TestMethod]
    public void player_join_rejected_already_in_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");
        var cain = NewUser("cain");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2));
        eve.router.Process(new Packet("CreateGame", "eve's game", 2));
        cain.router.Process(new Packet("JoinGame", "adam's game"));
        cain.router.Process(new Packet("JoinGame", "eve's game"));

        cain.conn.Assert("JoinRejected");
    }    

    /// <summary>
    /// Players can't join multiple games
    /// </summary>
    [TestMethod]
    public void player_join_rejected_hosting_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2));
        eve.router.Process(new Packet("CreateGame", "eve's game", 2));
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        eve.conn.Assert("JoinRejected");
    }       

    /// <summary>
    /// Passwords must match
    /// </summary>
    [TestMethod]
    public void player_join_rejected_password() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2, "password"));
        eve.router.Process(new Packet("JoinGame", "adam's game", "wrong password"));

        eve.conn.Assert("JoinRejected");
    }        

    /// <summary>
    /// Passwords must match
    /// </summary>
    [TestMethod]
    public void player_join_rejected_password_not_provided() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2, "password"));
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        eve.conn.Assert("JoinRejected");
    }               
}

/// <summary>
/// Test the JoinGame method of the LobbyRouter class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.JoinGameTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class InviteTest : LobbyTest {
    /// <summary>
    /// Join a game without a password.
    /// Player joins a game because they are not already in a game.
    /// - Client receives a JoinAccepted packet.
    /// </summary>
    [TestMethod]
    public void player_join_invited_with_password() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 4, "password"));
        adam.router.Process(new Packet("InvitePlayer", "eve"));

        // Client receives a InviteAccepted  packet.
        adam.conn.Assert("InviteAccepted");

        // Packet has invited players name
        var packet = adam.conn.Get("InviteAccepted");
        Assert.AreEqual("eve", packet.Get<string>("playername"));

        // Invited client joins game w/o password.
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        if (eve.conn.Has("JoinRejected")) {
            System.Console.WriteLine(eve.conn.Peek("JoinRejected"));
        }

        // Invited client recieves JoinAccepted packet.
        eve.conn.Assert("JoinAccepted");
    }

    /// <summary>
    /// Players can be invited w/o a password
    /// </summary>
    [TestMethod]
    public void player_join_invited_without_password() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 4));
        adam.router.Process(new Packet("InvitePlayer", "eve"));

        // Client receives a InviteAccepted  packet.
        Assert.IsTrue(adam.conn.Has("InviteAccepted"));

        // Packet has invited players name
        var packet = adam.conn.Get("InviteAccepted");
        Assert.AreEqual("eve", packet.Get<string>("playername"));

        // Invited client joins game w/o password.
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        // Invited client recieves JoinAccepted packet.
        Assert.IsTrue(eve.conn.Has("JoinAccepted"));
    }

    /// <summary>
    /// No game exists for inviting player
    /// </summary>
    [TestMethod]
    public void player_join_rejected_login() {
        var adam = NewUser("adam");
        var eve = NewUser("eve", login: false);

        adam.router.Process(new Packet("CreateGame", "adam's game", 4));
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        // Client receives a JoinRejected packet.
        eve.conn.Assert("AuthError");
    }

    /// <summary>
    /// Players can't join a game that doesn't exist
    /// </summary>
    [TestMethod]
    public void player_join_rejected_unknown_game() {
        var eve = NewUser("eve");
        eve.router.Process(new Packet("JoinGame", "adam's game"));
        eve.conn.Assert("JoinRejected");
    }    

    /// <summary>
    /// Players can't join a full game
    /// </summary>
    [TestMethod]
    public void player_join_rejected_full_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");
        var cain = NewUser("cain");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2));
        eve.router.Process(new Packet("JoinGame", "adam's game"));
        cain.router.Process(new Packet("JoinGame", "adam's game"));

        cain.conn.Assert("JoinRejected");
    }

    /// <summary>
    /// Players can't join multiple games
    /// </summary>
    [TestMethod]
    public void player_join_rejected_already_in_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");
        var cain = NewUser("cain");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2));
        eve.router.Process(new Packet("CreateGame", "eve's game", 2));
        cain.router.Process(new Packet("JoinGame", "adam's game"));
        cain.router.Process(new Packet("JoinGame", "eve's game"));

        cain.conn.Assert("JoinRejected");
    }    

    /// <summary>
    /// Players can't join multiple games
    /// </summary>
    [TestMethod]
    public void player_join_rejected_hosting_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2));
        eve.router.Process(new Packet("CreateGame", "eve's game", 2));
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        eve.conn.Assert("JoinRejected");
    }       

    /// <summary>
    /// Passwords must match
    /// </summary>
    [TestMethod]
    public void player_join_rejected_password() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2, "password"));
        eve.router.Process(new Packet("JoinGame", "adam's game", "wrong password"));

        eve.conn.Assert("JoinRejected");
    }        

    /// <summary>
    /// Passwords must match
    /// </summary>
    [TestMethod]
    public void player_join_rejected_password_not_provided() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "adam's game", 2, "password"));
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        eve.conn.Assert("JoinRejected");
    }               
}



public class TestConnection : IConnection {
    public List<Packet> Packets = new List<Packet>();

    public Packet Read() {
        throw new NotImplementedException();
    }

    public void Shutdown() {
        throw new NotImplementedException();
    }

    public void Write(Packet packet) {
        Packets.Add(Packet.FromString(packet.ToString()));
    }

    public Packet Get(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                this.Packets.Remove(packet);
                return packet;
            }
        }

        this.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
        throw new Exception($"Unknown Packet: {action}");
    }

    public Packet Pop() {
        if (this.Packets.Count == 0) throw new Exception($"No available packets");
        var packet = this.Packets[0];
        this.Packets.RemoveAt(0);
        return packet;
    }

    public bool Has(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                return true;
            }
        }

        return false;
    }

    public void Assert(string action) {
        if (!this.Has(action)) {
            this.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail();
        }
    }

    public Packet Peek(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                return packet;
            }
        }

        this.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
        throw new Exception($"Unknown Packet: {action}");
    }

    public List<string> AvailablePackets() {
        var available = new List<string>();

        foreach (Packet packet in this.Packets) {
            available.Add(packet.Action);
        }
        return available;
    }
}