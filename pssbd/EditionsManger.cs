using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class EditionsManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _editionsTable;
        private int _currentPage = 1;
        private const int EditionsPerPage = 20;
        private int _totalEditions;
        private bool _isSearchMode = false;
        private string _currentSearchTerm = "";

        public EditionsManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
            SetupDataGridViewEvents();
        }

        private void InitializeTable()
        {
            _editionsTable = new DataTable();
            _editionsTable.Columns.Add("book_title", typeof(string));
            _editionsTable.Columns.Add("edition_id", typeof(int));
            _editionsTable.Columns.Add("publisher_name", typeof(string));
            _editionsTable.Columns.Add("language_name", typeof(string));
            _editionsTable.Columns.Add("binding_type_name", typeof(string));
            _editionsTable.Columns.Add("publication_type_name", typeof(string));
            _editionsTable.Columns.Add("price", typeof(decimal));
            _editionsTable.Columns.Add("publication_year", typeof(int));
            _editionsTable.Columns.Add("printrun", typeof(int));
            _editionsTable.Columns.Add("authors", typeof(string));
            _editionsTable.Columns.Add("genres", typeof(string));

            _dataGridView.DataSource = _editionsTable;
            _dataGridView.Columns["edition_id"].Visible = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _dataGridView.Columns["book_title"].HeaderText = "Название книги";
            _dataGridView.Columns["publisher_name"].HeaderText = "Издательство";
            _dataGridView.Columns["language_name"].HeaderText = "Язык";
            _dataGridView.Columns["binding_type_name"].HeaderText = "Тип переплета";
            _dataGridView.Columns["publication_type_name"].HeaderText = "Тип издания";
            _dataGridView.Columns["price"].HeaderText = "Цена";
            _dataGridView.Columns["publication_year"].HeaderText = "Год издания";
            _dataGridView.Columns["printrun"].HeaderText = "Тираж";
            _dataGridView.Columns["authors"].HeaderText = "Авторы";
            _dataGridView.Columns["genres"].HeaderText = "Жанры";
        }

        private void SetupDataGridViewEvents()
        {
            _dataGridView.UserDeletingRow += (s, e) => {
                if (MessageBox.Show("Удалить эту запись?", "Подтверждение",
                    MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                }
            };
        }

        public void LoadData()
        {
            _editionsTable.Clear();
            int offset = (_currentPage - 1) * EditionsPerPage;

            using (var connection = _database.getConnection())
            {
                connection.Open();

                string query = _isSearchMode
                    ? "SELECT * FROM search_editions(@search_term) LIMIT @limit OFFSET @offset"
                    : "SELECT * FROM editions_view LIMIT @limit OFFSET @offset";

                string countQuery = _isSearchMode
                    ? "SELECT COUNT(*) FROM search_editions(@search_term)"
                    : "SELECT COUNT(*) FROM editions_view";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (_isSearchMode) cmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    cmd.AddParam("@limit", EditionsPerPage);
                    cmd.AddParam("@offset", offset);
                    new NpgsqlDataAdapter(cmd).Fill(_editionsTable);
                }

                using (var countCmd = new NpgsqlCommand(countQuery, connection))
                {
                    if (_isSearchMode) countCmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    _totalEditions = Convert.ToInt32(countCmd.ExecuteScalar());
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
                    foreach (DataRow row in _editionsTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            DeleteEdition((int)row["edition_id", DataRowVersion.Original], connection);
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            UpdateEdition(row, connection);
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            InsertEdition(row, connection);
                        }
                    }

                    transaction.Commit();
                    _editionsTable.AcceptChanges();
                    MessageBox.Show("Изменения сохранены успешно");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                }
            }
        }

        private void InsertEdition(DataRow row, NpgsqlConnection connection)
        {
            string bookTitle = row["book_title"].ToString();
            string authors = row["authors"].ToString();
            string genres = row["genres"].ToString();

            // 1. Создаем или получаем книгу
            int bookId = GetOrCreateBook(bookTitle, connection);

            // 2. Добавляем авторов
            AddAuthorsToBook(bookId, authors, connection);

            // 3. Добавляем только существующие жанры
            AddExistingGenresToBook(bookId, genres, connection);

            // 4. Создаем издание с проверкой существования справочных данных
            new NpgsqlCommand(
                "SELECT create_edition_with_existing_references(@book_id, @publisher_name, @language_name, " +
                "@binding_type_name, @publication_type_name, @price, @publication_year, @printrun)",
                connection)
                .AddParams(new
                {
                    book_id = bookId,
                    publisher_name = row["publisher_name"],
                    language_name = row["language_name"],
                    binding_type_name = row["binding_type_name"],
                    publication_type_name = row["publication_type_name"],
                    price = row["price"],
                    publication_year = row["publication_year"],
                    printrun = row["printrun"]
                })
                .ExecuteNonQuery();
        }

        private int GetOrCreateBook(string title, NpgsqlConnection connection)
        {
            using (var cmd = new NpgsqlCommand("SELECT get_or_create_book(@title)", connection))
            {
                cmd.AddParam("@title", title);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void AddAuthorsToBook(int bookId, string authors, NpgsqlConnection connection)
        {
            if (string.IsNullOrWhiteSpace(authors)) return;

            foreach (var authorName in authors.Split(','))
            {
                string trimmedName = authorName.Trim();
                if (string.IsNullOrEmpty(trimmedName)) continue;

                int authorId = GetOrCreateAuthor(trimmedName, connection);

                new NpgsqlCommand(
                    "SELECT add_author_to_book(@book_id, @author_id)",
                    connection)
                    .AddParams(new { book_id = bookId, author_id = authorId })
                    .ExecuteNonQuery();
            }
        }

        private int GetOrCreateAuthor(string name, NpgsqlConnection connection)
        {
            string[] nameParts = name.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            string firstName = nameParts.Length > 0 ? nameParts[0] : "";
            string lastName = nameParts.Length > 1 ? nameParts[1] : "";

            using (var cmd = new NpgsqlCommand(
                "SELECT get_or_create_author(@first, @last)",
                connection))
            {
                cmd.AddParams(new { first = firstName, last = lastName });
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void AddExistingGenresToBook(int bookId, string genres, NpgsqlConnection connection)
        {
            if (string.IsNullOrWhiteSpace(genres)) return;

            foreach (var genreName in genres.Split(','))
            {
                string trimmedName = genreName.Trim();
                if (string.IsNullOrEmpty(trimmedName)) continue;

                int? genreId = GetExistingGenreId(trimmedName, connection);
                if (genreId.HasValue)
                {
                    new NpgsqlCommand(
                        "SELECT add_genre_to_book(@book_id, @genre_id)",
                        connection)
                        .AddParams(new { book_id = bookId, genre_id = genreId.Value })
                        .ExecuteNonQuery();
                }
            }
        }

        private int? GetExistingGenreId(string name, NpgsqlConnection connection)
        {
            using (var cmd = new NpgsqlCommand("SELECT get_genre_id_by_name(@name)", connection))
            {
                cmd.AddParam("@name", name);
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? (int?)Convert.ToInt32(result) : null;
            }
        }

        private void UpdateEdition(DataRow row, NpgsqlConnection connection)
        {
            new NpgsqlCommand(
                "SELECT update_edition_with_existing_references(@edition_id, @book_title, @publisher_name, @language_name, " +
                "@binding_type_name, @publication_type_name, @price, @publication_year, @printrun)",
                connection)
                .AddParams(new
                {
                    edition_id = row["edition_id"],
                    book_title = row["book_title"],
                    publisher_name = row["publisher_name"],
                    language_name = row["language_name"],
                    binding_type_name = row["binding_type_name"],
                    publication_type_name = row["publication_type_name"],
                    price = row["price"],
                    publication_year = row["publication_year"],
                    printrun = row["printrun"]
                })
                .ExecuteNonQuery();

            int bookId = GetBookIdForEdition((int)row["edition_id"], connection);
            UpdateBookAuthors(bookId, row["authors"].ToString(), connection);
            UpdateBookGenres(bookId, row["genres"].ToString(), connection);
        }

        private int GetBookIdForEdition(int editionId, NpgsqlConnection connection)
        {
            using (var cmd = new NpgsqlCommand(
                "SELECT get_book_id_for_edition(@edition_id)",
                connection))
            {
                cmd.AddParam("@edition_id", editionId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void UpdateBookAuthors(int bookId, string authors, NpgsqlConnection connection)
        {
            new NpgsqlCommand(
                "SELECT clear_book_authors(@book_id)",
                connection)
                .AddParam("@book_id", bookId)
                .ExecuteNonQuery();

            AddAuthorsToBook(bookId, authors, connection);
        }

        private void UpdateBookGenres(int bookId, string genres, NpgsqlConnection connection)
        {
            new NpgsqlCommand(
                "SELECT clear_book_genres(@book_id)",
                connection)
                .AddParam("@book_id", bookId)
                .ExecuteNonQuery();

            AddExistingGenresToBook(bookId, genres, connection);
        }

        private void DeleteEdition(int editionId, NpgsqlConnection connection)
        {
            new NpgsqlCommand("SELECT delete_edition(@id)", connection)
                .AddParam("@id", editionId)
                .ExecuteNonQuery();
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
            int totalPages = (int)Math.Ceiling((double)_totalEditions / EditionsPerPage);
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
            $"Page {_currentPage} of {(int)Math.Ceiling((double)_totalEditions / EditionsPerPage)}";
    }
}