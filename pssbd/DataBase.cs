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
    }
}
