using Character;
using System.Diagnostics;
using System.IO;
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

        public static BitmapImage LoadImage(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show($"Ошибка: файл {path} не найден!");
                    return null;
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();

                if (image.PixelWidth == 0 || image.PixelHeight == 0)
                {
                    MessageBox.Show($"Ошибка: файл {path} загружен, но имеет размер 0x0!");
                    return null;
                }

                return image;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения {path}: {ex.Message}");
                return null;
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
            private List<Rect> rooms = new();

            private Level current_level;

            public static readonly byte MAX_CAT_COUNT = 3;
            public static readonly int PLAYER_VISIBILITY_RADIUS = (int)(Character.Character.character_size * 3.5);
            private readonly byte BLOCK_SIZE = Character.Character.character_size;
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

                if (!File.Exists(floorPath))
                {
                    MessageBox.Show($"Ошибка: файл {floorPath} не найден!");
                    return;
                }

                BitmapImage floorTileImage = new(new Uri(floorPath, UriKind.Absolute));

                while (currentY < height)
                {
                    while (currentX < width)
                    {
                        GameObject newTile = new FloorTile(floorTileImage, new Point(currentX, currentY));
                        level.StaticObjects.Add(newTile);

                        currentX += floorTileImage.PixelWidth;
                    }

                    currentX = 0;
                    currentY += floorTileImage.PixelHeight;
                }
            }

            public void InitializeGrid(int width, int height)
            {
                width = Math.Max(10, width);
                height = Math.Max(10, height);

                grid = new bool[width, height];

                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        grid[x, y] = false; // Пустая карта
            }

            public Level GenerateLevel(int levelIndex)
            {
                try
                {
                    int width = GameData.SCREEN_WIDTH, height = GameData.SCREEN_HEIGHT;
                    InitializeGrid(width, height);
                    int blockSize = Math.Max(1, (int)BLOCK_SIZE);
                    current_level = new Level(width * blockSize, height * blockSize, new Point(0, 0), new Point(width * blockSize, height * blockSize));

                    GenerateFloor(current_level);

                    MessageBox.Show("Генерация комнат...");
                    GenerateRooms();

                    MessageBox.Show("Соединение комнат...");
                    ConnectRooms(current_level);

                    MessageBox.Show("Генерация стен...");
                    GenerateWalls(current_level);

                    MessageBox.Show("Добавление деталей...");
                    AddDetails(current_level);

                    MessageBox.Show("Генерация динамических объектов...");
                    GenerateDynamicObjects(current_level);

                    MessageBox.Show("Уровень сгенерирован!");
                    return current_level;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в GenerateLevel(): {ex.Message}");
                    return null;
                }
            }

            private List<Rect> GenerateRooms()
            {
                List<Rect> rooms = new(ROOM_COUNT);
                int maxAttempts = 1000;

                for (int i = 0; i < ROOM_COUNT; i++)
                {
                    int roomWidthChunks = random.Next(8, 15);
                    int roomHeightChunks = random.Next(8, 15);
                    int attempts = 0;

                    int roomX, roomY;
                    do
                    {
                        var roomWidth = roomWidthChunks * BLOCK_SIZE;
                        var roomHeight = roomHeightChunks * BLOCK_SIZE;

                        roomX = random.Next(0, (int)width - roomWidth + 1);
                        roomY = random.Next(0, (int)height - roomHeight + 1);
                        attempts++;
                    } while ((roomX + roomWidthChunks * BLOCK_SIZE > GameData.SCREEN_WIDTH ||
                              roomY + roomHeightChunks * BLOCK_SIZE > GameData.SCREEN_HEIGHT) && attempts < maxAttempts);

                    if (attempts >= maxAttempts)
                    {
                        MessageBox.Show("Failed to generate a valid room within the maximum number of attempts.");
                        continue; 
                    }

                    if (IsRoomValid(grid, roomX, roomY, roomWidthChunks, roomHeightChunks))
                        CreateRoom(grid, roomX, roomY, roomWidthChunks, roomHeightChunks, current_level);
                }
                return rooms;
            }

            private void FillRoom(Rect room, Level level) 
            {
                string roomTilePath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_main_1_32px.png";
                BitmapImage roomTileImage = Level.LoadImage(roomTilePath);

                if (roomTileImage == null)
                    return;

                for (int x = (int)room.X / BLOCK_SIZE; x < (int)(room.X + room.Width) / BLOCK_SIZE; x++)
                {
                    for (int y = (int)room.Y / BLOCK_SIZE; y < (int)(room.Y + room.Height) / BLOCK_SIZE; y++)
                    {
                        if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
                        {
                            grid[x, y] = true;
                            GameObject roomTile = new RoomTile(roomTileImage, new Point(x * BLOCK_SIZE, y * BLOCK_SIZE));
                            level.StaticObjects.Add(roomTile);
                        }
                    }
                }
            }


            private void GenerateWalls(Level level)
            {
                string wallPath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_partition_32px.png";
                BitmapImage wallImage = Level.LoadImage(wallPath);
                if (wallImage == null) return;

                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    for (int y = 0; y < grid.GetLength(1); y++)
                    {
                        if (!grid[x, y] && HasWallNeighbor(x, y)) // Используем HasWallNeighbor
                        {
                            GameObject wall = new WallTile(wallImage, new Point(x * BLOCK_SIZE, y * BLOCK_SIZE));
                            level.StaticObjects.Add(wall);
                        }
                    }
                }
            }


            private bool IsNearFloor(int x, int y)
            {
                int width = grid.GetLength(0);
                int height = grid.GetLength(1);

                return (x > 0 && grid[x - 1, y]) ||
                       (x < width - 1 && grid[x + 1, y]) ||
                       (y > 0 && grid[x, y - 1]) ||
                       (y < height - 1 && grid[x, y + 1]);
            }

            private void PlaceWall(int x, int y)
            {
                if (current_level == null)
                {
                    MessageBox.Show("Ошибка: current_level не инициализирован!");
                    return;
                }

                string wallPath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_partition_32px.png";

                if (!File.Exists(wallPath))
                {
                    MessageBox.Show($"Ошибка: файл {wallPath} не найден!");
                    return;
                }

                BitmapImage wallImage = new BitmapImage(new Uri(wallPath, UriKind.Absolute));

                GameObject wall = new FloorTile(wallImage, new Point(x * BLOCK_SIZE, y * BLOCK_SIZE));
                current_level.StaticObjects.Add(wall);
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
                string roomTilePath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\images\level_objects\common\floor\floor_main_1.png";

                if (!File.Exists(roomTilePath))
                {
                    MessageBox.Show($"Ошибка: файл {roomTilePath} не найден!");
                    return;
                }

                BitmapImage roomTileImage = new BitmapImage(new Uri(roomTilePath, UriKind.Absolute));

                for (int i = x; i < x + width; i += BLOCK_SIZE)
                {
                    for (int j = y; j < y + height; j += BLOCK_SIZE)
                    {
                        grid[i, j] = true;
                        GameObject roomTile = new RoomTile(roomTileImage, new Point(i, j));
                        level.StaticObjects.Add(roomTile);
                    }
                }

                AddWallsAroundRoom(x, y, width, height, level);
            }

            private void AddWallsAroundRoom(int x, int y, int width, int height, Level level)
            {
                string wallPath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_main_1_32px.png";
                BitmapImage wallTileImage = Level.LoadImage(wallPath);
                if (wallTileImage == null) return;

                for (int i = x; i <= x + width; i++)
                {
                    level.StaticObjects.Add(new WallTile(wallTileImage, new Point(i * BLOCK_SIZE, (y - 1) * BLOCK_SIZE))); // Используем WallTile
                    level.StaticObjects.Add(new WallTile(wallTileImage, new Point(i * BLOCK_SIZE, (y + height) * BLOCK_SIZE))); // Используем WallTile
                }

                for (int j = y; j <= y + height; j++)
                {
                    level.StaticObjects.Add(new WallTile(wallTileImage, new Point((x - 1) * BLOCK_SIZE, j * BLOCK_SIZE))); // Используем WallTile
                    level.StaticObjects.Add(new WallTile(wallTileImage, new Point((x + width) * BLOCK_SIZE, j * BLOCK_SIZE))); // Используем WallTile
                }
            }

            private bool HasWallNeighbor(int x, int y)
            {
                int width = grid.GetLength(0);
                int height = grid.GetLength(1);

                // Проверяем наличие стены у соседних клеток
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (Math.Abs(dx) + Math.Abs(dy) != 1) continue; // Пропускаем текущую клетку
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height && grid[nx, ny]) return true;
                    }
                }
                return false;
            }

            private void ConnectRooms(Level level)
            {
                if (rooms.Count < 2) return;

                HashSet<Rect> connectedRooms = new();
                connectedRooms.Add(rooms[0]);

                while (connectedRooms.Count < rooms.Count)
                {
                    Rect? closestRoom = null;
                    Rect? fromRoom = null;
                    double minDistance = double.MaxValue;

                    foreach (var room in connectedRooms)
                    {
                        foreach (var targetRoom in rooms)
                        {
                            if (connectedRooms.Contains(targetRoom)) continue;

                            double distance = GetDistance(room, targetRoom);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestRoom = targetRoom;
                                fromRoom = room;
                            }
                        }
                    }

                    if (closestRoom.HasValue && fromRoom.HasValue)
                    {
                        Point center1 = GetRoomCenter(fromRoom.Value);
                        Point center2 = GetRoomCenter(closestRoom.Value);
                        CreateCorridor(level, center1, center2);
                        connectedRooms.Add(closestRoom.Value);
                    }
                }
            }

            private double GetDistance(Rect a, Rect b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }

            private Point GetRoomCenter(Rect room)
            {
                return new Point(room.X + room.Width / 2, room.Y + room.Height / 2);
            }
            private void CreateCorridor(Level level, Point p1, Point p2)
            {
                string corridorTilePath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\wall\wall_partition_32px.png";
                BitmapImage corridorTileImage = Level.LoadImage(corridorTilePath);
                if (corridorTileImage == null) return;

                // Рисуем коридор, добавляя FloorTile на канвас
                int x = (int)p1.X;
                int y = (int)p1.Y;

                while (x != (int)p2.X || y != (int)p2.Y)
                {
                    GameObject corridorTile = new FloorTile(corridorTileImage, new Point(x, y));
                    level.StaticObjects.Add(corridorTile);
                    grid[x / BLOCK_SIZE, y / BLOCK_SIZE] = true; //Заполняем grid

                    if (Math.Abs(x - p2.X) > Math.Abs(y - p2.Y)) x += Math.Sign(p2.X - x); else y += Math.Sign(p2.Y - y);
                }
                GameObject lastCorridorTile = new FloorTile(corridorTileImage, new Point((int)p2.X, (int)p2.Y));
                level.StaticObjects.Add(lastCorridorTile);
            }

            private bool AreRoomsAdjacent(Rect room1, Rect room2)
            {
                return (room1.X + room1.Width == room2.X || room2.X + room2.Width == room1.X ||
                        room1.Y + room1.Height == room2.Y || room2.Y + room2.Height == room1.Y);
            }


            private void DrawCorridor(Level level, Point p1, Point p2)
            {
                string corridorTilePath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\images\level_objects\common\floor\corridor_tile.png";
                BitmapImage corridorTileImage = Level.LoadImage(corridorTilePath);
                if (corridorTileImage == null) return;

                int xMin = Math.Min((int)p1.X, (int)p2.X);
                int xMax = Math.Max((int)p1.X, (int)p2.X);
                for (int x = xMin; x <= xMax; x += BLOCK_SIZE)
                {
                    GameObject corridorTile = new FloorTile(corridorTileImage, new Point(x, (int)p1.Y));
                    level.StaticObjects.Add(corridorTile);
                }

                int yMin = Math.Min((int)p1.Y, (int)p2.Y);
                int yMax = Math.Max((int)p1.Y, (int)p2.Y);
                for (int y = yMin; y <= yMax; y += BLOCK_SIZE)
                {
                    GameObject corridorTile = new FloorTile(corridorTileImage, new Point((int)p2.X, y));
                    level.StaticObjects.Add(corridorTile);
                }
            }

            private void AddDetails(Level level)
            {
                string doorPath = @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\level_objects\common\door\door_arch.png";

                if (!File.Exists(doorPath))
                {
                    MessageBox.Show($"Ошибка: файл {doorPath} не найден!");
                    return;
                }

                BitmapImage doorImage = new BitmapImage(new Uri(doorPath, UriKind.Absolute));

                foreach (var obj in level.StaticObjects)
                {
                    if (obj.Position.X > 0 && level.StaticObjects.Any(o => o.Position.X == obj.Position.X - 1 && o.Position.Y == obj.Position.Y))
                    {
                        var door = new Door(doorImage, new Point(obj.Position.X, obj.Position.Y));
                        level.StaticObjects.Add(door);
                    }
                }
            }


            public void RenderLevel(int levelIndex)
            {
                try
                {
                    if (GameData.Levels == null || levelIndex < 0 || levelIndex >= GameData.Levels.Count)
                    {
                        MessageBox.Show($"Ошибка: уровень {levelIndex} не существует");
                        return;
                    }

                    Level level = GameData.Levels[levelIndex];
                    if (level == null)
                    {
                        MessageBox.Show($"Ошибка: уровень {levelIndex} == null");
                        return;
                    }

                    if (level.StaticObjects == null || level.StaticObjects.Count == 0)
                    {
                        MessageBox.Show($"Ошибка: Уровень {levelIndex} не содержит статических объектов");
                        return;
                    }

                    if (level.DynamicObjects == null || level.DynamicObjects.Count == 0)
                    {
                        MessageBox.Show($"Ошибка: Уровень {levelIndex} не содержит динамических объектов");
                    }

                    foreach (var obj in level.StaticObjects)
                    {
                        obj.Render(canvas);
                        if (obj.RenderedElement == null)
                        {
                            MessageBox.Show($"Ошибка: объект {obj.GetType().Name} не имеет RenderedElement");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в RenderLevel(): {ex.Message}");
                }
            }
        }
    }
}