using Microsoft.VisualStudio.TestTools.UnitTesting;
using frar.lobbyserver;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System;

namespace frar.lobbyserver.test;


/// <summary>
/// Test the DatabaseInterface class.
/// 
/// To test just this class:
/// dotnet test --filter ClassName=frar.lobbyserver.test.DatabaseInterfaceTest
/// 
/// Run tests with code coverage
/// dotnet test --collect:"XPlat Code Coverage"
/// 
/// Generate coverage reports
/// reportgenerator -reports:lobbyServerTest/TestResults/**/*.xml -targetdir:"coverage" -reporttypes:Html
/// </summary>
[TestClass]
public class DatabaseInterfaceTest {
    static DatabaseInterface? dbi;

    [ClassInitialize]
    public static void before(TestContext context) {
        dbi = new DatabaseInterface();
        dbi.CreateTables(userTable: "test_users", sessionTable: "test_sessions");
    }

    [TestInitialize]
    public void testInitialize() {
        dbi!.ClearAll();
    }

    [TestMethod]
    public void register_new_player() {
        bool result = dbi!.RegisterPlayer("whoami", "super secret", "who@ami");
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void verify_password_pass() {
        bool r = dbi!.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.Verify("whoami", "super secret");
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void verify_password_fail() {
        bool r = dbi!.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.Verify("whoami", "i dunno");
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void has_username_true() {
        bool r = dbi!.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.HasUsername("whoami");
        dbi.DeleteRegistration("whoami");
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void has_username_false() {
        bool r = dbi!.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.HasUsername("wrong");
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void assign_session() {
        bool r = dbi!.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.AssignSession("whoami");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void verify_session() {
        bool r = dbi!.RegisterPlayer("whoami", "super secret", "who@ami");
        var hash = dbi.AssignSession("whoami");
        var result = dbi.VerifySession(hash);
        Assert.AreEqual("whoami", result);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidSessionException))]
    public void verify_session_expired() {
        var dbi = new DatabaseInterface(hashExpiry : 0);
        dbi.CreateTables(userTable: "test_users", sessionTable: "test_sessions");
        dbi.ClearAll();        

        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var hash = dbi.AssignSession("whoami");
        var result = dbi.VerifySession(hash);      
    }
}