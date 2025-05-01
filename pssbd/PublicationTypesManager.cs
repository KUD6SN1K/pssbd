using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class PublicationTypesManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _publicationTypesTable;

        public PublicationTypesManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _publicationTypesTable = new DataTable();
            _publicationTypesTable.Columns.Add("publication_type_id", typeof(int));
            _publicationTypesTable.Columns.Add("publication_type_name", typeof(string));

            _dataGridView.DataSource = _publicationTypesTable;
            _dataGridView.Columns["publication_type_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Настройка заголовков
            _dataGridView.Columns["publication_type_name"].HeaderText = "Тип издания";
        }

        public void LoadData()
        {
            _publicationTypesTable.Clear();

            using (var connection = _database.getConnection())
            {
                connection.Open();
                string query = "SELECT * FROM publication_types_view ORDER BY publication_type_id";
                new NpgsqlDataAdapter(query, connection).Fill(_publicationTypesTable);
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
                    foreach (DataRow row in _publicationTypesTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            var id = row["publication_type_id", DataRowVersion.Original];
                            new NpgsqlCommand("SELECT delete_publication_type(@id)", connection)
                                .AddParam("@id", (int)id)
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            new NpgsqlCommand("SELECT update_publication_type(@id, @name)", connection)
                                .AddParams(new
                                {
                                    id = (int)row["publication_type_id"],
                                    name = row["publication_type_name"]
                                })
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            new NpgsqlCommand("SELECT insert_publication_type(@name)", connection)
                                .AddParam("@name", row["publication_type_name"])
                                .ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    _publicationTypesTable.AcceptChanges();
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