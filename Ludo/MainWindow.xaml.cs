using System;
using System.Collections.Generic;
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
        private int _consecutiveSixes = 0; // Counter for consecutive sixes

        // Define the safe points (including additional safe points)
        private readonly Point[] safePoints = new Point[]
        {
            new Point(50, 242),   // Blue safe point
            new Point(240, 510),  // Red safe point
            new Point(510, 322),  // Green safe point
            new Point(320, 50),   // Yellow safe point
            new Point(240, 130),  // Additional safe point
            new Point(430, 242),  // Additional safe point
            new Point(320, 430),  // Additional safe point
            new Point(130, 322)   // Additional safe point
        };

        // Dictionary to track the number of pieces on each safe point
        private readonly Dictionary<Point, int> safePointOccupancy = new Dictionary<Point, int>();

        // Define base positions for each piece in each player's base area
        private readonly Point[] redBasePositions = new Point[]
        {
            new Point(50, 15), new Point(148, 15), new Point(50, 109), new Point(148, 109)
        };

        private readonly Point[] blueBasePositions = new Point[]
        {
            new Point(50, 50), new Point(140, 50), new Point(50, 140), new Point(140, 140)
        };

        private readonly Point[] yellowBasePositions = new Point[]
        {
            new Point(21, 50), new Point(108, 50), new Point(21, 140), new Point(108, 140)
        };

        private readonly Point[] greenBasePositions = new Point[]
        {
            new Point(12, 10), new Point(104, 10), new Point(12, 100), new Point(104, 100)
        };

        public class Player
        {
            public Point[] MainPath { get; set; } = Array.Empty<Point>(); // Main path on the outer path
            public Point[] FinalStretch { get; set; } = Array.Empty<Point>(); // Path leading to home
            public int StartingPositionIndex { get; set; } // The starting position index on the outer path
            public int FinalStretchEntryIndex { get; set; } // Entry point to the final stretch
            public Ellipse[] Pieces { get; set; }
            public int[] PiecePositions { get; set; } // Tracks position on MainPath or FinalStretch
            public bool[] InFinalStretch { get; set; } // Tracks if a piece is in final stretch
            public Point[] BasePositions { get; set; } // Specific starting positions in the base area
            public int GoalCount { get; set; } = 0;

            public Player(int pieceCount, Point[] basePositions)
            {
                Pieces = new Ellipse[pieceCount];
                PiecePositions = new int[pieceCount];
                InFinalStretch = new bool[pieceCount];
                BasePositions = basePositions;

                for (int i = 0; i < pieceCount; i++)
                {
                    PiecePositions[i] = -1; // Initially, all pieces are off the board
                    InFinalStretch[i] = false; // Not in final stretch initially
                }
            }

            // Check if all pieces are off the board (only available for re-entry with a 6)
            public bool AllPiecesOffBoard() => Array.TrueForAll(PiecePositions, pos => pos == -1);
        }

        private Player redPlayer;
        private Player bluePlayer;
        private Player yellowPlayer;
        private Player greenPlayer;

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

        // Final stretches for each player
        private readonly Point[] redFinalStretch = new Point[]
        {
            new Point(280, 550), new Point(280, 510), new Point(280, 470), new Point(280, 430), new Point(280, 390), new Point(280, 350)
        };

        private readonly Point[] blueFinalStretch = new Point[]
        {
            new Point(10, 282), new Point(50, 282), new Point(90, 282), new Point(130, 282), new Point(170, 282), new Point(210, 282)
        };

        private readonly Point[] yellowFinalStretch = new Point[]
        {
            new Point(280, 10), new Point(280, 50), new Point(280, 90), new Point(280, 130), new Point(280, 170), new Point(280, 210)
        };

        private readonly Point[] greenFinalStretch = new Point[]
        {
            new Point(550, 282), new Point(510, 282), new Point(470, 282), new Point(430, 282), new Point(390, 282), new Point(350, 282)
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
            redPlayer = new Player(4, redBasePositions)
            {
                StartingPositionIndex = 34,
                FinalStretchEntryIndex = 32,
                MainPath = outerPath,
                FinalStretch = redFinalStretch
            };
            bluePlayer = new Player(4, blueBasePositions)
            {
                StartingPositionIndex = 47,
                FinalStretchEntryIndex = 45,
                MainPath = outerPath,
                FinalStretch = blueFinalStretch
            };
            yellowPlayer = new Player(4, yellowBasePositions)
            {
                StartingPositionIndex = 8,
                FinalStretchEntryIndex = 6,
                MainPath = outerPath,
                FinalStretch = yellowFinalStretch
            };
            greenPlayer = new Player(4, greenBasePositions)
            {
                StartingPositionIndex = 21,
                FinalStretchEntryIndex = 19,
                MainPath = outerPath,
                FinalStretch = greenFinalStretch
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

            foreach (var piece in redPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in bluePlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in yellowPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
            foreach (var piece in greenPlayer.Pieces) piece.MouseLeftButtonUp += Piece_Clicked;
        }

        private void RollDice_Click(object sender, RoutedEventArgs e)
        {
            if (_waitingForPieceSelection) return;
            _diceValue = _random.Next(1, 7);
            UpdateDiceGraphic(_diceValue);

            // Handle consecutive sixes logic
            if (_diceValue == 6)
            {
                _consecutiveSixes++;
                if (_consecutiveSixes == 3)
                {
                    MessageBox.Show("Three consecutive sixes rolled. Skipping turn!", "Turn Skipped");
                    _consecutiveSixes = 0;
                    NextTurn();
                    return;
                }
            }
            else
            {
                _consecutiveSixes = 0;
            }

            _waitingForPieceSelection = true;

            Player currentPlayer = GetCurrentPlayer();
            if (!CanMovePiece(currentPlayer))
            {
                _waitingForPieceSelection = false;
                if (_diceValue != 6) NextTurn();
            }
        }

        private bool CanMovePiece(Player player)
        {
            return player.AllPiecesOffBoard() ? _diceValue == 6 : true;
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
            Point startPosition = currentPlayer.MainPath[currentPlayer.StartingPositionIndex];
            AnimatePieceMovement(currentPlayer.Pieces[pieceIndex], startPosition);

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
                ResolveEndOfTurnCollision();
                if (_diceValue != 6) NextTurn();
                return;
            }

            Player currentPlayer = GetCurrentPlayer();
            int pieceIndex = Array.IndexOf(currentPlayer.Pieces, _currentPiece);

            if (currentPlayer.InFinalStretch[pieceIndex])
            {
                MoveOnFinalStretch(currentPlayer, pieceIndex);
            }
            else
            {
                int currentPosition = currentPlayer.PiecePositions[pieceIndex];
                int newPosition = (currentPosition + 1) % outerPath.Length;
                Point endPosition = outerPath[newPosition];

                AnimatePieceMovement(_currentPiece, endPosition, IsSafePoint(endPosition));
                currentPlayer.PiecePositions[pieceIndex] = newPosition;

                if (newPosition == currentPlayer.FinalStretchEntryIndex)
                {
                    currentPlayer.InFinalStretch[pieceIndex] = true;
                    currentPlayer.PiecePositions[pieceIndex] = 0;
                    MoveOnFinalStretch(currentPlayer, pieceIndex);
                }
            }

            _remainingSteps--;
        }

        private void ResolveEndOfTurnCollision()
        {
            Player currentPlayer = GetCurrentPlayer();
            Point currentPiecePosition = currentPlayer.MainPath[currentPlayer.PiecePositions[Array.IndexOf(currentPlayer.Pieces, _currentPiece)]];

            foreach (var player in new[] { redPlayer, bluePlayer, yellowPlayer, greenPlayer })
            {
                if (player == currentPlayer) continue;

                for (int i = 0; i < player.Pieces.Length; i++)
                {
                    if (player.PiecePositions[i] != -1 && !player.InFinalStretch[i])
                    {
                        Point piecePosition = player.MainPath[player.PiecePositions[i]];
                        if (piecePosition == currentPiecePosition && !IsSafePoint(piecePosition))
                        {
                            player.PiecePositions[i] = -1;
                            AnimatePieceMovement(player.Pieces[i], player.BasePositions[i]);
                        }
                    }
                }
            }
        }

        private void MoveOnFinalStretch(Player player, int pieceIndex)
        {
            int currentPosition = player.PiecePositions[pieceIndex];
            if (currentPosition >= player.FinalStretch.Length - 1)
            {
                player.GoalCount++;
                AnimatePieceMovement(player.Pieces[pieceIndex], player.FinalStretch[currentPosition]);
                player.PiecePositions[pieceIndex] = -2;

                if (player.GoalCount == 4)
                {
                    MessageBox.Show($"{GetPlayerColor(player)} wins the game!", "Game Over");
                }

                return;
            }

            int newPosition = currentPosition + 1;
            Point endPosition = player.FinalStretch[newPosition];
            AnimatePieceMovement(player.Pieces[pieceIndex], endPosition);
            player.PiecePositions[pieceIndex] = newPosition;
        }

        private bool IsSafePoint(Point position) => Array.Exists(safePoints, p => p == position);

        private void AnimatePieceMovement(Ellipse piece, Point endPosition, bool isSafePoint = false)
        {
            var duration = 0.5;
            double tileSize = 40;
            double pieceSize = isSafePoint ? 20 : 30;

            if (isSafePoint)
            {
                int occupancyCount = safePointOccupancy.ContainsKey(endPosition) ? safePointOccupancy[endPosition] : 0;
                safePointOccupancy[endPosition] = occupancyCount + 1;

                double xOffset = (occupancyCount % 2) * 10 - 5; // Offset in X
                double yOffset = (occupancyCount / 2) * 10 - 5; // Offset in Y
                endPosition.Offset(xOffset, yOffset);
            }

            piece.Width = piece.Height = pieceSize;

            double offset = (tileSize - pieceSize) / 2;
            endPosition = new Point(endPosition.X + offset, endPosition.Y + offset);

            DoubleAnimation moveX = new()
            {
                To = endPosition.X,
                Duration = new Duration(TimeSpan.FromSeconds(duration)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            DoubleAnimation moveY = new()
            {
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

        private string GetPlayerColor(Player player) => player switch
        {
            var p when p == redPlayer => "Red",
            var p when p == bluePlayer => "Blue",
            var p when p == yellowPlayer => "Yellow",
            var p when p == greenPlayer => "Green",
            _ => "Unknown"
        };
    }
}
