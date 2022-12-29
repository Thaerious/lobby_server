using MySql.Data.MySqlClient;

DotEnv.Load();
var env = Environment.GetEnvironmentVariables();

string cs = @$"
               server={env["SQL_SERVER"]};
               userid={env["SQL_USER"]};
               password={env["SQL_PW"]};
               database={env["SQL_DB"]}
            ";

var con = new MySqlConnection(cs);
con.Open();


string sql = "select * from names";
using (var cmd = new MySqlCommand(sql, con)) {
    MySqlDataReader reader = cmd.ExecuteReader();
    while (reader.Read()) {
        var name = reader[0];
        System.Console.WriteLine(name);
        System.Console.WriteLine(reader.GetBodyDefinition("name"));
    }
}


con.Close();