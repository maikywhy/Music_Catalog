using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace Курсова
{
    public partial class RegisterForm : Form
    {
        public int UserID { get; private set; }
        MySqlConnection connection = new MySqlConnection("server=localhost;database=music_catalog;port=3306;username=root;password=1234");
        public RegisterForm()
        {
            InitializeComponent();
        }

        private void Register_Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Username_TextBox.Text) || string.IsNullOrEmpty(Password_TextBox.Text) || string.IsNullOrEmpty(Email_TextBox.Text))
                {
                    MessageBox.Show("Please Fill The All Information");
                    return;
                }

                connection.Open(); // Відкриваємо з'єднання один раз

                // Перевіряємо чи користувач вже існує
                MySqlCommand mySqlCommand1 = new MySqlCommand("SELECT * FROM users WHERE Username = @Username", connection);
                mySqlCommand1.Parameters.AddWithValue("@Username", Username_TextBox.Text);
                bool userExists = false;

                using (var dr1 = mySqlCommand1.ExecuteReader())
                {
                    if (userExists = dr1.HasRows)
                    {
                        MessageBox.Show("Username Already Exist");
                    }
                }

                if (!userExists)
                {
                    // Додаємо користувача
                    string iquery = "INSERT INTO users(`Username`, `Password`, `Email`) VALUES(@Username, @Password, @Email); SELECT LAST_INSERT_ID();";
                    MySqlCommand commandDatabase = new MySqlCommand(iquery, connection);
                    commandDatabase.Parameters.AddWithValue("@Username", Username_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@Password", Password_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@Email", Email_TextBox.Text);

                    int userID = Convert.ToInt32(commandDatabase.ExecuteScalar()); // Отримати UserID

                    MessageBox.Show("Account Created Successful", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.Hide();
                    MainForm mainform = new MainForm(userID); // Передати UserID у форму MainForm
                    mainform.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An Error: " + ex.Message, "Error");
            }
            finally
            {
                connection.Close(); // Закриваємо з'єднання у блоку finally
            }
        }

        private void Login_Button_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 loginForm = new Form1();
            loginForm.Show();
        }
    }
}
