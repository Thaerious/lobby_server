using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver.test;


/// <summary>
/// Test the LobbyModel class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.LobbyModelTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class LobbyModelTest {
    [TestMethod]
    public void lobby_model_constructor_sanity_test() {
        var lobbyModel = new LobbyModel();
        Assert.AreEqual(0, lobbyModel.Players.Count);
        Assert.AreEqual(0, lobbyModel.Games.Count);
    }

    [TestMethod]
    public void added_player_has_correct_name() {
        LobbyModel lobbyModel = new LobbyModel();
        Player player = lobbyModel.AddPlayer("Adam");
        Assert.AreEqual(player.Name, "Adam");
    }

    [TestMethod]
    [ExpectedException(typeof(PlayerNameException))]
    public void adding_same_name_twice_exception() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.AddPlayer("Adam");
    }

    [TestMethod]
    [ExpectedException(typeof(UnknownPlayerException))]
    public void create_game_unknown_player() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
    }

    [TestMethod]
    public void create_game_sanity() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        Assert.IsNotNull(lobbyModel.GetPlayer("Adam"));  
        lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
    }

    [TestMethod]
    public void add_then_get_player() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        Assert.IsNotNull(lobbyModel.GetPlayer("Adam"));        
    }

    [TestMethod]
    public void add_invited_player() {
        LobbyModel lobbyModel = new LobbyModel();
        var adam = lobbyModel.AddPlayer("Adam");
        var eve = lobbyModel.AddPlayer("Eve");
        var game = lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
        game.AddInvite("eve");

        Assert.IsTrue(game.AddPlayer("eve"));
    }

    [TestMethod]
    [ExpectedException(typeof(RepeatedPlayerException))]
    public void add_invited_player_twice() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        var eve = lobbyModel.AddPlayer("Eve");
        var game = lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
        game.AddInvite("eve");
        game.AddInvite("eve");
    }

    [TestMethod]
    public void uninvite_player() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        var eve = lobbyModel.AddPlayer("Eve");
        var game = lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
        game.AddInvite("eve");
        game.RemoveInvite("eve");

        Assert.IsFalse(game.AddPlayer("eve"));
    }

    [TestMethod]
    [ExpectedException(typeof(UnknownPlayerException))]
    public void uninvite_unknown_player() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        var eve = new Player("eve");
        var game = lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
        game.RemoveInvite("eve");

        Assert.IsFalse(game.AddPlayer("eve"));
    }

    // The lobby can not have repeated game names.
    [TestMethod]
    [ExpectedException(typeof(GameNameException))]
    public void create_game_same_name() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.AddPlayer("Eve");
        lobbyModel.CreateGame("Game Name", "Adam", 4, "password");
        lobbyModel.CreateGame("Game Name", "Eve", 4, "password");
    }

    // One player can not start 2 games.
    [TestMethod]
    [ExpectedException(typeof(PlayerInGameException))]
    public void create_game_same_owner() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 4, "password");
        lobbyModel.CreateGame("Adam's Other Game", "Adam", 4, "password");
    }

    // Game can not be less than 2 players
    [TestMethod]
    [ExpectedException(typeof(MaxPlayersException))]
    public void too_few_players() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 1, "password");
    }

    // Game can not be more than 5 players
    [TestMethod]
    [ExpectedException(typeof(MaxPlayersException))]
    public void too_many_players() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 6, "password");
    }

    // 2 Players is permitted
    [TestMethod]
    public void player_count_min() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 2, "password");

        bool actual = lobbyModel.Players["Adam"].HasGame;
        bool expected = true;
        Assert.AreEqual(expected, actual);
    }

    // 5 Players is permitted
    [TestMethod]
    public void player_count_max() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 5, "password");

        bool actual = lobbyModel.Players["Adam"].HasGame;
        bool expected = true;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void contains_game_true() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 5, "password");

        bool actual = lobbyModel.HasGame("Adam's Game");
        bool expected = true;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void contains_game_false() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 5, "password");

        bool actual = lobbyModel.HasGame("Eve's Game");
        bool expected = false;
        Assert.AreEqual(expected, actual);
    }

    // Retrieving a game with an unknown player throws an exception.
    [TestMethod]
    [ExpectedException(typeof(UnknownPlayerException))]
    public void get_unknown_player() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.GetPlayer("Eve");
    }

    // Retrieving a game by it's name
    [TestMethod]
    public void get_game_by_name() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 2, "password");
        Game game = lobbyModel.GetGame("Adam's Game");
        string actual = game.Name;
        string expected = "Adam's Game";
        Assert.AreEqual(expected, actual);
    }

    // Retrieving a game that doesn't exists throws an exception.
    [TestMethod]
    [ExpectedException(typeof(UnknownGameException))]
    public void get_unknown_game_by_name() {
        LobbyModel lobbyModel = new LobbyModel();
        Game game = lobbyModel.GetGame("Adam's Game");
    }

    [TestMethod]
    public void get_all_players() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.AddPlayer("Eve");
        lobbyModel.AddPlayer("Cain");
        lobbyModel.AddPlayer("Able");
        Dictionary<string, Player> players = lobbyModel.Players;

        var actual = players.Count;
        var expected = 4;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void get_all_games() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.AddPlayer("Eve");
        lobbyModel.AddPlayer("Cain");
        lobbyModel.AddPlayer("Able");
        lobbyModel.CreateGame("Adam's Game", "Adam", 2, "password");
        lobbyModel.CreateGame("Eve's Game", "Eve", 2, "password");
        lobbyModel.CreateGame("Cain's Game", "Cain", 2, "password");
        lobbyModel.CreateGame("Able's Game", "Able", 2, "password");
        Dictionary<string, Game> games = lobbyModel.Games;

        var actual = games.Count;
        var expected = 4;
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void remove_existing_player_from_lobby() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.RemovePlayer("Adam");

        Assert.IsFalse(lobbyModel.HasPlayer("Adam"));
    }


    [TestMethod]
    [ExpectedException(typeof(UnknownPlayerException))]
    public void remove_unknown_player_from_lobby() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.RemovePlayer("Steve");
    }

    [TestMethod]
    public void has_player_true() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");

        Assert.IsTrue(lobbyModel.HasPlayer("Adam"));
    }

    [TestMethod]
    public void has_player_false() {
        LobbyModel lobbyModel = new LobbyModel();

        Assert.IsFalse(lobbyModel.HasPlayer("Adam"));
    }

    [TestMethod]
    public void has_game_true() {
        LobbyModel lobbyModel = new LobbyModel();
        lobbyModel.AddPlayer("Adam");
        lobbyModel.CreateGame("Adam's Game", "Adam", 2, "password");

        Assert.IsTrue(lobbyModel.HasGame("Adam's Game"));
    }

    [TestMethod]
    public void has_game_false() {
        LobbyModel lobbyModel = new LobbyModel();

        Assert.IsFalse(lobbyModel.HasGame("Adam's Game"));
    }
}