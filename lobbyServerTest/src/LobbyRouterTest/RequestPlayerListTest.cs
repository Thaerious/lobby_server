using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using frar.clientserver;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver.test;

[TestClass]
public class RequestPlayerListTest : ALobbyTest {
    public delegate void PlayerListDel(Dictionary<string, Player> players);

    /// <summary>
    /// Players not logged in get an AuthError
    /// </summary>
    [TestMethod]
    public void request_players_rejected_login() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("RequestPlayers"));
        adam.conn.Assert("AuthError");
    }

    /// <summary>
    /// Normal operation, sends PlayerList packet in response.
    /// </summary>
    [TestMethod]
    public void request_players_accepted() {
        var adam = NewUser("adam");
        var eve = NewUser("eve");

        adam.router.Process(new Packet("RequestPlayers"));

        adam.conn
            .Assert("PlayerList")
            .Assert("players");

        Dictionary<string, Player> players =
            adam.conn.Get("PlayerList")
            .Get<Dictionary<string, Player>>("players");

        Assert.IsTrue(players.ContainsKey("adam"));
        Assert.IsTrue(players.ContainsKey("eve"));
    }
}
