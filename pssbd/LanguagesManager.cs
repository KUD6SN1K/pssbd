using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class LanguagesManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _languagesTable;
        private int _currentPage = 1;
        private const int LanguagesPerPage = 20;
        private int _totalLanguages;
        private bool _isSearchMode = false;
        private string _currentSearchTerm = "";

        public LanguagesManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _languagesTable = new DataTable();
            _languagesTable.Columns.Add("language_id", typeof(int));
            _languagesTable.Columns.Add("language_name", typeof(string));

            _dataGridView.DataSource = _languagesTable;
            _dataGridView.Columns["language_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Настройка заголовков
            _dataGridView.Columns["language_name"].HeaderText = "Язык";
        }

        public void LoadData()
        {
            _languagesTable.Clear();
            int offset = (_currentPage - 1) * LanguagesPerPage;

            using (var connection = _database.getConnection())
            {
                connection.Open();

                string query = _isSearchMode
                    ? "SELECT * FROM search_languages(@search_term) LIMIT @limit OFFSET @offset"
                    : "SELECT * FROM languages_view LIMIT @limit OFFSET @offset";

                string countQuery = _isSearchMode
                    ? "SELECT COUNT(*) FROM search_languages(@search_term)"
                    : "SELECT COUNT(*) FROM languages_view";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (_isSearchMode) cmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    cmd.AddParam("@limit", LanguagesPerPage);
                    cmd.AddParam("@offset", offset);
                    new NpgsqlDataAdapter(cmd).Fill(_languagesTable);
                }

                using (var countCmd = new NpgsqlCommand(countQuery, connection))
                {
                    if (_isSearchMode) countCmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    _totalLanguages = Convert.ToInt32(countCmd.ExecuteScalar());
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
                    foreach (DataRow row in _languagesTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            var id = row["language_id", DataRowVersion.Original];
                            new NpgsqlCommand("SELECT delete_language(@id)", connection)
                                .AddParam("@id", (int)id)
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            new NpgsqlCommand("SELECT update_language(@id, @name)", connection)
                                .AddParams(new
                                {
                                    id = (int)row["language_id"],
                                    name = row["language_name"]
                                })
                                .ExecuteNonQuery();
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            new NpgsqlCommand("SELECT insert_language(@name)", connection)
                                .AddParam("@name", row["language_name"])
                                .ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    _languagesTable.AcceptChanges();
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
            int totalPages = (int)Math.Ceiling((double)_totalLanguages / LanguagesPerPage);
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
            $"Page {_currentPage} of {(int)Math.Ceiling((double)_totalLanguages / LanguagesPerPage)}";
    }
}