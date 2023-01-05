using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.clientserver;
using Newtonsoft.Json;

namespace frar.lobbyserver.test;

/// <summary>
/// Test the LeaveGame method of the LobbyRouter class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.StartGameTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class StartGameTest : ALobbyTest {

    [TestMethod]
    public void start_rejected_login() {
        var eve = NewUser("eve", login: false);
        eve.router.Process(new Packet("StartGame"));
        eve.conn.Assert("AuthError");
    }

    [TestMethod]
    public void start_player_not_owner(){
        var adam = NewUser("adam").CreateGame();
        var eve = NewUser("eve");

        eve.router.Process(new Packet("JoinGame", "adam's game"));
        eve.router.Process(new Packet("StartGame"));

        eve.conn.Assert("StartRejected");
    }

    [TestMethod]
    public void start_player_not_in_game(){
        var eve = NewUser("eve");

        eve.router.Process(new Packet("StartGame"));
        eve.conn.Assert("StartRejected");
    }    

    [TestMethod]
    public void start_game_normal() {
        var adam = NewUser("adam").CreateGame();
        var eve = NewUser("eve");
        var able = NewUser("able");

        // Player joins game.
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        // Owner starts game.
        adam.router.Process(new Packet("StartGame"));

        // Notify Players
        eve.conn
            .Assert("StartGame")
            .Assert("ip")
            .Assert("port");

        adam.conn
            .Assert("StartGame")
            .Assert("ip")
            .Assert("port");


        // Notify global
        able.conn
            .Assert("LeaveLobby", "playername", "adam");

        able.conn
            .Assert("LeaveLobby", "playername", "eve");

        able.conn
            .Assert("RemoveGame")
            .Assert("gamename", "adam's game");

        // The game no longer exits
        Assert.IsFalse(able.GetGames().ContainsKey("adam's game"));

        // The players are no longer in lobby
        Assert.IsFalse(able.GetPlayers().ContainsKey("adam"));
        Assert.IsFalse(able.GetPlayers().ContainsKey("eve"));
    }
}