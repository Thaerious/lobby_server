using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.lobbyserver;
using System.Collections.Generic;
using frar.clientserver;
using System;

namespace frar.lobbyserver.test;


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
public class LobbyRouterTest {
    static DatabaseInterface? dbi;

    [ClassInitialize]
    public static void before(TestContext context) {
        dbi = new DatabaseInterface();
        dbi.CreateTables(userTable: "test_users", sessionTable: "test_sessions");
    }

    [TestInitialize]
    public void testInitialize() {
        var dbi = new DatabaseInterface();
        dbi.ClearAll();
        LobbyRouter.sharedModel = new LobbyModel();
    }

    [TestMethod]
    public void register_new_player() {
        var adam = new User("adam");
        adam.router.RegisterPlayer("adam", "super secret", "who@ami");
        Assert.IsNotNull(adam.conn.Get("RegisterAccepted"));
    }

    [TestMethod]
    public void register_new_player_reject_repeat() {
        var adam = new User("adam");
        adam.router.RegisterPlayer("adam", "super secret", "who@ami");
        adam.router.RegisterPlayer("adam", "super secret", "who@ami");
        Assert.IsNotNull(adam.conn.Get("RegisterRejected"));
    }

    [TestMethod]
    public void login_accept() {
        var adam = new User("adam");
        var clientPacket = adam.conn.Get("LoginAccepted");
        var globalPacket = adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("adam", globalPacket!["playername"]);
    }

    [TestMethod]
    public void login_rejected() {
        var adam = new User("adam");
        adam.router.Login("adam", "super secret");

        var clientPacket = adam.conn.Get("LoginRejected");
        Assert.IsNotNull(clientPacket);
    }

    [TestMethod]
    public void logout_accepted() {
        var adam = new User("adam");

        adam.router.RegisterPlayer("adam", "super secret", "who@ami");
        adam.router.Login("adam", "super secret");
        adam.router.Logout();

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
        var adam = new User("adam", false);
        adam.router.Logout();

        var clientPacket = adam.conn.Get("LogoutRejected");
        Assert.IsNotNull(clientPacket);
    }

    /// <summary>
    /// Login with credentials then logout.
    /// Relogin with the session hash.
    /// </summary>
    [TestMethod]
    public void login_session_accept() {
        var adam = new User("adam");

        Assert.IsNotNull(adam.conn.Peek("LoginAccepted"));
        Assert.IsNotNull(adam.conn.Get("PlayerLogin"));
        string hash = (string)(adam.conn.Peek("LoginAccepted")["hash"]);

        adam.router.Logout();
        adam.router.LoginSession(hash!);

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
        var adam = new User("adam");

        var loginPacket = adam.conn.Get("LoginAccepted");
        adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(loginPacket);
        string hash = (string)("I ain't no hash");

        adam.router.Logout();
        adam.router.LoginSession(hash!);

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
public class CreateGameTest {
    public CreateGameTest() {
        var dbi = new DatabaseInterface();
        dbi.CreateTables(userTable: "userTest", sessionTable: "sessionTest");
        dbi.ClearAll();
    }

    [TestInitialize]
    public void testInitialize() {
        var dbi = new DatabaseInterface();
        dbi.ClearAll();
        LobbyRouter.sharedModel = new LobbyModel();
    }

    /// <summary>
    /// Creating a game will send packet to user.
    /// The password on the packet will be undefined.
    /// </summary> 
    [TestMethod]
    public void create_game_without_password() {
        var adam = new User("adam");
        adam.router.CreateGame("my game", 4);

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
        var adam = new User("adam");
        adam.router.CreateGame("my game", 4, "game pw");

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
        var adam = new User("adam", false);
        adam.router.CreateGame("my game", 4, "game pw");
        Assert.IsNotNull(adam.conn.Get("CreateRejected"));
    }

    [TestMethod]
    public void create_game_twice() {
        var adam = new User("adam");

        adam.router.CreateGame("my game", 4, "game pw");
        adam.router.CreateGame("my game", 4, "game pw");
        Assert.IsNotNull(adam.conn.Get("CreateRejected"));
    }

    [TestMethod]
    public void create_game_repeat_name() {
        var adam = new User("adam");
        var eve = new User("eve");

        adam.router.CreateGame("my game", 4, "game pw");
        eve.router.CreateGame("my game", 4, "game pw");
        Assert.IsNotNull(eve.conn.Get("CreateRejected"));
    }
}

public class User {
    public DatabaseInterface dbi;
    public LobbyRouter router;
    public TestConnection conn;

    public User(string name, bool login = true) {
        dbi = new DatabaseInterface();
        router = new LobbyRouter(dbi);
        conn = new TestConnection();
        router.Connection = conn;

        if (login) {
            router.RegisterPlayer(name, "super secret", "who@ami");
            router.Login(name, "super secret");
        }
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

    public bool Has(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                return true;
            }
        }

        return false;
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