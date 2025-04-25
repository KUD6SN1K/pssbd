using Npgsql;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    enum RowState
    {
        Existed,
        New,
        Modified,
        ModifiedNew,
        Deleted
    }

    public partial class Form1 : Form
    {

        DataBase database = new DataBase();
        int selectedRow;

        public Form1()
        {
            InitializeComponent();
        }

        private void CreateColumns()
        {
            dataGridView1.Columns.Add("author_id", "id");
            dataGridView1.Columns.Add("first_name", "Имя");
            dataGridView1.Columns.Add("last_name", "Фамилия");
            dataGridView1.Columns.Add("o_name", "Отчество");
            dataGridView1.Columns.Add("country_name", "Страна");
            dataGridView1.Columns.Add("IsNew", String.Empty);
            dataGridView1.Columns["author_id"].Visible = false;
            dataGridView1.Columns["IsNew"].Visible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void ReadSingleRow(DataGridView dgw, IDataRecord record)
        {

            dgw.Rows.Add(
                record.GetInt32(0),     // authord_id
                record.GetString(1),    // first_name
                record.GetString(2),    // last_name
                record.GetString(3),    // o_name
                record.GetString(4),    //county_id
                RowState.ModifiedNew);
        }

        private void RefreshDataGrid(DataGridView dgw)
        {
            dgw.Rows.Clear();
        }

        private int currentPage = 1;
        private int authorsPerPage = 20;

        private int totalAuthors;

        private void LoadViewData(DataGridView dgw)
        {
            dgw.Rows.Clear();

            int offset = (currentPage - 1) * authorsPerPage;
            string query = $"SELECT * FROM authors_view LIMIT {authorsPerPage} OFFSET {offset}";

            using (var connection = database.getConnection())
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string oName = reader.IsDBNull(3) ? "" : reader.GetString(3); // Handle NULL for o_name (patronymic)
                        dgw.Rows.Add(
                            reader.GetInt32(0),    // author_id
                            reader.GetString(1),   // first_name
                            reader.GetString(2),   // last_name
                            oName,                 // o_name (patronymic)
                            reader.GetString(4),   // country_name
                            RowState.Existed);
                    }
                }

                // Get total number of authors for pagination purposes
                using (var command = new NpgsqlCommand("SELECT COUNT(*) FROM authors_view", connection))
                {
                    totalAuthors = Convert.ToInt32(command.ExecuteScalar());
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

            CreateColumns();
            LoadViewData(dataGridView1);
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalAuthors / authorsPerPage);
            if (currentPage < totalPages)
            {
                currentPage++;
                LoadViewData(dataGridView1);
            }
        }

        private void previousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                LoadViewData(dataGridView1);
            }
        }
    }
}
