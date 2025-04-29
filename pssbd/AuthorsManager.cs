using Npgsql;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class AuthorsManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _authorsTable;
        private int _currentPage = 1;
        private const int AuthorsPerPage = 20;
        private int _totalAuthors;
        private bool _isSearchMode = false;
        private string _currentSearchTerm = "";

        public AuthorsManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _authorsTable = new DataTable();
            _authorsTable.Columns.Add("author_id", typeof(int));
            _authorsTable.Columns.Add("first_name", typeof(string));
            _authorsTable.Columns.Add("last_name", typeof(string));
            _authorsTable.Columns.Add("o_name", typeof(string));
            _authorsTable.Columns.Add("country_name", typeof(string));

            _dataGridView.DataSource = _authorsTable;
            _dataGridView.Columns["author_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Настройка заголовков
            _dataGridView.Columns["first_name"].HeaderText = "Имя";
            _dataGridView.Columns["last_name"].HeaderText = "Фамилия";
            _dataGridView.Columns["o_name"].HeaderText = "Отчество";
            _dataGridView.Columns["country_name"].HeaderText = "Страна";
        }

        public void LoadData()
        {
            _authorsTable.Clear();

            int offset = (_currentPage - 1) * AuthorsPerPage;
            string query;
            string countQuery;

            using (var connection = _database.getConnection())
            {
                connection.Open();

                if (_isSearchMode)
                {
                    query = $"SELECT * FROM search_authors(@search_term) LIMIT {AuthorsPerPage} OFFSET {offset}";
                    countQuery = "SELECT COUNT(*) FROM search_authors(@search_term)";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@search_term", _currentSearchTerm);
                        new NpgsqlDataAdapter(cmd).Fill(_authorsTable);
                    }

                    using (var countCmd = new NpgsqlCommand(countQuery, connection))
                    {
                        countCmd.Parameters.AddWithValue("@search_term", _currentSearchTerm);
                        _totalAuthors = Convert.ToInt32(countCmd.ExecuteScalar());
                    }
                }
                else
                {
                    query = $"SELECT * FROM authors_view LIMIT {AuthorsPerPage} OFFSET {offset}";
                    countQuery = "SELECT COUNT(*) FROM authors_view";

                    new NpgsqlDataAdapter(query, connection).Fill(_authorsTable);
                    _totalAuthors = Convert.ToInt32(new NpgsqlCommand(countQuery, connection).ExecuteScalar());
                }
            }
        }

        public void SaveChanges()
        {
            using (var connection = _database.getConnection())
            {
                connection.Open();

                foreach (DataRow row in _authorsTable.Rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                    {
                        var id = row["author_id", DataRowVersion.Original];
                        new NpgsqlCommand("SELECT delete_author(@id)", connection)
                            .AddParam("@id", (int)id)
                            .ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Modified)
                    {
                        new NpgsqlCommand("SELECT update_author(@id, @first, @last, @o, @country)", connection)
                            .AddParams(new
                            {
                                id = (int)row["author_id"],
                                first = row["first_name"],
                                last = row["last_name"],
                                o = row["o_name"],
                                country = row["country_name"]
                            })
                            .ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Added)
                    {
                        new NpgsqlCommand("SELECT insert_author(@first, @last, @o, @country)", connection)
                            .AddParams(new
                            {
                                first = row["first_name"],
                                last = row["last_name"],
                                o = row["o_name"],
                                country = row["country_name"]
                            })
                            .ExecuteNonQuery();
                    }
                }

                _authorsTable.AcceptChanges();
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
            int totalPages = (int)Math.Ceiling((double)_totalAuthors / AuthorsPerPage);
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

        public string GetPageInfo() => $"Page {_currentPage} of {(int)Math.Ceiling((double)_totalAuthors / AuthorsPerPage)}";
    }
}