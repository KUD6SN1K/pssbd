using System;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public partial class log_in_form : Form
    {
        public DataBase Database { get; private set; }

        public log_in_form()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text;
            string password = textBox2.Text;

            string host = "localhost";
            string databaseName = "pssbd";

            // Инициализируем DataBase с введёнными данными
            Database = new DataBase(host, username, password, databaseName);

            if (TestConnection())
            {
                // Если подключение успешно — ставим DialogResult и закрываем форму
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Login failed. Invalid username or password.");
            }
        }

        private bool TestConnection()
        {
            try
            {
                using (var connection = Database.getConnection())
                {
                    connection.Open(); // просто открываем соединение
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
                return false;
            }
        }
    }
}
