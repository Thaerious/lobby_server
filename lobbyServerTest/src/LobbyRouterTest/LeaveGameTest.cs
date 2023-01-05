using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.clientserver;
using Newtonsoft.Json;

namespace frar.lobbyserver.test;

/// <summary>
/// Test the LeaveGame method of the LobbyRouter class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.LeaveTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class LeaveGameTest : ALobbyTest {
    /// <summary>
    /// Leave game before logging in.
    /// Client receives AuthError.
    /// </summary>
    [TestMethod]
    public void player_join_rejected_login() {
        var eve = NewUser("eve", login: false);
        eve.router.Process(new Packet("LeaveGame", "adam's game"));
        eve.conn.Assert("AuthError");        
    }

    [TestMethod]
    public void player_leave_game_normally() {
        var adam = NewUser("adam").CreateGame();
        var eve = NewUser("eve");
        var able = NewUser("able");

        // Player joins game.
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        // Non-owner leaves game
        eve.router.Process(new Packet("LeaveGame"));

        // Notify the player
        eve.conn.Assert("LeaveAccepted");

        // Notify global
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

    [TestMethod]
    public void player_leaves_game_while_not_in_game() {
        var adam = NewUser("adam").CreateGame();
        var eve = NewUser("eve");

        // Non-owner leaves game
        eve.router.Process(new Packet("LeaveGame"));
        eve.conn.Assert("LeaveRejected");
    }

    /// <summary>
    /// Owner leaves game, the game terminates, all players are removed.
    /// </summary>
    [TestMethod]
    public void owner_leaves_game() {
        var adam = NewUser("adam").CreateGame();
        var eve = NewUser("eve");
        var able = NewUser("able");  // doesn't join

        eve.router.Process(new Packet("JoinGame", "adam's game"));
        adam.router.Process(new Packet("LeaveGame"));

        // Notify the player (owner)
        adam.conn.Assert("LeaveAccepted");

        // Notify the non-owner
        eve.conn.Assert("KickedFromGame");

        // Notify global
        adam.conn
            .Assert("RemoveGame")
            .Assert("gamename", "adam's game");

        eve.conn
            .Assert("RemoveGame")
            .Assert("gamename", "adam's game");

        able.conn
            .Assert("RemoveGame")
            .Assert("gamename", "adam's game");

        // Game no longer exists
        var contains = adam.GetGames().ContainsKey("adams's game");
        Assert.IsFalse(contains);
System.Console.WriteLine(JsonConvert.SerializeObject(adam.GetPlayer("eve")));
System.Console.WriteLine(adam.GetPlayer("eve").HasGame);
        // Players no longer have a game
        Assert.IsFalse(adam.GetPlayer("adam").HasGame);
        Assert.IsFalse(adam.GetPlayer("eve").HasGame);        
    }

}
