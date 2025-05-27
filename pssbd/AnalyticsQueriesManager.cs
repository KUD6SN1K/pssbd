using Npgsql;
using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp1;

namespace pssbd
{
    public class AnalyticsQueriesManager
    {
        private readonly DataBase _database;
        private readonly ComboBox _comboBox;
        private readonly TextBox _firstParamTextBox;
        private readonly TextBox _secondParamTextBox;
        private readonly DataGridView _dataGridView;

        public AnalyticsQueriesManager(DataBase database, ComboBox comboBox, TextBox firstParamTextBox, TextBox secondParamTextBox, DataGridView dataGridView)
        {
            _database = database;
            _comboBox = comboBox;
            _firstParamTextBox = firstParamTextBox;
            _secondParamTextBox = secondParamTextBox;
            _dataGridView = dataGridView;
            _comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            InitializeComboBox();
        }

        private void InitializeComboBox()
        {
            _comboBox.Items.AddRange(new string[]
            {
                "Книги издательств с указанным типом собственности",
                "Книги авторов из указанной страны",
                "Книги, написанные в указанный период",
                "Авторы, рождённые после указанного года",
                "Все жанры и книги",
                "Средний год написания книг по жанрам",
                "Топ-5 авторов по количеству изданий",
                "Все издательства и их книги (включая без книг)",
                "Все авторы и их книги (включая без книг)",
                "Средняя цена книг по издательствам",
                "Количество книг по жанрам",
                "Тираж книг по жанрам с долей от общего",
                "Книги дороже указанной цены",
                "Авторы с фамилией по маске",
                "Книги, изданные в указанный год",
                "Книги изданные на разных языках",
                "Книги с тиражом больше указанного",
                "Книги с условием по году публикации и тиражу",
                "Топ-5 издательств по количеству изданий",
                "Государственные и частные издательства в городе",
                "Книги, изданные издательствами указанного города",
                "Авторы, чьи книги не издавались в указанный период",
                "Активность издательств за период",
                "Количество книг по авторам и их доля от общего числа книг",
            });

            _comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            _firstParamTextBox.Visible = false;
            _secondParamTextBox.Visible = false;
        }

        private int _previousSelectedIndex = -1;

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_comboBox.SelectedIndex == _previousSelectedIndex)
                return;

            _previousSelectedIndex = _comboBox.SelectedIndex;

            _firstParamTextBox.Text = string.Empty;
            _secondParamTextBox.Text = string.Empty;
            _firstParamTextBox.Visible = false;
            _secondParamTextBox.Visible = false;

            switch (_comboBox.SelectedIndex)
            {
                case 0:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите тип собственности";
                    break;
                case 1:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите страну";
                    break;
                case 2:
                    _firstParamTextBox.Visible = true;
                    _secondParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Год от";
                    _secondParamTextBox.PlaceholderText = "Год до";
                    break;
                case 3:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Год рождения после";
                    break;
                case 12:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите цену";
                    break;
                case 13:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите маску фамилии (например, 'Иванов%' или '%ов')";
                    break;
                case 14:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите год издания";
                    break;
                case 15:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Минимум языков";
                    break;
                case 16: 
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите минимальный тираж";
                    break;
                case 17:
                    _firstParamTextBox.Visible = true;
                    _secondParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите год публикации (минимум)";
                    _secondParamTextBox.PlaceholderText = "Введите минимальный тираж";
                    break;
                case 19:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите название города";
                    break;
                case 20:
                    _firstParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Введите название города";
                    break;
                case 21:
                    _firstParamTextBox.Visible = true;
                    _secondParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Год начала";
                    _secondParamTextBox.PlaceholderText = "Год окончания";
                    break;
                case 22:
                    _firstParamTextBox.Visible = true;
                    _secondParamTextBox.Visible = true;
                    _firstParamTextBox.PlaceholderText = "Год начала";
                    _secondParamTextBox.PlaceholderText = "Год окончания";
                    break;
            }
        }

        public void ExecuteSelectedQuery()
        {
            if (_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите запрос из списка");
                return;
            }

            using (var connection = _database.getConnection())
            {
                connection.Open();

                try
                {
                    string functionCall = GetFunctionCall();
                    var command = new NpgsqlCommand($"SELECT * FROM {functionCall}", connection);
                    AddCommandParameters(command);

                    var adapter = new NpgsqlDataAdapter(command);
                    var table = new DataTable();
                    adapter.Fill(table);

                    _dataGridView.DataSource = table;
                    _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка выполнения запроса: {ex.Message}");
                }
            }
        }

        private string GetFunctionCall()
        {
            return _comboBox.SelectedIndex switch
            {
                0 => "get_books_by_publisher_type_name(@type)",                     // симметричное внутреннее соединение с условием по внешнему ключу  
                1 => "get_books_by_author_country_name(@country)",                  // симметричное внутреннее соединение с условием по внешнему ключу  
                2 => "get_books_by_year_of_writing_range(@year_from, @year_to)",    // симметричное внутреннее соединение с условием по датам  
                3 => "get_authors_and_books_after_birth_year(@birth_year)",         // симметричное внутреннее соединение с условием по датам
                4 => "get_all_genres_and_books()",                                  // симметричное внутреннее соединение без условия; 
                5 => "get_avg_year_of_writing_by_genre()",                          // симметричное внутреннее соединение без условия;
                6 => "get_top_5_authors_by_editions()",                             // симметричное внутреннее соединение без условия;
                7 => "get_all_publishers_and_their_books()",                        // левое внешнее соединение; 
                8 => "get_authors_and_their_books()",                               // правое внешнее соединение; 
                9 => "get_avg_price_by_publisher()",                                // запрос на запросе по принципу левого соединения; 
                10 => "get_book_count_by_genre()",                                  // итоговый запрос без условия; 
                11 => "get_printrun_by_genre_with_percentage()",                    // итоговый запрос без условия c итоговыми данными вида: «всего», «в том числе»;
                12 => "get_books_above_price(@price_limit)",                        // итоговые запросы с условием на данные по значению 
                13 => "get_authors_by_lastname_pattern(@pattern)",                  // итоговые запросы с условием на данные по маске 
                14 => "get_books_published_in_year(@pub_year)",                     // итоговые запросы с использованием индекса  
                15 => "get_books_with_min_languages(@min_languages)",               // итоговые запросы без использования индекса  
                16 => "get_books_with_total_printrun_above(@printrun_limit)",       // итоговый запрос с условием на группы; 
                17 => "get_books_with_conditions(@year_limit, @printrun_limit)",    // итоговый запрос с условием на данные и на группы;
                18 => "get_top5_publishers_by_editions()",                          // запрос на запросе по принципу итогового запроса;
                19 => "get_gov_and_private_pubs_in_city(@city_name)",               // запрос с использованием объединения
                20 => "get_books_published_in_city(@city_name)",                    // in 
                21 => "get_authors_without_publications(@start_year, @end_year)",   // not in
                22 => "get_publishers_by_activity_period(@start_year, @end_year)",  // case 
                23 => "get_authors_books_stats()",                                  // операциями над итоговыми данным
                _ => throw new InvalidOperationException("Неизвестный запрос")
            };
        }

        private void AddCommandParameters(NpgsqlCommand command)
        {
            switch (_comboBox.SelectedIndex)
            {
                case 0:
                    command.Parameters.AddWithValue("@type", _firstParamTextBox.Text);
                    break;
                case 1:
                    command.Parameters.AddWithValue("@country", _firstParamTextBox.Text);
                    break;
                case 2:
                    command.Parameters.AddWithValue("@year_from", int.Parse(_firstParamTextBox.Text));
                    command.Parameters.AddWithValue("@year_to", int.Parse(_secondParamTextBox.Text));
                    break;
                case 3:
                    int year = int.Parse(_firstParamTextBox.Text);
                    var date = new DateTime(year, 1, 1);
                    command.Parameters.AddWithValue("@birth_year", date);
                    break;
                case 12:
                    command.Parameters.AddWithValue("@price_limit", decimal.Parse(_firstParamTextBox.Text));
                    break;
                case 13:
                    command.Parameters.AddWithValue("@pattern", _firstParamTextBox.Text);
                    break;
                case 14:
                    command.Parameters.AddWithValue("@pub_year", int.Parse(_firstParamTextBox.Text));
                    break;
                case 15:
                    command.Parameters.AddWithValue("@min_languages", int.Parse(_firstParamTextBox.Text));
                    break;
                case 16:
                    command.Parameters.AddWithValue("@printrun_limit", int.Parse(_firstParamTextBox.Text));
                    break;
                case 17:
                    command.Parameters.AddWithValue("@year_limit", int.Parse(_firstParamTextBox.Text));
                    command.Parameters.AddWithValue("@printrun_limit", int.Parse(_secondParamTextBox.Text));
                    break;
                case 19:
                    command.Parameters.AddWithValue("@city_name", _firstParamTextBox.Text);
                    break;
                case 20:
                    command.Parameters.AddWithValue("@city_name", _firstParamTextBox.Text);
                    break;
                case 21:
                    command.Parameters.AddWithValue("@start_year", int.Parse(_firstParamTextBox.Text));
                    command.Parameters.AddWithValue("@end_year", int.Parse(_secondParamTextBox.Text));
                    break;
                case 22:
                    command.Parameters.AddWithValue("@start_year", int.Parse(_firstParamTextBox.Text));
                    command.Parameters.AddWithValue("@end_year", int.Parse(_secondParamTextBox.Text));
                    break;

            }
        }
    }
}
