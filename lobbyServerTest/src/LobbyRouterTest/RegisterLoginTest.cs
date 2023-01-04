using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.clientserver;

namespace frar.lobbyserver.test;

/// <summary>
/// Test the LobbyModel class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.LobbyRouterTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class RegisterLoginTest : ALobbyTest {
    static DatabaseInterface? dbi;

    [TestMethod]
    public void register_new_player_accept() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        Assert.IsNotNull(adam.conn.Get("RegisterAccepted"));
    }

    [TestMethod]
    public void register_new_player_reject_repeat() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        Assert.IsNotNull(adam.conn.Get("RegisterRejected"));
    }

    [TestMethod]
    public void login_accept() {
        var adam = NewUser("adam");
        var clientPacket = adam.conn.Get("LoginAccepted");
        var globalPacket = adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("adam", globalPacket!["playername"]);
    }

    [TestMethod]
    public void login_rejected() {
        var adam = NewUser("adam");
        adam.router.Process(new Packet("Login", "adam", "super secret"));

        var clientPacket = adam.conn.Get("LoginRejected");
        Assert.IsNotNull(clientPacket);
    }

    [TestMethod]
    public void logout_accepted() {
        var adam = NewUser("adam");

        adam.router.Process(new Packet("RegisterPlayer", "adam", "super secret", "who@ami"));
        adam.router.Process(new Packet("Login", "adam", "super secret"));
        adam.router.Process(new Packet("Logout"));

        var clientPacket = adam.conn.Get("LogoutAccepted");
        Assert.IsNotNull(clientPacket);

        var globalPacket = adam.conn.Get("LeaveLobby");
        Assert.IsNotNull(globalPacket);
    }

    /// <summary>
    /// Can not log out a player that hasn't logged in.
    /// </summary>
    [TestMethod]
    public void logout_rejected() {
        var adam = NewUser("adam", false);
        adam.router.Process(new Packet("Logout"));

        var clientPacket = adam.conn.Get("AuthError");
        Assert.IsNotNull(clientPacket);
    }

    /// <summary>
    /// Login with credentials then logout.
    /// Relogin with the session hash.
    /// </summary>
    [TestMethod]
    public void login_session_accept() {
        var adam = NewUser("adam");

        Assert.IsNotNull(adam.conn.Peek("LoginAccepted"));
        Assert.IsNotNull(adam.conn.Get("PlayerLogin"));
        string hash = (string)(adam.conn.Peek("LoginAccepted")["hash"]);

        adam.router.Process(new Packet("Logout"));
        adam.router.Process(new Packet("LoginSession", hash!));

        var clientPacket = adam.conn.Get("LoginAccepted");
        var globalPacket = adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(clientPacket);
        Assert.IsNotNull(clientPacket!["hash"]);
        Assert.IsNotNull(globalPacket);
        Assert.AreEqual("adam", globalPacket!["playername"]);
    }

    /// <summary>
    /// This test passes in an incorrect hash for the session verification.
    /// </summary>
    [TestMethod]
    public void login_session_reject() {
        var adam = NewUser("adam");

        var loginPacket = adam.conn.Get("LoginAccepted");
        adam.conn.Get("PlayerLogin");

        Assert.IsNotNull(loginPacket);
        string hash = (string)("I ain't no hash");

        adam.router.Process(new Packet("Logout"));
        adam.router.Process(new Packet("LoginSession", hash!));

        adam.conn.AvailablePackets().ForEach(s => System.Console.WriteLine(s));
        Assert.IsNotNull(adam.conn.Get("LoginRejected"));
    }
}
