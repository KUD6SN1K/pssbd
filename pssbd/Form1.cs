using Npgsql;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public partial class Form1 : Form
    {
        private AuthorsManager _authorsManager;
        private PublishersManager _publishersManager;
        private EditionsManager _editionsManager;
        private BooksManager _booksManager;
        private PublicationTypesManager _publicationtypesManager;
        private OwnershipTypesManager _ownershiptypesManager;
        private LanguagesManager _languagesManager;
        private GenresManager _genresManager;
        private CountriesManager _countriesManager;
        private CitiesManager _citiesManager;
        private BindingTypesManager _bindingtypesManager;
        private readonly DataBase _database;
        private bool _isAdmin;

        public Form1(DataBase database)
        {
            InitializeComponent();
            _database = database;

            // Основные менеджеры всегда создаются
            _authorsManager = new AuthorsManager(database, dataGridView1);
            _publishersManager = new PublishersManager(database, dataGridView2);
            _editionsManager = new EditionsManager(database, dataGridView3);
            _booksManager = new BooksManager(database, dataGridView4);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckUserRole();

            if (_isAdmin)
            {
                // Только если админ — создаём справочники и загружаем
                _publicationtypesManager = new PublicationTypesManager(_database, dataGridView5);
                _ownershiptypesManager = new OwnershipTypesManager(_database, dataGridView6);
                _languagesManager = new LanguagesManager(_database, dataGridView7);
                _genresManager = new GenresManager(_database, dataGridView8);
                _countriesManager = new CountriesManager(_database, dataGridView9);
                _citiesManager = new CitiesManager(_database, dataGridView10);
                _bindingtypesManager = new BindingTypesManager(_database, dataGridView11);

                _publicationtypesManager.LoadData();
                _ownershiptypesManager.LoadData();
                _languagesManager.LoadData();
                _genresManager.LoadData();
                _countriesManager.LoadData();
                _citiesManager.LoadData();
                _bindingtypesManager.LoadData();
            }
            else
            {
                // Удаляем вкладку со справочниками
                tabControl1.TabPages.Remove(tabPage5);
            }

            // Загружаем данные основных таблиц
            _authorsManager.LoadData();
            _publishersManager.LoadData();
            _editionsManager.LoadData();
            _booksManager.LoadData();

            UpdatePageLabels();
        }

        private void CheckUserRole()
        {
            var roles = _database.GetUserRoles();
            _isAdmin = roles.Contains("admin");
        }


        private void UpdatePageLabels()
        {
            label1.Text = _authorsManager.GetPageInfo();
            label2.Text = _publishersManager.GetPageInfo();
            label3.Text = _editionsManager.GetPageInfo();
            label4.Text = _booksManager.GetPageInfo();
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

        private void nextToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            _editionsManager.NextPage();
            label3.Text = _editionsManager.GetPageInfo();
        }

        private void previousToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            _editionsManager.PreviousPage();
            label3.Text = _editionsManager.GetPageInfo();
        }

        private void btnSaveChanges3_Click(object sender, EventArgs e)
        {
            _booksManager.SaveChanges();
        }

        private void btnSearch3_Click(object sender, EventArgs e)
        {
            _booksManager.Search(textBox4.Text);
            label4.Text = _booksManager.GetPageInfo();
        }

        private void btnClearSearch3_Click(object sender, EventArgs e)
        {
            textBox4.Text = "";
            _booksManager.Search("");
            label4.Text = _booksManager.GetPageInfo();
        }

        private void previousToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            _booksManager.PreviousPage();
            label4.Text = _booksManager.GetPageInfo();
        }

        private void nextToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            _booksManager.NextPage();
            label4.Text = _booksManager.GetPageInfo();
        }

        private void btnSaveChanges4_Click(object sender, EventArgs e)
        {
            _publicationtypesManager.SaveChanges();
        }

        private void btnSaveChanges5_Click(object sender, EventArgs e)
        {
            _ownershiptypesManager.SaveChanges();
        }

        private void btnSaveChanges6_Click(object sender, EventArgs e)
        {
            _languagesManager.SaveChanges();
        }

        private void btnSearch4_Click(object sender, EventArgs e)
        {
            _languagesManager.Search(textBox5.Text);          
        }

        private void btnClearSearch4_Click(object sender, EventArgs e)
        {
            textBox5.Text = "";
            _languagesManager.Search("");
        }

        private void btnSaveChanges7_Click(object sender, EventArgs e)
        {
            _genresManager.SaveChanges();
        }

        private void btnSaveChanges8_Click(object sender, EventArgs e)
        {
            _countriesManager.SaveChanges();
        }

        private void btnSearch5_Click(object sender, EventArgs e)
        {
            _countriesManager.Search(textBox5.Text);
        }

        private void btnClearSearch5_Click(object sender, EventArgs e)
        {
            textBox6.Text = "";
            _countriesManager.Search("");
        }

        private void btnSaveChanges10_Click(object sender, EventArgs e)
        {
            _citiesManager.SaveChanges();
        }

        private void btnSearch6_Click(object sender, EventArgs e)
        {
            _citiesManager.Search(textBox5.Text);
        }

        private void btnClearSearch6_Click(object sender, EventArgs e)
        {
            textBox7.Text = "";
            _citiesManager.Search("");
        }

        private void btnSaveChanges11_Click(object sender, EventArgs e)
        {
            _bindingtypesManager.SaveChanges();
        }
    }
}