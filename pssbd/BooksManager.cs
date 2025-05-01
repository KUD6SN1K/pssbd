using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class BooksManager
    {
        private readonly DataBase _database;
        private readonly DataGridView _dataGridView;

        private DataTable _booksTable;
        private int _currentPage = 1;
        private const int BooksPerPage = 20;
        private int _totalBooks;
        private bool _isSearchMode = false;
        private string _currentSearchTerm = "";

        public BooksManager(DataBase database, DataGridView dataGridView)
        {
            _database = database;
            _dataGridView = dataGridView;
            InitializeTable();
            SetupDataGridViewEvents();
        }

        private void InitializeTable()
        {
            _booksTable = new DataTable();
            _booksTable.Columns.Add("book_id", typeof(int));
            _booksTable.Columns.Add("title", typeof(string));
            _booksTable.Columns.Add("year_of_writing", typeof(int));
            _booksTable.Columns.Add("authors", typeof(string));
            _booksTable.Columns.Add("genres", typeof(string));

            _dataGridView.DataSource = _booksTable;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _dataGridView.Columns["title"].HeaderText = "Название книги";
            _dataGridView.Columns["year_of_writing"].HeaderText = "Год написания";
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
            _booksTable.Clear();
            int offset = (_currentPage - 1) * BooksPerPage;

            using (var connection = _database.getConnection())
            {
                connection.Open();

                string query = _isSearchMode
                    ? "SELECT * FROM search_books(@search_term) LIMIT @limit OFFSET @offset"
                    : "SELECT * FROM books_view LIMIT @limit OFFSET @offset";

                string countQuery = _isSearchMode
                    ? "SELECT COUNT(*) FROM search_books(@search_term)"
                    : "SELECT COUNT(*) FROM books_view";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    if (_isSearchMode) cmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    cmd.AddParam("@limit", BooksPerPage);
                    cmd.AddParam("@offset", offset);
                    new NpgsqlDataAdapter(cmd).Fill(_booksTable);
                }

                using (var countCmd = new NpgsqlCommand(countQuery, connection))
                {
                    if (_isSearchMode) countCmd.AddParam("@search_term", $"%{_currentSearchTerm}%");
                    _totalBooks = Convert.ToInt32(countCmd.ExecuteScalar());
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
                    foreach (DataRow row in _booksTable.Rows)
                    {
                        if (row.RowState == DataRowState.Deleted)
                        {
                            DeleteBook((int)row["book_id", DataRowVersion.Original], connection);
                        }
                        else if (row.RowState == DataRowState.Modified)
                        {
                            UpdateBook(row, connection);
                        }
                        else if (row.RowState == DataRowState.Added)
                        {
                            InsertBook(row, connection);
                        }
                    }

                    transaction.Commit();
                    _booksTable.AcceptChanges();
                    MessageBox.Show("Изменения сохранены успешно");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                }
            }
        }

        private void InsertBook(DataRow row, NpgsqlConnection connection)
        {
            string title = row["title"].ToString();
            int? yearOfWriting = row["year_of_writing"] != DBNull.Value ? (int?)row["year_of_writing"] : null;
            string authors = row["authors"].ToString();
            string genres = row["genres"].ToString();

            // Создаем книгу
            int bookId = CreateBook(title, yearOfWriting, connection);

            // Добавляем авторов
            AddAuthorsToBook(bookId, authors, connection);

            // Добавляем жанры
            AddExistingGenresToBook(bookId, genres, connection);
        }

        private int CreateBook(string title, int? yearOfWriting, NpgsqlConnection connection)
        {
            using (var cmd = new NpgsqlCommand("SELECT create_book(@title, @year_of_writing)", connection))
            {
                cmd.AddParam("@title", title);
                cmd.AddParam("@year_of_writing", yearOfWriting ?? (object)DBNull.Value);
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

        private void UpdateBook(DataRow row, NpgsqlConnection connection)
        {
            int bookId = (int)row["book_id"];
            string title = row["title"].ToString();
            int? yearOfWriting = row["year_of_writing"] != DBNull.Value ? (int?)row["year_of_writing"] : null;
            string authors = row["authors"].ToString();
            string genres = row["genres"].ToString();

            // Обновляем книгу
            UpdateBookDetails(bookId, title, yearOfWriting, connection);

            // Обновляем авторов
            UpdateBookAuthors(bookId, authors, connection);

            // Обновляем жанры
            UpdateBookGenres(bookId, genres, connection);
        }

        private void UpdateBookDetails(int bookId, string title, int? yearOfWriting, NpgsqlConnection connection)
        {
            new NpgsqlCommand(
                "SELECT update_book(@book_id, @title, @year_of_writing)",
                connection)
                .AddParams(new
                {
                    book_id = bookId,
                    title = title,
                    year_of_writing = yearOfWriting ?? (object)DBNull.Value
                })
                .ExecuteNonQuery();
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

        private void DeleteBook(int bookId, NpgsqlConnection connection)
        {
            new NpgsqlCommand("SELECT delete_book(@id)", connection)
                .AddParam("@id", bookId)
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
            int totalPages = (int)Math.Ceiling((double)_totalBooks / BooksPerPage);
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
            $"Page {_currentPage} of {(int)Math.Ceiling((double)_totalBooks / BooksPerPage)}";
    }
}