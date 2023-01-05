using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.clientserver;

namespace frar.lobbyserver.test;

/// <summary>
/// Test the RemovePlayer method of the LobbyRouter class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.RemovePlayerTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class RemovePlayerTest : ALobbyTest {

    [TestMethod]
    public void player_not_logged_in() {
        var eve = NewUser("eve", login: false);
        eve.router.Process(new Packet("RemovePlayer"));
        eve.conn.Assert("AuthError");
    }

    /// <summary>
    /// Can not remove a player if the owner has no game.
    /// </summary>
    [TestMethod]
    public void owner_not_in_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");
        adam.router.Process(new Packet("KickPlayer", "eve"));

        adam.conn.Assert("KickRejected");
    }

    /// <summary>
    /// Can not remove a player that is not in the game.
    /// </summary>
    [TestMethod]
    public void target_not_in_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");
        adam.CreateGame();

        adam.router.Process(new Packet("KickPlayer", "eve"));
        adam.conn.Assert("KickRejected");        
    }

    /// <summary>
    /// Can not remove the owner from a game.
    /// </summary>
    [TestMethod]
    public void target_is_owner() {
        var adam = NewUser("adam");
        adam.CreateGame();
        
        adam.router.Process(new Packet("KickPlayer", "adam"));
        adam.conn.Assert("KickRejected");           
    }

    /// <summary>
    /// Removes a valid player from the game.
    /// </summary>
    [TestMethod]
    public void from_game() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");
        var able = NewUser("able");

        adam.CreateGame();

        eve.router.Process(new Packet("JoinGame", "adam's game"));
        adam.router.Process(new Packet("KickPlayer", "eve"));

        // Owner receive's accepted notification.
        adam.conn.Assert("KickAccepted");

        // Target receive's kick notification.
        eve.conn.Assert("KickedFromGame");

        // Global receive's game update notification.
        adam.conn
            .Assert("PlayerLeave")
            .Assert("gamename", "adam's game")
            .Assert("playername", "eve");

        eve.conn
            .Assert("PlayerLeave")
            .Assert("gamename", "adam's game")
            .Assert("playername", "eve");

        able.conn
            .Assert("PlayerLeave")
            .Assert("gamename", "adam's game")
            .Assert("playername", "eve");

        // The game no longer has the player
        var contains = adam.GetGame("adam's game").Players.Contains("eve");
        Assert.IsFalse(contains);

        // The player no longer has the game
        var hasGame = adam.GetPlayer("eve").HasGame;
        Assert.IsFalse(hasGame);
    }
}
