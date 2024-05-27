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
using static System.Net.Mime.MediaTypeNames;

namespace Questionnaire.Views.Windows
{
    /// <summary>
    /// Логика взаимодействия для AdministrationWindow.xaml
    /// </summary>
    public partial class AdministrationWindow : Window
    {
        private int currentTestId = -1;
        public AdministrationWindow()
        {
            InitializeComponent();
            LoadTests();
        }

        private void AddTestButton_Click(object sender, RoutedEventArgs e)
        {
            string testTitle = txbTestTitle.Text;

            if (string.IsNullOrEmpty(testTitle))
            {
                MessageBox.Show("Введите название теста.");
                return;
            }

            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = "INSERT INTO Tests (Title) VALUES (@Title)";
                    cmd.Parameters.AddWithValue("@Title", testTitle);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid()";
                    currentTestId = (int)(long)cmd.ExecuteScalar();
                }
            }

            MessageBox.Show("Тест добавлен. Теперь вы можете добавлять вопросы.");
            LoadTests();
        }

        private void AddQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentTestId == -1)
            {
                MessageBox.Show("Сначала добавьте тест.");
                return;
            }

            string questionText = txbQuestion.Text;
            string[] answers = txbAnswers.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            if (string.IsNullOrEmpty(questionText) || answers.Length == 0)
            {
                MessageBox.Show("Введите вопрос и ответы.");
                return;
            }

            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                using (var cmd = new SQLiteCommand(con))
                {
                    cmd.CommandText = "INSERT INTO Questions (TestId, QuestionText) VALUES (@TestId, @QuestionText)";
                    cmd.Parameters.AddWithValue("@TestId", currentTestId);
                    cmd.Parameters.AddWithValue("@QuestionText", questionText);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT last_insert_rowid()";
                    int questionId = (int)(long)cmd.ExecuteScalar();

                    foreach (var answer in answers)
                    {
                        bool isCorrect = answer.StartsWith("*");
                        string answerText = isCorrect ? answer.Substring(1).Trim() : answer.Trim();

                        cmd.CommandText = "INSERT INTO Answers (QuestionId, AnswerText, IsCorrect) VALUES (@QuestionId, @AnswerText, @IsCorrect)";
                        cmd.Parameters.AddWithValue("@QuestionId", questionId);
                        cmd.Parameters.AddWithValue("@AnswerText", answerText);
                        cmd.Parameters.AddWithValue("@IsCorrect", isCorrect);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            MessageBox.Show("Вопрос и ответы добавлены.");
            txbQuestion.Clear();
            txbAnswers.Clear();
        }

        private void LoadTests()
        {
            lstTests.Items.Clear();
            using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
            {
                con.Open();
                string query = "SELECT * FROM Tests";
                using (var cmd = new SQLiteCommand(query, con))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            lstTests.Items.Add(new Test
                            {
                                Id = rdr.GetInt32(0),
                                Title = rdr.GetString(1)
                            });
                        }
                    }
                }
            }
        }

        private void DeleteTestButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTest = lstTests.SelectedItem as Test;
            if (selectedTest != null)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить тест '{selectedTest.Title}'?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var con = new SQLiteConnection("Data Source=QuestionnaireDb.db"))
                    {
                        con.Open();

                        // Удаление вопросов и ответов, связанных с тестом
                        string deleteAnswersQuery = "DELETE FROM Answers WHERE QuestionId IN (SELECT Id FROM Questions WHERE TestId = @TestId)";
                        using (var cmd = new SQLiteCommand(deleteAnswersQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@TestId", selectedTest.Id);
                            cmd.ExecuteNonQuery();
                        }

                        string deleteQuestionsQuery = "DELETE FROM Questions WHERE TestId = @TestId";
                        using (var cmd = new SQLiteCommand(deleteQuestionsQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@TestId", selectedTest.Id);
                            cmd.ExecuteNonQuery();
                        }

                        // Удаление самого теста
                        string deleteTestQuery = "DELETE FROM Tests WHERE Id = @TestId";
                        using (var cmd = new SQLiteCommand(deleteTestQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@TestId", selectedTest.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Обновление интерфейса после удаления
                    lstTests.Items.Remove(selectedTest);
                    MessageBox.Show("Тест успешно удален.", "Удаление завершено");
                    LoadTests();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите тест для удаления.", "Ошибка удаления");
            }
        }
    }
}

