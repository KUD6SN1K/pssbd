using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class BindingTypesManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _bindingTypesTable;

        public BindingTypesManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _bindingTypesTable = new DataTable();
            _bindingTypesTable.Columns.Add("binding_type_id", typeof(int));
            _bindingTypesTable.Columns.Add("binding_type_name", typeof(string));

            _dataGridView.DataSource = _bindingTypesTable;
            _dataGridView.Columns["binding_type_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _dataGridView.Columns["binding_type_name"].HeaderText = "Тип переплёта";
        }

        public void LoadData()
        {
            _bindingTypesTable.Clear();

            using (var connection = _database.getConnection())
            {
                connection.Open();
                string query = "SELECT * FROM bindingtypes_view";
                new NpgsqlDataAdapter(query, connection).Fill(_bindingTypesTable);
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
                    foreach (DataRow row in _bindingTypesTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            var id = row["binding_type_id", DataRowVersion.Original];
                            new NpgsqlCommand("SELECT delete_binding_type(@id)", connection)
                                .AddParam("@id", (int)id)
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            new NpgsqlCommand("SELECT update_binding_type(@id, @name)", connection)
                                .AddParams(new
                                {
                                    id = (int)row["binding_type_id"],
                                    name = row["binding_type_name"]
                                })
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            new NpgsqlCommand("SELECT insert_binding_type(@name)", connection)
                                .AddParam("@name", row["binding_type_name"])
                                .ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    _bindingTypesTable.AcceptChanges();
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
