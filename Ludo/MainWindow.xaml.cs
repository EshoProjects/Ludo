using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace LudoGame
{
    public partial class MainWindow : Window
    {
        private Random _random = new Random();
        private int _diceValue;
        private int _currentPlayer = 1; // 1: Red, 2: Yellow, 3: Blue, 4: Green

        // Path coordinates for all players based on a standard Ludo board
        private Point[] redPath =
        {
            new Point(100, 200), new Point(120, 200), new Point(140, 200), new Point(160, 200),
            new Point(180, 200), new Point(200, 180), new Point(200, 160), new Point(200, 140),
            new Point(200, 120), new Point(200, 100), // Red's path continues...
        };

        private Point[] yellowPath =
        {
            new Point(300, 100), new Point(300, 120), new Point(300, 140), new Point(300, 160),
            new Point(300, 180), new Point(280, 200), new Point(260, 200), new Point(240, 200),
            new Point(220, 200), new Point(200, 200), // Yellow's path continues...
        };

        private Point[] bluePath =
        {
            new Point(100, 300), new Point(120, 300), new Point(140, 300), new Point(160, 300),
            new Point(180, 300), new Point(200, 320), new Point(200, 340), new Point(200, 360),
            new Point(200, 380), new Point(200, 400), // Blue's path continues...
        };

        private Point[] greenPath =
        {
            new Point(300, 300), new Point(300, 320), new Point(300, 340), new Point(300, 360),
            new Point(300, 380), new Point(280, 400), new Point(260, 400), new Point(240, 400),
            new Point(220, 400), new Point(200, 400), // Green's path continues...
        };

        // Track positions of each player's pieces on their respective paths
        private int[] redPiecePositions = { -1, -1, -1, -1 }; // -1 means in base
        private int[] yellowPiecePositions = { -1, -1, -1, -1 };
        private int[] bluePiecePositions = { -1, -1, -1, -1 };
        private int[] greenPiecePositions = { -1, -1, -1, -1 };

        // Number of pieces that have reached the goal
        private int redGoalCount = 0;
        private int yellowGoalCount = 0;
        private int blueGoalCount = 0;
        private int greenGoalCount = 0;

        public Ellipse YellowPiece1 { get; private set; }
        public Ellipse GreenPiece1 { get; private set; }
        public Ellipse BluePiece1 { get; private set; }
        public Ellipse RedPiece1 { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            UpdateCurrentPlayerText();
            UpdateDiceGraphic(1); // Initialize the dice graphic to show "1"
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
            switch (_currentPlayer)
            {
                case 1:
                    MoveRedPiece();
                    break;
                case 2:
                    MoveYellowPiece();
                    break;
                case 3:
                    MoveBluePiece();
                    break;
                case 4:
                    MoveGreenPiece();
                    break;
            }

            CheckForWin();

            // Move to the next player
            _currentPlayer = _currentPlayer == 4 ? 1 : _currentPlayer + 1;
            UpdateCurrentPlayerText();
        }

        private void MoveRedPiece()
        {
            for (int i = 0; i < 4; i++)
            {
                if (redPiecePositions[i] == -1 && _diceValue == 6)
                {
                    redPiecePositions[i] = 0;
                    AnimatePieceMovement(RedPiece1, new Point(50, 50), redPath[0]);
                    return;
                }
                else if (redPiecePositions[i] != -1)
                {
                    int newPosition = redPiecePositions[i] + _diceValue;
                    if (newPosition < redPath.Length)
                    {
                        Point startPosition = redPath[redPiecePositions[i]];
                        Point endPosition = redPath[newPosition];
                        AnimatePieceMovement(RedPiece1, startPosition, endPosition);
                        redPiecePositions[i] = newPosition;

                        CheckForOpponentCapture(newPosition, redPath, 1);
                    }
                    else
                    {
                        redGoalCount++;
                    }
                    return;
                }
            }
        }

        private void MoveYellowPiece()
        {
            for (int i = 0; i < 4; i++)
            {
                if (yellowPiecePositions[i] == -1 && _diceValue == 6)
                {
                    yellowPiecePositions[i] = 0;
                    AnimatePieceMovement(YellowPiece1, new Point(350, 50), yellowPath[0]);
                    return;
                }
                else if (yellowPiecePositions[i] != -1)
                {
                    int newPosition = yellowPiecePositions[i] + _diceValue;
                    if (newPosition < yellowPath.Length)
                    {
                        Point startPosition = yellowPath[yellowPiecePositions[i]];
                        Point endPosition = yellowPath[newPosition];
                        AnimatePieceMovement(YellowPiece1, startPosition, endPosition);
                        yellowPiecePositions[i] = newPosition;

                        CheckForOpponentCapture(newPosition, yellowPath, 2);
                    }
                    else
                    {
                        yellowGoalCount++;
                    }
                    return;
                }
            }
        }

        private void MoveBluePiece()
        {
            for (int i = 0; i < 4; i++)
            {
                if (bluePiecePositions[i] == -1 && _diceValue == 6)
                {
                    bluePiecePositions[i] = 0;
                    AnimatePieceMovement(BluePiece1, new Point(50, 350), bluePath[0]);
                    return;
                }
                else if (bluePiecePositions[i] != -1)
                {
                    int newPosition = bluePiecePositions[i] + _diceValue;
                    if (newPosition < bluePath.Length)
                    {
                        Point startPosition = bluePath[bluePiecePositions[i]];
                        Point endPosition = bluePath[newPosition];
                        AnimatePieceMovement(BluePiece1, startPosition, endPosition);
                        bluePiecePositions[i] = newPosition;

                        CheckForOpponentCapture(newPosition, bluePath, 3);
                    }
                    else
                    {
                        blueGoalCount++;
                    }
                    return;
                }
            }
        }

        private void MoveGreenPiece()
        {
            for (int i = 0; i < 4; i++)
            {
                if (greenPiecePositions[i] == -1 && _diceValue == 6)
                {
                    greenPiecePositions[i] = 0;
                    AnimatePieceMovement(GreenPiece1, new Point(350, 350), greenPath[0]);
                    return;
                }
                else if (greenPiecePositions[i] != -1)
                {
                    int newPosition = greenPiecePositions[i] + _diceValue;
                    if (newPosition < greenPath.Length)
                    {
                        Point startPosition = greenPath[greenPiecePositions[i]];
                        Point endPosition = greenPath[newPosition];
                        AnimatePieceMovement(GreenPiece1, startPosition, endPosition);
                        greenPiecePositions[i] = newPosition;

                        CheckForOpponentCapture(newPosition, greenPath, 4);
                    }
                    else
                    {
                        greenGoalCount++;
                    }
                    return;
                }
            }
        }

        private void CheckForOpponentCapture(int position, Point[] playerPath, int currentPlayer)
        {
            if (currentPlayer == 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (yellowPiecePositions[i] != -1 && playerPath[position] == yellowPath[yellowPiecePositions[i]])
                    {
                        yellowPiecePositions[i] = -1; // Send yellow piece back to base
                        AnimatePieceMovement(YellowPiece1, yellowPath[yellowPiecePositions[i]], new Point(350, 50));
                    }
                    // Add logic for blue and green pieces
                }
            }
            // Add similar checks for other players
        }

        private void CheckForWin()
        {
            if (redGoalCount == 4)
            {
                MessageBox.Show("Red Wins!");
            }
            else if (yellowGoalCount == 4)
            {
                MessageBox.Show("Yellow Wins!");
            }
            else if (blueGoalCount == 4)
            {
                MessageBox.Show("Blue Wins!");
            }
            else if (greenGoalCount == 4)
            {
                MessageBox.Show("Green Wins!");
            }
        }
    }
}
