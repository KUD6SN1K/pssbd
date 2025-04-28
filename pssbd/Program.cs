namespace pssbd
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var loginForm = new log_in_form();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new Form1(loginForm.Database));
            }
            else
            {
                Application.Exit();
            }
        }
    }
}
