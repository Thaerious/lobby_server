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
        dbi!.ClearAll();
    }

    [TestMethod]
    public void register_new_player() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        Assert.IsNotNull(conn.Get("RegisterAccepted"));
    }

    [TestMethod]
    public void register_new_player_reject_repeat() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        router.RegisterPlayer("whoami", "super secret", "who@ami");
        Assert.IsNotNull(conn.Get("RegisterRejected"));
    }

    [TestMethod]
    public void login_accept() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        router.Login("whoami", "super secret");

        var clientPacket = conn.Get("LoginAccepted");
        var globalPacket = conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("whoami", globalPacket!["playername"]);
    }

    [TestMethod]
    public void login_rejected() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.Login("whoami", "super secret");

        var clientPacket = conn.Get("LoginRejected");
        Assert.IsNotNull(clientPacket);
    }

    [TestMethod]
    public void logout_accepted() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        router.Login("whoami", "super secret");
        router.Logout();

        var clientPacket = conn.Get("LogoutAccepted");
        Assert.IsNotNull(clientPacket);

        var globalPacket = conn.Get("LeaveLobby");
        Assert.IsNotNull(globalPacket);
    }

    [TestMethod]
    public void logout_rejected() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.Logout();

        var clientPacket = conn.Get("LogoutRejected");
        Assert.IsNotNull(clientPacket);
    }

    /// <summary>
    /// Login with credentials then logout.
    /// Relogin with the session hash.
    /// </summary>
    [TestMethod]
    public void login_session_accept() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        router.Login("whoami", "super secret");

        var loginPacket = conn.Get("LoginAccepted");
        conn.Get("PlayerLogin");

        Assert.IsNotNull(loginPacket);
        string hash = (string)(loginPacket["hash"]);

        router.Logout();
        router.LoginSession(hash!);
        
        var rej = conn.Get("LoginRejected");
        if (rej != null) System.Console.WriteLine(rej.ToString());

        conn.AvailablePackets().ForEach(s => System.Console.WriteLine(s));

        var clientPacket = conn.Get("LoginAccepted");
        var globalPacket = conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("whoami", globalPacket!["playername"]);
    }

    /// <summary>
    /// This test passes in an incorrect hash for the session verification.
    /// </summary>
    [TestMethod]
    public void login_session_reject() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface(hashExpiry : 0);
        var conn = new TestConnection();
        router.Connection = conn;

        dbi.ClearAll();

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        router.Login("whoami", "super secret");

        var loginPacket = conn.Get("LoginAccepted");
        conn.Get("PlayerLogin");

        Assert.IsNotNull(loginPacket);
        string hash = (string)("I ain't no hash");

        router.Logout();
        router.LoginSession(hash!);

        conn.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
        Assert.IsNotNull(conn.Get("LoginRejected"));
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
    /// <summary>
    /// Creating a game will send packet to user.
    /// The password on the packet will be undefined.
    /// </summary> 
    [TestMethod]
    public void create_game_without_password() {
        var router = new LobbyRouter();
        new DatabaseInterface().ClearAll();
        var conn = new TestConnection();
        router.Connection = conn;

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        router.Login("whoami", "super secret");
        router.CreateGame("my game", 4);

        var createGamePacket = conn.Get("CreateAccepted");
        Assert.IsNotNull(createGamePacket);
        Assert.AreEqual("my game", createGamePacket["gamename"]);

        var newGamePacket = conn.Get("NewGame");
        Game game = (Game)(newGamePacket!.Get<Game>("game"));
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
        var router = new LobbyRouter();
        new DatabaseInterface().ClearAll();
        var conn = new TestConnection();
        router.Connection = conn;       

        router.RegisterPlayer("whoami", "super secret", "who@ami");
        router.Login("whoami", "super secret");
        router.CreateGame("my game", 4, "game pw");

        var createGamePacket = conn.Get("CreateAccepted");
        Assert.IsNotNull(createGamePacket);
        Assert.AreEqual("my game", createGamePacket["gamename"]);

        var newGamePacket = conn.Get("NewGame");

System.Console.WriteLine(newGamePacket!["game"]);

        Game game = (Game)(newGamePacket!.Get<Game>("game"));
        Assert.IsNotNull(game);

System.Console.WriteLine(game);

        Assert.AreEqual(null, game.Password); // password is not sent in packet
        Assert.IsTrue(game.PasswordRequired);
    }    
}

class TestConnection : IConnection {
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

    public Packet? Get(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                this.Packets.Remove(packet);
                return packet;
            }
        }
        return null;
    }

    public Packet? Peek(string action) {
        foreach (Packet packet in this.Packets) {
            if (packet.Action == action) {
                return packet;
            }
        }
        return null;
    }

    public List<string> AvailablePackets() {
        var available = new List<string>();

        foreach (Packet packet in this.Packets) {
            available.Add(packet.Action);
        }
        return available;
    }
}