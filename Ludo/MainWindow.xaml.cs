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
        private bool _waitingForPieceSelection = false; // Flag to check if the player is selecting a piece after rolling a 6

        public class Player
        {
            public Point[] Path { get; set; } = Array.Empty<Point>();  // Initialize with an empty array
            public Point StartingBlock { get; set; } // The block where the piece moves when a 6 is rolled
            public Ellipse[] Pieces { get; set; }
            public int[] PiecePositions { get; set; }
            public int GoalCount { get; set; } = 0;

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

        private Player redPlayer = null!;
        private Player yellowPlayer = null!;
        private Player bluePlayer = null!;
        private Player greenPlayer = null!;

        public MainWindow()
        {
            InitializeComponent();  // This should be called to initialize the XAML components
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

            // Define entry positions (starting blocks) for each color
            redPlayer.StartingBlock = new Point(240, 510);   // Red shifted left
            yellowPlayer.StartingBlock = new Point(320, 50); // Yellow starts at the top
            bluePlayer.StartingBlock = new Point(50, 242);   // Blue shifted up
            greenPlayer.StartingBlock = new Point(510, 322); // Green shifted down

            // Define the path for each player (example, you can adjust based on your board layout)
            redPlayer.Path = new Point[] { redPlayer.StartingBlock, new Point(280, 470), new Point(280, 430) };
            yellowPlayer.Path = new Point[] { yellowPlayer.StartingBlock, new Point(280, 90), new Point(280, 130) };
            bluePlayer.Path = new Point[] { bluePlayer.StartingBlock, new Point(90, 282), new Point(130, 282) };
            greenPlayer.Path = new Point[] { greenPlayer.StartingBlock, new Point(470, 282), new Point(430, 282) };

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

            // Add Mouse Click event handlers to allow piece selection
            foreach (var piece in redPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in yellowPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in bluePlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in greenPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
        }

        // Roll dice and trigger movement
        private void RollDice_Click(object sender, RoutedEventArgs e)
        {
            _diceValue = _random.Next(1, 7); // Generate dice roll between 1 and 6
            UpdateDiceGraphic(_diceValue);   // Update the dice roll display

            if (_diceValue == 6 && IsAnyPieceInStartingArea())
            {
                // If 6 is rolled and there are pieces in the starting area, let the player choose a piece
                _waitingForPieceSelection = true;
                MessageBox.Show("You rolled a 6! Select a piece to move out.");
            }
            else
            {
                // Automatically move the first piece for now (you can extend this to other pieces)
                MovePlayerPiece(0);

                // If the player rolled a 6, they get another turn.
                if (_diceValue != 6)
                {
                    NextTurn(); // Move to the next player's turn if 6 is not rolled.
                }
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
                default:
                    break;
            }
        }

        // Event handler for piece clicks
        private void Piece_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_waitingForPieceSelection && sender is Ellipse clickedPiece)
            {
                // Find which player's piece was clicked and move that piece to the starting block
                Player currentPlayer = GetCurrentPlayer();
                for (int i = 0; i < currentPlayer.Pieces.Length; i++)
                {
                    if (currentPlayer.Pieces[i] == clickedPiece && currentPlayer.PiecePositions[i] == -1)
                    {
                        // Move the piece to its starting block (the first block on the path)
                        MovePieceToStartingBlock(i);
                        break;
                    }
                }
                _waitingForPieceSelection = false; // Reset the flag after selecting the piece
            }
        }

        // Move the selected piece to the starting block (the first block on the path)
        private void MovePieceToStartingBlock(int pieceIndex)
        {
            Player currentPlayer = GetCurrentPlayer();
            currentPlayer.PiecePositions[pieceIndex] = 0; // Move out of the starting area
            AnimatePieceMovement(currentPlayer.Pieces[pieceIndex], new Point(30, 30), currentPlayer.StartingBlock);
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
                    // Move the piece to the starting block
                    MovePieceToStartingBlock(pieceIndex);
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
                _ => throw new InvalidOperationException("Invalid current player") // Handle invalid cases
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

        // Check if any piece is in the starting area (not yet moved)
        private bool IsAnyPieceInStartingArea()
        {
            Player currentPlayer = GetCurrentPlayer();
            foreach (int position in currentPlayer.PiecePositions)
            {
                if (position == -1) return true; // Found a piece in the starting area
            }
            return false;
        }
    }
}
