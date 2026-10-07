using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace Курсова
{
    public partial class MainForm : Form
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;database=music_catalog;port=3306;username=root;password=1234");
        private string selectedFileName;
        private WaveOutEvent waveOut;
        private int LoggedUserID;

        public MainForm(int userId)
        {
            InitializeComponent();
            LoggedUserID = userId; 
        }


        private void Search_Button_Click(object sender, EventArgs e)
        {
            string searchQuery = Search_TextBox.Text.Trim(); 
            string query;

            if (string.IsNullOrEmpty(searchQuery))
            {
                query = "SELECT song_name, genre ,tags, song_file FROM Songs";
            }
            else
            {
                query = "SELECT song_name, genre ,tags, song_file FROM Songs WHERE song_name LIKE @SearchQuery OR tags LIKE @SearchQuery OR Genre LIKE @SearchQuery";
            }

            try
            {
                connection.Open();
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");

                MySqlDataReader reader = command.ExecuteReader();

                DataTable dataTable = new DataTable();
                dataTable.Load(reader);
                dataGridView1.DataSource = dataTable;

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(song_name_TextBox.Text) || string.IsNullOrEmpty(genre_TextBox.Text) || string.IsNullOrEmpty(Tags_TextBox.Text))
                {
                    MessageBox.Show("Please Fill The All Information");
                    return;
                }

                connection.Open();
                MySqlCommand mySqlCommand1 = new MySqlCommand("SELECT * FROM songs WHERE song_name = @song_name", connection);
                mySqlCommand1.Parameters.AddWithValue("@song_name", song_name_TextBox.Text);
                bool songExists = false;

                using (var dr1 = mySqlCommand1.ExecuteReader())
                    if (songExists = dr1.HasRows) MessageBox.Show("Song Already Exists");

                if (!songExists)
                {
                    // Додаємо нову пісню
                    string iquery = "INSERT INTO songs(`song_name`, `genre`, `tags`, `song_file`) VALUES(@song_name, @genre, @tags, @song_file)";
                    MySqlCommand commandDatabase = new MySqlCommand(iquery, connection);
                    commandDatabase.Parameters.AddWithValue("@song_name", song_name_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@genre", genre_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@tags", Tags_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@song_file", selectedFileName);

                    commandDatabase.CommandTimeout = 60;
                    commandDatabase.ExecuteNonQuery();

                    long newSongId = commandDatabase.LastInsertedId;

                    MessageBox.Show($"Song Added Successfully. Song ID: {newSongId}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    string authorshipQuery = "INSERT INTO authorship(id_song, id_user) VALUES(@id_song, @id_user)";
                    MySqlCommand authorshipCommand = new MySqlCommand(authorshipQuery, connection);
                    authorshipCommand.Parameters.AddWithValue("@id_song", newSongId);
                    authorshipCommand.Parameters.AddWithValue("@id_user", LoggedUserID); 

                    authorshipCommand.ExecuteNonQuery();

                    this.Hide();
                    MainForm mainform = new MainForm(LoggedUserID);
                    mainform.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An Error" + ex.Message, "Error");
            }
            finally
            {
                connection.Close();
            }
        }

        private void ChooseFile_Button_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav|All Files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFileName = openFileDialog.FileName;
                    File_TextBox.Text = selectedFileName;
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dataGridView1.Columns["song_name"].Index)
            {
                try
                {
                    string songName = dataGridView1.Rows[e.RowIndex].Cells["song_name"].Value.ToString();

                    connection.Open();
                    string queryId = "SELECT id_song FROM Songs WHERE song_name = @song_name";
                    MySqlCommand commandId = new MySqlCommand(queryId, connection);
                    commandId.Parameters.AddWithValue("@song_name", songName);
                    int songId = Convert.ToInt32(commandId.ExecuteScalar());

                    string queryAddition = "SELECT song_text, notes, logo FROM Addition WHERE id_song = @id_song";
                    MySqlCommand commandAddition = new MySqlCommand(queryAddition, connection);
                    commandAddition.Parameters.AddWithValue("@id_song", songId);
                    MySqlDataAdapter adapterAddition = new MySqlDataAdapter(commandAddition);
                    DataTable dataTableAddition = new DataTable();
                    adapterAddition.Fill(dataTableAddition);
                    dataGridView2.DataSource = dataTableAddition;

                    string queryRating = "SELECT rating, timestamp FROM Ratings WHERE id_song = @id_song";
                    MySqlCommand commandRating = new MySqlCommand(queryRating, connection);
                    commandRating.Parameters.AddWithValue("@id_song", songId);
                    MySqlDataAdapter adapterRating = new MySqlDataAdapter(commandRating);
                    DataTable dataTableRating = new DataTable();
                    adapterRating.Fill(dataTableRating);
                    listBox1.Items.Clear();
                    foreach (DataRow row in dataTableRating.Rows)
                    {
                        string rating = row["rating"].ToString();
                        string timestamp = row["timestamp"].ToString();
                        listBox1.Items.Add($"Rating: {rating}, Timestamp: {timestamp}");
                    }

                    string queryComments = "SELECT c.com_text, u.username FROM Comments c " +
                                           "INNER JOIN has_comment hc ON c.id_com = hc.id_com " +
                                           "INNER JOIN Users u ON hc.UserID = u.UserID " +
                                           "WHERE hc.id_song = @id_song";
                    MySqlCommand commandComments = new MySqlCommand(queryComments, connection);
                    commandComments.Parameters.AddWithValue("@id_song", songId);
                    MySqlDataReader reader = commandComments.ExecuteReader();
                    listBoxComments.Items.Clear();
                    while (reader.Read())
                    {
                        string username = reader["username"].ToString();
                        string comment = reader["com_text"].ToString();
                        string commentWithUser = $"{username}: {comment}";
                        listBoxComments.Items.Add(commentWithUser);
                    }
                    reader.Close();

                    connection.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An Error occurred: " + ex.Message, "Error");
                    connection.Close();
                }
            }
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dataGridView2 = (DataGridView)sender;
            int rowClicked = dataGridView2.CurrentRow.Index;
            string logo = dataGridView2.Rows[rowClicked].Cells[2].Value.ToString();
            pictureBox1.Load(logo);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files (*.jpg;*.jpeg;*.gif;*.png)|*.jpg;*.jpeg;*.gif;*.png|All Files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFileName = openFileDialog.FileName;
                    Logo_TextBox.Text = selectedFileName;

                }
            }
        }

        private void Add_Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Song_TextBox.Text) || string.IsNullOrEmpty(Notes_TextBox.Text) || string.IsNullOrEmpty(ID_TextBox.Text) || string.IsNullOrEmpty(Logo_TextBox.Text))
                {
                    MessageBox.Show("Please Fill The All Information");
                    return;
                }

                connection.Open();
                MySqlCommand mySqlCommand1 = new MySqlCommand("SELECT * FROM addition WHERE id_song = @id_song", connection);
                mySqlCommand1.Parameters.AddWithValue("@id_song", ID_TextBox.Text);
                bool additionExists = false;

                using (var dr1 = mySqlCommand1.ExecuteReader())
                    if (additionExists = dr1.HasRows) MessageBox.Show("Song Allready Exist");

                if (!additionExists)
                {
                    string iquery = "INSERT INTO music_catalog.addition(`id_song`, `song_text`, `notes`, `logo`) VALUES(@id_song, @song_text, @notes, @logo)";
                    MySqlCommand commandDatabase = new MySqlCommand(iquery, connection);
                    commandDatabase.Parameters.AddWithValue("@id_song", ID_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@song_text", Song_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@notes", Notes_TextBox.Text);
                    commandDatabase.Parameters.AddWithValue("@logo", selectedFileName);

                    commandDatabase.CommandTimeout = 60;
                    commandDatabase.ExecuteNonQuery();
                    MessageBox.Show("Info Added Successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Hide();
                    MainForm mainform = new MainForm(LoggedUserID);
                    mainform.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An Error" + ex.Message, "Error");
            }
            finally
            {
                connection.Close();
            }
        }

        private void PlaySong(string filePath)
        {
            try
            {
                StopSong();

                waveOut = new WaveOutEvent();

                var audioFile = new AudioFileReader(filePath);

                waveOut.Init(audioFile);

                waveOut.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        private void StopSong()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;
        }

        private void PlaySongButton_Click(object sender, EventArgs e)
        {
            try
            {
                string songFilePath = GetSelectedSongFilePath();
                PlaySong(songFilePath);

                int songId = GetSelectedSongId();
                if (songId == -1)
                {
                    MessageBox.Show("Failed to get the song ID.");
                    return;
                }

                if (IsUserAuthor(songId, LoggedUserID))
                {
                    return;
                }

                if (IsSongListenedByUser(songId, LoggedUserID))
                {
                    return;
                }

                connection.Open();
                string iquery = "INSERT INTO music_catalog.listeners(id_song, id_user) VALUES(@id_song, @id_user)";
                MySqlCommand commandDatabase = new MySqlCommand(iquery, connection);
                commandDatabase.Parameters.AddWithValue("@id_song", songId);
                commandDatabase.Parameters.AddWithValue("@id_user", LoggedUserID);
                commandDatabase.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private bool IsSongListenedByUser(int songId, int userId)
        {
            try
            {
                connection.Open();
                string queryCheck = "SELECT COUNT(*) FROM listeners WHERE id_song = @id_song AND id_user = @id_user";
                MySqlCommand commandCheck = new MySqlCommand(queryCheck, connection);
                commandCheck.Parameters.AddWithValue("@id_song", songId);
                commandCheck.Parameters.AddWithValue("@id_user", userId);
                int count = Convert.ToInt32(commandCheck.ExecuteScalar());
                return count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while checking listener record: " + ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        private bool IsUserAuthor(int songId, int userId)
        {
            try
            {
                connection.Open();
                string queryCheck = "SELECT COUNT(*) FROM authorship WHERE id_song = @id_song AND id_user = @id_user";
                MySqlCommand commandCheck = new MySqlCommand(queryCheck, connection);
                commandCheck.Parameters.AddWithValue("@id_song", songId);
                commandCheck.Parameters.AddWithValue("@id_user", userId);
                int count = Convert.ToInt32(commandCheck.ExecuteScalar());
                return count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while checking listener record: " + ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        private void StopSongButton_Click(object sender, EventArgs e)
        {
            StopSong();
        }
        private void PauseSong()
        {
            waveOut?.Pause();
        }

        private void ResumeSong()
        {
            waveOut?.Play();
        }

        private string GetSelectedSongFilePath()
        {
            int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
            DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
            return selectedRow.Cells["song_file"].Value.ToString();
        }

        private void PauseResumeButton_Click(object sender, EventArgs e)
        {
            if (waveOut != null && waveOut.PlaybackState == PlaybackState.Playing)
            {
                PauseSong();
            }
            else if (waveOut != null && waveOut.PlaybackState == PlaybackState.Paused)
            {
                ResumeSong();
            }
        }

        private void AddCommentButton_Click(object sender, EventArgs e)
        {
            try
            {
                string commentText = CommentTextBox.Text;

                int songId = GetSelectedSongId();

                if (songId != -1)
                {
                    int parentId = -1; 
                    object parentIdValue = (parentId == -1) ? DBNull.Value : (object)parentId;

                    connection.Open();
                    string query = "INSERT INTO Comments (com_text, parent_id) VALUES (@com_text, @parent_id)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@com_text", commentText);
                    command.Parameters.AddWithValue("@parent_id", parentIdValue);
                    command.ExecuteNonQuery();
                    MessageBox.Show("Comment added successfully.");

                    string query1 = "INSERT INTO has_comment (id_song, UserID) VALUES (@id_song, @UserID)";
                    MySqlCommand command1 = new MySqlCommand(query1, connection);
                    command1.Parameters.AddWithValue("@id_song", songId);
                    command1.Parameters.AddWithValue("@UserID", LoggedUserID);
                    command1.ExecuteNonQuery();
                    this.Hide();
                    MainForm mainform = new MainForm(LoggedUserID);
                    mainform.Show();
                }
                else
                {
                    MessageBox.Show("Unable to add comment. Selected song ID is invalid.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        private int GetSelectedSongId()
        {
            int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
            DataGridViewRow selectedRow = dataGridView1.Rows[rowIndex];
            string songName = selectedRow.Cells["song_name"].Value.ToString();

            try
            {
                connection.Open();
                string queryId = "SELECT id_song FROM Songs WHERE song_name = @song_name";
                MySqlCommand commandId = new MySqlCommand(queryId, connection);
                commandId.Parameters.AddWithValue("@song_name", songName);
                int songId = Convert.ToInt32(commandId.ExecuteScalar());
                return songId;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching song ID: " + ex.Message);
                return -1;
            }
            finally
            {
                connection.Close();
            }
        }

        private void RateSongButton_Click(object sender, EventArgs e)
        {
            try
            {
                int rating = (int)RatingNumericUpDown.Value;
                int songId = GetSelectedSongId();

                if (songId != -1)
                {
                    if (rating < 1 || rating > 5)
                    {
                        MessageBox.Show("Please enter a rating between 1 and 5.");
                        return;
                    }

                    connection.Open();
                    MySqlCommand checkRating = new MySqlCommand("SELECT * FROM Ratings WHERE id_song = @id_song AND UserID = @UserID", connection);
                    checkRating.Parameters.AddWithValue("@id_song", songId);
                    checkRating.Parameters.AddWithValue("@UserID", LoggedUserID);
                    using (var reader = checkRating.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            MessageBox.Show("You have already rated this song.");
                            return;
                        }
                    }

                    MySqlCommand checkAuthor = new MySqlCommand("SELECT * FROM authorship WHERE id_song = @id_song AND id_user = @id_user", connection);
                    checkAuthor.Parameters.AddWithValue("@id_song", songId);
                    checkAuthor.Parameters.AddWithValue("@id_user", LoggedUserID);
                    using (var reader = checkAuthor.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            MessageBox.Show("You cannot rate your own song.");
                            return;
                        }
                    }

                    string query = "INSERT INTO Ratings (rating, id_song, UserID) VALUES (@rating, @id_song, @UserID)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@rating", rating);
                    command.Parameters.AddWithValue("@id_song", songId);
                    command.Parameters.AddWithValue("@UserID", LoggedUserID);
                    command.ExecuteNonQuery();

                    long idRating = command.LastInsertedId;

                    string queryHasRating = "INSERT INTO has_rating (id_rating, id_song, UserID) VALUES (@id_rating, @id_song, @UserID)";
                    MySqlCommand commandHasRating = new MySqlCommand(queryHasRating, connection);
                    commandHasRating.Parameters.AddWithValue("@id_rating", idRating);
                    commandHasRating.Parameters.AddWithValue("@id_song", songId);
                    commandHasRating.Parameters.AddWithValue("@UserID", LoggedUserID);
                    commandHasRating.ExecuteNonQuery();

                    MessageBox.Show("Rating added successfully.");

                    this.Hide();
                    MainForm mainform = new MainForm(LoggedUserID);
                    mainform.Show();
                }
                else
                {
                    MessageBox.Show("Unable to add rating. Selected song ID is invalid.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

        private void LogOut_Button_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 loginForm = new Form1();
            loginForm.Show();
        }
    }
}
