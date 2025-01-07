using Character;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameManager
{
    public class GameData
    {
        public static Canvas GameCanvas;
        public static int SCREEN_WIDTH = 1280;
        public static int SCREEN_HEIGHT = 720;
        public static List<Level> Levels = new(3);

        public static Player Player;
        public static Point playerInitPosition;
        public static List<Enemy> Enemies = new();
        public static List<Cat> Cats = new();
        public static readonly byte MAX_CAT_COUNT = 3;

        public Stack<Item> UsedItems = new();

        public static Dictionary<Enemy, DispatcherTimer> enemyAttackTimers = new();
        public static DispatcherTimer enemyMovementTimer;
        public static DispatcherTimer animationTimer;
        public const int ANIMATION_INTERVAL = 200;

        public static readonly Random random = new();
        public static List<Type> enemyTypes = new() { typeof(ExhaustedStudent), typeof(ExcellentStudent) };
        private static readonly int MAX_ENEMY_COUNT = 3;
        private static bool isEnemiesSpawned = false;


        public const double SAFE_DISTANCE = Character.Character.character_size;
        public const double MAX_ATTACK_DISTANCE = Character.Character.character_size * 1.15;
        public const double MAX_ENEMY_ATTACK_DISTANCE = Character.Character.character_size;

        public static bool isWPressed = false;
        public static bool isSPressed = false;
        public static bool isAPressed = false;
        public static bool isDPressed = false;
        public static int lastDirection = 0;

        public static DateTime lastMoveTime = DateTime.Now;

        private static void InitializePlayer()
        {
            playerInitPosition = new Point(0, 0);
            Player = new Technician("Игрок", playerInitPosition);
        }

        private static void InitializeEnemies()
        {
            if (Enemies == null)
                return;

            for (int i = Enemies.Count; i < MAX_ENEMY_COUNT; ++i)
            {
                var enemyType = enemyTypes[random.Next(enemyTypes.Count)];
                string enemyName = enemyType == typeof(ExhaustedStudent) ? "Exhausted Student" : "Student on Scholarship";
                var enemy = (Enemy)Activator.CreateInstance(enemyType, new object[] { enemyName, new Point(random.Next(SCREEN_WIDTH), random.Next(SCREEN_HEIGHT)) });
                Enemies.Add(enemy);
            }
        }

        public static void InitializeEntities()
        {
            InitializePlayer();
            InitializeEnemies();
        }

        public static Image CreateTexture(Point position, ImageSource sprite)
        {
            Image img = new()
            {
                Source = sprite,
                Width = Character.Character.character_size,
                Height = Character.Character.character_size
            };
            Canvas.SetLeft(img, position.X);
            Canvas.SetTop(img, position.Y);
            return img;
        }
        public static TextBlock CreateEnemyHpText(Enemy enemy)
        {
            var enemyHpText = new TextBlock
            {
                FontSize = 14,
                Foreground = Brushes.White,
                Text = enemy.hp.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = Visibility.Hidden
            };

            Canvas.SetLeft(enemyHpText, enemy.position.X);
            Canvas.SetTop(enemyHpText, enemy.position.Y - Enemy.character_size / 2);

            return enemyHpText;
        }
    }
}