using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Questionnaire.Views.Windows
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationWindow.xaml
    /// </summary>
    public partial class AuthorizationWindow : Window
    {
        public AuthorizationWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txbLogin.Text != "" && txbPass.Text != "")
            {
                string login = txbLogin.Text;
                string password = txbPass.Text;

                // Подключение к базе данных
                using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
                {
                    con.Open();

                    // Проверка учетных данных пользователя
                    string query = "SELECT IsAdmin FROM Users WHERE Username = @Username AND Password = @Password";
                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", login);
                        cmd.Parameters.AddWithValue("@Password", password);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int isAdmin = Convert.ToInt32(result);
                            if (isAdmin == 1)
                            {
                                // Пользователь является администратором
                                MessageBox.Show("Вы вошли как администратор.");
                                var win = new AdministrationWindow();
                                win.Show();
                                Close();
                                // Открыть окно администратора или выполнить другие действия
                            }
                            else
                            {
                                var id = GetUserId(txbLogin.Text, txbPass.Text);
                                if (id != -1)
                                {
                                    MessageBox.Show($"Вы вошли как обычный пользователь {id}.");
                                    var win = new MainWindow(id);
                                    win.Show();
                                    Close();
                                }
                                
                                // Открыть главное окно или выполнить другие действия
                            }
                        }
                        else
                        {
                            // Неверные учетные данные
                            MessageBox.Show("Неверный логин или пароль.");
                        }
                    }
                }
            }
            else
                MessageBox.Show("Все поля должны быть заполнены");
        }

        private int GetUserId(string username, string password)
        {
            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();

                string query = "SELECT Id FROM Users WHERE Username = @Login AND Password = @Password";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Login", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }

            return -1; // Если пользователь не найден, возвращаем -1 или другое значение по умолчанию
        }

        private bool ValidateUser(string login, string password)
        {
            bool isValid = false;

            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                string query = "SELECT COUNT(1) FROM Admins WHERE Login=@Login AND Password=@Password";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Login", login);
                    cmd.Parameters.AddWithValue("@Password", password);

                    isValid = Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                }
            }

            return isValid;
        }
    }
}
