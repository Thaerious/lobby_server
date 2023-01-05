using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.lobbyserver;
using System.Collections.Generic;
using frar.clientserver;
using static frar.lobbyserver.LobbyModel;

namespace frar.lobbyserver.test;


public class ALobbyTest {
    private DatabaseInterface dbi;

    public ALobbyTest() {
        dbi = new DatabaseInterface();
        dbi.CreateTables(userTable: "userTest", sessionTable: "sessionTest");
        dbi.ClearAll();
    }

    [TestInitialize]
    public void testInitialize() {
        dbi.ClearAll();
        LobbyRouter.sharedModel = new LobbyModel();
        LobbyRouter.liveConnections = new Dictionary<string, IConnection>();
    }

    public User NewUser(string name, bool login = true) {
        return new User(this, name, login);
    }

    public class User {
        public LobbyRouter router;
        public MockConnection conn;
        private ALobbyTest outer;
        private string name;

        public User(ALobbyTest outer, string name, bool login = true) {
            this.name = name;
            this.outer = outer;
            router = new LobbyRouter(outer.dbi);
            conn = new MockConnection();
            router.Connection = conn;

            if (login) {
                router.Process(new Packet("RegisterPlayer", name, "super secret", "who@ami"));
                router.Process(new Packet("Login", name, "super secret"));
            }
        }

        public User CreateGame() {
            return this.CreateGame($"{this.name}'s game", 4);
        }

        public User CreateGame(string gameName, int max) {
            router.Process(new Packet("CreateGame", gameName, max));
            return this;
        }

        /// <summary>
        /// Retrieve a game object for specified game.
        /// </summary>
        /// <param name="gameName"></param>
        /// <returns></returns>
        public Game GetGame(string gameName) {
            this.router.Process(new Packet("RequestGames"));

            Dictionary<string, Game> games =
                this.conn.Get("GameList")
                .Get<Dictionary<string, Game>>("games");

            return games[gameName];
        }

        public Dictionary<string, Game> GetGames() {
            this.router.Process(new Packet("RequestGames"));

            Dictionary<string, Game> games =
                this.conn.Get("GameList")
                .Get<Dictionary<string, Game>>("games");

            return games;
        }

        /// <summary>
        /// Retrieve a player object for specified player.
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public Player GetPlayer(string playerName) {
            this.router.Process(new Packet("RequestPlayers"));

            Dictionary<string, Player> players =
                this.conn.Get("PlayerList")
                .Get<Dictionary<string, Player>>("players");

            return players[playerName];
        }
    }
}
