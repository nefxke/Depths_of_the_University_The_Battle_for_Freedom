using Character;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GameManager
{
    public class Level
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public List<GameObject> StaticObjects { get; set; }
        public List<Character.Character> DynamicObjects { get; set; }

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        public Level(double width, double height, Point startPoint, Point endPoint)
        {
            Width = width;
            Height = height;
            StartPoint = startPoint;
            EndPoint = endPoint;
            StaticObjects = new List<GameObject>();
            DynamicObjects = new List<Character.Character>();
        }

        public void UpdateLevelSize(Point startPoint, Point endPoint)
        {
            if (Width <= 0 || Height <= 0)
            {
                MessageBox.Show("Размеры уровня некорректны.");
                return;
            }

            if (GameData.Levels == null)
            {
                MessageBox.Show("Коллекция GameData.Levels не инициализирована.");
                return;
            }

            for (int i = 0; i < GameData.Levels.Count; ++i)
            {
                Level level = GameData.Levels[i];
                if (level != null)
                {
                    this.StartPoint = startPoint;
                    this.EndPoint = endPoint;
                    this.Width = endPoint.X - startPoint.X;
                    this.Height = endPoint.Y - startPoint.Y;

                    UpdateLevelObjectsSize();
                }
            }
        }

        private void UpdateLevelObjectsSize()
        {
            double oldWidth = Width;
            double oldHeight = Height;

            foreach (var obj in StaticObjects)
            {
                obj.Position = new Point(obj.Position.X * (Width / oldWidth), obj.Position.Y * (Height / oldHeight));
            }

            oldWidth = Width;
            oldHeight = Height;
        }
    }

    public class LevelManager
    {
        public static void AddLevel(Level level)
        {
            GameData.Levels.Add(level);
        }

        public void LoadLevel(LevelGenerator levelGenerator, int levelIndex)
        {
            try
            {
                if (levelIndex < 0 || levelIndex >= GameData.Levels.Count)
                {
                    MessageBox.Show($"Ошибка: индекс уровня {levelIndex} выходит за пределы допустимого диапазона.");
                    return;
                }

                Level level = levelGenerator.GenerateLevel(levelIndex);
                levelGenerator.RenderLevel(levelIndex);
                AddLevel(level);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void LoadNextLevel(LevelGenerator levelGenerator, byte previousLevelIndex)
        {
            if (GameData.Levels.Count <= 0 || GameData.Levels == null)
            {
                MessageBox.Show("Ошибка со списком уровней");
                return;
            }

            previousLevelIndex++;
            var nextLevelIndex = previousLevelIndex <= GameData.Levels.Count() ? previousLevelIndex : -1;
            if (nextLevelIndex == -1)
            {
                MessageBox.Show("Ошибка: индекс загружаемого уровня = " + nextLevelIndex);
                return;
            }

            LoadLevel(levelGenerator, nextLevelIndex);
        }

        public void UpdateLevelSize(Point startPoint, Point endPoint)
        {
            if (GameData.Levels != null)
            {
                foreach (var level in GameData.Levels.ToList())
                    level?.UpdateLevelSize(startPoint, endPoint);
            }
        }
    }

    public class LevelGenerator
    {
        private readonly double width;
        private readonly double height;
        private readonly Canvas canvas;
        private readonly Point startPoint;
        private readonly Point endPoint;
        private readonly Random random;
        private readonly LevelManager levelManager;

        private bool[,] grid;
        private List<Rect> rooms;

        private Level current_level;

        public static readonly byte MAX_CAT_COUNT = 3;
        public static readonly int PLAYER_VISIBILITY_RADIUS = (int)(Character.Character.character_size * 3.5);
        private readonly byte BLOCK_SIZE = Character.Character.character_size / 3;
        private readonly byte ROOM_COUNT = 1;

        public delegate void PlayerSpawnedHandler(Player player);
        public delegate void PlayerInitializedHandler(Player player);

        public event PlayerSpawnedHandler PlayerSpawned;
        public event PlayerInitializedHandler PlayerInitialized;

        public LevelGenerator(double width, double height, Canvas canvas, Point startPoint, Point endPoint)
        {
            this.width = width;
            this.height = height;
            this.canvas = canvas;
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            this.random = new Random();
            this.levelManager = new LevelManager();
        }

        private void GenerateFloor(Level level)
        {
            double currentX = 0;
            double currentY = 0;

            string floorPath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\floor\floor_main_1.png";
            BitmapImage floorTileImage = new(new Uri(floorPath, UriKind.Absolute));
            GameObject floorTile = new FloorTile(floorTileImage);
            double tileWidth = floorTile.Sprite.PixelWidth;
            double tileHeight = floorTile.Sprite.PixelHeight;

            while (currentY < height)
            {
                while (currentX < width)
                {
                    GameObject newTile = new FloorTile(floorTile.Sprite)
                    {
                        Position = new Point(currentX, currentY)
                    };
                    level.StaticObjects.Add(newTile);
                    currentX += tileWidth;
                }

                currentX = 0;
                currentY += tileHeight;
            }
        }

        public Level GenerateLevel(int levelIndex)
        {
            current_level = new Level(width, height, startPoint, endPoint);

            // Инициализация сетки
            grid = new bool[(int)width, (int)height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    grid[x, y] = false;
            }

            GenerateFloor(current_level);
            rooms = GenerateRooms();

            GenerateWalls(current_level);

            ConnectRooms(grid, current_level);

            GenerateDynamicObjects(current_level);

            return current_level;
        }

        private List<Rect> GenerateRooms()
        {
            List<Rect> rooms = new(ROOM_COUNT);

            for (int i = 0; i < ROOM_COUNT; i++)
            {
                int roomWidthChunks = random.Next(8, 15);            // Количество блоков по ширине
                int roomHeightChunks = random.Next(8, 15);           // Количество блоков по высоте

                int roomX = -1;
                int roomY = -1;

                do
                {
                    var roomWidth = roomWidthChunks * BLOCK_SIZE;
                    var roomHeight = roomHeightChunks * BLOCK_SIZE;

                    roomX = random.Next(0, (int)width - roomWidth + 1);
                    roomY = random.Next(0, (int)height - roomHeight + 1);

                } while (roomX + roomWidthChunks * BLOCK_SIZE > GameData.SCREEN_WIDTH || roomY + roomHeightChunks * BLOCK_SIZE > GameData.SCREEN_HEIGHT);

                if (IsRoomValid(grid, roomX, roomY, roomWidthChunks, roomHeightChunks))
                    CreateRoom(grid, roomX, roomY, roomWidthChunks, roomHeightChunks, current_level);
            }
            return rooms;
        }

        private void GenerateWallAroundRoom(Level level, Rect room)
        {
            string wallPath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_partition_32px.png";
            BitmapImage wallTileImage = new(new Uri(wallPath, UriKind.Absolute));

            int roomWidth = (int)(room.Width * BLOCK_SIZE);
            int roomHeight = (int)(room.Height * BLOCK_SIZE);

            int roomX = (int)(room.X % BLOCK_SIZE) * BLOCK_SIZE;
            int roomY = (int)(room.Y % BLOCK_SIZE) * BLOCK_SIZE;

            // Генерация стен вокруг комнаты
            for (int x = roomX; x <= roomX + roomWidth; x += BLOCK_SIZE)
            {
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(x, roomY - BLOCK_SIZE) });
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(x, roomY + roomHeight) });
            }

            for (int y = roomY; y <= roomY + roomHeight; y += BLOCK_SIZE)
            {
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(roomX - BLOCK_SIZE, y) });
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(roomX + roomWidth, y) });
            }
        }



        private void GenerateWalls(Level level)
        {
            if (rooms == null)
            {
                MessageBox.Show("rooms == null");
                return;
            }

            foreach (var room in rooms)
                GenerateWallAroundRoom(level, room);
        }

        private void GenerateDynamicObjects(Level level)
        {
            GameData.InitializeEntities();
            SpawnEnemies(level);
            SpawnCats(MAX_CAT_COUNT);
        }

        public void SpawnEnemies(Level level)
        {
            if (GameData.Enemies.Count <= 0 || GameData.Enemies == null)
            {
                MessageBox.Show("Ошибка при спавне врагов");
                return;
            }

            foreach (var entity in GameData.Enemies)
            {
                level.DynamicObjects.Add(entity);
            }

            foreach (var enemy in level.DynamicObjects.OfType<Enemy>())
            {
                DispatcherTimer attackTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(enemy.attackInterval)
                };

                attackTimer.Tick += (s, args) =>
                {
                    double distanceToPlayer = Player.CalculateDistance(GameData.Player.playerImage, enemy.enemyImage);
                    if (distanceToPlayer <= GameData.MAX_ENEMY_ATTACK_DISTANCE)
                    {
                        enemy.Attack(GameData.Player, distanceToPlayer, GameData.MAX_ATTACK_DISTANCE);
                    }
                };

                attackTimer.Start();
                GameData.enemyAttackTimers[enemy] = attackTimer;

                if (!canvas.Children.Contains(enemy.enemyImage))
                {
                    enemy.enemyImage = GameData.CreateTexture(enemy.position, enemy.enemySprites[0]);
                    canvas.Children.Add(enemy.enemyImage);
                }

                if (!canvas.Children.Contains(enemy.hpText))
                {
                    enemy.hpText = GameData.CreateEnemyHpText(enemy);
                    canvas.Children.Add(enemy.hpText);
                }
            }
        }

        public static void SpawnCats(byte maxCats)
        {
            try
            {
                if (GameData.Cats.Count < maxCats && GameData.Cats != null)
                {
                    int catsToSpawn = Math.Min(0, maxCats - GameData.Cats.Count);
                    for (int i = 0; i < catsToSpawn; ++i)
                    {
                        Point spawnPoint = GetValidSpawnPoint();
                        if (spawnPoint != new Point(-1, -1))
                        {
                            Cat cat = new Cat("cat", Character.Character.CharacterClass.Cat, spawnPoint);
                            GameData.Cats.Add(cat);
                            GameData.GameCanvas.Children.Add(cat.catImage);

                            Canvas.SetLeft(cat.catImage, spawnPoint.X);
                            Canvas.SetTop(cat.catImage, spawnPoint.Y);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при спавне котов: {ex.Message}");
            }
        }

        private static Point GetValidSpawnPoint()
        {
            byte maxAttempts = 100;
            byte currentAttempt = 0;
            double safeDistance = Character.Character.character_size;

            var width = (int)(GameData.GameCanvas.ActualWidth);
            var height = (int)(GameData.GameCanvas.ActualHeight);

            do
            {
                int x = new Random().Next(0, width);
                int y = new Random().Next(0, height);

                bool isSafePosition = true;
                foreach (var entity in GameData.Enemies)
                {
                    if (entity != null && Math.Abs(x - entity.position.X) <= safeDistance && Math.Abs(y - entity.position.Y) <= safeDistance)
                    {
                        isSafePosition = false;
                        break;
                    }
                }

                foreach (var entity in GameData.Cats)
                {
                    if (entity != null && Math.Abs(x - entity.position.X) <= safeDistance && Math.Abs(y - entity.position.Y) <= safeDistance)
                    {
                        isSafePosition = false;
                        break;
                    }
                }

                if (isSafePosition)
                {
                    return new Point(x, y);
                }

            } while (++currentAttempt < maxAttempts);

            return new Point(-1, -1);
        }

        public static void DeleteCat(Level level, Cat cat)
        {
            if (!GameData.GameCanvas.Children.Contains(cat.catImage) || cat == null)
                return;

            GameData.GameCanvas.Children.Remove(cat.catImage);
            GameData.Cats.Remove(cat);
        }

        private double GetOpacity(double distance)
        {
            return Math.Max(0, 1 - (distance / PLAYER_VISIBILITY_RADIUS) / 1.25);
        }

        private bool IsRoomValid(bool[,] grid, int x, int y, int width, int height)
        {
            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    if (grid[i, j]) // Комната пересекается с другой комнатой
                        return false;
                }
            }
            return true;
        }

        private void CreateRoom(bool[,] grid, int x, int y, int width, int height, Level level)
        {
            for (int i = x; i < x + width; i += BLOCK_SIZE)
            {
                for (int j = y; j < y + height; j += BLOCK_SIZE)
                    grid[i, j] = true;
            }

            AddWallsAroundRoom(x, y, width, height, level);
        }

        private void AddWallsAroundRoom(int x, int y, int width, int height, Level level)
        {
            string wallPath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_main_1_32px.png";
            BitmapImage wallTileImage = new(new Uri(wallPath, UriKind.Absolute));

            // Генерация стен вокруг комнаты
            for (int i = x; i <= x + width * BLOCK_SIZE; i += BLOCK_SIZE)
            {
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(i, y - BLOCK_SIZE) });
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(i, y + height * BLOCK_SIZE) });
            }

            for (int j = y; j <= y + height * BLOCK_SIZE; j += BLOCK_SIZE)
            {
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(x - BLOCK_SIZE, j) });
                level.StaticObjects.Add(new FloorTile(wallTileImage) { Position = new Point(x + width * BLOCK_SIZE, j) });
            }
        }

        private void ConnectRooms(bool[,] grid, Level level)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    if (AreRoomsAdjacent(rooms[i], rooms[j]))
                        CreateCorridor(grid, rooms[i], rooms[j], level);
                }
            }
        }

        private bool AreRoomsAdjacent(Rect room1, Rect room2)
        {
            return (room1.X + room1.Width == room2.X || room2.X + room2.Width == room1.X ||
                    room1.Y + room1.Height == room2.Y || room2.Y + room2.Height == room1.Y);
        }

        private void CreateCorridor(bool[,] grid, Rect room1, Rect room2, Level level)
        {
            // Находим центры комнат
            Point center1 = new(room1.X + room1.Width / 2, room1.Y + room1.Height / 2);
            Point center2 = new(room2.X + room2.Width / 2, room2.Y + room2.Height / 2);

            // Рисуем коридор между центрами комнат
            DrawCorridor(level, center1, center2);
        }

        private void DrawCorridor(Level level, Point p1, Point p2)
        {
            //Point t = p1;
            //double distance = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            //double frac = 1 / distance;
            //double ctr = 0;

            //while ((int)t.X != (int)p2.X || (int)t.Y != (int)p2.Y)
            //{
            //    t.X = p1.X + (p2.X - p1.X) * ctr;
            //    t.Y = p1.Y + (p2.Y - p1.Y) * ctr;
            //    ctr += frac;
            //    level.StaticObjects.Add(new FloorTile(new BitmapImage(new Uri(@"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_main.png", UriKind.Absolute)))
            //    {
            //        Position = t
            //    });
            //}
        }

        private void AddDetails(Level level)
        {
            //  двери
            foreach (var obj in level.StaticObjects)
            {
                if (obj.Position.X > 0 && level.StaticObjects.Any(o => o.Position.X == obj.Position.X - 1 && o.Position.Y == obj.Position.Y))
                {
                    var door = new Door(new BitmapImage(new Uri(@"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\door_arch.png", UriKind.Absolute)))
                    {
                        Position = new Point(obj.Position.X, obj.Position.Y)
                    };
                    level.StaticObjects.Add(door);
                }
                if (obj.Position.Y > 0 && level.StaticObjects.Any(o => o.Position.X == obj.Position.X && o.Position.Y == obj.Position.Y - 1))
                {
                    var door = new Door(new BitmapImage(new Uri(@"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\door\door_arch.png", UriKind.Absolute)))
                    {
                        Position = new Point(obj.Position.X, obj.Position.Y)
                    };
                    level.StaticObjects.Add(door);
                }
            }
        }




        public void RenderLevel(int levelIndex)
        {
            if (GameData.Levels == null || levelIndex < 0 || levelIndex >= GameData.Levels.Count)
            {
                MessageBox.Show($"Ошибка: уровень с индексом {levelIndex} не существует");
                return;
            }

            Level level = GameData.Levels[levelIndex];
            if (level == null)
            {
                MessageBox.Show($"Ошибка: уровень с индексом {levelIndex} не существует");
                return;
            }

            Point playerPosition = GameData.Player?.position ?? new Point(0, 0);

            int startX = 0;
            int startY = 0;
            int endX = (int)(playerPosition.X + PLAYER_VISIBILITY_RADIUS);
            int endY = (int)(playerPosition.Y + PLAYER_VISIBILITY_RADIUS);

            try
            {
                if (level.StaticObjects == null)
                {
                    MessageBox.Show($"Ошибка: на уровне {levelIndex} нет статических объектов");
                    return;
                }
                if (level.DynamicObjects == null)
                {
                    MessageBox.Show($"Ошибка: на уровне {levelIndex} нет динамических объектов");
                    return;
                }

                // Устанавливаем ZIndex для пола
                //foreach (var obj in level.StaticObjects)
                //{
                //    if (obj is FloorTile floorTile && floorTile.RenderedElement != null)
                //    {
                //        Canvas.SetZIndex(floorTile.RenderedElement, 0);
                //    }
                //}

                // Устанавливаем ZIndex для игрока и врагов
                //foreach (var obj in level.DynamicObjects)
                //{
                //    if (obj is Enemy enemy)
                //        Canvas.SetZIndex(enemy.enemyImage, 122);
                //    else if (obj is Cat cat)
                //        Canvas.SetZIndex(cat.catImage, 122);
                //    else if (obj is Player)
                //        Canvas.SetZIndex(GameData.Player.playerImage, 233);
                //}

                // Рендеринг объектов
                var objectsToAdd = new List<GameObject>();
                var objectsToRemove = new List<GameObject>();


                foreach (var obj in level.StaticObjects)
                {
                    double distance = Math.Sqrt(Math.Pow(obj.Position.X - playerPosition.X, 2) + Math.Pow(obj.Position.Y - playerPosition.Y, 2));
                    double opacity = GetOpacity(distance);

                    if (obj.Position.X >= startX && obj.Position.X <= endX && obj.Position.Y >= startY && obj.Position.Y <= endY)
                    {
                        if (!canvas.Children.Contains(obj.RenderedElement))
                        {
                            objectsToAdd.Add(obj);
                        }
                        else
                        {
                            obj.RenderedElement.Visibility = Visibility.Visible;
                            obj.RenderedElement.Opacity = opacity;
                        }
                    }
                    else
                    {
                        if (canvas.Children.Contains(obj.RenderedElement))
                        {
                            obj.RenderedElement.Visibility = Visibility.Hidden;
                        }
                    }
                }

                foreach (var obj in level.DynamicObjects)
                {
                    double distance = Math.Sqrt(Math.Pow(obj.position.X - playerPosition.X, 2) + Math.Pow(obj.position.Y - playerPosition.Y, 2));
                    double opacity = GetOpacity(distance);

                    if (obj is Enemy enemy)
                    {
                        if (obj.position.X >= startX && obj.position.X <= endX && obj.position.Y >= startY && obj.position.Y <= endY)
                        {
                            if (enemy.enemyImage == null)
                            {
                                enemy.enemyImage = GameData.CreateTexture(enemy.position, enemy.enemySprites[0]);
                                canvas.Children.Add(enemy.enemyImage);
                            }
                            else
                            {
                                enemy.enemyImage.Visibility = Visibility.Visible;
                                enemy.enemyImage.Opacity = opacity;
                            }

                            if (enemy.hpText == null)
                                canvas.Children.Add(enemy.hpText);

                        }
                        else
                        {
                            if (enemy.enemyImage != null)
                                enemy.enemyImage.Visibility = Visibility.Hidden;
                            if (enemy.hpText != null)
                                enemy.hpText.Visibility = Visibility.Hidden;
                        }
                    }
                    else if (obj is Cat cat)
                    {
                        if (cat.catImage == null)
                        {
                            cat.catImage = GameData.CreateTexture(cat.position, cat.catSprites[0]);
                            canvas.Children.Add(cat.catImage);
                        }
                        else
                        {
                            cat.catImage.Visibility = Visibility.Visible;
                            cat.catImage.Opacity = opacity;
                        }
                    }
                    else if (obj is Player)
                    {
                        if (GameData.Player.playerImage == null)
                        {
                            GameData.Player.playerImage = GameData.CreateTexture(GameData.Player.position, Player.playerSprites[0]);
                            canvas.Children.Add(GameData.Player.playerImage);
                        }
                        else
                        {
                            GameData.Player.playerImage.Visibility = Visibility.Visible;
                            GameData.Player.playerImage.Opacity = 1;
                            Canvas.SetZIndex(GameData.Player.playerImage, int.MaxValue);
                        }
                    }
                }

                foreach (var obj in objectsToAdd)
                {
                    obj.Render(canvas);
                }
                foreach (var obj in objectsToRemove)
                {
                    obj.RenderedElement.Visibility = Visibility.Hidden;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при рендеринге уровня {levelIndex}: {ex.Message}");
            }
        }
    }
}