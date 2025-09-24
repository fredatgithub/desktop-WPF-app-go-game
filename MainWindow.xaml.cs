using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GoGame.Models;

namespace GoGame
{
    public partial class MainWindow : Window
    {
        private GameState gameState;
        private Button[,] intersectionButtons;
        private const double CELL_SIZE = 30.0;
        private const double BOARD_MARGIN = 15.0;

        public MainWindow()
        {
            InitializeComponent();
            gameState = new GameState();
            InitializeBoard();
            UpdateUI();
        }

        private void InitializeBoard()
        {
            intersectionButtons = new Button[GameState.BOARD_SIZE, GameState.BOARD_SIZE];
            
            // Clear any existing children
            BoardCanvas.Children.Clear();
            
            // Draw board lines
            DrawBoardLines();
            
            // Add star points (handicap points)
            DrawStarPoints();
            
            // Create intersection buttons
            CreateIntersectionButtons();
        }

        private void DrawBoardLines()
        {
            var lineBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            
            // Vertical lines
            for (int i = 0; i < GameState.BOARD_SIZE; i++)
            {
                var line = new Line
                {
                    X1 = BOARD_MARGIN + i * CELL_SIZE,
                    Y1 = BOARD_MARGIN,
                    X2 = BOARD_MARGIN + i * CELL_SIZE,
                    Y2 = BOARD_MARGIN + (GameState.BOARD_SIZE - 1) * CELL_SIZE,
                    Stroke = lineBrush,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(line);
            }
            
            // Horizontal lines
            for (int i = 0; i < GameState.BOARD_SIZE; i++)
            {
                var line = new Line
                {
                    X1 = BOARD_MARGIN,
                    Y1 = BOARD_MARGIN + i * CELL_SIZE,
                    X2 = BOARD_MARGIN + (GameState.BOARD_SIZE - 1) * CELL_SIZE,
                    Y2 = BOARD_MARGIN + i * CELL_SIZE,
                    Stroke = lineBrush,
                    StrokeThickness = 1
                };
                BoardCanvas.Children.Add(line);
            }
        }

        private void DrawStarPoints()
        {
            var starBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
            var starPoints = new[] { 3, 9, 15 }; // Traditional star point positions for 19x19 board
            
            foreach (int row in starPoints)
            {
                foreach (int col in starPoints)
                {
                    var starPoint = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = starBrush
                    };
                    
                    Canvas.SetLeft(starPoint, BOARD_MARGIN + col * CELL_SIZE - 3);
                    Canvas.SetTop(starPoint, BOARD_MARGIN + row * CELL_SIZE - 3);
                    BoardCanvas.Children.Add(starPoint);
                }
            }
        }

        private void CreateIntersectionButtons()
        {
            for (int row = 0; row < GameState.BOARD_SIZE; row++)
            {
                for (int col = 0; col < GameState.BOARD_SIZE; col++)
                {
                    var button = new Button
                    {
                        Style = (Style)FindResource("IntersectionButton"),
                        Tag = new Position(row, col)
                    };
                    
                    button.Click += IntersectionButton_Click;
                    
                    Canvas.SetLeft(button, BOARD_MARGIN + col * CELL_SIZE - 15);
                    Canvas.SetTop(button, BOARD_MARGIN + row * CELL_SIZE - 15);
                    
                    intersectionButtons[row, col] = button;
                    BoardCanvas.Children.Add(button);
                }
            }
        }

        private void IntersectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Position position)
            {
                if (gameState.MakeMove(position.Row, position.Col))
                {
                    UpdateBoard();
                    UpdateUI();
                    
                    // Show captured stones briefly
                    if (gameState.LastCapturedStones.Count > 0)
                    {
                        ShowCapturedStones();
                    }
                }
            }
        }

        private void ShowCapturedStones()
        {
            // Visual feedback for captured stones could be added here
            // For now, the stones are simply removed from the board
        }

        private void UpdateBoard()
        {
            for (int row = 0; row < GameState.BOARD_SIZE; row++)
            {
                for (int col = 0; col < GameState.BOARD_SIZE; col++)
                {
                    var button = intersectionButtons[row, col];
                    button.Content = null;
                    
                    var stoneColor = gameState.Board[row, col];
                    if (stoneColor != StoneColor.Empty)
                    {
                        var stone = new Ellipse
                        {
                            Style = (Style)FindResource("StoneEllipse"),
                            Fill = stoneColor == StoneColor.Black ? 
                                   new SolidColorBrush(Colors.Black) : 
                                   new SolidColorBrush(Colors.White)
                        };
                        
                        // Add subtle gradient for more realistic appearance
                        if (stoneColor == StoneColor.Black)
                        {
                            stone.Fill = new RadialGradientBrush(
                                Color.FromRgb(0x44, 0x44, 0x44), 
                                Color.FromRgb(0x11, 0x11, 0x11));
                        }
                        else
                        {
                            stone.Fill = new RadialGradientBrush(
                                Colors.White, 
                                Color.FromRgb(0xE0, 0xE0, 0xE0));
                        }
                        
                        button.Content = stone;
                    }
                }
            }
        }

        private void UpdateUI()
        {
            // Update current player display
            if (gameState.Result == GameResult.Ongoing)
            {
                CurrentPlayerStone.Fill = gameState.CurrentPlayer == StoneColor.Black ? 
                                        new SolidColorBrush(Colors.Black) : 
                                        new SolidColorBrush(Colors.White);
                CurrentPlayerText.Text = gameState.CurrentPlayer.ToString();
                GameStatusText.Text = "Game in Progress";
                GameStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60));
            }
            else
            {
                // Game ended
                string resultText = gameState.Result switch
                {
                    GameResult.BlackWins => "Black Wins!",
                    GameResult.WhiteWins => "White Wins!",
                    GameResult.Draw => "Game is a Draw!",
                    _ => "Game Over"
                };
                
                GameStatusText.Text = resultText;
                GameStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
            }
            
            // Update captured stones count
            BlackCapturedText.Text = $" : {gameState.BlackCaptured}";
            WhiteCapturedText.Text = $" : {gameState.WhiteCaptured}";
            
            // Enable/disable pass button
            PassButton.IsEnabled = gameState.Result == GameResult.Ongoing;
        }

        private void PassButton_Click(object sender, RoutedEventArgs e)
        {
            gameState.Pass();
            UpdateUI();
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to start a new game?", 
                "New Game", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                gameState.NewGame();
                UpdateBoard();
                UpdateUI();
            }
        }
    }
}