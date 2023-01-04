using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.clientserver;

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
public class LeaveTest : ALobbyTest {
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

    /// <summary>
    /// Join a game without a password.
    /// Player joins a game because they are not already in a game.
    /// - Client receives a JoinAccepted packet.
    /// </summary>
    [TestMethod]
    public void player_leave_game_normally() {
        var adam = NewUser("adam").CreateGame();
        var eve = NewUser("eve");

        // Player joins game.
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        // Non-owner leaves game
        eve.router.Process(new Packet("LeaveGame"));

        eve.conn.Assert("LeaveAccepted");

        adam.conn
            .Assert("PlayerLeave")
            .Assert("gamename", "adam's game")
            .Assert("playername", "eve");
    }

    /// <summary>
    /// Players can be invited w/o a password
    /// </summary>
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
    // [TestMethod]
    public void owner_leaves_game() {
        var adam = NewUser("adam").CreateGame();
        var eve = NewUser("eve");

        eve.router.Process(new Packet("JoinGame", "adam's game"));
        adam.router.Process(new Packet("LeaveGame"));

        adam.conn.Assert("LeaveAccepted");
    }

}
