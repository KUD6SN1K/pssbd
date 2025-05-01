using Npgsql;
using System.Data;

namespace WindowsFormsApp1
{
    public class DataBase
    {
        // Connection string
        private string connectionString = "Host=localhost;Username=postgres;Password=8265;Database=pssbd";

        // Constructor to set the connection string with dynamic username and password
        public DataBase(string host, string username, string password, string database)
        {
            connectionString = $"Host={host};Username={username};Password={password};Database={database}";
        }

       
        // Method to get a new connection using the current connection string
        public NpgsqlConnection getConnection()
        {
            return new NpgsqlConnection(connectionString);
        }

        public List<string> GetUserRoles()
        {
            var roles = new List<string>();

            using (var connection = getConnection())
            {
                connection.Open();
                string query = @"
            SELECT c.rolname
            FROM pg_roles a
            INNER JOIN pg_auth_members b ON a.oid = b.member
            INNER JOIN pg_roles c ON b.roleid = c.oid
            WHERE a.rolname = current_user;
        ";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(reader.GetString(0));
                    }
                }
            }

            return roles;
        }

    }
}
