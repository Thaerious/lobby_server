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

        var clientPacket = conn.Get("LoginAccepted");
        var globalPacket = conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("whoami", globalPacket!["playername"]);
    }

    [TestMethod]
    public void login_session_reject() {
        var router = new LobbyRouter();
        var dbi = new DatabaseInterface();
        DatabaseInterface.HASH_EXPIRY_HOURS = 0;
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

        var clientPacket = conn.Get("LoginRejected");

        Assert

        .IsNotNull(clientPacket);
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
    /// Not including a password is undefined.
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
        Assert.Equals("my game", createGamePacket["gamename"]);
        Assert.Equals(4, createGamePacket["maxplayers"]);
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
        router.CreateGame("my game", 4, "game password");

        var createGamePacket = conn.Get("CreateAccepted");
        Assert.IsNotNull(createGamePacket);
        Assert.Equals("my game", createGamePacket["gamename"]);
        Assert.Equals(4, createGamePacket["maxplayers"]);
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
        Packets.Add(packet);
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
}