using Npgsql;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public partial class Form1 : Form
    {
        private DataBase database;
        public DataBase Database { get; private set; }
        public Form1(DataBase database)
        {
            InitializeComponent();
            this.database = database;
        }

        private DataTable authorsTable;
        private int currentPage = 1;
        private int authorsPerPage = 20;
        private int totalAuthors;
        private bool isSearchMode = false;
        private string currentSearchTerm = "";

        private void CreateTable()
        {
            authorsTable = new DataTable();

            authorsTable.Columns.Add("author_id", typeof(int));
            authorsTable.Columns.Add("first_name", typeof(string));
            authorsTable.Columns.Add("last_name", typeof(string));
            authorsTable.Columns.Add("o_name", typeof(string));
            authorsTable.Columns.Add("country_name", typeof(string));

            dataGridView1.DataSource = authorsTable;

            dataGridView1.Columns["author_id"].Visible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Set readable headers
            dataGridView1.Columns["first_name"].HeaderText = "Имя";
            dataGridView1.Columns["last_name"].HeaderText = "Фамилия";
            dataGridView1.Columns["o_name"].HeaderText = "Отчество";
            dataGridView1.Columns["country_name"].HeaderText = "Страна";
        }

        private void SaveChanges()
        {
            using (var connection = database.getConnection())
            {
                connection.Open();

                foreach (DataRow row in authorsTable.Rows)
                {
                    if (row.RowState == DataRowState.Deleted)
                    {
                        var id = row["author_id", DataRowVersion.Original];
                        var deleteCmd = new NpgsqlCommand("SELECT delete_author(@id)", connection);
                        deleteCmd.Parameters.AddWithValue("@id", (int)id);
                        deleteCmd.ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Modified)
                    {
                        var updateCmd = new NpgsqlCommand("SELECT update_author(@id, @first, @last, @o, @country)", connection);
                        updateCmd.Parameters.AddWithValue("@id", (int)row["author_id"]);
                        updateCmd.Parameters.AddWithValue("@first", row["first_name"]);
                        updateCmd.Parameters.AddWithValue("@last", row["last_name"]);
                        updateCmd.Parameters.AddWithValue("@o", row["o_name"]);
                        updateCmd.Parameters.AddWithValue("@country", row["country_name"]);
                        updateCmd.ExecuteNonQuery();
                    }
                    else if (row.RowState == DataRowState.Added)
                    {
                        var insertCmd = new NpgsqlCommand("SELECT insert_author(@first, @last, @o, @country)", connection);
                        insertCmd.Parameters.AddWithValue("@first", row["first_name"]);
                        insertCmd.Parameters.AddWithValue("@last", row["last_name"]);
                        insertCmd.Parameters.AddWithValue("@o", row["o_name"]);
                        insertCmd.Parameters.AddWithValue("@country", row["country_name"]);
                        insertCmd.ExecuteNonQuery();
                    }
                }

                authorsTable.AcceptChanges(); // Reset row states after save
            }
        }

        private void LoadViewData()
        {
            authorsTable.Clear();

            int offset = (currentPage - 1) * authorsPerPage;
            string query;
            string countQuery;

            using (var connection = database.getConnection())
            {
                connection.Open();

                if (isSearchMode)
                {
                    query = $"SELECT * FROM search_authors(@search_term) LIMIT {authorsPerPage} OFFSET {offset}";
                    countQuery = "SELECT COUNT(*) FROM search_authors(@search_term)";

                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@search_term", currentSearchTerm);
                        using (var adapter = new NpgsqlDataAdapter(cmd))
                        {
                            adapter.Fill(authorsTable);
                        }
                    }

                    using (var countCmd = new NpgsqlCommand(countQuery, connection))
                    {
                        countCmd.Parameters.AddWithValue("@search_term", currentSearchTerm);
                        totalAuthors = Convert.ToInt32(countCmd.ExecuteScalar());
                    }
                }
                else
                {
                    query = $"SELECT * FROM authors_view LIMIT {authorsPerPage} OFFSET {offset}";
                    countQuery = "SELECT COUNT(*) FROM authors_view";

                    using (var adapter = new NpgsqlDataAdapter(query, connection))
                    {
                        adapter.Fill(authorsTable);
                    }

                    using (var countCmd = new NpgsqlCommand(countQuery, connection))
                    {
                        totalAuthors = Convert.ToInt32(countCmd.ExecuteScalar());
                    }
                }
            }

            UpdatePageInfo();
        }

        private void UpdatePageInfo()
        {
            int totalPages = (int)Math.Ceiling((double)totalAuthors / authorsPerPage);
            label1.Text = $"Page {currentPage} of {totalPages}";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateTable();
            LoadViewData();
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalAuthors / authorsPerPage);
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadViewData();
            }
        }

        private void previousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadViewData();
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }

       
        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                isSearchMode = true;
                currentSearchTerm = textBox1.Text;
            }
            else
            {
                isSearchMode = false;
                currentSearchTerm = "";
            }

            currentPage = 1; // Reset to first page when searching
            LoadViewData();
        }

        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            isSearchMode = false;
            currentSearchTerm = "";
            currentPage = 1;
            LoadViewData();
        }
    }
}