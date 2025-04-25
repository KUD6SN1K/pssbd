using Npgsql;
using System.Data;

namespace WindowsFormsApp1
{
    internal class DataBase
    {
        // Connection string
        private string connectionString = "Host=localhost;Username=postgres;Password=8265;Database=pssbd";

        // Method to open connection
        public void openConnection(NpgsqlConnection sqlConnection)
        {
            if (sqlConnection.State == ConnectionState.Closed)
            {
                sqlConnection.Open();
            }
        }

        // Method to close connection
        public void closeConnection(NpgsqlConnection sqlConnection)
        {
            if (sqlConnection.State == ConnectionState.Open)
            {
                sqlConnection.Close();
            }
        }

        // Method to get a new connection each time
        public NpgsqlConnection getConnection()
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
