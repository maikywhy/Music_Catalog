using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Курсова
{
    public partial class Form1 : Form
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;database=music_catalog;port=3306;username=root;password=1234");
        MySqlCommand command;
        MySqlDataReader dr;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void LoginButton_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Username_TextBox.Text) || string.IsNullOrEmpty(Password_TextBox.Text))
                {
                    MessageBox.Show("Please enter username and password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                connection.Open();
                string selectQuery = "SELECT * FROM users WHERE Username = @Username AND Password = @Password";
                command = new MySqlCommand(selectQuery, connection);
                command.Parameters.AddWithValue("@Username", Username_TextBox.Text);
                command.Parameters.AddWithValue("@Password", Password_TextBox.Text);
                dr = command.ExecuteReader();

                if (dr.Read())
                {
                    MessageBox.Show("Login Successful", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    int LoggedUserID = Convert.ToInt32(dr["UserID"]); // Отримати UserID
                    this.Hide();
                    MainForm mainform = new MainForm(LoggedUserID); // Передати UserID у форму MainForm
                    mainform.Show();
                }

                else
                {
                    MessageBox.Show("Invalid username or password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            RegisterForm registerForm = new RegisterForm();
            registerForm.Show();
        }
    }
}
