using System;
using System.IO;
using IOPath = System.IO.Path;
using System.Text.Json; 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging; 
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows.Input;

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
        private int[,]? previousMap;

        private int MapWidth = 12;
        private int MapHeight = 12;
        private int mapCharges = 4; 
        private bool showMap = false;
        private string notificationText = "";
        private int notificationFrames = 0;

        private int exitX;
        private int exitY;
        private int hallPassX;
        private int hallPassY;

        private bool vapeDeadZoneActive = false;

        private int vapeDeadZoneX;
        private int vapeDeadZoneY;

        private double vapeDeadZoneRadius = 2.5;

        private int vapeDrainCounter = 0;
        private bool hallucinationZoneActive = false;
        private double hallucinationIntensity = 0;
        private int hallucinationSeed = 0;
        private int hallucinationTimer = 0;
        private int smokePlumesSinceClear = 0;

        private bool mazeShifting = false;
        private int mazeShiftFrames = 0;

        private DispatcherTimer echoMazeTimer = null!;
        
        private readonly List<(int x, int y)> playerHistory = new();

        private bool memoryLoopActive = false;
        private int memoryLoopTimer = 0;

        private List<(int x, int y)> loopSequence = new();

        private int memoryLoopCooldown = 0;

        private Random loopRandom = new();
        private int loopTeleportCooldown = 0;

        private (int x, int y) teleportStart;
        private (int x, int y) teleportEnd;

        private double shakeIntensity = 0;

        private bool hallPassCollected = false;
        private readonly List<string> recoveredPasses = new();
        private string currentExplorerName = "";
        private string escapedExplorerName = "";
        private bool showJournal = false;

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
        private bool isInputLocked = false;

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
        private bool vapeFailureActive = false;

        private DateTime vapeFailureStart;

        private int vapeFailureLevel;

        private const int VapeFailureMinutes =  10;
        private const int VapeFailureLevels = 2;
        private bool hasSmokeLoaded = false;
        private readonly List<VapeCloud> clouds = new();
        private const double CloudSpeed = 0.06;

        private enum VapePickupType
        {
            Battery,
            Juice,
            Coil
        }

        private class VapePickup
        {
            public double X { get; set; }
            public double Y { get; set; }
            public VapePickupType Type { get; set; }
        }

        private readonly List<VapePickup> vapePickups = new List<VapePickup>();

        private readonly string[] firstNames =
        {
            "Ethan", "Maya", "Logan", "Avery", "Jules",
            "Carter", "Sienna", "Noah", "Dylan", "Lena",
            "Kai", "Milo", "Olivia", "Lucas", "Harper",
            "Wyatt", "Zoe", "Elijah", "Nora", "Caleb",
            "Ivy", "Mason", "Ruby", "Leo", "Violet",
            "Finn", "Hazel", "Jasper", "Aria", "Rowan",
            "Aiden", "Brooklyn", "Connor", "Delilah", "Emmett",
            "Freya", "Gavin", "Hannah", "Isaac", "Josephine",
            "Kieran", "Layla", "Micah", "Naomi", "Owen",
            "Piper", "Quinn", "Ryder", "Sadie", "Theo"
        };

        private readonly string[] lastNames =
        {
            "Frost", "Hollow", "Voss", "Black", "Mercer",
            "Vale", "Graves", "Ash", "Reed", "Hart",
            "Winters", "Crow", "Stone", "Drake", "Cross",
            "Wilde", "Hawthorne", "Blake", "Knight", "Fox",
            "Rivera", "Cole", "Brooks", "Pierce", "Shaw",
            "West", "Quinn", "Lane", "Thorne", "Maddox",
            "Holloway", "Sinclair", "Bennett", "Carver", "Sterling",
            "Vaughn", "Sawyer", "Monroe", "Bishop", "Fletcher",
            "Locke", "Griffin", "Hayes", "Palmer", "Baxter",
            "Sullivan", "Caldwell", "Harrison", "Wren", "Donovan"
        };

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
            About,
            Ending,
            GameOver
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
                "Ctrl + J → Toggle Journal\n" +
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

                "Version 2.21.13\n" +
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
            StartEchoMaze();
        }

        public MainWindow()
        { 
            InitializeComponent();
            GenerateExplorerNames();
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
            GenerateHallPass();
            GenerateVapeDeadZone();
            SpawnVapePickups();

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

        private void GenerateVapeDeadZone()
        {
            vapeDeadZoneActive = false;

            if (random.NextDouble() > 0.13)
                return;

            while (true)
            {
                int x = random.Next(1, MapWidth - 1);
                int y = random.Next(1, MapHeight - 1);

                if (map[y, x] != 0)
                    continue;

                if ((x == 1 && y == 1) ||
                    (x == exitX && y == exitY) ||
                    (x == hallPassX && y == hallPassY))
                    continue;

                vapeDeadZoneX = x;
                vapeDeadZoneY = y;
                vapeDeadZoneActive = true; 
                break;
            }
        }

        private bool PlayerInsideVapeDeadZone()
        {
            if (!vapeDeadZoneActive)
                return false;

            double dx = playerX - (vapeDeadZoneX + 0.5);
            double dy = playerY - (vapeDeadZoneY + 0.5);

            return Math.Sqrt(dx * dx + dy * dy) <= vapeDeadZoneRadius;
        }

        private string[] explorerNames = Array.Empty<string>();

        private void GenerateExplorerNames()
        {
            var random = new Random();
            var names = new HashSet<string>();

            while (names.Count < 50)
            {
                names.Add(
                    $"{firstNames[random.Next(firstNames.Length)]} " +
                    $"{lastNames[random.Next(lastNames.Length)]}");
            }

            explorerNames = names.ToArray();
        } 

        private void StartEchoMaze()
        {
            if (echoMazeTimer != null)
                return;


            echoMazeTimer = new DispatcherTimer();

            echoMazeTimer.Interval =
                TimeSpan.FromSeconds(
                    GetEchoShiftInterval()
                );


            echoMazeTimer.Tick += (s,e)=>
            {
               double chance = random.NextDouble();

                if (chance < GetMazeShiftChance())
                {
                    BeginMazeShift();
                }

                echoMazeTimer.Interval =
                    TimeSpan.FromSeconds(
                        GetEchoShiftInterval()
                    );
            };


            echoMazeTimer.Start();
        }

        private double GetMazeShiftChance()
        {
            if(currentLevel <= 10)
                return 0.35;  

            if(currentLevel <= 20)
                return 0.45;

            if(currentLevel <= 35)
                return 0.60;

            return 0.75;
        }

        private double GetEchoShiftInterval()
        {
            if(currentLevel <= 10)
                return random.Next(90, 150);

            if(currentLevel <=20)
                return random.Next(70, 120); 

            if(currentLevel <=35)
                return random.Next(50, 90);

             return random.Next(35, 70);
        }

        private void BeginMazeShift()
        {
            if(mazeShifting)
                return;


            mazeShifting=true;
            mazeShiftFrames=300; 
            shakeIntensity=8;

            ShowNotification(
                "The maze shifts..."
            );

            PerformMazeShift();

            if(random.NextDouble()<GetExitMoveChance())
            {
                RelocateExit();
            }
        }

        private double GetExitMoveChance()
        {
            if(currentLevel < 21)
                return 0;

            if(currentLevel < 36)
                return .20;

            return .30;
        }

        private void UpdateEchoMazeEffects()
        {

            if(mazeShiftFrames>0)
            {
                mazeShiftFrames--;

                shakeIntensity*=0.98;


                if(mazeShiftFrames<=0)
                {
                    mazeShifting=false;
                    shakeIntensity=0;
                }
            }

        } 

        private void PerformMazeShift()
        {
            previousMap = (int[,])map.Clone();
            int changes = GetShiftAmount();

            for(int i = 0; i < changes; i++)
            {
                int x = random.Next(1, MapWidth - 1);
                int y = random.Next(1, MapHeight - 1);

                if(x == 1 && y == 1)
                    continue;

                if(x == exitX &&
                y == exitY)
                    continue;

                if(x == hallPassX &&
                y == hallPassY)
                    continue;


                ToggleCorridor(x,y);
            }

            int playerTileX = (int)playerX;
            int playerTileY = (int)playerY;


            if(map[playerTileY, playerTileX] == 1)
            {
                RestorePreviousMaze();

                ShowNotification(
                    "The maze rejects the shift..."
                );

                return;
            }


            if(!MazeIsConnected())
            {
                RestorePreviousMaze();
                ShowNotification(
                    "The maze rejects the shift..."
                );
            }
            else
            {
                ShowNotification(
                    "The maze shifts..."
                );
            }
        }

    private void RestorePreviousMaze()
    {
        if(previousMap == null)
            return;


        map = (int[,])previousMap.Clone();
    }

        private int GetShiftAmount()
        {
            if(currentLevel<=10)
                return random.Next(2,4);


            if(currentLevel<=20)
                return random.Next(4,7);


            if(currentLevel<=35)
                return random.Next(6,11);


            return random.Next(8,15);
        }

        private void ToggleCorridor(int x,int y)
        {

            int old = map[y,x];

            if(old==1)
            {
                int neighbors=0;


                if(map[y+1,x]==0)
                    neighbors++;

                if(map[y-1,x]==0)
                    neighbors++;

                if(map[y,x+1]==0)
                    neighbors++;

                if(map[y,x-1]==0)
                    neighbors++;


                if(neighbors>=2)
                {
                    map[y,x]=0;
                }

            }

            else
            {

                int exits=0;


                if(map[y+1,x]==0)
                    exits++;

                if(map[y-1,x]==0)
                    exits++;

                if(map[y,x+1]==0)
                    exits++;

                if(map[y,x-1]==0)
                    exits++;

                if(exits<=1)
                    return;


                map[y,x]=1;
            }

        } 

        private void RelocateExit()
        {

            int oldX=exitX;
            int oldY=exitY;

            GenerateExit();

            if(
                Math.Abs(exitX-playerX)<5 &&
                Math.Abs(exitY-playerY)<5)
            {
                exitX=oldX;
                exitY=oldY;
                return;
            }

            ShowNotification(
                "The exit has moved."
            );

        }
        private bool MazeIsConnected()
        {

            bool[,] visited =
                new bool[MapHeight,MapWidth];


            Queue<(int,int)> queue=new();
            queue.Enqueue((1,1));
            visited[1,1]=true;

            while(queue.Count>0)
            {
                var p=queue.Dequeue();

                int[] dx =
                {
                    1,-1,0,0
                };

                int[] dy =
                {
                    0,0,1,-1
                };

                for(int i=0;i<4;i++)
                {

                    int nx=p.Item1+dx[i];
                    int ny=p.Item2+dy[i];

                    if(nx<0||
                    ny<0||
                    nx>=MapWidth||
                    ny>=MapHeight)
                    continue;

                    if(visited[ny,nx])
                        continue;

                    if(map[ny,nx]==1)
                        continue;


                    visited[ny,nx]=true;

                    queue.Enqueue((nx,ny));
                }

            }

            return visited[exitY,exitX] && visited[hallPassY,hallPassX];

        }

        private void TrackPlayerHistory()
        {
            if(currentLevel < 20 || currentLevel > 40)
                return;

            int tileX = (int)playerX;
            int tileY = (int)playerY;


            if(playerHistory.Count > 0)
            {
                var last = playerHistory[^1];

                if(last.x == tileX &&
                last.y == tileY)
                    return;
            }

            playerHistory.Add((tileX,tileY));

            if(playerHistory.Count > 80)
                playerHistory.RemoveAt(0);


            CheckForMemoryLoop();
        }

        private void CheckForMemoryLoop()
        {
            if(memoryLoopActive)
                return;


            if(playerHistory.Count < 18)
                return;


            int length = loopRandom.Next(6,10);


            var recent =
                playerHistory
                .Skip(playerHistory.Count-length)
                .ToList();


            int previousStart =
                playerHistory.Count - (length * 2);


            if(previousStart < 0)
                return;


            var previous =
                playerHistory
                .Skip(previousStart)
                .Take(length)
                .ToList();



            bool same = true;


            for(int i=0;i<length;i++)
            {
                if(recent[i] != previous[i])
                {
                    same=false;
                    break;
                }
            }


            if(same)
            {
                ActivateMemoryLoop(previous);
            }
        }

        private void ActivateMemoryLoop(List<(int,int)> sequence)
        {
            if(memoryLoopCooldown > 0) return;

            memoryLoopActive = true;
            memoryLoopTimer = 600; 
            loopSequence = sequence;
            teleportStart = sequence[0];
            teleportEnd = sequence[^1]; 
            loopTeleportCooldown = 120;

            ShowNotification(
                "Something feels wrong..."
            );
        }

        private void UpdateLoopTeleport()
        {
            if (!memoryLoopActive)
                return;

            if (loopTeleportCooldown > 0)
            {
                loopTeleportCooldown--;
                return;
            }

            int tileX = (int)playerX;
            int tileY = (int)playerY;

            if (tileX == teleportEnd.x &&
                tileY == teleportEnd.y)
            {
                playerX = teleportStart.x + 0.5;
                playerY = teleportStart.y + 0.5;
                loopTeleportCooldown = 90;

                ShowNotification(
                    "..."
                );
            }
        }

        private void UpdateMemoryLoop()
        {
            if(memoryLoopCooldown > 0)
            {
                memoryLoopCooldown--;
            }

            if(!memoryLoopActive)
                return;

            memoryLoopTimer--;

            if(loopSequence.Count > 0)
            {
                var current =
                    ((int)playerX,(int)playerY);

                if(!loopSequence.Contains(current))
                {
                    BreakMemoryLoop();
                    return;
                }
            }

            if(memoryLoopTimer <=0)
            {
                BreakMemoryLoop();
            }
        }

        private void BreakMemoryLoop()
        {
            memoryLoopActive=false;
            loopSequence.Clear();
            playerHistory.Clear();
            memoryLoopCooldown = 900; 

            ShowNotification(
                "The corridor finally changes..."
            );
        }
        private void GenerateHallPass()
        {
            int bestX = -1;
            int bestY = -1;
            int bestDistance = -1;

            for (int y = 1; y < MapHeight - 1; y++)
            {
                for (int x = 1; x < MapWidth - 1; x++)
                {
                    if (map[y, x] != 0)
                        continue;

                    if (x == exitX && y == exitY)
                        continue;

                    int distance = Math.Abs(x - 1) + Math.Abs(y - 1);

                    if (distance > bestDistance)
                    {
                        bestDistance = distance;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            hallPassX = bestX;
            hallPassY = bestY;
            hallPassCollected = false;

            currentExplorerName =
                explorerNames[random.Next(explorerNames.Length)];

            ShowNotification(
                $"Missing Vapor Report:\n" +
                $"{currentExplorerName}'s Hall Pass detected.");
        }
        private void DrawHallPassLight(double width, double height)
        {
            if (hallPassCollected)
                return;

            double dx =
                hallPassX + 0.5 - playerX;

            double dy =
                hallPassY + 0.5 - playerY;

            double distance =
                Math.Sqrt(dx * dx + dy * dy);

            if (distance < 4)
            {
                ShowNotification(
                    "You sense traces of another explorer...",
                    30);
            }
            
            if (distance > 3.0)
                return;

            if (!HasLineOfSightToHallPass())
                return;
    
            double angle =
                Math.Atan2(dy, dx) -
                playerAngle;

            while (angle < -Math.PI)
                angle += Math.PI * 2;

            while (angle > Math.PI)
                angle -= Math.PI * 2;

            double fov = Math.PI / 7;

            if (Math.Abs(angle) > fov / 2)
                return;

            double screenX =
                (angle / (fov / 2) + 1) *
                (width / 2);

            double size =
                Math.Max(10,
                140 / (distance + 0.5));

            Rectangle card = new Rectangle
            {
                Width = size * 0.6,
                Height = size,
                RadiusX = 3,
                RadiusY = 3,

                Fill = Brushes.Gold
            };

            Canvas.SetLeft(
                card,
                screenX - card.Width / 2);

            Canvas.SetTop(
                card,
                height / 2 - card.Height / 2);

            GameCanvas.Children.Add(card);
        }

        private bool HasLineOfSightToHallPass()
        {
            double dx = (hallPassX + 0.5) - playerX;
            double dy = (hallPassY + 0.5) - playerY;

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

                if (mx < 0 || my < 0 ||
                    mx >= MapWidth || my >= MapHeight)
                    return false;

                if (map[my, mx] == 1)
                    return false;
            }

            return true;
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
                escapedExplorerName = name;

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
            switch (gameState)
            {
                case GameState.Playing:

                    if (notificationFrames > 0)
                        notificationFrames--;

                    UpdatePlayer();
                    UpdateMemoryLoop();
                    UpdateLoopTeleport();

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
                    UpdateEchoMazeEffects();
                    RenderScene();
                    break;

                case GameState.Ending:
                    ShowEndingScreen();
                    break;

                case GameState.GameOver:
                    ShowGameOverScreen();
                    break;
            }
        }

        private void UpdatePlayer()
        {
            double newX = playerX;
            double newY = playerY;

            if (moveForward)
            {
                double speed =
                mazeShifting ?
                MoveSpeed * .45 :
                MoveSpeed; 

                newX += Math.Cos(playerAngle) * speed;
                newY += Math.Sin(playerAngle) * MoveSpeed;
            }

            if (moveBackward)
            {
                double speed =
                mazeShifting ?
                MoveSpeed * .45 :
                MoveSpeed;

                newX += Math.Cos(playerAngle) * speed;
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

            if (!hallPassCollected &&
                (int)playerX == hallPassX &&
                (int)playerY == hallPassY)
            {
                hallPassCollected = true;
                recoveredPasses.Add($"{currentExplorerName} - Level {currentLevel}");

                ShowNotification(
                    $"Recovered Hall Pass\n" +
                    $"{currentExplorerName}");
            }

            CheckVapePickupCollection();
            UpdateVapeFailure();

            if ((int)playerX == exitX && (int)playerY == exitY)
            { 
                if (!hallPassCollected)
                {
                    ShowNotification(
                        "Exit Locked\n" +
                        "Hall Pass Required");

                    return;
                }

                mazesCompleted++;

                if (vapeFailureActive)
                {
                    GameOver();
                    return;
                }

                if (currentLevel >= 50)
                {
                    GameEnd();
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
                showJournal = false;
            }  

            if (PlayerInsideVapeDeadZone())
            {
                vapeDrainCounter++;

                if (vapeDrainCounter >= 60)
                {
                    vapeDrainCounter = 0;

                    battery = Math.Max(0, battery - 1);
                    liquid = Math.Max(0, liquid - 1);
                    coil = Math.Max(0, coil - 1);

                    UpdateVapeStats();
                }
            }
            else
            {
                vapeDrainCounter = 0;
            }

            double smoke = CalculateLocalSmokeDensity();

            bool heavyVapeState =
                vaping || vapeCount % 20 > 15;

            if (smokePlumesSinceClear >= 3 && smoke > 0.6 && heavyVapeState)
            {
                hallucinationZoneActive = true;

                double levelFactor = 1.0 + (currentLevel * 0.015);

                hallucinationIntensity = Math.Min(
                    1.0,
                    hallucinationIntensity + (0.01 * levelFactor)
                );

                hallucinationSeed = (int)DateTime.Now.Ticks;
            }
            else
            {
                hallucinationIntensity = Math.Max(0, hallucinationIntensity - 0.005);

                if (hallucinationIntensity <= 0.05)
                    hallucinationZoneActive = false;
            }

            if (hallucinationZoneActive)
                hallucinationTimer++;
            else
                hallucinationTimer = 0;

            TrackPlayerHistory();
            
        }

        private void GameOver()
        {
            moveForward = false;
            moveBackward = false;
            moveLeft = false;
            moveRight = false;

            showMap = false;
            showJournal = false;
            ControlsPanel.Visibility = Visibility.Collapsed;
            ResetVapeStats();

            gameState = GameState.GameOver; 
        }
        private void GameEnd()
        {
            moveForward = false;
            moveBackward = false;
            moveLeft = false;
            moveRight = false;

            showMap = false;
            showJournal = false;
            ControlsPanel.Visibility = Visibility.Collapsed;
            ResetVapeStats();

            leaderboardNameInput = "";
            leaderboardPromptVisible = true;

            gameState = GameState.Ending; 
        }

        private void ShowGameOverScreen()
        {
            SetGameplayUIVisible(false);
            GameCanvas.Children.Clear();

            Rectangle bg = new Rectangle
            {
                Width = ActualWidth,
                Height = ActualHeight,
                Fill = Brushes.Black
            };

            GameCanvas.Children.Add(bg);

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock gameOverText = new TextBlock
            {
                Text =
                    "YOUR VAPE HAS FAILED\n\n" +

                    "The last glow fades into the fog...\n\n" +

                    "The smoke escapes into the darkness...\n\n" +

                    "The maze seals itself around you.\n\n\n" +

                    $"Level Reached: {currentLevel}\n\n" +

                    $"Mazes Completed: {mazesCompleted}\n\n" +

                    $"Recovered Hall Passes: {recoveredPasses.Count}\n\n" +

                    "Press ENTER to Return to Menu",

                Foreground = Brushes.White,
                FontSize = 16,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            content.Children.Add(gameOverText);

            content.Measure(
                new Size(
                    double.PositiveInfinity,
                    double.PositiveInfinity));

            Canvas.SetLeft(
                content,
                (ActualWidth - content.DesiredSize.Width) / 2);

            Canvas.SetTop(
                content,
                (ActualHeight - content.DesiredSize.Height) / 2);

            GameCanvas.Children.Add(content);
        }

        private void ShowEndingScreen()
        {
            SetGameplayUIVisible(false); 
            GameCanvas.Children.Clear();

            Rectangle bg = new Rectangle
            {
                Width = ActualWidth,
                Height = ActualHeight,
                Fill = Brushes.Black
            };

            GameCanvas.Children.Add(bg);

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock endingText = new TextBlock
            {
                Text =
                    "YOU ESCAPED VAPE SCAPE\n\n" +
                    "Few explorers make it this far.\n\n" +
                    $"Mazes Completed: {mazesCompleted}\n\n" +
                    "Claim your place in the Hall of Vapors.\n" +
                    "Submit your name and press ENTER:",

                Foreground = Brushes.White,
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            content.Children.Add(endingText);

            Border inputBox = new Border
            {
                Width = 300,
                Height = 50,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1)
            };

            TextBlock inputText = new TextBlock
            {
                Text = string.IsNullOrEmpty(leaderboardNameInput)
                    ? "_"
                    : ToSentenceCase(leaderboardNameInput), 

                Foreground = Brushes.LightGreen,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            inputBox.Child = inputText;

            content.Children.Add(inputBox);

            content.Measure(
                new Size(
                    double.PositiveInfinity,
                    double.PositiveInfinity));

            Canvas.SetLeft(
                content,
                (ActualWidth - content.DesiredSize.Width) / 2);

            Canvas.SetTop(
                content,
                (ActualHeight - content.DesiredSize.Height) / 2);

            GameCanvas.Children.Add(content);
        }

        private void ShowLostJournalScreen()
        {
            SetGameplayUIVisible(false); 
            GameCanvas.Children.Clear();
            isInputLocked = true;

            Rectangle bg = new Rectangle
            {
                Width = ActualWidth,
                Height = ActualHeight,
                Fill = Brushes.Black
            };

            GameCanvas.Children.Add(bg);

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            DrawJournalOverlay();

            TextBlock endingText = new TextBlock
            {
                Text =
                    "…this entry was never meant to be read\n" +
                    "but it is being shown anyway.\n\n" +
                    "The journal is no longer passive.\n" +
                    "Something is choosing what exists.\n\n" +
                    "Do not trust what remains.\n",

                Foreground = Brushes.White,
                FontSize = 15,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };  

            endingText.Opacity = 0;
            endingText.RenderTransformOrigin = new Point(0.5, 0.5);

            var transform = new ScaleTransform(1.0, 1.0);
            endingText.RenderTransform = transform;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(2),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(fadeIn, endingText);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(TextBlock.OpacityProperty));

            var scaleX = new DoubleAnimation
            {
                From = 1.0,
                To = 1.03,
                Duration = TimeSpan.FromSeconds(3),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            var scaleY = scaleX.Clone();

            Storyboard.SetTarget(scaleX, endingText);
            Storyboard.SetTarget(scaleY, endingText);

            Storyboard.SetTargetProperty(scaleX, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("RenderTransform.ScaleY"));

            var sb = new Storyboard();
            sb.Children.Add(fadeIn);
            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);

            sb.Begin();

            var flickerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };

            var rand = new Random();

            flickerTimer.Tick += (s, e) =>
            {
                double baseOpacity = 1.0;

                double flicker = rand.NextDouble() > 0.85
                    ? rand.NextDouble() * 0.4
                    : 0;

                endingText.Opacity = baseOpacity - flicker;
            };

            flickerTimer.Start();

            content.Children.Add(endingText);  

            content.Measure(
                new Size(
                    double.PositiveInfinity,
                    double.PositiveInfinity));

            Canvas.SetLeft(
                content,
                (ActualWidth - content.DesiredSize.Width) / 2);

            Canvas.SetTop(
                content,
                (ActualHeight - content.DesiredSize.Height) / 2);

            GameCanvas.Children.Add(content);

            var returnTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };

            returnTimer.Tick += (s, e) =>
            {
                returnTimer.Stop();
                isInputLocked = false;
                gameState = GameState.Menu;
                ShowMainMenu();
            };

            returnTimer.Start();
        }

        private string ToSentenceCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim().ToLower();

            string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] =
                        char.ToUpper(words[i][0]) +
                        words[i].Substring(1);
                }
            }

            return string.Join(" ", words);
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

                if(memoryLoopActive)
                {
                    shade =
                        (byte)Math.Max(
                            0,
                            shade + Math.Sin(
                                Environment.TickCount * 0.01) * 20);
                }

                Canvas.SetLeft(wall, x);
                Canvas.SetTop(wall, top);

                GameCanvas.Children.Add(wall);
            }

            if (HasLineOfSightToExit())
            {
                DrawExitLight(width, height);
            }

            DrawHallPassLight(width, height);
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
            DrawVapePickups(width, height);
            GameCanvas.Children.Add(vignette);

            if (hallucinationZoneActive)
            {
                Rectangle distortion = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)(40 * hallucinationIntensity),
                        120,
                        180,
                        255))
                };

                GameCanvas.Children.Add(distortion);
            }

            if(memoryLoopActive)
            {
                Rectangle loopEffect =
                    new Rectangle
                    {
                        Width = width,
                        Height = height,

                        Fill =
                        new SolidColorBrush(
                            Color.FromArgb(
                                35,
                                120,
                                120,
                                120))
                    };


                GameCanvas.Children.Add(loopEffect);
            }

            if (showMap)
            {
                DrawMapOverlay();
            }

            if (showJournal)
            {
                DrawJournalOverlay();
            }

            if(mazeShifting)
            {
                Rectangle dust =
                new Rectangle
                {
                    Width=width,
                    Height=height,
                    Fill=
                    new SolidColorBrush(
                        Color.FromArgb(
                            25,
                            180,
                            160,
                            120))
                };


                GameCanvas.Children.Add(dust);
            }

            DrawNotification();

            if (leaderboardPromptVisible)
            {   
                DrawLeaderboardPrompt();
            }

            if (PlayerInsideVapeDeadZone())
            {
                byte alpha = (byte)(
                    55 +
                    Math.Abs(Math.Sin(Environment.TickCount * 0.002)) * 25);

                Rectangle interference = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new SolidColorBrush(
                        Color.FromArgb(alpha, 20, 5, 5))
                };

                GameCanvas.Children.Add(interference);
            }

            if (hallucinationZoneActive)
            {
                DrawHallucinationEchoes(width, height);
                DrawFalseExitFlicker(width, height);
            }
            
        }

        private void DrawHallucinationEchoes(double width, double height)
        {
            Random rng = new Random(hallucinationSeed);

            int echoes = rng.NextDouble() < hallucinationIntensity
                ? 1 + rng.Next(2)
                : 0;

            for (int i = 0; i < echoes; i++)
            {
                double angle = rng.NextDouble() * Math.PI * 2;
                double distance = 260 + rng.NextDouble() * 160;

                double screenX = width / 2 + Math.Cos(angle) * distance;
                double screenY = height / 2 + Math.Sin(angle) * distance;

                Canvas smoke = new Canvas();

                byte baseAlpha = (byte)(70 * hallucinationIntensity * rng.NextDouble());

                double coreX = 20 + rng.NextDouble() * 20;
                double coreY = 20 + rng.NextDouble() * 40;

                int blobs = 18 + rng.Next(10);

                for (int b = 0; b < blobs; b++)
                {
                    double size = 18 + rng.NextDouble() * 50;

                    double t = rng.NextDouble();

                    double targetX = coreX + Math.Sin(b * 0.6) * 10;
                    double targetY = coreY + b * 2.2;

                    double x = rng.Next(-25, 45) * (1 - t) + targetX * t;
                    double y = rng.Next(-30, 80) * (1 - t) + targetY * t;

                    double density = 1.0 - Math.Abs(b - blobs / 2.0) / blobs;
                    byte alpha = (byte)(baseAlpha * density * rng.NextDouble());

                    Ellipse puff = new Ellipse
                    {
                        Width = size * (0.6 + rng.NextDouble() * 0.5),
                        Height = size * (1.4 + rng.NextDouble() * 1.2),

                        Fill = new SolidColorBrush(Color.FromArgb(
                            alpha,
                            8, 8, 8))
                    };

                    Canvas.SetLeft(puff, x);
                    Canvas.SetTop(puff, y);

                    smoke.Children.Add(puff);
                }

                int wisps = 10 + rng.Next(6);

                for (int w = 0; w < wisps; w++)
                {
                    double startX = coreX + rng.NextDouble() * 20;

                    Line wisp = new Line
                    {
                        X1 = startX,
                        Y1 = coreY + rng.NextDouble() * 20,
                        X2 = startX + rng.NextDouble() * 20 - 10,
                        Y2 = coreY + 90 + rng.NextDouble() * 80,

                        Stroke = new SolidColorBrush(Color.FromArgb(
                            (byte)(baseAlpha / 3),
                            6, 6, 6)),

                        StrokeThickness = 1
                    };

                    smoke.Children.Add(wisp);
                }

                smoke.RenderTransform = new RotateTransform(
                    rng.Next(-8, 8),
                    25,
                    25);

                Canvas.SetLeft(smoke, screenX);
                Canvas.SetTop(smoke, screenY);

                GameCanvas.Children.Add(smoke);
            }
        }
        private void DrawFalseExitFlicker(double width, double height)
        {
            if (random.NextDouble() > hallucinationIntensity)
                return;

            double fakeX = width * (0.3 + random.NextDouble() * 0.4);
            double fakeY = height * (0.3 + random.NextDouble() * 0.4);

            byte coreAlpha = (byte)(40 + random.Next(40)); 

            Color center = Color.FromArgb(coreAlpha, 40, 40, 40);   
            Color edge   = Color.FromArgb(0, 20, 20, 20);           

            Ellipse fakeExit = new Ellipse
            {
                Width = 50 + random.Next(0, 70),
                Height = 50 + random.Next(0, 70),

                Fill = new RadialGradientBrush(center, edge)
                {
                    RadiusX = 0.6 + random.NextDouble() * 0.2,
                    RadiusY = 0.6 + random.NextDouble() * 0.2,
                    Opacity = 0.25 + random.NextDouble() * 0.25
                }
            };

            Canvas.SetLeft(fakeExit, fakeX);
            Canvas.SetTop(fakeExit, fakeY);

            GameCanvas.Children.Add(fakeExit);
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

        private void DrawJournalOverlay()
        {   
            int entryCount =
                (recoveredPasses?.Count ?? 0) +
                (gameState == GameState.Ending ? explorerNames.Length : 0) +
                2; 

            double baseHeight = 80;
            double entryHeight = 18; 
            double computedHeight = baseHeight + (entryCount * entryHeight);

            double finalHeight = Math.Min(computedHeight, 500); 

            Border panel = new Border
            {
                Width = 300,
                Height = finalHeight,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            StackPanel content = new StackPanel
            {
                Margin = new Thickness(10)
            };

            content.Children.Add(new TextBlock
            {
                Text = "Missing Vapors Journal",
                Foreground = Brushes.Gold,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            if (recoveredPasses?.Count == 0 && gameState != GameState.Ending)
            {
                content.Children.Add(new TextBlock
                {
                    Text = "No Hall Passes recovered.",
                    Foreground = Brushes.White,
                    FontSize = 12
                });
            }   
            else
            {
                foreach (string pass in recoveredPasses!.TakeLast(10).Reverse())
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = pass,
                        Foreground = Brushes.White,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }

            if (gameState == GameState.Ending)
            {
                int recoveredCount = recoveredPasses?.Count ?? 0;
                int fillerCount = Math.Max(0, 50 - recoveredCount);

                foreach (string entry in GenerateExplorerEntries(fillerCount))
                {
                    bool isPlayerEntry =
                        !string.IsNullOrWhiteSpace(escapedExplorerName) &&
                        entry.StartsWith(
                            ToSentenceCase(escapedExplorerName),
                            StringComparison.OrdinalIgnoreCase);

                    var tb = new TextBlock
                    {
                        Text = entry,
                        Foreground = isPlayerEntry ? Brushes.Silver : Brushes.Gray,
                        FontWeight = isPlayerEntry ? FontWeights.Bold : FontWeights.Normal,
                        Margin = new Thickness(0, 2, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    };

                    if (isPlayerEntry)
                    {
                        var rand = new Random();

                        var flickerTimer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(60)
                        };

                        flickerTimer.Tick += (s, e) =>
                        {
                            tb.Opacity = rand.NextDouble() > 0.15 ? 1.0 : rand.Next(2) * 0.3;
                        };

                        flickerTimer.Start();
                    }

                    content.Children.Add(tb);
                } 
            }
            
            scroll.Content = content;
            panel.Child = scroll;

            Canvas.SetLeft(panel, ActualWidth - panel.Width - 20);
            Canvas.SetTop(panel, ActualHeight - panel.Height - 40);

            GameCanvas.Children.Add(panel);
        } 
        private List<string> GenerateExplorerEntries(int maxLevel)
        {   
            Random rng = new Random();

            List<string> entries = new();

            HashSet<int> existingLevels = new();

            foreach (string pass in recoveredPasses)
            {
                Match match = Regex.Match(pass, @"Level\s+(\d+)");

                if (match.Success)
                {
                    existingLevels.Add(
                        int.Parse(match.Groups[1].Value));
                }
            }

            bool playerNameAdded = false;

            for (int level = 1; level <= maxLevel; level++)
            {
                if (existingLevels.Contains(level))
                    continue;

                string first;
                string last;

                if (!playerNameAdded &&
                    !string.IsNullOrWhiteSpace(escapedExplorerName))
                {
                    string playerName =
                        ToSentenceCase(escapedExplorerName).Trim();

                    string[] parts =
                        playerName.Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries);

                    first = parts[0];

                    last =
                        parts.Length > 1
                        ? string.Join(" ", parts.Skip(1))
                        : lastNames[rng.Next(lastNames.Length)];

                    playerNameAdded = true;
                }
                else
                {
                    first = firstNames[rng.Next(firstNames.Length)];
                    last = lastNames[rng.Next(lastNames.Length)];
                }

                entries.Add($"{first} {last} - Level {level}");
            }

            return entries;
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
            if (PlayerInsideVapeDeadZone())
            {
                ShowNotification("Your vape won't fire in this area.");
                return;
            }

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
            if (PlayerInsideVapeDeadZone())
            return;

            smokePlumesSinceClear++;
            
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
            if (clouds.Count == 0)
            {
                smokePlumesSinceClear = 0;
            }
        }

        private double CalculateLocalSmokeDensity()
        {
            double density = 0;

            foreach (var c in clouds)
            {
                double dx = c.X - playerX;
                double dy = c.Y - playerY;

                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < 3.5) 
                {
                    density += (1.0 - dist / 3.5) * c.Density;
                }
            }

            return Math.Min(1.0, density);
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

        private void ResetVapeStats()
        {
            battery = 100;
            liquid = 100;
            coil = 100;

            vapeDead = false;
            vapeFailureActive = false;
            vapeDrainCounter = 0;

            UpdateVapeStats();
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

        private double GetBatterySpawnChance()
        {
            if (currentLevel <= 10)
                return 0.45;

            if (currentLevel <= 20)
                return 0.35;

            if (currentLevel <= 30)
                return 0.27;

            if (currentLevel <= 40)
                return 0.20;

            return 0.14;
        }

        private double GetJuiceSpawnChance()
        {
            if (currentLevel <= 10)
                return 0.40;

            if (currentLevel <= 20)
                return 0.30;

            if (currentLevel <= 30)
                return 0.23;

            if (currentLevel <= 40)
                return 0.17;

            return 0.12;
        }

        private double GetCoilSpawnChance()
        {
            if (currentLevel <= 10)
                return 0.25;

            if (currentLevel <= 20)
                return 0.20;

            if (currentLevel <= 30)
                return 0.15;

            if (currentLevel <= 40)
                return 0.11;

            return 0.08;
        }

        private void SpawnVapePickups()
        {
            vapePickups.Clear();

            if (random.NextDouble() < GetBatterySpawnChance())
            {
                SpawnVapePickup(VapePickupType.Battery);
            }

            if (random.NextDouble() < GetJuiceSpawnChance())
            {
                SpawnVapePickup(VapePickupType.Juice);
            }

            if (random.NextDouble() < GetCoilSpawnChance())
            {
                SpawnVapePickup(VapePickupType.Coil);
            }
        }

        private void SpawnVapePickup(VapePickupType type)
        {
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = random.Next(1, MapWidth - 1);
                int y = random.Next(1, MapHeight - 1);

                if (map[y, x] != 0)
                    continue;

                double pickupX = x + 0.5;
                double pickupY = y + 0.5;

                double distanceFromPlayer =
                    Math.Sqrt(
                        Math.Pow(pickupX - playerX, 2) +
                        Math.Pow(pickupY - playerY, 2));

                if (distanceFromPlayer < 5)
                    continue;

                bool occupied = vapePickups.Any(p =>
                    Math.Abs(p.X - pickupX) < 0.1 &&
                    Math.Abs(p.Y - pickupY) < 0.1);

                if (occupied)
                    continue;

                vapePickups.Add(new VapePickup
                {
                    X = pickupX,
                    Y = pickupY,
                    Type = type
                });

                return;
            }
        }

        private void DrawVapePickups(double width, double height)
        {
            foreach (var pickup in vapePickups)
            {
                double dx = pickup.X - playerX;
                double dy = pickup.Y - playerY;

                double distance =
                    Math.Sqrt(dx * dx + dy * dy);

                if (distance < 0.1)
                    distance = 0.1;

                if (!HasLineOfSightToPickup(pickup.X, pickup.Y))
                    continue;

                double angleToPickup =
                    Math.Atan2(dy, dx) - playerAngle;

                while (angleToPickup < -Math.PI)
                    angleToPickup += Math.PI * 2;

                while (angleToPickup > Math.PI)
                    angleToPickup -= Math.PI * 2;

                double fov = Math.PI / 7;

                if (Math.Abs(angleToPickup) > fov / 2)
                    continue;

                double screenX =
                    (angleToPickup / (fov / 2) + 1) *
                    (width / 2);

                double size =
                    Math.Max(10, 55 / distance);

                size = Math.Min(size, 60);

                double screenY =
                    height / 2 - size / 2;

                DrawVapePickupObject(
                    pickup,
                    screenX,
                    screenY,
                    size,
                    distance);
            }
        }

        private void DrawVapePickupObject(
        VapePickup pickup,
        double screenX,
        double screenY,
        double size,
        double distance)
    {
        StackPanel panel = new StackPanel
        {
            Width = size,
            Height = size + 22,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Border icon = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size * 0.2),
            Background = Brushes.Black,
            BorderThickness = new Thickness(2),
            BorderBrush = GetPickupBrush(pickup.Type),
            Opacity = Math.Max(0.55, 1.0 - distance * 0.02)
        };

        TextBlock symbol = new TextBlock
        {
            Text = GetPickupSymbol(pickup.Type),
            FontSize = size * 0.55,
            FontWeight = FontWeights.Bold,
            Foreground = GetPickupBrush(pickup.Type),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        icon.Child = symbol;

        TextBlock label = new TextBlock
        {
            Text = GetPickupName(pickup.Type),
            FontSize = Math.Max(8, size * 0.22),
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        panel.Children.Add(icon);
        panel.Children.Add(label);

        Canvas.SetLeft(
            panel,
            screenX - size / 2);

        Canvas.SetTop(
            panel,
            screenY);

        GameCanvas.Children.Add(panel);
    }

    private Brush GetPickupBrush(VapePickupType type)
    {
        switch (type)
        {
            case VapePickupType.Battery:
                return Brushes.LimeGreen;

            case VapePickupType.Juice:
                return Brushes.DeepSkyBlue;

            case VapePickupType.Coil:
                return Brushes.Orange;

            default:
                return Brushes.White;
        }
    }

    private string GetPickupSymbol(VapePickupType type)
    {
        switch (type)
        {
            case VapePickupType.Battery:
                return "⚡";

            case VapePickupType.Juice:
                return "💧";

            case VapePickupType.Coil:
                return "♨";

            default:
                return "?";
        }
    }

    private string GetPickupName(VapePickupType type)
    {
        switch (type)
        {
            case VapePickupType.Battery:
                return "BATTERY";

            case VapePickupType.Juice:
                return "JUICE";

            case VapePickupType.Coil:
                return "COIL";

            default:
                return "ITEM";
        }
    }

    private bool HasLineOfSightToPickup(double targetX, double targetY)
    {
        double dx = targetX - playerX;
        double dy = targetY - playerY;

        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance <= 0.1)
            return true;

        double stepSize = 0.05;

        int steps = (int)(distance / stepSize);

        for (int i = 1; i < steps; i++)
        {
            double checkX = playerX + (dx * i / steps);
            double checkY = playerY + (dy * i / steps);

            int mapX = (int)Math.Floor(checkX);
            int mapY = (int)Math.Floor(checkY);

            if (mapX < 0 || mapX >= MapWidth ||
                mapY < 0 || mapY >= MapHeight)
            {
                return false;
            }

            if (map[mapY, mapX] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private void CheckVapePickupCollection()
    {
        for (int i = vapePickups.Count - 1; i >= 0; i--)
        {
            VapePickup pickup = vapePickups[i];

            double dx = pickup.X - playerX;
            double dy = pickup.Y - playerY;

            double distance =
                Math.Sqrt(dx * dx + dy * dy);

            if (distance > 0.6)
                continue;

            bool collected = false;

            switch (pickup.Type)
            {
                case VapePickupType.Battery:
                    if (battery < 100)
                    {
                        battery = Math.Min(100, battery + 25);
                        collected = true;

                        ShowNotification(
                            "Battery Recovered\n" +
                            $"Battery: {battery}%");
                    }

                    break;

                case VapePickupType.Juice:
                    if (liquid < 100)
                    {
                        liquid = Math.Min(100, liquid + 25);
                        collected = true;

                        ShowNotification(
                            "Juice Recovered\n" +
                            $"Liquid: {liquid}%");
                    }

                    break;

                case VapePickupType.Coil:
                    if (coil < 100)
                    {
                        coil = Math.Min(100, coil + 25);
                        collected = true;

                        ShowNotification(
                            "Coil Recovered\n" +
                            $"Coil: {coil}%");
                    }

                    break;
            }

            if (collected)
            {
                vapePickups.RemoveAt(i);

                if (battery > 0 && coil > 0)
                {
                    vapeDead = false;
                }

                UpdateVapeStats();
            }
        }
    }
        private void UpdateVapeFailure()
        { 
            int failedParts = 0;

            if (battery <= 0) failedParts++;
            if (liquid <= 0) failedParts++;
            if (coil <= 0) failedParts++;

            bool depleted = failedParts >= 2;

            if (!depleted)
            {
                vapeFailureActive = false;
                return;
            }

            if (!vapeFailureActive)
            {
                vapeFailureActive = true;
                vapeFailureStart = DateTime.Now;
                vapeFailureLevel = currentLevel;

                ShowNotification(
                    "WARNING\n" +
                    "Your vape has completely failed.\n" +
                    "You have limited time remaining."
                );

                return;
            }

            bool timeExpired =
                DateTime.Now - vapeFailureStart >=
                TimeSpan.FromMinutes(VapeFailureMinutes);

            bool levelExpired =
                currentLevel >= vapeFailureLevel + VapeFailureLevels;

            if (timeExpired || levelExpired)
            {
                GameOver();
            }

            if (vapeFailureActive && notificationFrames <= 0)
            {
                ShowNotification(
                    "WARNING\n" +
                    "Your vape has completely failed.\n" +
                    "You have limited time remaining.",
                    180);
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

            if (gameState == GameState.GameOver)
            {
                if (e.Key == Key.Enter)
                {
                    gameState = GameState.Menu;
                    vapeFailureActive = false;
                    ShowMainMenu();
                    return;
                }

                return;
            }

            if (gameState == GameState.Ending)
            {
                if (isInputLocked)
                return;
                
                if (e.Key == Key.Enter)
                { 
                    leaderboardNameInput = ToSentenceCase(leaderboardNameInput);
                    SubmitLeaderboardEntry();
                    leaderboard.RemoveAt(leaderboard.Count - 1);
            
                    ShowLostJournalScreen();
                    return;
                }

                if (e.Key == Key.Back &&
                    leaderboardNameInput.Length > 0)
                { 
                    leaderboardNameInput =
                        leaderboardNameInput[..^1];

                    ShowEndingScreen();
                    return;
                }

                if (e.Key == Key.Space)
                {
                    if (!string.IsNullOrWhiteSpace(leaderboardNameInput) &&
                        !leaderboardNameInput.EndsWith(" "))
                    {
                        leaderboardNameInput += " ";
                        ShowEndingScreen();
                    }

                    return;
                }

                if (e.Key >= Key.A && e.Key <= Key.Z)
                { 
                    leaderboardNameInput += e.Key.ToString();
                    ShowEndingScreen();
                    return;
                }

                if (e.Key >= Key.D0 && e.Key <= Key.D9)
                {
                    leaderboardNameInput += e.Key.ToString().Last();
                    ShowEndingScreen();
                    return;
                }
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
                if (PlayerInsideVapeDeadZone())
                {
                    ShowNotification("The dead zone suppresses your vape.");
                    return;
                }

                vaping = true;
                vapeFrames = Math.Max(5, battery / 3);

                if (coil < 50)
                    vapeFrames = (int)(vapeFrames * 0.8);

                if (coil < 10)
                    vapeFrames = (int)(vapeFrames * 0.4);

                UseVape();
                hasSmokeLoaded = true;
            }

            if ((e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                if (PlayerInsideVapeDeadZone())
                {
                    ShowNotification("Smoke cannot form inside the dead zone.");
                    return;
                }

                if (!vapeDead && hasSmokeLoaded)
                {
                    BlowVapeCloud();
                    hasSmokeLoaded = false;
                }
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

            if (e.Key == Key.J && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                showJournal = !showJournal;
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
