using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LudoGame
{
    public partial class MainWindow : Window
    {
        private Random _random = new Random();
        private int _diceValue;
        private int _currentPlayer = 1; // 1: Red, 2: Yellow, 3: Blue, 4: Green

        private class Player
        {
            public Point[] Path { get; set; }    // Path of the player pieces
            public Ellipse[] Pieces { get; set; } // Ellipse references for the player's pieces
            public int[] PiecePositions { get; set; } // Piece positions on the path
            public int GoalCount { get; set; } = 0;  // Count of pieces that have reached the goal

            public Player(int pieceCount)
            {
                Pieces = new Ellipse[pieceCount];
                PiecePositions = new int[pieceCount];
                for (int i = 0; i < pieceCount; i++)
                {
                    PiecePositions[i] = -1; // Initially, all pieces are off the board
                }
            }
        }

        private Player redPlayer, yellowPlayer, bluePlayer, greenPlayer;

        public MainWindow()
        {
            InitializePlayers();
            UpdateCurrentPlayerText();
        }

        // Initialize players and their paths
        private void InitializePlayers()
        {
            redPlayer = new Player(4);
            yellowPlayer = new Player(4);
            bluePlayer = new Player(4);
            greenPlayer = new Player(4);

            // Link UI pieces to player data
            redPlayer.Pieces[0] = RedPiece1;
            redPlayer.Pieces[1] = RedPiece2;
            redPlayer.Pieces[2] = RedPiece3;
            redPlayer.Pieces[3] = RedPiece4;

            yellowPlayer.Pieces[0] = YellowPiece1;
            yellowPlayer.Pieces[1] = YellowPiece2;
            yellowPlayer.Pieces[2] = YellowPiece3;
            yellowPlayer.Pieces[3] = YellowPiece4;

            bluePlayer.Pieces[0] = BluePiece1;
            bluePlayer.Pieces[1] = BluePiece2;
            bluePlayer.Pieces[2] = BluePiece3;
            bluePlayer.Pieces[3] = BluePiece4;

            greenPlayer.Pieces[0] = GreenPiece1;
            greenPlayer.Pieces[1] = GreenPiece2;
            greenPlayer.Pieces[2] = GreenPiece3;
            greenPlayer.Pieces[3] = GreenPiece4;

            // Define paths for each player (simplified paths)
            redPlayer.Path = new Point[] { new Point(50, 400), new Point(100, 400), new Point(150, 400), new Point(150, 350) };
            yellowPlayer.Path = new Point[] { new Point(400, 50), new Point(450, 100), new Point(500, 150) };
            bluePlayer.Path = new Point[] { new Point(50, 50), new Point(100, 50), new Point(150, 50) };
            greenPlayer.Path = new Point[] { new Point(400, 400), new Point(450, 450), new Point(500, 500) };
        }

        // Roll dice and trigger movement
        private void RollDice_Click(object sender, RoutedEventArgs e)
        {
            _diceValue = _random.Next(1, 7); // Generate dice roll between 1 and 6
            UpdateDiceGraphic(_diceValue);   // Update the dice roll display

            // Example: Move first red piece if 6 is rolled
            if (_diceValue == 6)
            {
                MovePlayerPiece(0); // Moves the first red piece (as an example)
            }
        }

        // Update the text to reflect dice result
        private void UpdateDiceGraphic(int diceValue)
        {
            DiceResultText.Text = $"Roll: {diceValue}";
        }

        // Animate the movement of a piece
        private void AnimatePieceMovement(Ellipse piece, Point startPosition, Point endPosition)
        {
            double duration = 0.5; // Time for animation

            DoubleAnimation moveX = new DoubleAnimation
            {
                From = startPosition.X,
                To = endPosition.X,
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };

            DoubleAnimation moveY = new DoubleAnimation
            {
                From = startPosition.Y,
                To = endPosition.Y,
                Duration = new Duration(TimeSpan.FromSeconds(duration))
            };

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(moveX);
            storyboard.Children.Add(moveY);

            Storyboard.SetTarget(moveX, piece);
            Storyboard.SetTargetProperty(moveX, new PropertyPath("(Canvas.Left)"));

            Storyboard.SetTarget(moveY, piece);
            Storyboard.SetTargetProperty(moveY, new PropertyPath("(Canvas.Top)"));

            storyboard.Begin();
        }

        // Update the current player's text
        private void UpdateCurrentPlayerText()
        {
            switch (_currentPlayer)
            {
                case 1:
                    CurrentPlayerText.Text = "Current Player: Red";
                    break;
                case 2:
                    CurrentPlayerText.Text = "Current Player: Yellow";
                    break;
                case 3:
                    CurrentPlayerText.Text = "Current Player: Blue";
                    break;
                case 4:
                    CurrentPlayerText.Text = "Current Player: Green";
                    break;
            }
        }

        // Move player piece based on dice roll
        private void MovePlayerPiece(int pieceIndex)
        {
            Player currentPlayer = GetCurrentPlayer();

            // If the piece is still in the starting area, it can only move out if a 6 is rolled
            if (currentPlayer.PiecePositions[pieceIndex] == -1)
            {
                if (_diceValue == 6)
                {
                    currentPlayer.PiecePositions[pieceIndex] = 0; // Move out of starting area
                    AnimatePieceMovement(currentPlayer.Pieces[pieceIndex], new Point(30, 30), currentPlayer.Path[0]);
                }
                else
                {
                    return; // Can't move if the dice roll isn't 6
                }
            }
            else
            {
                // Move the piece along the path
                int newPosition = currentPlayer.PiecePositions[pieceIndex] + _diceValue;

                // Make sure the new position is within bounds
                if (newPosition < currentPlayer.Path.Length)
                {
                    Point startPosition = currentPlayer.Path[currentPlayer.PiecePositions[pieceIndex]];
                    Point endPosition = currentPlayer.Path[newPosition];

                    AnimatePieceMovement(currentPlayer.Pieces[pieceIndex], startPosition, endPosition);
                    currentPlayer.PiecePositions[pieceIndex] = newPosition;
                }

                CapturePiece(currentPlayer, pieceIndex); // Handle capturing logic
            }

            CheckForWin(currentPlayer); // Check for winning condition
            NextTurn(); // Move to the next player's turn
        }

        // Handle capturing pieces
        private void CapturePiece(Player currentPlayer, int pieceIndex)
        {
            Player[] players = { redPlayer, yellowPlayer, bluePlayer, greenPlayer };

            foreach (var player in players)
            {
                if (player != currentPlayer)
                {
                    for (int i = 0; i < player.PiecePositions.Length; i++)
                    {
                        if (player.PiecePositions[i] != -1 && player.Path[player.PiecePositions[i]] == currentPlayer.Path[pieceIndex])
                        {
                            player.PiecePositions[i] = -1; // Capture the piece and send it back to start
                            AnimatePieceMovement(player.Pieces[i], player.Pieces[i].TransformToAncestor(this).Transform(new Point(0, 0)), new Point(30, 30));
                            return;
                        }
                    }
                }
            }
        }

        // Get the current player
        private Player GetCurrentPlayer()
        {
            return _currentPlayer switch
            {
                1 => redPlayer,
                2 => yellowPlayer,
                3 => bluePlayer,
                4 => greenPlayer,
                _ => null
            };
        }

        // Check if the player has won
        private void CheckForWin(Player player)
        {
            if (player.GoalCount == player.Pieces.Length)
            {
                MessageBox.Show($"{GetPlayerColor(player)} Wins!");
            }
        }

        // Get the player's color as a string
        private string GetPlayerColor(Player player)
        {
            if (player == redPlayer) return "Red";
            if (player == yellowPlayer) return "Yellow";
            if (player == bluePlayer) return "Blue";
            return "Green";
        }

        // Move to the next player's turn
        private void NextTurn()
        {
            _currentPlayer = _currentPlayer == 4 ? 1 : _currentPlayer + 1;
            UpdateCurrentPlayerText();
        }

        // Example button handlers for moving pieces
        private void MoveRedPiece1_Click(object sender, RoutedEventArgs e)
        {
            MovePlayerPiece(0); // Move RedPiece1
        }

        private void MoveRedPiece2_Click(object sender, RoutedEventArgs e)
        {
            MovePlayerPiece(1); // Move RedPiece2
        }
    }
}
