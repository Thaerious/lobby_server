using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.clientserver;

namespace frar.lobbyserver.test;

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
public class JoinGameTest : ALobbyTest {
    public JoinGameTest() {
    }

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
