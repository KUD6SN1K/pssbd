using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class GenresManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _genresTable;

        public GenresManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _genresTable = new DataTable();
            _genresTable.Columns.Add("genre_id", typeof(int));
            _genresTable.Columns.Add("genre_name", typeof(string));

            _dataGridView.DataSource = _genresTable;
            _dataGridView.Columns["genre_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Настройка заголовков
            _dataGridView.Columns["genre_name"].HeaderText = "Жанр";
        }

        public void LoadData()
        {
            _genresTable.Clear();

            using (var connection = _database.getConnection())
            {
                connection.Open();
                string query = "SELECT * FROM genres_view";
                new NpgsqlDataAdapter(query, connection).Fill(_genresTable);
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
                    foreach (DataRow row in _genresTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            var id = row["genre_id", DataRowVersion.Original];
                            new NpgsqlCommand("SELECT delete_genre(@id)", connection)
                                .AddParam("@id", (int)id)
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            new NpgsqlCommand("SELECT update_genre(@id, @name)", connection)
                                .AddParams(new
                                {
                                    id = (int)row["genre_id"],
                                    name = row["genre_name"]
                                })
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            new NpgsqlCommand("SELECT insert_genre(@name)", connection)
                                .AddParam("@name", row["genre_name"])
                                .ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    _genresTable.AcceptChanges();
                    MessageBox.Show("Изменения сохранены успешно");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                }
            }
        }
    }
}