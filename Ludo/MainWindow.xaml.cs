using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LudoGame
{
    public partial class MainWindow : Window
    {
        private Random _random = new Random();
        private int _diceValue;
        private int _currentPlayer = 1; // 1: Red, 2: Yellow, 3: Blue, 4: Green

        // Define a class for player data to reduce redundancy
        private class Player
        {
            public Point[] Path { get; set; }
            public Ellipse Piece { get; set; }
            public int[] PiecePositions { get; set; }
            public int GoalCount { get; set; } = 0;
        }

        private Player redPlayer, yellowPlayer, bluePlayer, greenPlayer;

        public Ellipse GreenPiece1 { get; private set; }
        public Ellipse BluePiece1 { get; private set; }
        public Ellipse YellowPiece1 { get; private set; }
        public Ellipse RedPiece1 { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializePlayers();
            UpdateCurrentPlayerText();
            UpdateDiceGraphic(1); // Initialize the dice graphic to show "1"
        }

        private void InitializePlayers()
        {
            redPlayer = new Player
            {
                Path = new Point[] { new Point(100, 200), new Point(120, 200), new Point(140, 200), new Point(160, 200), new Point(180, 200), new Point(200, 180) },
                Piece = RedPiece1,
                PiecePositions = new int[] { -1 } // -1 means not yet on board
            };

            yellowPlayer = new Player
            {
                Path = new Point[] { new Point(300, 100), new Point(300, 120), new Point(300, 140), new Point(300, 160), new Point(300, 180), new Point(280, 200) },
                Piece = YellowPiece1,
                PiecePositions = new int[] { -1 }
            };

            bluePlayer = new Player
            {
                Path = new Point[] { new Point(100, 300), new Point(120, 300), new Point(140, 300), new Point(160, 300), new Point(180, 300), new Point(200, 320) },
                Piece = BluePiece1,
                PiecePositions = new int[] { -1 }
            };

            greenPlayer = new Player
            {
                Path = new Point[] { new Point(300, 300), new Point(300, 320), new Point(300, 340), new Point(300, 360), new Point(300, 380), new Point(280, 400) },
                Piece = GreenPiece1,
                PiecePositions = new int[] { -1 }
            };
        }

        // Dice roll event handler
        private void RollDice_Click(object sender, RoutedEventArgs e)
        {
            _diceValue = _random.Next(1, 7); // Generate a number between 1 and 6
            UpdateDiceGraphic(_diceValue);
            MovePlayerPiece();
        }

        // Update the dice graphic based on the roll
        private void UpdateDiceGraphic(int diceValue)
        {
            string diceImagePath = $"Images/dice{diceValue}.png"; // Ensure these images exist
            DiceImage.Source = new BitmapImage(new Uri(diceImagePath, UriKind.Relative));
        }

        // Animate piece movement
        private void AnimatePieceMovement(Ellipse piece, Point startPosition, Point endPosition)
        {
            double duration = 0.5; // Animation duration in seconds

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

        // Update the current player's turn text
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
        private void MovePlayerPiece()
        {
            Player currentPlayer = GetCurrentPlayer();

            if (currentPlayer.PiecePositions[0] == -1 && _diceValue == 6)
            {
                currentPlayer.PiecePositions[0] = 0;
                AnimatePieceMovement(currentPlayer.Piece, new Point(30, 30), currentPlayer.Path[0]);
            }
            else if (currentPlayer.PiecePositions[0] != -1)
            {
                int newPosition = currentPlayer.PiecePositions[0] + _diceValue;
                if (newPosition < currentPlayer.Path.Length)
                {
                    Point startPosition = currentPlayer.Path[currentPlayer.PiecePositions[0]];
                    Point endPosition = currentPlayer.Path[newPosition];
                    AnimatePieceMovement(currentPlayer.Piece, startPosition, endPosition);
                    currentPlayer.PiecePositions[0] = newPosition;
                }
                else
                {
                    currentPlayer.GoalCount++;
                }
            }

            CheckForWin(currentPlayer);
            NextTurn();
        }

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

        private void CheckForWin(Player player)
        {
            if (player.GoalCount == 1) // Change this if more pieces
            {
                MessageBox.Show($"{GetPlayerColor(player)} Wins!");
            }
        }

        private string GetPlayerColor(Player player)
        {
            if (player == redPlayer) return "Red";
            if (player == yellowPlayer) return "Yellow";
            if (player == bluePlayer) return "Blue";
            return "Green";
        }

        private void NextTurn()
        {
            _currentPlayer = _currentPlayer == 4 ? 1 : _currentPlayer + 1;
            UpdateCurrentPlayerText();
        }

        // New event handler for the 'MoveRedPlayer_Click' error
        private void MoveRedPlayer_Click(object sender, RoutedEventArgs e)
        {
            // This is just a sample action. You can either trigger movement or dice roll here
            MovePlayerPiece(); // Move red piece (or you can put specific logic for red here)
        }
    }
}
