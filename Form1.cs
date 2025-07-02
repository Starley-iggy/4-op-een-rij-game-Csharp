using System;
using System.Data;
using System.Data.SqlClient; // Nodig voor databaseverbinding
using System.Drawing;
using System.Windows.Forms;

namespace quatro


{
    public partial class Form1 : Form
    {
        //  Connection string naar jouw SQL Server instance + database
        // Belangrijk: zorg dat deze overeenkomt met jouw lokale setup
        private string connectionString = @"Server=starley_laptop\sqlexpress;Database=4inarowdb;Trusted_Connection=True;";

        //  Afmetingen van het speelbord: 6 rijen en 7 kolommen
        private int rows = 6;
        private int cols = 7;

        //  Bordstatus in een 2D-array: 0 = leeg, 1 = speler 1, 2 = speler 2
        private int[,] board;

        //  De huidige speler (start met speler 1)
        private int currentPlayer = 1;

        //  Visuele representatie van het bord (elke cel = PictureBox)
        private PictureBox[,] boxes;

        //  Harde speler-IDs, in echte app zou je deze ophalen uit de database
        private int speler1Id = 1;
        private int speler2Id = 2;

        public Form1()
        {

            naarForm2(); // Navigeer naar Form2 (als dat nodig is)
                         // Component-initialisatie (normaal staat hier ook InitializeComponent())

            //InitBoard(); // Start het spelbord
        }

        //  Dynamisch bord en visuele componenten tekenen
        void naarForm2() { 
        
            Form2 form2 = new Form2();
            var result = form2.ShowDialog();
            if (result == DialogResult.OK) {
                InitBoard();
                // Hier zou je eventueel iets kunnen doen met de gegevens van Form2
            }
            else {
                // Als de gebruiker Form2 sluit zonder OK, sluit dan ook Form1
                this.Close();
            }

        }
        
        private void InitBoard()
        {
            board = new int[rows, cols];
            boxes = new PictureBox[rows, cols];

            int boxSize = 50; // pixels per vakje
            this.ClientSize = new Size(cols * boxSize + 20, rows * boxSize + 70);

            // Maak elke cel van het speelbord als een klikbare PictureBox
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    PictureBox pb = new PictureBox
                    {
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.White,
                        Width = boxSize,
                        Height = boxSize,
                        Left = c * boxSize + 10,
                        Top = r * boxSize + 10,
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        Tag = c // sla kolomnummer op in Tag (wordt later gebruikt bij klik)
                    };
                    pb.Click += Box_Click;
                    this.Controls.Add(pb);
                    boxes[r, c] = pb;
                }
            }

            // Label onder het bord voor wie aan de beurt is
            Label infoLabel = new Label
            {
                Text = "Speler 1 (Rood) begint",
                Left = 10,
                Top = rows * boxSize + 15,
                Width = 200,
                Name = "infoLabel"
            };
            this.Controls.Add(infoLabel);
        }

        //  Wordt uitgevoerd als speler op een vakje klikt
        private void Box_Click(object sender, EventArgs e)
        {
            PictureBox clickedBox = (PictureBox)sender;
            int col = (int)clickedBox.Tag;

            // Zoek laagste lege rij in die kolom
            for (int r = rows - 1; r >= 0; r--)
            {
                if (board[r, col] == 0)
                {
                    board[r, col] = currentPlayer;
                    UpdateBoardVisual();

                    if (CheckWin(r, col))
                    {
                        MessageBox.Show($"Speler {currentPlayer} wint!");

                        // 📥 Sla overwinning op in database
                        SaveWinToDatabase(currentPlayer);

                        ResetGame(); // Nieuw spel
                    }
                    else if (CheckDraw())
                    {
                        MessageBox.Show("Gelijkspel!");
                        ResetGame();
                    }
                    else
                    {
                        // Wissel naar de andere speler
                        currentPlayer = (currentPlayer == 1) ? 2 : 1;
                        UpdateInfoLabel();
                    }
                    return;
                }
            }

            // Als geen lege rij is gevonden
            MessageBox.Show("Deze kolom is vol, kies een andere kolom.");
        }

        //  Werk de visuele kleuren van het bord bij
        private void UpdateBoardVisual()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    switch (board[r, c])
                    {
                        case 1:
                            boxes[r, c].BackColor = Color.Red;
                            break;
                        case 2:
                            boxes[r, c].BackColor = Color.Yellow;
                            break;
                        default:
                            boxes[r, c].BackColor = Color.White;
                            break;
                    }
                }
            }
        }

        //  Update het label dat laat zien wie aan de beurt is
        private void UpdateInfoLabel()
        {
            Label infoLabel = (Label)this.Controls["infoLabel"];
            infoLabel.Text = $"Beurt speler {currentPlayer} ({(currentPlayer == 1 ? "Rood" : "Geel")})";
        }

        //  Controleer of er een winnaar is ontstaan bij de laatste zet
        private bool CheckWin(int r, int c)
        {
            int player = board[r, c];

            return (CountInDirection(r, c, 0, 1, player) + CountInDirection(r, c, 0, -1, player) - 1 >= 4) || // horizontaal
                   (CountInDirection(r, c, 1, 0, player) + CountInDirection(r, c, -1, 0, player) - 1 >= 4) || // verticaal
                   (CountInDirection(r, c, 1, 1, player) + CountInDirection(r, c, -1, -1, player) - 1 >= 4) || // diagonaal \
                   (CountInDirection(r, c, 1, -1, player) + CountInDirection(r, c, -1, 1, player) - 1 >= 4);   // diagonaal /
        }

        //  Tel het aantal opeenvolgende stenen in een richting
        private int CountInDirection(int r, int c, int dr, int dc, int player)
        {
            int count = 0;
            int row = r;
            int col = c;

            while (row >= 0 && row < rows && col >= 0 && col < cols && board[row, col] == player)
            {
                count++;
                row += dr;
                col += dc;
            }
            return count;
        }

        //  Controleer of het bord volledig gevuld is (gelijkspel)
        private bool CheckDraw()
        {
            for (int c = 0; c < cols; c++)
            {
                if (board[0, c] == 0)
                    return false;
            }
            return true;
        }

        //  Start een nieuw spel
        private void ResetGame()
        {
            board = new int[rows, cols];
            currentPlayer = 1;
            UpdateBoardVisual();
            UpdateInfoLabel();
        }

        //  Sla de winnaar op in de database (alleen winnaarSpelerID)
        private void SaveWinToDatabase(int winnaar)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL-instructie: voeg een nieuwe rij toe aan Games-tabel
                    string sql = "INSERT INTO dbo.Games (WinnerID) VALUES (@winnerId)";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        // Bepaal welk ID bij de winnaar hoort
                        int winnaarSpelerId = (winnaar == 1) ? speler1Id : speler2Id;
                        cmd.Parameters.AddWithValue("@winnerId", winnaarSpelerId);

                        // ⛳ Voer SQL-opdracht uit
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                //  Toon foutmelding bij mislukte connectie/invoer
                MessageBox.Show("Fout bij opslaan win-data: " + ex.Message);
            }

        }
    }
}

