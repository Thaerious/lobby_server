using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.clientserver;

namespace frar.lobbyserver.test;

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
public class CreateGameTest : ALobbyTest {
    /// <summary>
    /// Creating a game will send packet to user.
    /// The password on the packet will be undefined.
    /// </summary> 
    [TestMethod]
    public void create_game_without_password() {
        var adam = NewUser("adam");
        adam.router.Process(new Packet("CreateGame", "my game", 4));

        Assert.AreEqual("my game", adam.conn.Get("CreateAccepted").Get<string>("gamename"));
        Game game = (Game)(adam.conn.Get("NewGame").Get<Game>("game"));
        Assert.IsNotNull(game);

        Assert.AreEqual(null, game.Password); // password is not sent in packet
        Assert.IsFalse(game.PasswordRequired);
    }

    /// Check that the information on the game matches the submission.
    public void create_game_check_values() {
        var adam = NewUser("adam");
        var eve = NewUser("adam");

        adam.CreateGame();
        eve.router.Process(new Packet("JoinGame", "adam's game"));

        Game game = adam.GetGame("adam's game");
        Assert.AreEqual("adam's game", game.Name);
        Assert.AreEqual("adam", game.Owner);
        Assert.AreEqual(4, game.MaxPlayers);
        Assert.AreEqual(false, game.PasswordRequired);
        Assert.AreEqual(0, game.Invited.Count);
        Assert.AreEqual(2, game.Players.Count);
    }

    /// <summary>
    /// Creating a game will send packet to user.
    /// Including a password.
    /// </summary> 
    [TestMethod]
    public void create_game_with_password() {
        var adam = NewUser("adam");
        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));

        if (adam.conn.Has("LoginRejected")) {
            System.Console.WriteLine(
                adam.conn.Get("LoginRejected")
            );
        }

        Assert.AreEqual("my game", adam.conn.Get("CreateAccepted")["gamename"]);
        Game game = (Game)(adam.conn.Get("NewGame").Get<Game>("game"));
        Assert.IsNotNull(game);

        Assert.AreEqual(null, game.Password); // password is not sent in packet
        Assert.IsTrue(game.PasswordRequired);
    }

    [TestMethod]
    public void create_game_not_logged_in() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        Assert.IsNotNull(adam.conn.Get("AuthError"));
    }

    [TestMethod]
    public void create_game_twice() {
        var adam = NewUser("adam");

        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        Assert.IsNotNull(adam.conn.Get("CreateRejected"));
    }

    [TestMethod]
    public void create_game_repeat_name() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        eve.router.Process(new Packet("CreateGame", "my game", 4, "game pw"));
        Assert.IsNotNull(eve.conn.Get("CreateRejected"));
    }
}
