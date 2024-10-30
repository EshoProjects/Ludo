using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LudoGame
{
    public partial class MainWindow : Window
    {
        private Random _random = new Random();
        private int _diceValue;
        private int _currentPlayer = 1; // 1: Red, 2: Blue, 3: Yellow, 4: Green
        private bool _waitingForPieceSelection = false; // Flag to check if the player is selecting a piece
        private DispatcherTimer _moveTimer; // Timer to move piece step by step
        private int _remainingSteps; // Tracks how many steps are left to move
        private Ellipse _currentPiece; // The current piece being moved

        public class Player
        {
            public Point[] MainPath { get; set; } = Array.Empty<Point>();  // Main path on the outer path
            public int StartingPositionIndex { get; set; } // The starting position index on the outer path
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
        private Player bluePlayer = null!;
        private Player yellowPlayer = null!;
        private Player greenPlayer = null!;

        // Define the outer path (anti-clockwise path for all pieces)
        private readonly Point[] outerPath = new Point[]
        {
            new Point(240, 170), new Point(240, 210), new Point(240, 130), new Point(240, 90),
            new Point(240, 50), new Point(240, 10), new Point(280, 10), new Point(320, 10),
            new Point(320, 50), // Yellow starting point
            new Point(320, 90), new Point(320, 130), new Point(320, 170), new Point(320, 210),
            new Point(350, 242), new Point(390, 242), new Point(430, 242), new Point(470, 242),
            new Point(510, 242), new Point(550, 242), new Point(550, 282), new Point(550, 322),
            new Point(510, 322), // Green starting point
            new Point(470, 322), new Point(430, 322), new Point(390, 322), new Point(350, 322),
            new Point(320, 350), new Point(320, 390), new Point(320, 430), new Point(320, 470),
            new Point(320, 510), new Point(320, 550), new Point(280, 550), new Point(240, 550),
            new Point(240, 510), // Red starting point
            new Point(240, 470), new Point(240, 430), new Point(240, 390), new Point(240, 350),
            new Point(210, 322), new Point(170, 322), new Point(130, 322), new Point(90, 322),
            new Point(50, 322), new Point(10, 322), new Point(10, 282), new Point(10, 242),
            new Point(50, 242), // Blue starting point
            new Point(90, 242), new Point(130, 242), new Point(170, 242), new Point(210, 242)
        };

        public MainWindow()
        {
            InitializeComponent();
            InitializePlayers();
            UpdateCurrentPlayerText();

            // Initialize the move timer
            _moveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _moveTimer.Tick += MovePieceStepByStep;
        }

        // Initialize players and their paths
        private void InitializePlayers()
        {
            redPlayer = new Player(4)
            {
                StartingPositionIndex = 34, // Start position for the red player on the outer path
                MainPath = outerPath
            };
            bluePlayer = new Player(4)
            {
                StartingPositionIndex = 47, // Start position for the blue player on the outer path
                MainPath = outerPath
            };
            yellowPlayer = new Player(4)
            {
                StartingPositionIndex = 8, // Start position for the yellow player on the outer path
                MainPath = outerPath
            };
            greenPlayer = new Player(4)
            {
                StartingPositionIndex = 21, // Start position for the green player on the outer path
                MainPath = outerPath
            };

            // Link UI pieces to player data
            redPlayer.Pieces[0] = RedPiece1;
            redPlayer.Pieces[1] = RedPiece2;
            redPlayer.Pieces[2] = RedPiece3;
            redPlayer.Pieces[3] = RedPiece4;

            bluePlayer.Pieces[0] = BluePiece1;
            bluePlayer.Pieces[1] = BluePiece2;
            bluePlayer.Pieces[2] = BluePiece3;
            bluePlayer.Pieces[3] = BluePiece4;

            yellowPlayer.Pieces[0] = YellowPiece1;
            yellowPlayer.Pieces[1] = YellowPiece2;
            yellowPlayer.Pieces[2] = YellowPiece3;
            yellowPlayer.Pieces[3] = YellowPiece4;

            greenPlayer.Pieces[0] = GreenPiece1;
            greenPlayer.Pieces[1] = GreenPiece2;
            greenPlayer.Pieces[2] = GreenPiece3;
            greenPlayer.Pieces[3] = GreenPiece4;

            // Add Mouse Click event handlers to allow piece selection
            foreach (var piece in redPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in bluePlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in yellowPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in greenPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
        }

        // Roll dice and trigger movement
        private void RollDice_Click(object sender, RoutedEventArgs e)
        {
            if (_waitingForPieceSelection)
                return; // Prevent rerolling during a turn

            _diceValue = _random.Next(1, 7); // Roll dice between 1 and 6
            UpdateDiceGraphic(_diceValue);

            _waitingForPieceSelection = true; // Allow the player to select a piece

            Player currentPlayer = GetCurrentPlayer();
            if (!CanMovePiece(currentPlayer))
            {
                _waitingForPieceSelection = false;

                // Switch to the next turn only if the dice value is not 6
                if (_diceValue != 6)
                {
                    NextTurn();
                }
            }
        }

        private bool CanMovePiece(Player player)
        {
            foreach (var pos in player.PiecePositions)
                if (pos != -1 || _diceValue == 6) return true;
            return false;
        }

        private void UpdateDiceGraphic(int diceValue)
        {
            DiceResultText.Text = $"Roll: {diceValue}";
        }

        private void Piece_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_waitingForPieceSelection || sender is not Ellipse clickedPiece) return;

            Player currentPlayer = GetCurrentPlayer();
            for (int i = 0; i < currentPlayer.Pieces.Length; i++)
            {
                if (currentPlayer.Pieces[i] != clickedPiece) continue;

                if (currentPlayer.PiecePositions[i] == -1 && _diceValue == 6)
                    MovePieceToColorBlock(i);
                else if (currentPlayer.PiecePositions[i] >= 0)
                    StartPieceMovement(i, currentPlayer);

                break;
            }

            _waitingForPieceSelection = false;
        }

        private void MovePieceToColorBlock(int pieceIndex)
        {
            Player currentPlayer = GetCurrentPlayer();

            // Move the piece to the player's starting position on the outer path
            Point startPosition = currentPlayer.MainPath[currentPlayer.StartingPositionIndex];
            AnimatePieceMovement(
                currentPlayer.Pieces[pieceIndex],
                currentPlayer.Pieces[pieceIndex].TransformToAncestor(this).Transform(new Point(30, 30)),
                startPosition
            );

            // Update the piece's position to the starting index
            currentPlayer.PiecePositions[pieceIndex] = currentPlayer.StartingPositionIndex;
        }

        private void StartPieceMovement(int pieceIndex, Player currentPlayer)
        {
            _remainingSteps = _diceValue;
            _currentPiece = currentPlayer.Pieces[pieceIndex];
            _moveTimer.Start();
        }

        private void MovePieceStepByStep(object? sender, EventArgs e)
        {
            if (_remainingSteps <= 0)
            {
                _moveTimer.Stop();

                // Check if the dice value is not 6 to determine if we switch turns
                if (_diceValue != 6)
                {
                    NextTurn();
                }

                return;
            }

            Player currentPlayer = GetCurrentPlayer();
            int pieceIndex = Array.IndexOf(currentPlayer.Pieces, _currentPiece);

            int currentPosition = currentPlayer.PiecePositions[pieceIndex];
            int newPosition = (currentPosition + 1) % outerPath.Length;

            if (currentPosition != newPosition)
            {
                Point startPosition = outerPath[currentPosition];
                Point endPosition = outerPath[newPosition];

                AnimatePieceMovement(_currentPiece, startPosition, endPosition);
                currentPlayer.PiecePositions[pieceIndex] = newPosition;

                _remainingSteps--;
            }
        }

        private void AnimatePieceMovement(Ellipse piece, Point startPosition, Point endPosition)
        {
            var duration = 0.5;
            double tileSize = 30;  // Updated tile size
            double pieceSize = 20; // Piece size (width and height)

            // Calculate centered position for each tile
            double offset = (tileSize - pieceSize) / 2;
            startPosition = new Point(startPosition.X + offset, startPosition.Y + offset);
            endPosition = new Point(endPosition.X + offset, endPosition.Y + offset);

            DoubleAnimation moveX = new()
            {
                From = startPosition.X,
                To = endPosition.X,
                Duration = new Duration(TimeSpan.FromSeconds(duration)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation moveY = new()
            {
                From = startPosition.Y,
                To = endPosition.Y,
                Duration = new Duration(TimeSpan.FromSeconds(duration)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard storyboard = new();
            storyboard.Children.Add(moveX);
            storyboard.Children.Add(moveY);

            Storyboard.SetTarget(moveX, piece);
            Storyboard.SetTargetProperty(moveX, new PropertyPath("(Canvas.Left)"));
            Storyboard.SetTarget(moveY, piece);
            Storyboard.SetTargetProperty(moveY, new PropertyPath("(Canvas.Top)"));

            storyboard.Begin();
        }

        private Player GetCurrentPlayer() => _currentPlayer switch
        {
            1 => redPlayer,
            2 => bluePlayer,
            3 => yellowPlayer,
            4 => greenPlayer,
            _ => throw new InvalidOperationException("Invalid player")
        };

        private void NextTurn()
        {
            _currentPlayer = _currentPlayer == 4 ? 1 : _currentPlayer + 1;
            UpdateCurrentPlayerText();
        }

        private void UpdateCurrentPlayerText()
        {
            CurrentPlayerText.Text = _currentPlayer switch
            {
                1 => "Current Player: Red",
                2 => "Current Player: Blue",
                3 => "Current Player: Yellow",
                4 => "Current Player: Green",
                _ => throw new InvalidOperationException("Invalid player")
            };
        }
    }
}
