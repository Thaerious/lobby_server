using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
namespace frar.lobbyserver;

public class DatabaseInterface {
    public static int SALT_SIZE = 64;
    public static int ITERATIONS = 32000;
    public static int HASH_EXPIRY_HOURS = 12;

    public delegate void SQL(MySqlConnection conn);
    private readonly string cs;
    private readonly HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA512;

    public DatabaseInterface() {
        DotEnv.Load();
        var env = Environment.GetEnvironmentVariables();

        cs = @$"
            server={env["SQL_SERVER"]};
            userid={env["SQL_USER"]};
            password={env["SQL_PW"]};
            database={env["SQL_DB"]}
        ";
    }

    public bool RegisterPlayer(string name, string password, string email) {
        var salt = RandomNumberGenerator.GetBytes(SALT_SIZE);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            ITERATIONS,
            hashAlgorithm,
            SALT_SIZE
        );

        return StoreCredentials(
            name,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            ITERATIONS,
            email,
            "pending"
        );
    }

    private bool StoreCredentials(string name, string salt, string hash, int iterations, string email, string status) {
        using (var conn = new MySqlConnection(cs)) {
            conn.Open();

            string sql = "INSERT INTO users(username, salt, password, iterations, email, status) values (@username, @salt, @password, @iterations, @email, @status)";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", name);
            cmd.Parameters.AddWithValue("@salt", salt);
            cmd.Parameters.AddWithValue("@password", hash);
            cmd.Parameters.AddWithValue("@iterations", iterations);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@status", status);

            return cmd.ExecuteNonQuery() >= 1;
        }
    }

    public bool Verify(string username, string password) {
        using (var conn = new MySqlConnection(cs)) {
            conn.Open();
            string sql = "SELECT * FROM users where username = @username";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);

            MySqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows) return false;
            reader.Read();

            byte[] salt = Convert.FromBase64String((string)reader["salt"]);
            byte[] hash = Convert.FromBase64String((string)reader["password"]);
            int iterations = (int)reader["iterations"];

            byte[] verifyThis = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                ITERATIONS,
                hashAlgorithm,
                SALT_SIZE
            );

            if (hash.Length != verifyThis.Length) return false;
            for (int i = 0; i < hash.Length; i++) {
                if (hash[i] != verifyThis[i]) return false;
            }

            return true;
        }
    }

    public bool DeleteRegistration(string username) {
        var conn = new MySqlConnection(cs);
        conn.Open();

        string sql = "DELETE FROM users where username = @username";
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@username", username);

        var r = cmd.ExecuteNonQuery() >= 1;
        conn.Close();
        return r;
    }

    public bool HasUsername(string username) {
        using (var conn = new MySqlConnection(cs)) {
            conn.Open();
            string sql = "SELECT * FROM users where username = @username";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);

            MySqlDataReader reader = cmd.ExecuteReader();
            return reader.HasRows;
        }
    }

    public bool DeleteSession(string username) {
        using (var conn = new MySqlConnection(cs)) {
            conn.Open();

            string sql = "DELETE FROM sessions where username = @username";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);
            return cmd.ExecuteNonQuery() >= 1;
        }
    }

    public string AssignSession(string username) {
        DeleteSession(username);

        var conn = new MySqlConnection(cs);
        conn.Open();

        var expire = DateTime.Now.AddHours(HASH_EXPIRY_HOURS);
        string formatForSql = expire.ToString("yyyy-MM-dd HH:mm:ss");

        var hash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(SALT_SIZE));

        string sql = "INSERT INTO sessions(username, hash, expire) values (@username, @hash, @expire)";
        var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Parameters.AddWithValue("@expire", formatForSql);

        cmd.ExecuteNonQuery();

        System.Console.WriteLine(" - " + hash);
        return hash;
    }

    /// <summary>
    /// Retrieve the username associated with a valid session hash.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns>If valid, the username, else an empty string ("").</returns>
    public string VerifySession(string hash) {
        var conn = new MySqlConnection(cs);
        conn.Open();

        string sql = "SELECT * FROM sessions where hash = @hash";
        var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@hash", hash);

        MySqlDataReader reader = cmd.ExecuteReader();
        if (!reader.HasRows) return "";
        reader.Read();

        if (DateTime.Now > (DateTime)reader["expire"]) return "";

        return (string)reader["username"];
    }

    public void ClearAll() {
        using (var conn = new MySqlConnection(cs)) {
            conn.Open();
            new MySqlCommand("DELETE FROM users", conn).ExecuteNonQuery();            
            new MySqlCommand("DELETE FROM sessions", conn).ExecuteNonQuery();            
        }
    }
}