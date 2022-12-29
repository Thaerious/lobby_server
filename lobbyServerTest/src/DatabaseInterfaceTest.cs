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

    [ClassInitialize]
    public static void before(TestContext context) {
        DotEnv.Load();
        var env = Environment.GetEnvironmentVariables();

        string cs = @$"
            server={env["SQL_SERVER"]};
            userid={env["SQL_USER"]};
            password={env["SQL_PW"]};
            database={env["SQL_DB"]}
        ";

        using (var conn = new MySqlConnection(cs)) {
            conn.Open();
            string sql = "delete from users";
            var cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }

    [TestMethod]
    public void register_new_player() {
        var dbi = new DatabaseInterface();
        bool result = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        dbi.DeleteRegistration("whoami");
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void verify_password_pass() {
        var dbi = new DatabaseInterface();
        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.Verify("whoami", "super secret");
        dbi.DeleteRegistration("whoami");
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void verify_password_fail() {
        var dbi = new DatabaseInterface();
        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.Verify("whoami", "i dunno");
        dbi.DeleteRegistration("whoami");
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void has_username_true() {
        var dbi = new DatabaseInterface();
        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.HasUsername("whoami");
        dbi.DeleteRegistration("whoami");
        Assert.AreEqual(true, result);
    }

    [TestMethod]
    public void has_username_false() {
        var dbi = new DatabaseInterface();
        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.HasUsername("wrong");
        dbi.DeleteRegistration("whoami");
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void assign_session() {
        var dbi = new DatabaseInterface();
        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var result = dbi.AssignSession("whoami");
        dbi.DeleteRegistration("whoami");
    }

    [TestMethod]
    public void verify_session() {
        var dbi = new DatabaseInterface();
        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var hash = dbi.AssignSession("whoami");
        var result = dbi.VerifySession(hash);
        Assert.AreEqual("whoami", result);
    }

    [TestMethod]
    public void verify_session_expired() {
        var dbi = new DatabaseInterface();
        dbi.ClearAll();
        DatabaseInterface.HASH_EXPIRY_HOURS = 0;
        bool r = dbi.RegisterPlayer("whoami", "super secret", "who@ami");
        var hash = dbi.AssignSession("whoami");
        var result = dbi.VerifySession(hash);
        Assert.AreEqual("", result);
    }
}