using Npgsql;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public partial class Form1 : Form
    {
        private readonly AuthorsManager _authorsManager;
        private readonly PublishersManager _publishersManager;
        private readonly EditionsManager _editionsManager;

        public Form1(DataBase database)
        {
            InitializeComponent();
            _authorsManager = new AuthorsManager(database, dataGridView1);
            _publishersManager = new PublishersManager(database, dataGridView2);
            _editionsManager = new EditionsManager(database, dataGridView3);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _authorsManager.LoadData();
            _publishersManager.LoadData();
            _editionsManager.LoadData();
            UpdatePageLabels();
        }

        private void UpdatePageLabels()
        {
            label1.Text = _authorsManager.GetPageInfo();
            label2.Text = _publishersManager.GetPageInfo();
            label3.Text = _editionsManager.GetPageInfo();
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _authorsManager.NextPage();
            label1.Text = _authorsManager.GetPageInfo();
        }

        private void previousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _authorsManager.PreviousPage();
            label1.Text = _authorsManager.GetPageInfo();
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            _authorsManager.SaveChanges();
        }


        private void btnSearch_Click(object sender, EventArgs e)
        {
            _authorsManager.Search(textBox1.Text);
            label1.Text = _authorsManager.GetPageInfo();
        }

        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            _authorsManager.Search("");
            label1.Text = _authorsManager.GetPageInfo();
        }

        private void nextToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            _publishersManager.NextPage();
            label2.Text = _publishersManager.GetPageInfo();
        }

        private void previousToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            _publishersManager.PreviousPage();
            label2.Text = _publishersManager.GetPageInfo();
        }

        private void btnSaveChanges1_Click(object sender, EventArgs e)
        {
            _publishersManager.SaveChanges();
        }

        private void btnSearch1_Click(object sender, EventArgs e)
        {
            _publishersManager.Search(textBox2.Text);
            label2.Text = _publishersManager.GetPageInfo();
        }

        private void btnClearSearch1_Click(object sender, EventArgs e)
        {
            textBox2.Text = "";
            _publishersManager.Search("");
            label2.Text = _publishersManager.GetPageInfo();
        }

        private void btnSaveChanges2_Click(object sender, EventArgs e)
        {
            _editionsManager.SaveChanges();
        }

        private void btnSearch2_Click(object sender, EventArgs e)
        {
            _editionsManager.Search(textBox3.Text);
            label3.Text = _editionsManager.GetPageInfo();
        }

        private void btnClearSearch2_Click(object sender, EventArgs e)
        {
            textBox3.Text = "";
            _editionsManager.Search("");
            label3.Text = _editionsManager.GetPageInfo();
        }
    }
}