using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class OwnershipTypesManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _ownershipTypesTable;

        public OwnershipTypesManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _ownershipTypesTable = new DataTable();
            _ownershipTypesTable.Columns.Add("owner_ship_type_id", typeof(int));
            _ownershipTypesTable.Columns.Add("owner_ship_type_name", typeof(string));

            _dataGridView.DataSource = _ownershipTypesTable;
            _dataGridView.Columns["owner_ship_type_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Настройка заголовков
            _dataGridView.Columns["owner_ship_type_name"].HeaderText = "Тип собственности";
        }

        public void LoadData()
        {
            _ownershipTypesTable.Clear();

            using (var connection = _database.getConnection())
            {
                connection.Open();
                string query = "SELECT * FROM ownership_types_view ORDER BY owner_ship_type_id";
                new NpgsqlDataAdapter(query, connection).Fill(_ownershipTypesTable);
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
                    foreach (DataRow row in _ownershipTypesTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            var id = row["owner_ship_type_id", DataRowVersion.Original];
                            new NpgsqlCommand("SELECT delete_ownership_type(@id)", connection)
                                .AddParam("@id", (int)id)
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            new NpgsqlCommand("SELECT update_ownership_type(@id, @name)", connection)
                                .AddParams(new
                                {
                                    id = (int)row["owner_ship_type_id"],
                                    name = row["owner_ship_type_name"]
                                })
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            new NpgsqlCommand("SELECT insert_ownership_type(@name)", connection)
                                .AddParam("@name", row["owner_ship_type_name"])
                                .ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    _ownershipTypesTable.AcceptChanges();
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