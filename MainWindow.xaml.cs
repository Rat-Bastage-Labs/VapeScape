using System;
using System.IO;
using System.Text.Json; 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging; 
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Input;

using IOPath = System.IO.Path;

namespace WPF_Game_NET
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer splashTimer = null!;
        private double splashOpacity = 0;
        private int splashFrames = 0;
        private bool showPressAnyKey = false;
        private DispatcherTimer gameTimer;
        private int[,] map  = null!;

        private int MapWidth = 12;
        private int MapHeight = 12;
        private int mapCharges = 4; 
        private bool showMap = false;
        private string notificationText = "";
        private int notificationFrames = 0;

        private int exitX;
        private int exitY;

        private int currentLevel = 1;
        private int mazesCompleted = 0;
        private bool leaderboardPromptVisible = false;
        private bool leaderboardEntryOffered = false;
        private string leaderboardNameInput = "";

        private readonly string leaderboardFile = IOPath.Combine(
        AppDomain.CurrentDomain.BaseDirectory,"leaderboard.json");
        private List<LeaderboardEntry> leaderboard = new();
        
        private readonly List<SmokeParticle> menuSmoke = new();
        private DispatcherTimer smokeTimer = null!;

        private Random random = new Random();

        double playerX = 1.5;
        double playerY = 1.5;
        double playerAngle = 0;

        bool moveForward;
        bool moveBackward;
        bool moveLeft;
        bool moveRight; 

        const double MoveSpeed = 0.025;
        const double TurnSpeed = 0.02;

        private int vapeCount = 0;

        private int battery = 100;
        private int liquid = 100;
        private int coil = 100;
        private bool vapeDead = false;
        private bool hasSmokeLoaded = false;
        private readonly List<VapeCloud> clouds = new();
        private const double CloudSpeed = 0.06;

        public class LeaderboardEntry
        {
            public string Name { get; set; } = "";
            public int Level { get; set; }
            public int MazesCompleted { get; set; }
            public DateTime DateAchieved { get; set; }
        }

        private class VapeCloud
        {
            public double X;
            public double Y;
            public double Angle;
            public double Radius = 0.4;
            public double Density = 1.0;
            public double Life = 600;
        }

        private class SmokeParticle
        {
            public Ellipse Shape = null!;

            public double X;
            public double Y;

            public double VX;
            public double VY;

            public double Life;
            public double MaxLife;

            public double Size;
        }

        private void StartMenuSmoke()
        {
            if (smokeTimer != null)
                return;

            smokeTimer = new DispatcherTimer();
            smokeTimer.Interval = TimeSpan.FromMilliseconds(16);
            smokeTimer.Tick += UpdateMenuSmoke;
            smokeTimer.Start();
        }

        private void StopMenuSmoke()
        {
            if (smokeTimer != null)
            {
                smokeTimer.Stop();
                smokeTimer.Tick -= UpdateMenuSmoke;
                smokeTimer = null!;
            }

            menuSmoke.Clear();
            MenuSmokeCanvas.Children.Clear();
        }

        private void SpawnSmoke()
        {
            double width = GameCanvas.ActualWidth;
            double height = GameCanvas.ActualHeight;

            Ellipse smoke = new Ellipse
            {
                Width = random.Next(40, 100),
                Height = random.Next(25, 70),

                 Fill = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(
                            Color.FromArgb(50,255,255,255), 0),

                        new GradientStop(
                            Color.FromArgb(15,220,220,220), 0.5),

                        new GradientStop(
                            Color.FromArgb(0,255,255,255), 1)
                    }
                }
            };

            MenuSmokeCanvas.Children.Add(smoke);

            menuSmoke.Add(new SmokeParticle
            {
                Shape = smoke,

                X = random.NextDouble() * width,

                Y = height + 50,

                VX = (random.NextDouble() - 0.5) * 0.3,

                VY = -(0.2 + random.NextDouble() * 0.4),

                Life = 0,
                MaxLife = random.Next(500, 900),

                Size = smoke.Width
            });
        }

        private void UpdateMenuSmoke(object? sender, EventArgs e)
        {
            if (gameState == GameState.Playing)
                return;

            if (random.NextDouble() < 0.15)
                SpawnSmoke();

            for (int i = menuSmoke.Count - 1; i >= 0; i--)
            {
                SmokeParticle p = menuSmoke[i];

                p.Life++;

                p.X += p.VX;
                p.Y += p.VY;

                p.X += Math.Sin(
                    p.Life * 0.02) * 0.25;

                double fade =
                    Math.Sin(
                        (p.Life / p.MaxLife) * Math.PI);

                p.Shape.Opacity = fade * 0.35;

                double scale =
                    1.0 +
                    p.Life / p.MaxLife;

                p.Shape.Width = p.Size * scale;
                p.Shape.Height = p.Size * 0.6 * scale;

                Canvas.SetLeft(
                    p.Shape,
                    p.X - p.Shape.Width / 2);

                Canvas.SetTop(
                    p.Shape,
                    p.Y - p.Shape.Height / 2);

                if (p.Life >= p.MaxLife)
                {
                    MenuSmokeCanvas.Children.Remove(
                        p.Shape);

                    menuSmoke.RemoveAt(i);
                }
            }
        }

        private double SampleFog(double x, double y)
        {
            double fog = 0;

            foreach (var cloud in clouds)
            {
                double dx = x - cloud.X;
                double dy = y - cloud.Y;

                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < cloud.Radius)
                {
                    double strength =
                        (1.0 - dist / cloud.Radius) *
                        cloud.Density;

                    fog += strength;
                }
            }

            return Math.Min(1.0, fog);
        }

        private void DrawVolumetricFog(double width, double height)
        {
            double totalFog = 0;

            foreach (var cloud in clouds)
            {
                double dx = cloud.X - playerX;
                double dy = cloud.Y - playerY;

                double distance = Math.Sqrt(dx * dx + dy * dy);

                totalFog +=
                    cloud.Density /
                    Math.Max(distance, 0.5);
            }

            totalFog = Math.Min(totalFog, 1.0);

            Rectangle haze = new Rectangle
            {
                Width = width,
                Height = height, 
                Fill = new SolidColorBrush(
                Color.FromArgb(
                    (byte)(totalFog * 150),
                    10,
                    10,
                    10))
            };

            GameCanvas.Children.Add(haze);
        }

        private enum GameState
        {
            Splash,
            Menu,
            Playing,
            Guide,
            Leaderboard,
            About
        }

        private GameState gameState = GameState.Menu;

        private void ShowMainMenu()
        {
            SetGameplayUIVisible(false);
            GameCanvas.Children.Clear();

            double w = ActualWidth;
            double h = ActualHeight;

            GameCanvas.Children.Add(new Rectangle
            {
                Width = w,
                Height = h,
                Fill = new SolidColorBrush(Color.FromRgb(10, 10, 10))
            });

            DrawMenuButton("Enter Maze (Play)", w, h, 1);
            DrawMenuButton("Guide / Manual", w, h, 2);
            DrawMenuButton("Leaderboard", w, h, 3);
            DrawMenuButton("About the Game", w, h, 4);

            TextBlock title = new TextBlock
            {
                Text = "Vape Scape",
                FontSize = 22,
                Foreground = Brushes.White
            };

            title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(
                title,
                (w - title.DesiredSize.Width) / 2);

            Canvas.SetTop(title, 100);
            GameCanvas.Children.Add(title);
            StartMenuSmoke();
        }

        private void DrawMenuButton(string text, double w, double h, int index)
        {
            double buttonWidth = 300;
            double buttonHeight = 60;
            double spacing = 20;

            double totalMenuHeight =
                (4 * buttonHeight) +
                (3 * spacing);

            double startY =
                (h - totalMenuHeight) / 2;

            double x =
                (w - buttonWidth) / 2;

            double y =
                startY +
                (index - 1) * (buttonHeight + spacing);

            Border btn = new Border
            {
                Width = buttonWidth,
                Height = buttonHeight,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };

            TextBlock label = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 15,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            btn.Child = label;

            btn.MouseLeftButtonDown += (s, e) =>
            {
                HandleMenuClick(index);
            };

            Canvas.SetLeft(btn, x);
            Canvas.SetTop(btn, y);

            GameCanvas.Children.Add(btn);
        }

        private void HandleMenuClick(int index)
        {
            switch (index)
            {
                case 1: 
                    StartGame();
                    break;

                case 2: 
                    gameState = GameState.Guide;
                    ShowGuide();
                    break;

                case 3: 
                    gameState = GameState.Leaderboard;
                    ShowLeaderboard();
                    break;

                case 4: 
                    gameState = GameState.About;
                    ShowAbout();
                    break;
            }
        }

        private void ShowCenteredPage(string text)
        {
            GameCanvas.Children.Clear();

            TextBlock pageText = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 15,
                TextAlignment = TextAlignment.Center
            };

            pageText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(
                pageText,
                (GameCanvas.ActualWidth - pageText.DesiredSize.Width) / 2);

            Canvas.SetTop(
                pageText,
                (GameCanvas.ActualHeight - pageText.DesiredSize.Height) / 2);

            GameCanvas.Children.Add(pageText);
            AddBackButton();
        }

        private void AddBackButton()
        {
            Border backButton = new Border
            {
                Width = 220,
                Height = 50,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };

            TextBlock label = new TextBlock
            {
                Text = "Back to Main Menu",
                Foreground = Brushes.White,
                FontSize = 15,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            backButton.Child = label;

            backButton.MouseLeftButtonDown += (s, e) =>
            {
                gameState = GameState.Menu;
                ShowMainMenu();
            };

            Canvas.SetLeft(
                backButton,
                (GameCanvas.ActualWidth - backButton.Width) / 2);

            Canvas.SetTop(
                backButton,
                GameCanvas.ActualHeight - 120);

            GameCanvas.Children.Add(backButton);
        }

        private void ShowGuide()
        {
            SetGameplayUIVisible(false);
            ShowCenteredPage(
                "Vape Scape - Survival Guide\n\n" +

                "Overview\n" +
                "You are trapped within a sequence of dark, shifting mazes.\n" +
                "Visibility is limited. The environment is unstable.\n" +
                "Your only companion is a handheld vape device —\n" +
                "serving as both your light source and potential means of survival.\n\n" +

                "Objective\n" +
                "Navigate through increasingly complex fog-drenched mazes\n" +
                "and locate the exit to progress deeper into the system.\n\n" +

                "Controls\n" +
                "Arrow Keys → Move / Turn\n" +
                "Space → Activate Vape Light\n" +
                "Shift → Release Smoke Cloud\n" +
                "Ctrl + P → Use Map Charge\n" +
                "C → Toggle Controls Panel"
            );
        }

        private void ShowLeaderboard()
        {
            SetGameplayUIVisible(false);

            string text = "Vape Scape - Hall of Vapors\n\n";

            if (leaderboard.Count == 0)
            {
                text += "No entries yet.";
            }
            else
            {
                int rank = 1;

                foreach (var entry in leaderboard.Take(20))
                {
                    text +=
                        $"{rank}. {entry.Name} - " +
                        $"Level {entry.Level}\n";

                    rank++;
                }
            }

            ShowCenteredPage(text);
        }
        private void ShowAbout()
        {
            SetGameplayUIVisible(false);
             ShowCenteredPage(
                "VAPE SCAPE\n\n" +
                "A First-Person Atmospheric Maze Experience\n\n" +

                "Built with:\n" +
                "• WPF (.NET 10)\n" +
                "• Custom Raycasting Engine\n" +
                "• Procedural Maze Generation\n" +
                "• Dynamic Fog & Particle Systems\n" +
                "• Survival Mechanics\n\n" +

                "Development Team:\n" +
                "Lead Developer: Javier Yzaguirre\n" +
                "Game Concept: Taylor Watson\n\n" +

                "© 2026 Vape Scape. All rights reserved."
            );
        }

        private void SetGameplayUIVisible(bool visible)
        {
            Visibility state =
                visible ? Visibility.Visible : Visibility.Collapsed;

            LevelText.Visibility = state;
            StatsPanel.Visibility = state;

            if (!visible)
                ControlsPanel.Visibility = Visibility.Collapsed;
        }
        private void StartGame()
        {
            StopMenuSmoke();
            gameState = GameState.Playing;
            SetGameplayUIVisible(true);

            GenerateDungeon();
            gameTimer.Start();
        }

        public MainWindow()
        { 
            InitializeComponent();
            LoadLeaderboard();
            UpdateVapeStats(); 
            UpdateLevelDisplay(); 

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;

            KeyUp += MainWindow_KeyUp;

            Loaded += MainWindow_Loaded; 
        }

        private void ShowSplashScreen()
        {
            gameState = GameState.Splash;
            SetGameplayUIVisible(false);
            GameCanvas.Children.Clear();

            splashOpacity = 0;
            splashFrames = 0;
            showPressAnyKey = false;

            StartMenuSmoke();

            splashTimer = new DispatcherTimer();
            splashTimer.Interval = TimeSpan.FromMilliseconds(16);
            splashTimer.Tick += SplashTick;
            splashTimer.Start();
        }

        private void SplashTick(object? sender, EventArgs e)
        {
            splashFrames++;

            if (splashOpacity < 1)
                splashOpacity += 0.01;

            if (splashFrames > 180)
                showPressAnyKey = true;

            DrawSplash();

            if (splashFrames > 600)
                ExitSplash();
        }

        private void ExitSplash()
        {
            splashTimer.Stop();
            splashTimer.Tick -= SplashTick;

            gameState = GameState.Menu;

            ShowMainMenu();
        }

        private void DrawSplash()
        {
            GameCanvas.Children.Clear();

            double w = ActualWidth;
            double h = ActualHeight;

            Rectangle bg = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = Brushes.Black
            };

            GameCanvas.Children.Add(bg);

            Ellipse glow = new Ellipse
            {
                Width = 300,
                Height = 300,
                Opacity = splashOpacity,

                Fill = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(
                            Color.FromArgb(120,255,140,60),0),

                        new GradientStop(
                            Color.FromArgb(0,255,140,60),1)
                    }
                }
            };

            Canvas.SetLeft(glow, w / 2 - 150);
            Canvas.SetTop(glow, h / 2 - 120);

            GameCanvas.Children.Add(glow);

            TextBlock title = new TextBlock
            {
                Text = "VAPE SCAPE",
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Opacity = splashOpacity
            };

            title.Measure(new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

            Canvas.SetLeft(
                title,
                (w - title.DesiredSize.Width) / 2);

            Canvas.SetTop(title, h / 2 - 100);

            GameCanvas.Children.Add(title);

            TextBlock subtitle = new TextBlock
            {
                Text = "The Smoking Maze",
                FontSize = 18,
                Foreground = Brushes.LightGray,
                Opacity = splashOpacity
            };

            subtitle.Measure(new Size(
                double.PositiveInfinity,
                double.PositiveInfinity));

            Canvas.SetLeft(
                subtitle,
                (w - subtitle.DesiredSize.Width) / 2);

            Canvas.SetTop(subtitle, h / 2 - 25);

            GameCanvas.Children.Add(subtitle);

            if (showPressAnyKey)
            {
                double pulse =
                    0.4 +
                    Math.Abs(
                        Math.Sin(
                            Environment.TickCount * 0.003));

                TextBlock press = new TextBlock
                {
                    Text = "PRESS ANY KEY",
                    FontSize = 15,
                    Foreground = Brushes.White,
                    Opacity = pulse
                };

                press.Measure(new Size(
                    double.PositiveInfinity,
                    double.PositiveInfinity));

                Canvas.SetLeft(
                    press,
                    (w - press.DesiredSize.Width) / 2);

                Canvas.SetTop(press, h - 140);

                GameCanvas.Children.Add(press);
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSplashScreen();
        }

        private void GenerateDungeon()
        {
            UpdateMapSize();
            map = new int[MapHeight, MapWidth];

            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    map[y, x] = 1;
                }
            }

            Carve(1, 1); 
            GenerateExit();

            playerX = 1.5;
            playerY = 1.5;
            playerAngle = 0;
        }

        private void GenerateExit()
        {
            bool[,] visited = new bool[MapHeight, MapWidth];
            Queue<(int x, int y, int dist)> queue = new();

            queue.Enqueue((1, 1, 0));
            visited[1, 1] = true;

            int bestX = 1;
            int bestY = 1;
            int maxDist = 0;

            int[] dirs = { -1, 1, 0, 0, 0, 0 };
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                var (x, y, dist) = queue.Dequeue();

                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestX = x;
                    bestY = y;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];

                    if (nx < 0 || ny < 0 || nx >= MapWidth || ny >= MapHeight)
                        continue;

                    if (visited[ny, nx])
                        continue;

                    if (map[ny, nx] == 1) 
                        continue;

                    visited[ny, nx] = true;
                    queue.Enqueue((nx, ny, dist + 1));
                }
            }

            exitX = bestX;
            exitY = bestY;
        }

        private void UpdateMapSize()
        {
            if (currentLevel <= 5)
            {
                MapWidth = 13;
                MapHeight = 13;
            }
            else if (currentLevel <= 15)
            {
                MapWidth = 15;
                MapHeight = 15;
            }
            else if (currentLevel <= 25)
            {
                MapWidth = 21;
                MapHeight = 21;
            }
            else if (currentLevel <= 35)
            {
                MapWidth = 25;
                MapHeight = 25;
            }
            else if (currentLevel <= 45)
            {
                MapWidth = 29;
                MapHeight = 29;
            }
            else 
            {
                MapWidth = 33;
                MapHeight = 33;
            }
        }

        private void DrawExitLight(double width, double height)
        {
            double dx = exitX + 0.5 - playerX;
            double dy = exitY + 0.5 - playerY;

            double distance = Math.Sqrt(dx * dx + dy * dy);

            double angleToExit = Math.Atan2(dy, dx) - playerAngle;

            while (angleToExit < -Math.PI) angleToExit += Math.PI * 2;
            while (angleToExit > Math.PI) angleToExit -= Math.PI * 2;

            double fov = Math.PI / 7;

            if (Math.Abs(angleToExit) > fov / 2)
                return;

            double screenX = (angleToExit / (fov / 2) + 1) * (width / 2);
            double size = Math.Max(20, 260 / (distance + 0.25));
            double pulse = 1.0 + Math.Sin(Environment.TickCount * 0.004) * 0.1;
            size *= pulse;
            double alpha = Math.Min(240, 900 / (distance + 0.5));
            double screenY = height / 2 - size / 2;

            Ellipse orb = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.35, 0.35), 
                    Center = new Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb((byte)alpha, 120, 210, 255), 0.0),
                        new GradientStop(Color.FromArgb((byte)(alpha * 0.6), 60, 160, 255), 0.3),
                        new GradientStop(Color.FromArgb((byte)(alpha * 0.2), 0, 80, 200), 0.7),
                        new GradientStop(Color.FromArgb(0, 0, 40, 120), 1.0)
                    }
                }
            };

            Canvas.SetLeft(orb, screenX - size / 2);
            Canvas.SetTop(orb, screenY);

            GameCanvas.Children.Add(orb);
        }

        private bool HasLineOfSightToExit()
        {
            double dx = (exitX + 0.5) - playerX;
            double dy = (exitY + 0.5) - playerY;

            double distance = Math.Sqrt(dx * dx + dy * dy);

            double stepX = dx / distance;
            double stepY = dy / distance;

            double x = playerX;
            double y = playerY;

            for (double i = 0; i < distance; i += 0.05)
            {
                x += stepX * 0.05;
                y += stepY * 0.05;

                int mx = (int)x;
                int my = (int)y;

                if (mx < 0 || my < 0 || mx >= MapWidth || my >= MapHeight)
                    return false;

                if (map[my, mx] == 1)
                    return false;
            }

            return true;
        }

        private void UpdateLevelDisplay()
        {
            LevelText.Text =
                $"Level: {currentLevel} | Mazes Completed: {mazesCompleted}";
        }

        private void LoadLeaderboard()
        {
            try
            {
                if (!File.Exists(leaderboardFile))
                {
                    leaderboard = new List<LeaderboardEntry>();
                    return;
                }

                string json = File.ReadAllText(leaderboardFile);

                leaderboard =
                    JsonSerializer.Deserialize<List<LeaderboardEntry>>(json)
                    ?? new List<LeaderboardEntry>();
            }
            catch
            {
                leaderboard = new List<LeaderboardEntry>();
            }
        }

        private void SaveLeaderboard()
        {
            try
            {
                string json =
                    JsonSerializer.Serialize(
                        leaderboard,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                File.WriteAllText(
                    leaderboardFile,
                    json);
            }
            catch
            {
                ShowNotification(
                    "Failed to save leaderboard!");
            }
        }

        private void CheckLeaderboardEligibility()
        {
            if (leaderboardEntryOffered)
                return;

            if (currentLevel >= 30 &&
                battery > 65 &&
                liquid > 65 &&
                coil > 65)
            {
                leaderboardEntryOffered = true;
                leaderboardPromptVisible = true;
            }
        }

        private void DrawLeaderboardPrompt()
        {
            Rectangle overlay = new Rectangle
            {
                Width = ActualWidth,
                Height = ActualHeight,
                Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0))
            };

            GameCanvas.Children.Add(overlay);

            Border panel = new Border
            {
                Width = 360,
                Height = 200,
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 15)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10)
            };

            StackPanel content = new StackPanel
            {
                Margin = new Thickness(20)
            };

            content.Children.Add(new TextBlock
            {
                Text = "Leaderboard Entry",
                Foreground = Brushes.White,
                FontSize = 15,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });

            content.Children.Add(new TextBlock
            {
                Text =
                    "You reached Level 30+ with all vape stats above 65%.\n" +
                    "Enter your name below and press ENTER to submit:",
                Foreground = Brushes.White,
                FontSize = 13,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            });

            Border inputBox = new Border
            {
                Width = 300,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock inputText = new TextBlock
            {
                Text = leaderboardNameInput.Length == 0 ? "_" : leaderboardNameInput,
                Foreground = Brushes.LightGreen,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            inputBox.Child = inputText;

            content.Children.Add(inputBox);

            content.Children.Add(new TextBlock
            {
                Text = "Press ENTER to submit",
                Foreground = Brushes.LightBlue,
                FontSize = 12,
                Margin = new Thickness(0, 15, 0, 0),
                TextAlignment = TextAlignment.Center
            });

            panel.Child = content;

            Canvas.SetLeft(panel, (ActualWidth - panel.Width) / 2);
            Canvas.SetTop(panel, (ActualHeight - panel.Height) / 2);

            GameCanvas.Children.Add(panel);
        }

        private void SubmitLeaderboardEntry()
        {
            leaderboardPromptVisible = false;

            string name = string.IsNullOrWhiteSpace(leaderboardNameInput)
                ? "ANONYMOUS"
                : leaderboardNameInput;

            leaderboard.Add(new LeaderboardEntry
            {
                Name = name,
                Level = currentLevel,
                MazesCompleted = mazesCompleted,
                DateAchieved = DateTime.Now
            });

            leaderboard = leaderboard
                .OrderByDescending(x => x.Level)
                .ThenByDescending(x => x.MazesCompleted)
                .ToList();

            SaveLeaderboard();

            ShowNotification($"Submitted: {name}");

            leaderboardNameInput = "";
        }

        private void Carve(int x, int y)
        {
            map[y, x] = 0;

            int[] directions = { 0, 1, 2, 3 };

            for (int i = 0; i < directions.Length; i++)
            {
                int swap = random.Next(directions.Length);
                (directions[i], directions[swap]) =
                    (directions[swap], directions[i]);
            }

            foreach (int dir in directions)
            {
                int dx = 0;
                int dy = 0;

                switch (dir)
                {
                    case 0: dx = 2; break;
                    case 1: dx = -2; break;
                    case 2: dy = 2; break;
                    case 3: dy = -2; break;
                }

                int nx = x + dx;
                int ny = y + dy;

                if (nx > 0 &&
                    ny > 0 &&
                    nx < MapWidth - 1 &&
                    ny < MapHeight - 1 &&
                    map[ny, nx] == 1)
                {
                    map[y + dy / 2, x + dx / 2] = 0;
                    Carve(nx, ny);
                }
            }
        }

        private void GameLoop(object? sender, EventArgs e)
        {
              if (gameState != GameState.Playing)
              return;

            if (notificationFrames > 0)
            {
                notificationFrames--;
            }

            UpdatePlayer();

            if (vaping)
            {
                vapeFrames--;

                glowMultiplier = 2.0;

                if (vapeFrames <= 0)
                {
                    vaping = false;
                    glowMultiplier = 1.0;
                }
            }
 
            UpdateClouds();
            RenderScene();
        }

        private void UpdatePlayer()
        {
            double newX = playerX;
            double newY = playerY;

            if (moveForward)
            {
                newX += Math.Cos(playerAngle) * MoveSpeed;
                newY += Math.Sin(playerAngle) * MoveSpeed;
            }

            if (moveBackward)
            {
                newX -= Math.Cos(playerAngle) * MoveSpeed;
                newY -= Math.Sin(playerAngle) * MoveSpeed;
            }

            if (moveLeft)
            {
                playerAngle -= TurnSpeed;
            }

            if (moveRight)
            {
                playerAngle += TurnSpeed;
            }

            int mapX = (int)newX;
            int mapY = (int)newY;

            if (mapX >= 0 &&
                mapY >= 0 &&
                mapX < MapWidth &&
                mapY < MapHeight &&
                map[mapY, mapX] == 0)
            {
                playerX = newX;
                playerY = newY;
            }

            if ((int)playerX == exitX && (int)playerY == exitY)
            { 
                mazesCompleted++;

                if (currentLevel >= 50)
                {
                    gameTimer.Stop();

                    ShowNotification(
                        "CONGRATULATIONS!\n" +
                        "You escaped Vape Scape!\n" +
                        $"Mazes Completed: {mazesCompleted}",
                        600);

                    gameState = GameState.Menu;
                    ShowMainMenu();
                    return;
                }

                currentLevel++;

                CheckMapUnlockReward();
                UpdateLevelDisplay();
                CheckLeaderboardEligibility();
                GenerateDungeon();

                playerX = 1.5;
                playerY = 1.5;
                playerAngle = 0;

                moveForward = false;
                moveBackward = false;
                moveLeft = false;
                moveRight = false;

                showMap = false;
            }  
        }

        private void RenderScene()
        {
            GameCanvas.Children.Clear();

            double width = ActualWidth;
            double height = ActualHeight;

            Rectangle ceiling = new Rectangle
            {
                Width = width,
                Height = height / 2,
                Fill = new LinearGradientBrush(
                    Color.FromRgb(5, 5, 5),
                    Color.FromRgb(25, 25, 25),
                    90)
            };

            Canvas.SetLeft(ceiling, 0);
            Canvas.SetTop(ceiling, 0);
            GameCanvas.Children.Add(ceiling);

            Rectangle floor = new Rectangle
            {
                Width = width,
                Height = height / 2,
                Fill = new LinearGradientBrush(
                    Color.FromRgb(20, 20, 20),
                    Color.FromRgb(0, 0, 0),
                    90)
            };

            Canvas.SetLeft(floor, 0);
            Canvas.SetTop(floor, height / 2);
            GameCanvas.Children.Add(floor);

            int rays = (int)width;
            double fov = Math.PI / 7;

            for (int x = 0; x < rays; x++)
            {
                double t = (double)x / rays;

                double rayAngle =
                    playerAngle + (t - 0.5) * fov;

                double distance = CastRay(rayAngle);
                double fogDensity = 0;

                for (double d = 0; d < distance; d += 0.1)
                {
                    double fx = playerX + Math.Cos(rayAngle) * d;
                    double fy = playerY + Math.Sin(rayAngle) * d;

                    fogDensity += SampleFog(fx, fy) * 0.02;
                }

                fogDensity = Math.Min(1.0, fogDensity);

                double corrected =
                    distance *
                    Math.Cos(rayAngle - playerAngle);

                double wallHeight =
                    Math.Min(height,
                    (height * 1.5) / corrected);

                double top =
                    (height / 2) - wallHeight / 2;

                double ceilingDarkness =
                    Math.Max(0, 1 - (wallHeight / height));

                Rectangle shadow = new Rectangle
                {
                    Width = 1,
                    Height = top,
                    Fill = new SolidColorBrush(
                        Color.FromArgb(
                            (byte)(ceilingDarkness * 255),
                            0, 0, 0))
                };

                Canvas.SetLeft(shadow, x);
                Canvas.SetTop(shadow, 0);
                GameCanvas.Children.Add(shadow);  

                double vapeRadius = 2.5;
                double brightness =
                    Math.Max(0,
                    1.0 - (distance / vapeRadius));

                double fog = Math.Min(1.0, distance / 8.0);
                double intensity = brightness * (1.0 - fog);
                double darkFactor = 0.45;

                double wallBrightness = intensity * 140 * darkFactor;
                wallBrightness *= (1.0 - fogDensity * 0.85); 
                byte shade = (byte)Math.Max(0, wallBrightness);

                Rectangle wall = new Rectangle
                {
                    Width = 1,
                    Height = wallHeight,
                    Fill = new SolidColorBrush(
                        Color.FromRgb(
                            (byte)(shade * 0.7),
                            (byte)(shade * 0.85),
                            (byte)(shade)))
                };

                Canvas.SetLeft(wall, x);
                Canvas.SetTop(wall, top);

                GameCanvas.Children.Add(wall);
            }

            if (HasLineOfSightToExit())
            {
                DrawExitLight(width, height);
            }

            DrawVapeGlow(width, height); 
            DrawVolumetricFog(width, height);

            Rectangle vignette = new Rectangle
            {
                Width = width,
                Height = height,
                IsHitTestVisible = false,
                Fill = new RadialGradientBrush
                {
                    Center = new Point(0.5, 0.5),
                    GradientOrigin = new Point(0.5, 0.5),
                    RadiusX = 0.75,
                    RadiusY = 0.75,
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.4),
                        new GradientStop(Color.FromArgb(120, 0, 0, 0), 0.8),
                        new GradientStop(Color.FromArgb(220, 0, 0, 0), 1.0)
                    }
                }
            };

            DrawClouds(width, height);
            GameCanvas.Children.Add(vignette);

            if (showMap)
            {
                DrawMapOverlay();
            }

            DrawNotification();

            if (leaderboardPromptVisible)
            {   
                DrawLeaderboardPrompt();
            }
        }

        private void DrawMapOverlay()
        {
            double mapSize = 250;
            double cellSize = mapSize / Math.Max(MapWidth, MapHeight);

            Canvas miniMap = new Canvas
            {
                Width = mapSize,
                Height = mapSize,
                Background = new SolidColorBrush(
                    Color.FromArgb(90, 0, 0, 0))
            };

            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    double revealDistance = 3.5;
                    double dx = x - playerX;
                    double dy = y - playerY;

                    if (Math.Sqrt(dx * dx + dy * dy) > revealDistance)
                        continue;

                    Rectangle cell = new Rectangle
                    {
                        Width = cellSize,
                        Height = cellSize,
                        Fill = map[y, x] == 1
                            ? new SolidColorBrush(Color.FromArgb(120, 255, 255, 255))
                            : new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
                    };

                    Canvas.SetLeft(cell, x * cellSize);
                    Canvas.SetTop(cell, y * cellSize);

                    miniMap.Children.Add(cell);
                }
            } 

            Ellipse playerDot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.Red
            };

            Canvas.SetLeft(
                playerDot,
                playerX * cellSize - 4);

            Canvas.SetTop(
                playerDot,
                playerY * cellSize - 4);

            miniMap.Children.Add(playerDot);

            Line facing = new Line
            {
                X1 = playerX * cellSize,
                Y1 = playerY * cellSize,
                X2 = playerX * cellSize + Math.Cos(playerAngle) * 15,
                Y2 = playerY * cellSize + Math.Sin(playerAngle) * 15,
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            };

            miniMap.Children.Add(facing);

            double exitRevealDistance = 4.0;
            double exitDx = (exitX + 0.5) - playerX;
            double exitDy = (exitY + 0.5) - playerY;

            double exitDistance =
                Math.Sqrt(exitDx * exitDx + exitDy * exitDy);

            if (exitDistance <= exitRevealDistance)
            {
                Ellipse exitDot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.Blue
                };

                Canvas.SetLeft(
                    exitDot,
                    exitX * cellSize + cellSize / 2 - 4);

                Canvas.SetTop(
                    exitDot,
                    exitY * cellSize + cellSize / 2 - 4);

                miniMap.Children.Add(exitDot);
            }

            Canvas.SetLeft(miniMap, 20);
            Canvas.SetTop(miniMap, 80);

            GameCanvas.Children.Add(miniMap);
        }

        private void CheckMapUnlockReward()
        {
            switch (currentLevel)
            {
                case 5:
                case 10:
                case 15:
                case 20:
                case 25:
                case 30:
                case 35:
                case 45:
                    mapCharges++;
                    ShowNotification($"Map Charge Earned! ({mapCharges} available)");
                    break;
            }
        }

        private void DrawNotification()
        {
            if (notificationFrames <= 0)
                return;

            Border panel = new Border
            {
                Width = 320,
                Height = 60,
                Background = new SolidColorBrush(
                    Color.FromArgb(180, 0, 0, 0)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };

            TextBlock text = new TextBlock
            {
                Text = notificationText,
                Foreground = Brushes.White,
                FontSize = 15,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Child = text;

            Canvas.SetLeft(panel, (ActualWidth - panel.Width) / 2);
            Canvas.SetTop(panel, 50);

            GameCanvas.Children.Add(panel);
        }
        private void ShowNotification(string text, int duration = 180)
        {
            notificationText = text;
            notificationFrames = duration;
        }

        private void UseVape()
        { 
            if (vapeDead)
                return;

            if (liquid <= 0)
            {
                hasSmokeLoaded = false;
                return;
            }

            vapeCount++; 

            battery = Math.Max(0, 100 - (int)(vapeCount / 3.0));
            liquid = Math.Max(0, 100 - (int)(vapeCount / 2.0));
            coil = Math.Max(0, 100 - (int)(vapeCount / 15.0));

            if (battery <= 0)
            {
                vapeDead = true;
            }

            if (coil <= 0)
            {
                vapeDead = true;
            }

            UpdateVapeStats();
        }

        private void DrawClouds(double width, double height)
        {
            foreach (var c in clouds)
            {
                double dx = c.X - playerX;
                double dy = c.Y - playerY;

                double distance = Math.Sqrt(dx * dx + dy * dy);
                double angleToCloud = Math.Atan2(dy, dx) - playerAngle;

                while (angleToCloud < -Math.PI) angleToCloud += Math.PI * 2;
                while (angleToCloud > Math.PI) angleToCloud -= Math.PI * 2;

                double fov = Math.PI / 7;

                if (Math.Abs(angleToCloud) > fov / 2)
                    continue;

                double screenX = (angleToCloud / (fov / 2) + 1) * (width / 2);

                double size = Math.Max(10, 120 / (distance + 0.5));
                double alpha = Math.Min(200, 400 / (distance + 1));

                byte fogShade = (byte)random.Next(105, 105);

                Ellipse cloud = new Ellipse
                {
                    Width = size,
                    Height = size * 0.6,
                    Fill = new SolidColorBrush(
                        Color.FromArgb(
                            (byte)alpha,
                            fogShade,
                            fogShade,
                            fogShade))
                };

                Canvas.SetLeft(cloud, screenX - size / 2);
                Canvas.SetTop(cloud, height / 2 - size / 3);

                GameCanvas.Children.Add(cloud);
            }
        }

       private void BlowVapeCloud()
        {
            for (int i = 0; i < 10; i++)
            {
                clouds.Add(new VapeCloud
                {
                    X = playerX,
                    Y = playerY,

                    Angle =
                        playerAngle +
                        (random.NextDouble() - 0.5) * 0.4,

                    Radius = 0.3,
                    Density = 1.0,
                    Life = 600
                });
            }
        }

        private void UpdateClouds()
        {
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                var c = clouds[i];

                c.X += Math.Cos(c.Angle) * CloudSpeed;
                c.Y += Math.Sin(c.Angle) * CloudSpeed;

                c.Radius += 0.01;
                c.Density *= 0.997;
                c.Life--;

                if (c.Life <= 0 || c.Density < 0.05)
                    clouds.RemoveAt(i);
            }
        }

        private void UpdateVapeStats()
        {
            BatteryText.Text = $"Battery: {battery}%";
            LiquidText.Text = $"Liquid: {liquid}%";

            string coilCondition;

            if (coil > 50)
                coilCondition = "Good";
            else if (coil > 10)
                coilCondition = "Low";
            else
                coilCondition = "Burnt";

            CoilText.Text = $"Coil: {coilCondition}";
        }

        private readonly BitmapImage vapeImage = new BitmapImage(
        new Uri("https://images.vexels.com/media/users/3/220986/isolated/preview/e6ca9612ae8fe7560dec17409807f9f9-vape-e-cigarette-black.png")); 

        bool vaping;
        int vapeFrames;
        double glowMultiplier = 1.0;
        
        private void DrawVapeGlow(double width, double height)
        {
            double vapeWidth = 140;
            double vapeHeight = 140;

            double vapeX = (width - vapeWidth) / 2;
            double vapeY = height - vapeHeight;
            double currentGlow = glowMultiplier;

            if (battery < 20)
            {
                currentGlow *= 0.6 +
                    random.NextDouble() * 0.4;
            }

            double batteryFactor = battery / 100.0;
            double liquidFactor = liquid / 100.0;
            double vapePower = Math.Min(batteryFactor, liquidFactor);

            double glowSize = 280 * glowMultiplier * vapePower;

            double glowCenterX = vapeX + vapeWidth / 2;
            double glowCenterY = vapeY + vapeHeight * 0.25;

            Ellipse glow = new Ellipse
            {
                Width = glowSize,
                Height = glowSize,
                Fill = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.5, 0.5),
                    Center = new Point(0.5, 0.5),
                    RadiusX = 0.5,
                    RadiusY = 0.5,
                    GradientStops =
                    {
                        new GradientStop(
                            Color.FromArgb((byte)(120 * glowMultiplier * vapePower), 255, 180, 100), 0.0),
                        new GradientStop(
                            Color.FromArgb((byte)(60 * glowMultiplier), 255, 180, 100), 0.35),
                        new GradientStop(
                            Color.FromArgb(0, 255, 180, 100), 1.0)
                    }
                }
            };

            Canvas.SetLeft(glow, glowCenterX - glowSize / 2);
            Canvas.SetTop(glow, glowCenterY - glowSize / 2);

            GameCanvas.Children.Add(glow);

            Image vape = new Image
            {
                Source = vapeImage,
                Width = vapeWidth,
                Height = vapeHeight,
                Stretch = Stretch.Uniform
            };

            Canvas.SetLeft(vape, vapeX);
            Canvas.SetTop(vape, vapeY);

            GameCanvas.Children.Add(vape);

            if (vaping)
            {
                Ellipse vapor = new Ellipse
                {
                    Width = 120,
                    Height = 80,
                    Fill = new RadialGradientBrush(
                        Color.FromArgb(80, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255))
                };

                Canvas.SetLeft(vapor, width / 2 - 60);
                Canvas.SetTop(vapor, height - 250 - vapeFrames);

                GameCanvas.Children.Add(vapor);
            }
        }

        private double CastRay(double angle)
        {
            double distance = 0;

            while (distance < 20)
            {
                double testX =
                    playerX + Math.Cos(angle) * distance;

                double testY =
                    playerY + Math.Sin(angle) * distance;

                int mapX = (int)testX;
                int mapY = (int)testY;

                if (mapX < 0 ||
                    mapY < 0 ||
                    mapX >= MapWidth ||
                    mapY >= MapHeight)
                {
                    return 20;
                }

                if (map[mapY, mapX] == 1)
                {
                    return distance;
                }

                distance += 0.02;
            }

            return 20;
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (gameState == GameState.Splash)
            {
                ExitSplash();
                return;
            }

            if (e.Key == Key.C)
            {
                ControlsPanel.Visibility =
                    ControlsPanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }

            if (e.Key == Key.Up)
                moveForward = true;

            if (e.Key == Key.Down)
                moveBackward = true;

            if (e.Key == Key.Left)
                moveLeft = true;

            if (e.Key == Key.Right)
                moveRight = true;

            if (e.Key == Key.Space && !vaping)
            {
                vaping = true;
                vapeFrames = Math.Max(5, battery / 3);

                if (coil < 50)
                {
                    vapeFrames = (int)(vapeFrames * 0.8);
                }

                if (coil < 10)
                {
                    vapeFrames = (int)(vapeFrames * 0.4);
                }

                UseVape();
                hasSmokeLoaded = true;
            }

            if ((e.Key == Key.LeftShift || e.Key == Key.RightShift) && !vapeDead && hasSmokeLoaded)
            {
                BlowVapeCloud();
                hasSmokeLoaded = false;
            }

            if (e.Key == Key.P && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (showMap)
                {
                    showMap = false;
                }
                else if (mapCharges > 0)
                {
                    mapCharges--;
                    showMap = true;
                    ShowNotification($"Map Used - {mapCharges} charge(s) left");
                }
                else
                {
                    ShowNotification("No map charges remaining!");
                }
            }

            if (leaderboardPromptVisible)
            {
                if (e.Key == Key.Enter)
                {
                    SubmitLeaderboardEntry();
                    return;
                }

                if (e.Key == Key.Back && leaderboardNameInput.Length > 0)
                {
                    leaderboardNameInput = leaderboardNameInput[..^1];
                    return;
                }

                if (e.Key >= Key.A && e.Key <= Key.Z)
                {
                    leaderboardNameInput += e.Key.ToString();
                }
                else if (e.Key >= Key.D0 && e.Key <= Key.D9)
                {
                    leaderboardNameInput += e.Key.ToString().Last();
                }

                return;
            }
        } 

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
                moveForward = false;

            if (e.Key == Key.Down)
                moveBackward = false;

            if (e.Key == Key.Left)
                moveLeft = false;

            if (e.Key == Key.Right)
                moveRight = false;
        }
    }
}
