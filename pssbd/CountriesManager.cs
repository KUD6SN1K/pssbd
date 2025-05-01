using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class CountriesManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _countriesTable;
        private int _currentPage = 1;
        private const int CountriesPerPage = 20;
        private int _totalCountries;
        private bool _isSearchMode = false;
        private string _currentSearchTerm = "";

        public CountriesManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _countriesTable = new DataTable();
            _countriesTable.Columns.Add("country_id", typeof(int));
            _countriesTable.Columns.Add("country_name", typeof(string));

            _dataGridView.DataSource = _countriesTable;
            _dataGridView.Columns["country_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _dataGridView.Columns["country_name"].HeaderText = "Страна";
        }

        public void LoadData()
        {
            _countriesTable.Clear();
            int offset = (_currentPage - 1) * CountriesPerPage;

            using (var connection = _database.getConnection())
            {
                connection.Open();

                string query = _isSearchMode
                    ? "SELECT * FROM search_countries(@search_term) LIMIT @limit OFFSET @offset"
                    : "SELECT * FROM countries_view LIMIT @limit OFFSET @offset";

                string countQuery = _isSearchMode
                    ? "SELECT COUNT(*) FROM search_countries(@search_term)"
                    : "SELECT COUNT(*) FROM countries_view";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (_isSearchMode) cmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    cmd.AddParam("@limit", CountriesPerPage);
                    cmd.AddParam("@offset", offset);
                    new NpgsqlDataAdapter(cmd).Fill(_countriesTable);
                }

                using (var countCmd = new NpgsqlCommand(countQuery, connection))
                {
                    if (_isSearchMode) countCmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    _totalCountries = Convert.ToInt32(countCmd.ExecuteScalar());
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
                    foreach (DataRow row in _countriesTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            var id = row["country_id", DataRowVersion.Original];
                            new NpgsqlCommand("SELECT delete_country(@id)", connection)
                                .AddParam("@id", (int)id)
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            new NpgsqlCommand("SELECT update_country(@id, @name)", connection)
                                .AddParams(new
                                {
                                    id = (int)row["country_id"],
                                    name = row["country_name"]
                                })
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            new NpgsqlCommand("SELECT insert_country(@name)", connection)
                                .AddParam("@name", row["country_name"])
                                .ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    _countriesTable.AcceptChanges();
                    MessageBox.Show("Изменения успешно сохранены.");
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
            int totalPages = (int)Math.Ceiling((double)_totalCountries / CountriesPerPage);
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
            $"Page {_currentPage} of {(int)Math.Ceiling((double)_totalCountries / CountriesPerPage)}";
    }
}
