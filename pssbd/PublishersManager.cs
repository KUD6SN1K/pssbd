using Npgsql;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class PublishersManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _publishersTable;
        private int _currentPage = 1;
        private const int PublishersPerPage = 20;
        private int _totalPublishers;
        private bool _isSearchMode = false;
        private string _currentSearchTerm = "";

        public PublishersManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
        }

        private void InitializeTable()
        {
            _publishersTable = new DataTable();
            _publishersTable.Columns.Add("publisher_name", typeof(string));
            _publishersTable.Columns.Add("publisher_id", typeof(int));
            _publishersTable.Columns.Add("city_name", typeof(string));
            _publishersTable.Columns.Add("ownership_type", typeof(string));
            _publishersTable.Columns.Add("year_founded", typeof(int));
            _publishersTable.Columns.Add("phone_number", typeof(string));
            _publishersTable.Columns.Add("registration_address", typeof(string));
            _publishersTable.Columns.Add("email_address", typeof(string));


            _dataGridView.DataSource = _publishersTable;
            _dataGridView.Columns["publisher_id"].Visible = false;
           
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Настройка заголовков
            _dataGridView.Columns["publisher_name"].HeaderText = "Название";
            _dataGridView.Columns["city_name"].HeaderText = "Город";
            _dataGridView.Columns["ownership_type"].HeaderText = "Тип собственности";
            _dataGridView.Columns["year_founded"].HeaderText = "Год основания";
            _dataGridView.Columns["phone_number"].HeaderText = "Телефон";
            _dataGridView.Columns["registration_address"].HeaderText = "Адрес регистрации";
            _dataGridView.Columns["email_address"].HeaderText = "Email";

        }

        public void LoadData()
        {
            _publishersTable.Clear();

            int offset = (_currentPage - 1) * PublishersPerPage;
            string query;
            string countQuery;

            using (var connection = _database.getConnection())
            {
                connection.Open();

                if (_isSearchMode)
                {
                    query = $"SELECT * FROM search_publishers(@search_term) LIMIT {PublishersPerPage} OFFSET {offset}";
                    countQuery = "SELECT COUNT(*) FROM search_publishers(@search_term)";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@search_term", _currentSearchTerm);
                        new NpgsqlDataAdapter(cmd).Fill(_publishersTable);
                    }

                    using (var countCmd = new NpgsqlCommand(countQuery, connection))
                    {
                        countCmd.Parameters.AddWithValue("@search_term", _currentSearchTerm);
                        _totalPublishers = Convert.ToInt32(countCmd.ExecuteScalar());
                    }
                }
                else
                {
                    query = $"SELECT * FROM publishers_view LIMIT {PublishersPerPage} OFFSET {offset}";
                    countQuery = "SELECT COUNT(*) FROM publishers_view";

                    new NpgsqlDataAdapter(query, connection).Fill(_publishersTable);
                    _totalPublishers = Convert.ToInt32(new NpgsqlCommand(countQuery, connection).ExecuteScalar());
                }
            }
        }

        public void SaveChanges()
        {
            using (var connection = _database.getConnection())
            {
                connection.Open();

                foreach (DataRow row in _publishersTable.Rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                    {
                        var id = row["publisher_id", DataRowVersion.Original];
                        new NpgsqlCommand("SELECT delete_publisher(@id)", connection)
                            .AddParam("@id", (int)id)
                            .ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Modified)
                    {
                        new NpgsqlCommand("SELECT update_publisher(@id, @name, @city, @ownership, @year, @phone, @address, @email)", connection)
                            .AddParams(new
                            {
                                id = (int)row["publisher_id"],
                                name = row["publisher_name"],
                                city = row["city_name"],
                                ownership = row["ownership_type"],
                                year = row["year_founded"],
                                phone = row["phone_number"],
                                address = row["registration_address"],
                                email = row["email_address"]
                            })
                            .ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Added)
                    {
                        new NpgsqlCommand("SELECT insert_publisher(@name, @city, @ownership, @year, @phone, @address, @email)", connection)
                            .AddParams(new
                            {
                                name = row["publisher_name"],
                                city = row["city_name"],
                                ownership = row["ownership_type"],
                                year = row["year_founded"],
                                phone = row["phone_number"],
                                address = row["registration_address"],
                                email = row["email_address"]
                            })
                            .ExecuteNonQuery();
                    }
                }

                _publishersTable.AcceptChanges();
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
            int totalPages = (int)Math.Ceiling((double)_totalPublishers / PublishersPerPage);
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

        public string GetPageInfo() => $"Page {_currentPage} of {(int)Math.Ceiling((double)_totalPublishers / PublishersPerPage)}";
    }
}