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
        private int _currentPlayer = 1; // 1: Red, 2: Yellow, 3: Blue, 4: Green
        private bool _waitingForPieceSelection = false; // Flag to check if the player is selecting a piece
        private DispatcherTimer _moveTimer; // Timer to move piece step by step
        private int _remainingSteps; // Tracks how many steps are left to move
        private Ellipse _currentPiece; // The current piece being moved

        public class Player
        {
            public Point[] MainPath { get; set; } = Array.Empty<Point>();  // Main path on the light gray squares
            public Point ColorBlock { get; set; } // The color block where the piece moves when a 6 is rolled
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

        // Define the light-gray path (anti-clockwise path for all pieces)
        private Point[] lightGrayPath = new Point[]
        {
            new Point(240, 210), new Point(240, 170), new Point(240, 130), new Point(240, 90),
            new Point(240, 50), new Point(240, 10), new Point(280, 10), new Point(320, 10),
            new Point(320, 90), new Point(320, 130), new Point(320, 170), new Point(320, 210),
            new Point(350, 242), new Point(390, 242), new Point(430, 242), new Point(470, 242),
            new Point(510, 242), new Point(550, 242), new Point(550, 282), new Point(550, 322),
            new Point(470, 322), new Point(430, 322), new Point(390, 322), new Point(350, 322),
            new Point(320, 350), new Point(320, 390), new Point(320, 430), new Point(320, 470),
            new Point(320, 510), new Point(320, 550), new Point(280, 550), new Point(240, 550),
            new Point(240, 470), new Point(240, 430), new Point(240, 390), new Point(240, 350),
            new Point(210, 322), new Point(170, 322), new Point(130, 322), new Point(90, 322),
            new Point(50, 322), new Point(10, 322), new Point(10, 282), new Point(10, 242),
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
            redPlayer = new Player(4);
            yellowPlayer = new Player(4);
            bluePlayer = new Player(4);
            greenPlayer = new Player(4);

            // Define color block positions for each color (where they start after rolling a 6)
            redPlayer.ColorBlock = new Point(225 + 20, 493 + 20);
            yellowPlayer.ColorBlock = new Point(305 + 20, 35 + 20);
            bluePlayer.ColorBlock = new Point(50, 242);
            greenPlayer.ColorBlock = new Point(493.5 + 20, 306 + 20);

            // Define the starting block positions for the nearest light-gray block after the color block
            redPlayer.StartingBlock = lightGrayPath[31]; // Adjust for Red
            yellowPlayer.StartingBlock = lightGrayPath[13]; // Adjust for Yellow
            bluePlayer.StartingBlock = lightGrayPath[40]; // Adjust for Blue
            greenPlayer.StartingBlock = lightGrayPath[20]; // Adjust for Green

            // Assign the common path (lightGrayPath) to all players
            redPlayer.MainPath = lightGrayPath;
            yellowPlayer.MainPath = RotatePath(lightGrayPath, 13);  // Adjust so Yellow starts from its own block
            bluePlayer.MainPath = RotatePath(lightGrayPath, 26);    // Adjust for Blue's starting block
            greenPlayer.MainPath = RotatePath(lightGrayPath, 39);   // Adjust for Green's starting block

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
            if (_waitingForPieceSelection)
            {
                return; // Prevent rolling again before the turn is complete
            }

            _diceValue = _random.Next(1, 7); // Generate dice roll between 1 and 6
            UpdateDiceGraphic(_diceValue);   // Update the dice roll display

            _waitingForPieceSelection = true; // Player can now select a piece

            // Automatically move to the next player if the current player can't move
            Player currentPlayer = GetCurrentPlayer();
            bool canMove = false;

            for (int i = 0; i < currentPlayer.Pieces.Length; i++)
            {
                if (currentPlayer.PiecePositions[i] != -1 || _diceValue == 6)
                {
                    canMove = true;
                    break;
                }
            }

            // If the player can't move, skip their turn
            if (!canMove)
            {
                _waitingForPieceSelection = false;
                NextTurn();
            }
        }

        // Update the text to reflect dice result
        private void UpdateDiceGraphic(int diceValue)
        {
            DiceResultText.Text = $"Roll: {diceValue}";
        }

        // Event handler for piece clicks
        private void Piece_Clicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_waitingForPieceSelection && sender is Ellipse clickedPiece)
            {
                Player currentPlayer = GetCurrentPlayer();
                for (int i = 0; i < currentPlayer.Pieces.Length; i++)
                {
                    if (currentPlayer.Pieces[i] == clickedPiece)
                    {
                        // If the piece is still in the starting area and a 6 is rolled, move to the color block
                        if (currentPlayer.PiecePositions[i] == -1 && _diceValue == 6)
                        {
                            MovePieceToColorBlock(i);
                        }
                        else if (currentPlayer.PiecePositions[i] == 0) // If the piece is on the color block, move to the nearest light-gray block
                        {
                            MovePieceToNearestLightGrayBlock(i);
                        }
                        else if (currentPlayer.PiecePositions[i] > 0) // If the piece is already on the light-gray path
                        {
                            StartPieceMovement(i, currentPlayer); // Start moving piece step by step
                        }
                        break;
                    }
                }

                _waitingForPieceSelection = false;
            }
        }

        // Move the selected piece to the color block (the first step after rolling a 6)
        private void MovePieceToColorBlock(int pieceIndex)
        {
            Player currentPlayer = GetCurrentPlayer();
            AnimatePieceMovement(currentPlayer.Pieces[pieceIndex], currentPlayer.Pieces[pieceIndex].TransformToAncestor(this).Transform(new Point(30, 30)), currentPlayer.ColorBlock); // Move to color block
            currentPlayer.PiecePositions[pieceIndex] = 0; // Mark piece as being on the color block
        }

        // Move the selected piece to the nearest light-gray block
        private void MovePieceToNearestLightGrayBlock(int pieceIndex)
        {
            Player currentPlayer = GetCurrentPlayer();
            currentPlayer.PiecePositions[pieceIndex] = 1; // Nearest block will be index 1 of the path
            AnimatePieceMovement(currentPlayer.Pieces[pieceIndex], currentPlayer.ColorBlock, currentPlayer.MainPath[0]); // Move to first light-gray block
        }

        // Start moving the piece step by step
        private void StartPieceMovement(int pieceIndex, Player currentPlayer)
        {
            _remainingSteps = _diceValue; // Set steps equal to dice value
            _currentPiece = currentPlayer.Pieces[pieceIndex]; // Set the current piece to move
            _moveTimer.Start(); // Start moving the piece
        }

        // Move the piece step by step
        private void MovePieceStepByStep(object? sender, EventArgs e)
        {
            if (_remainingSteps > 0)
            {
                Player currentPlayer = GetCurrentPlayer();
                int pieceIndex = Array.IndexOf(currentPlayer.Pieces, _currentPiece); // Get piece index

                int currentPosition = currentPlayer.PiecePositions[pieceIndex];
                int newPosition = currentPosition + 1; // Move one step at a time

                // Ensure newPosition doesn't go past the path
                if (newPosition < currentPlayer.MainPath.Length)
                {
                    Point startPosition = currentPlayer.MainPath[currentPosition];
                    Point endPosition = currentPlayer.MainPath[newPosition];

                    AnimatePieceMovement(_currentPiece, startPosition, endPosition);
                    currentPlayer.PiecePositions[pieceIndex] = newPosition;

                    _remainingSteps--; // Decrease remaining steps
                }
                else
                {
                    _moveTimer.Stop(); // Stop timer if out of bounds
                }
            }
            else
            {
                _moveTimer.Stop(); // Stop moving when all steps are done
                NextTurn(); // Move to the next player's turn
            }
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

        // Move to the next player's turn, anti-clockwise
        private void NextTurn()
        {
            _currentPlayer = (_currentPlayer == 1) ? 4 : _currentPlayer - 1; // Move to the previous player, wrapping around
            UpdateCurrentPlayerText();
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
                    throw new InvalidOperationException("Invalid current player");
            }
        }

        // Utility method to rotate the path for each player, ensuring their paths start from their own block
        private Point[] RotatePath(Point[] originalPath, int shift)
        {
            Point[] newPath = new Point[originalPath.Length];
            for (int i = 0; i < originalPath.Length; i++)
            {
                newPath[i] = originalPath[(i + shift) % originalPath.Length];
            }
            return newPath;
        }
    }
}
