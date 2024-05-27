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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Questionnaire
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Question> questions;
        private int currentQuestionIndex = -1;
        private int currentTestId = -1;
        private static int userId = 3; // В реальном приложении используйте реальный идентификатор пользователя

        public MainWindow(int id)
        {
            InitializeComponent();
            userId = id;
            LoadTests();
        }

        private void LoadTests()
        {
            List<int> completedTests = GetCompletedTests(userId);

            // Загружаем доступные тесты из базы данных
            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                using (var cmd = new SQLiteCommand("SELECT Id, Title FROM Tests", con))
                using (SQLiteDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        int testId = rdr.GetInt32(0);
                        // Проверяем, не пройден ли уже этот тест
                        if (!completedTests.Contains(testId))
                        {
                            cmbTests.Items.Add(new { Id = testId, Title = rdr.GetString(1) });
                        }
                    }
                }
            }
        }

        private void cmbTests_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbTests.SelectedItem != null)
            {
                currentTestId = (int)(cmbTests.SelectedItem as dynamic).Id;
            }
        }

        private List<int> GetCompletedTests(int userId)
        {
            List<int> completedTests = new List<int>();
            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                string query = "SELECT DISTINCT TestId FROM UserAnswers WHERE UserId=@UserId";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            int testId;
                            if (int.TryParse(rdr["TestId"].ToString(), out testId))
                            {
                                completedTests.Add(testId);
                            }
                        }
                    }
                }
            }
            return completedTests;
        }

        private void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTestId == -1)
            {
                MessageBox.Show("Выберите тест.");
                return;
            }
            btnNext.Visibility = Visibility.Visible;
            LoadQuestions();
            ShowNextQuestion();
        }

        private void LoadQuestions()
        {
            questions = new List<Question>();
            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                string query = "SELECT Id, QuestionText FROM Questions WHERE TestId=@TestId";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TestId", currentTestId);
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            questions.Add(new Question
                            {
                                Id = rdr.GetInt32(0),
                                QuestionText = rdr.GetString(1)
                            });
                        }
                    }
                }
            }
        }

        private void ShowNextQuestion()
        {
            currentQuestionIndex++;
            if (currentQuestionIndex >= questions.Count)
            {
                MessageBox.Show("Тест завершен!");
                lblQuestion.Visibility = Visibility.Collapsed;
                lstAnswers.Visibility = Visibility.Collapsed;
                btnNext.Visibility = Visibility.Collapsed;
                ShowTestResult(currentTestId);
                LoadTests();
                return;
            }

            var question = questions[currentQuestionIndex];
            lblQuestion.Content = question.QuestionText;
            lblQuestion.Visibility = Visibility.Visible;

            lstAnswers.Items.Clear();
            lstAnswers.Visibility = Visibility.Visible;

            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                string query = "SELECT Id, AnswerText FROM Answers WHERE QuestionId=@QuestionId";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@QuestionId", question.Id);
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lstAnswers.Items.Add(new { Id = rdr["Id"], AnswerText = rdr["AnswerText"] });
                        }
                    }
                }
            }
        }

        private void NextQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            if (lstAnswers.SelectedItem != null)
            {
                var selectedAnswer = lstAnswers.SelectedItem as dynamic;
                int answerId = (int)selectedAnswer.Id;

                using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
                {
                    con.Open();
                    string query = "INSERT INTO UserAnswers (UserId, QuestionId, AnswerId, TestId) VALUES (@UserId, @QuestionId, @AnswerId, @TestId)";
                    using (var cmd = new SQLiteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@QuestionId", questions[currentQuestionIndex].Id);
                        cmd.Parameters.AddWithValue("@AnswerId", answerId);
                        cmd.Parameters.AddWithValue("@TestId", currentTestId); // Добавление TestId
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите ответ перед переходом к следующему вопросу.");
                return;
            }

            ShowNextQuestion();
        }


        private void ShowTestResult(int testId)
        {
            // Создание строкового сообщения для вывода результатов
            StringBuilder resultMessage = new StringBuilder();

            // Получение списка вопросов и ответов для заданного теста
            List<Question> questions = new List<Question>();
            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();

                // Получение списка вопросов для заданного теста
                string questionsQuery = "SELECT * FROM Questions WHERE TestId = @TestId";
                using (var cmd = new SQLiteCommand(questionsQuery, con))
                {
                    cmd.Parameters.AddWithValue("@TestId", testId);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            questions.Add(new Question
                            {
                                Id = rdr.GetInt32(0),
                                TestId = rdr.GetInt32(1),
                                QuestionText = rdr.GetString(2)
                            });
                        }
                    }
                }

                // Для каждого вопроса получаем ответ пользователя и правильный ответ
                foreach (var question in questions)
                {
                    // Получение ответа пользователя
                    string userAnswerQuery = "SELECT AnswerText FROM Answers WHERE Id = (SELECT AnswerId FROM UserAnswers WHERE TestId = @TestId AND QuestionId = @QuestionId)";
                    string userAnswerText;
                    using (var userAnswerCmd = new SQLiteCommand(userAnswerQuery, con))
                    {
                        userAnswerCmd.Parameters.AddWithValue("@TestId", testId);
                        userAnswerCmd.Parameters.AddWithValue("@QuestionId", question.Id);
                        userAnswerText = userAnswerCmd.ExecuteScalar()?.ToString();
                    }

                    // Получение правильного ответа
                    string correctAnswerQuery = "SELECT AnswerText FROM Answers WHERE QuestionId = @QuestionId AND IsCorrect = 1";
                    string correctAnswerText;
                    using (var correctAnswerCmd = new SQLiteCommand(correctAnswerQuery, con))
                    {
                        correctAnswerCmd.Parameters.AddWithValue("@QuestionId", question.Id);
                        correctAnswerText = correctAnswerCmd.ExecuteScalar()?.ToString();
                    }

                    // Добавление информации о вопросе, ответе пользователя и правильном ответе в строковое сообщение
                    resultMessage.AppendLine($"Вопрос: {question.QuestionText}");
                    resultMessage.AppendLine($"Ваш ответ: {userAnswerText}");
                    resultMessage.AppendLine($"Правильный ответ: {correctAnswerText}");
                    resultMessage.AppendLine(); // Добавляем пустую строку между вопросами
                }
            }

            // Отображение результатов прохождения теста
            MessageBox.Show(resultMessage.ToString(), "Результаты теста");
        }

    }
}
