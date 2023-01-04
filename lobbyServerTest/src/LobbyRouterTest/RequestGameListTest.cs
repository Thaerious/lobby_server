using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using frar.clientserver;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver.test;

[TestClass]
public class RequestGameListTest : ALobbyTest {

    /// <summary>
    /// Players not logged in get an AuthError
    /// </summary>
    [TestMethod]
    public void request_games_rejected_login() {
        var adam = NewUser("adam", false);

        adam.router.Process(new Packet("RequestGames"));

        // Client receives a JoinRejected packet.
        adam.conn.Assert("AuthError");
    }

    [TestMethod]
    public void request_games_accepted_empty() {
        var adam = NewUser("adam");

        adam.router.Process(new Packet("RequestGames"));

        adam.conn
            .Assert("GameList")
            .Assert("games");

        Dictionary<string, Game> games =
            adam.conn.Get("GameList")
            .Get<Dictionary<string, Game>>("games");

        Assert.AreEqual(0, games.Count);
    }

    [TestMethod]
    public void request_games_accepted_non_empty() {
        var adam = NewUser("adam");

        adam.router.Process(new Packet("CreateGame", "my game", 4));
        adam.router.Process(new Packet("RequestGames"));

        adam.conn
            .Assert("GameList")
            .Assert("games");

        Dictionary<string, Player> games =
            adam.conn.Get("GameList")
            .Get<Dictionary<string, Player>>("games");

        Assert.AreEqual(1, games.Count);
    }
}
