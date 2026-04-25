using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private int Rows = 100;
        private int Cols = 100;
        private double CellSize = 10;

        private bool[,] grid = new bool[100, 100];
        private Shape[,] shapes = new Shape[100, 100];
        private DispatcherTimer timer = new DispatcherTimer();

        private long genCount = 0, bornTotal = 0, deadTotal = 0;

        public MainWindow()
        {
            InitializeComponent();
            SetupGame();
            timer.Tick += (s, e) => NextStep();
            timer.Interval = TimeSpan.FromMilliseconds(100);
        }

        private void SetupGame()
        {
            GameCanvas.Children.Clear();
            GameCanvas.Width = Cols * CellSize;
            GameCanvas.Height = Rows * CellSize;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    Shape s = chkCircles.IsChecked == true ? (Shape)new Ellipse() : (Shape)new Rectangle();
                    s.Width = CellSize - 0.5;
                    s.Height = CellSize - 0.5;
                    s.Fill = grid[r, c] ? Brushes.Blue : Brushes.LightGray;

                    Canvas.SetLeft(s, c * CellSize);
                    Canvas.SetTop(s, r * CellSize);
                    GameCanvas.Children.Add(s);
                    shapes[r, c] = s;
                }
            }
        }

        private void NextStep()
        {
            bool[,] nextGrid = new bool[Rows, Cols];
            int bornStep = 0, deadStep = 0;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    int n = CountNeighbors(r, c);
                    if (grid[r, c])
                    {
                        if (n < 2 || n > 3) { nextGrid[r, c] = false; deadStep++; }
                        else nextGrid[r, c] = true;
                    }
                    else
                    {
                        if (n == 3) { nextGrid[r, c] = true; bornStep++; }
                    }
                }
            }

            grid = nextGrid;
            genCount++;
            bornTotal += bornStep;
            deadTotal += deadStep;
            UpdateUI();
        }

        private int CountNeighbors(int r, int c)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int nr = (r + i + Rows) % Rows, nc = (c + j + Cols) % Cols;
                    if (grid[nr, nc]) count++;
                }
            return count;
        }

        private void UpdateUI()
        {
            int alive = 0;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    shapes[r, c].Fill = grid[r, c] ? Brushes.Blue : Brushes.LightGray;
                    if (grid[r, c]) alive++;
                }
            txtGen.Text = $"Generation: {genCount}";
            txtAlive.Text = $"Alive: {alive}";
            txtBorn.Text = $"Born (total): {bornTotal}";
            txtDead.Text = $"Dead (total): {deadTotal}";
        }

        private void sldSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (timer != null) timer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
        }

        private void ZoomSmall_Click(object sender, RoutedEventArgs e) { CellSize = 5; SetupGame(); UpdateUI(); }
        private void ZoomLarge_Click(object sender, RoutedEventArgs e) { CellSize = 15; SetupGame(); UpdateUI(); }
        private void Presentation_Changed(object sender, RoutedEventArgs e) { SetupGame(); UpdateUI(); }

        private void btnStart_Click(object sender, RoutedEventArgs e) { if (timer.IsEnabled) timer.Stop(); else timer.Start(); }

        private void btnRandom_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++) grid[r, c] = rnd.Next(100) < 25;
            UpdateUI();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            grid = new bool[Rows, Cols]; genCount = 0; bornTotal = 0; deadTotal = 0; UpdateUI();
        }

        private void GameCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(GameCanvas);
            int c = (int)(p.X / CellSize), r = (int)(p.Y / CellSize);
            if (r >= 0 && r < Rows && c >= 0 && c < Cols) { grid[r, c] = !grid[r, c]; UpdateUI(); }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var lines = Enumerable.Range(0, Rows).Select(r => string.Concat(Enumerable.Range(0, Cols).Select(c => grid[r, c] ? "1" : "0")));
            File.WriteAllLines("save.txt", lines);
            MessageBox.Show("Simulation state saved successfully!");
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("save.txt"))
            {
                var lines = File.ReadAllLines("save.txt");
                for (int r = 0; r < Math.Min(Rows, lines.Length); r++)
                    for (int c = 0; c < Math.Min(Cols, lines[r].Length); c++)
                        grid[r, c] = lines[r][c] == '1';
                UpdateUI();
            }
        }
    }
}