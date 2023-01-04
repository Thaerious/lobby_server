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
public class InviteTest : ALobbyTest {
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

    [TestMethod]
    public void player_not_logged_in() {
        var eve = NewUser("eve", login: false);
        eve.router.Process(new Packet("LeaveGame"));
        eve.conn.Assert("AuthError");
    }
}
