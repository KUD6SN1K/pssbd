using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class CitiesManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _citiesTable;
        private int _currentPage = 1;
        private const int CitiesPerPage = 20;
        private int _totalCities;
        private bool _isSearchMode = false;
        private string _currentSearchTerm = "";

        public CitiesManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _citiesTable = new DataTable();
            _citiesTable.Columns.Add("city_id", typeof(int));
            _citiesTable.Columns.Add("city_name", typeof(string));

            _dataGridView.DataSource = _citiesTable;
            _dataGridView.Columns["city_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _dataGridView.Columns["city_name"].HeaderText = "Город";
        }

        public void LoadData()
        {
            _citiesTable.Clear();
            int offset = (_currentPage - 1) * CitiesPerPage;

            using (var connection = _database.getConnection())
            {
                connection.Open();

                string query = _isSearchMode
                    ? "SELECT * FROM search_cities(@search_term) LIMIT @limit OFFSET @offset"
                    : "SELECT * FROM cities_view LIMIT @limit OFFSET @offset";

                string countQuery = _isSearchMode
                    ? "SELECT COUNT(*) FROM search_cities(@search_term)"
                    : "SELECT COUNT(*) FROM cities_view";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (_isSearchMode) cmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    cmd.AddParam("@limit", CitiesPerPage);
                    cmd.AddParam("@offset", offset);
                    new NpgsqlDataAdapter(cmd).Fill(_citiesTable);
                }

                using (var countCmd = new NpgsqlCommand(countQuery, connection))
                {
                    if (_isSearchMode) countCmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    _totalCities = Convert.ToInt32(countCmd.ExecuteScalar());
                }
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
                    foreach (DataRow row in _citiesTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            var id = row["city_id", DataRowVersion.Original];
                            new NpgsqlCommand("SELECT delete_city(@id)", connection)
                                .AddParam("@id", (int)id)
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            new NpgsqlCommand("SELECT update_city(@id, @name)", connection)
                                .AddParams(new
                                {
                                    id = (int)row["city_id"],
                                    name = row["city_name"]
                                })
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            new NpgsqlCommand("SELECT insert_city(@name)", connection)
                                .AddParam("@name", row["city_name"])
                                .ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    _citiesTable.AcceptChanges();
                    MessageBox.Show("Изменения сохранены успешно");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                }
            }
        }

        public void Search(string searchTerm)
        {
            _isSearchMode = !string.IsNullOrWhiteSpace(searchTerm);
            _currentSearchTerm = searchTerm ?? "";
            _currentPage = 1;
            LoadData();
        }

        public void NextPage()
        {
            int totalPages = (int)Math.Ceiling((double)_totalCities / CitiesPerPage);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                LoadData();
            }
        }

        public void PreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadData();
            }
        }

        public string GetPageInfo() =>
            $"Page  {_currentPage}  of {(int)Math.Ceiling((double)_totalCities / CitiesPerPage)}";
    }
}
