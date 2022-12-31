using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.lobbyserver;
using System.Collections.Generic;
using System.Diagnostics;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver.test;

/// <summary>
/// Test the Game class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.GameTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:frarClientServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class GameTest {
    /// <summary>
    /// Make sure the public fields are set by the constructor.
    /// </summary>
    [TestMethod]
    public void game_constructor_sanity_test() {
        var adam = new Player("adam");

        var game = new Game(name: "adam's game", owner: "adam", password : "secret", maxplayers : 4);
        Assert.AreEqual("adam's game", game.Name);
        Assert.AreEqual("adam", game.Owner);
        Assert.AreEqual("secret", game.Password);
        Assert.AreEqual(4, game.MaxPlayers);
    }

    [TestMethod]
    public void invited_starts_empty() {
        var adam = new Player("adam");

        var game = new Game("adam's game", "adam", 4, "secret");
        Assert.AreEqual(0, game.Invited.Count);
    }

    /// <summary>
    /// The Players field of a new Game object contains the owner.
    /// </summary>
    [TestMethod]
    public void players_starts_with_owner() {
        var game = new Game("adam's game", "adam", 4, "");
        Assert.AreEqual(1, game.Players.Count);
        Assert.IsTrue(game.HasPlayer("adam"));
    }

    /// <summary>
    /// HasPlayer returns false if the player wasn't added.
    /// </summary>
    [TestMethod]
    public void has_player_false() {
        var game = new Game("adam's game", "adam", 4, "");
        Assert.AreEqual(1, game.Players.Count);
        Assert.IsFalse(game.HasPlayer("eve"));
    }

    [ExpectedException(typeof(RepeatedPlayerException))]
    [TestMethod]
    public void can_not_add_player_twice() {
        var game = new Game("adam's game", "adam", 4, "");
        game.AddPlayer("eve");
        game.AddPlayer("eve");
    }

    [ExpectedException(typeof(GameFullException))]
    [TestMethod]
    public void can_not_add_more_than_max_players() {
        var game = new Game("adam's game", "adam", 4, "");
        game.AddPlayer("eve");
        game.AddPlayer("cane");
        game.AddPlayer("able");
        game.AddPlayer("bob");
    }

    [TestMethod]
    public void has_added_player() {
        var game = new Game("adam's game", "adam", 4, "");
        game.AddPlayer("eve");
        Assert.IsTrue(game.HasPlayer("eve"));
    }

    /// <summary>
    /// Add player returns true when the player was added.
    /// </summary>
    [TestMethod]
    public void add_player_returns_true() {
        var game = new Game("adam's game", "adam", 4, "");
        Assert.IsTrue(game.AddPlayer("eve"));
    }

    /// <summary>
    /// Add player returns false when the player was not added.
    /// </summary>
    [TestMethod]
    public void add_player_returns_false() {
        var game = new Game("adam's game", "adam", 4, "secret");
        Assert.IsFalse(game.AddPlayer("eve", "dunno password"));
    }

    /// <summary>
    /// HasPlayer returns false when a player is removed.
    /// </summary>
    [TestMethod]
    public void has_removed_player_false() {
        var game = new Game("adam's game", "adam", 4, "");
        game.AddPlayer("eve");
        game.RemovePlayer("eve");
        Assert.IsFalse(game.HasPlayer("eve"));
    }

    [ExpectedException(typeof(RemoveOwnerException))]
    [TestMethod]
    public void can_not_remove_owner() {
        var game = new Game("adam's game", "adam", 4, "");
        game.RemovePlayer("adam");
    }

    [ExpectedException(typeof(UnknownPlayerException))]
    [TestMethod]
    public void can_not_remove_unknown_player() {
        var game = new Game("adam's game", "adam", 4, "");
        game.RemovePlayer("eve");
    }

    [TestMethod]
    public void add_invited() {
        var game = new Game("adam's game", "adam", 4, "");
        game.AddInvite("eve");
        Assert.IsTrue(game.Invited.Contains("eve"));
    }

    [TestMethod]
    public void remove_invited() {
        var game = new Game("adam's game", "adam", 4, "");
        game.AddInvite("eve");
        game.RemoveInvite("eve");
        Assert.IsFalse(game.Invited.Contains("eve"));
    }

    [TestMethod]
    public void check_name_valid() {
        Assert.IsTrue(Game.CheckName("apple"));
        Assert.IsTrue(Game.CheckName("apple sauce"));
        Assert.IsTrue(Game.CheckName("apple-sauce"));
        Assert.IsTrue(Game.CheckName("apple_sauce"));
        Assert.IsTrue(Game.CheckName("apple.sauce"));
        Assert.IsTrue(Game.CheckName("12345678901234567901234"));
        Assert.IsTrue(Game.CheckName("123"));
    }

    [TestMethod]
    public void check_name_invalid() {
        Assert.IsFalse(Game.CheckName("1234567890123456789012345"));
        Assert.IsFalse(Game.CheckName("***"));
        Assert.IsFalse(Game.CheckName("\"abc\""));
        Assert.IsFalse(Game.CheckName("1"));
        Assert.IsFalse(Game.CheckName("12"));
        Assert.IsFalse(Game.CheckName("     "));
    }    
}