using Npgsql;
using pssbd;
using System.Data;
using WindowsFormsApp1;

public class UsersManager
{
    private readonly DataBase _database;
    private readonly DataGridView _dataGridView;
    private DataTable _usersTable;

    public UsersManager(DataBase database, DataGridView dataGridView)
    {
        _database = database;
        _dataGridView = dataGridView;
        InitializeTable();
    }

    private void InitializeTable()
    {
        _usersTable = new DataTable();
        _usersTable.Columns.Add("username", typeof(string));
        _usersTable.Columns.Add("password", typeof(string));  // если не используем — можно скрыть
        _usersTable.Columns.Add("roles", typeof(string));

        _dataGridView.DataSource = _usersTable;
        _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    public void LoadData()
    {
        _usersTable.Clear();
        using (var connection = _database.getConnection())
        {
            connection.Open();
            string query = "SELECT * FROM user_roles_view";
            new NpgsqlDataAdapter(query, connection).Fill(_usersTable);
        }
    }

    public void SaveChanges()
    {
        using (var connection = _database.getConnection())
        {
            connection.Open();
            var transaction = connection.BeginTransaction();

            try
            {
                foreach (DataRow row in _usersTable.Rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                    {
                        var username = row["username", DataRowVersion.Original];
                        new NpgsqlCommand("SELECT delete_user(@username)", connection)
                            .AddParam("@username", username)
                            .ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Added)
                    {
                        new NpgsqlCommand("SELECT insert_user(@username, @password, string_to_array(@roles, ', '))", connection)
                            .AddParams(new
                            {
                                username = row["username"],
                                password = row["password"],
                                roles = row["roles"]
                            })
                            .ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Modified)
                    {
                        new NpgsqlCommand("SELECT update_user_roles(@username, string_to_array(@roles, ', '))", connection)
                            .AddParams(new
                            {
                                username = row["username"],
                                roles = row["roles"]
                            })
                            .ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                _usersTable.AcceptChanges();
                MessageBox.Show("Изменения сохранены");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}