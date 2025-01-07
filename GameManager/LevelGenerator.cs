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
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
            this.Width = endPoint.X - startPoint.X;
            this.Height = endPoint.Y - startPoint.Y;

            // Обновление размеров и позиции всех объектов уровня
            UpdateLevelObjectsSize();
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
        public void AddLevel(Level level)
        {
            GameData.Levels.Add(level);
        }

        public void LoadLevel(LevelGenerator levelGenerator, int levelIndex)
        {
            levelGenerator.GenerateLevel(levelIndex);
            levelGenerator.RenderLevel(levelIndex);
        }

        public void LoadNextLevel(LevelGenerator levelGenerator, byte previousLevelIndex)
        {
            var nextLevelIndex = ++previousLevelIndex <= GameData.Levels.Count() ? ++previousLevelIndex : -1;
            if (nextLevelIndex == -1)
            {
                MessageBox.Show("Ошибка: индекс загружаемого уровня = " + nextLevelIndex);
                return;
            }
        }

        public void UpdateLevelSize(Point startPoint, Point endPoint)
        {
            foreach (var level in GameData.Levels)
                level.UpdateLevelSize(startPoint, endPoint);
        }
    }

    public class LevelGenerator
    {
        private double width;
        private double height;
        private Canvas canvas;
        private Point startPoint;
        private Point endPoint;
        private Dictionary<string, GameObject> roomTiles;
        private Dictionary<string, List<GameObject>> roomChunks;
        private readonly Random random;

        public delegate void PlayerSpawnedHandler(Player player);
        public delegate void PlayerInitializedHandler(Player player);

        public event PlayerSpawnedHandler PlayerSpawned;
        public event PlayerInitializedHandler PlayerInitialized;

        public event Action<DispatcherTimer> AssignPlayerAnimationTickHandler;

        public static Point lastPlayerPosition;

        private static readonly double PLAYER_VISIBILITY_RADIUS = Character.Character.character_size * 3.5; // Радиус видимости вокруг игрока
        private static readonly byte MAX_CAT_COUNT = 3;

        public LevelGenerator(double width, double height, Canvas canvas, Point startPoint, Point endPoint)
        {
            this.width = width;
            this.height = height;
            this.canvas = canvas;
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            roomTiles = new Dictionary<string, GameObject>();
            roomChunks = new Dictionary<string, List<GameObject>>(); // Инициализация словаря для хранения кусочков комнат
            random = new Random();

            LoadRoomChunks();
        }


        private void LoadPlayerTexture()
        {
            if (Player.playerSprites != null && Player.playerSprites.Length > 0)
            {
                var playerImage = GameData.CreateTexture(GameData.Player.position, Player.playerSprites[0]);
                canvas.Children.Add(Player.playerImage);
                Canvas.SetZIndex(Player.playerImage, int.MaxValue);
            }
            else
            {
                MessageBox.Show("Ошибка: Спрайты игрока не инициализированы.");
            }
        }

        public virtual void OnPlayerSpawned(Player player)
        {
            PlayerSpawned?.Invoke(player);
        }

        public virtual void OnPlayerInitialized(Player player)
        {
            PlayerInitialized?.Invoke(player);
        }


        private void LoadRoomChunks()
        {
            string roomPath1 = "C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\level_objects\\common\\room\\room_exit_up\\";
            string roomPath2 = "C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\level_objects\\common\\room\\room_exit_down\\";

            LoadRoomChunksFromDirectory(roomPath1, "room_exit_up");
            LoadRoomChunksFromDirectory(roomPath2, "room_exit_down");
        }

        private void LoadRoomChunksFromDirectory(string directoryPath, string roomType)
        {
            string[] fileEntries = Directory.GetFiles(directoryPath);

            if (!roomChunks.ContainsKey(roomType))
            {
                roomChunks[roomType] = new List<GameObject>();
            }

            foreach (string fileName in fileEntries)
            {
                BitmapImage bitmap = new(new Uri(fileName, UriKind.Absolute));
                GameObject gameObject = new RoomTileChunk(bitmap, new Point(0, 0), bitmap.PixelWidth, bitmap.PixelHeight);
                roomChunks[roomType].Add(gameObject);
            }

            roomChunks[roomType] = roomChunks[roomType].OrderBy(chunk => Path.GetFileNameWithoutExtension(chunk.Sprite.UriSource.ToString())).ToList();
        }

        private void GenerateFloor(Level level)
        {
            double currentX = 0;
            double currentY = 0;
            BitmapImage floorTileImage = new(new Uri(@"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\level_objects\\common\\floor\\floor_main_1.png", UriKind.Absolute));
            GameObject floorTile = new FloorTile(floorTileImage);

            while (currentY < height)
            {
                double tileWidth = floorTile.Sprite.PixelWidth;
                double tileHeight = floorTile.Sprite.PixelHeight;

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

        private void GenerateRooms(Level level)
        {
            try
            {
                LoadRoomChunks();

                List<string> roomTypes = new List<string> { "room_exit_up", "room_exit_down" };
                List<GameObject> placedRooms = new List<GameObject>();
                byte roomCount = 2; // Установим количество комнат на 2

                if (roomTypes.Count == 0)
                {
                    MessageBox.Show("Ошибка: Нет доступных типов комнат для размещения.");
                    return;
                }

                for (int i = 0; i < roomCount; i++)
                {
                    string randomRoomType = roomTypes[random.Next(roomTypes.Count)];
                    if (!roomChunks.ContainsKey(randomRoomType) || roomChunks[randomRoomType].Count == 0)
                    {
                        MessageBox.Show($"Ошибка: Комната типа {randomRoomType} не найдена или пуста.");
                        continue;
                    }

                    GameObject room = roomChunks[randomRoomType][0]; // Используем первую картинку из списка
                    Point roomPosition = GetValidRoomPosition(room, placedRooms);

                    if (roomPosition != new Point(-1, -1))
                    {
                        room.Position = roomPosition;
                        level.StaticObjects.Add(room);
                        placedRooms.Add(room);

                        AddRoomColliders(room, roomPosition, level);
                        Console.WriteLine($"Добавлена комната: {room.GetType().Name} на позиции {room.Position}");
                    }
                    else
                    {
                        i--;
                    }
                }
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        public bool CheckCollision(Collider gameObjectCollider, Level level)
        {
            foreach (var obj in level.StaticObjects)
            {
                if (obj is Collider collider && gameObjectCollider.Intersects(collider))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddRoomColliders(GameObject room, Point roomPosition, Level level)
        {
            double roomWidth = room.Sprite.PixelWidth;
            double roomHeight = room.Sprite.PixelHeight;
            double doorWidth = 64;
            double doorHeight = 64;

            string roomName = room.Sprite.UriSource.ToString().Split('/').Last().Split('.').First();

            // Верхняя стена
            if (!roomName.Contains("room_exit_up"))
            {
                Collider topWall = new Collider(roomPosition.X, roomPosition.Y, roomWidth, 1);
                level.StaticObjects.Add(topWall);
                Console.WriteLine($"Добавлен коллайдер верхней стены: {topWall.Bounds}");
            }

            // Нижняя стена
            if (!roomName.Contains("room_exit_down"))
            {
                Collider bottomWall = new Collider(roomPosition.X, roomPosition.Y + roomHeight - 1, roomWidth, 1);
                level.StaticObjects.Add(bottomWall);
                Console.WriteLine($"Добавлен коллайдер нижней стены: {bottomWall.Bounds}");
            }

            // Левая стена
            if (!roomName.Contains("room_exit_left"))
            {
                Collider leftWall = new Collider(roomPosition.X, roomPosition.Y, 1, roomHeight);
                level.StaticObjects.Add(leftWall);
                Console.WriteLine($"Добавлен коллайдер левой стены: {leftWall.Bounds}");
            }

            // Правая стена
            if (!roomName.Contains("room_exit_right"))
            {
                Collider rightWall = new Collider(roomPosition.X + roomWidth - 1, roomPosition.Y, 1, roomHeight);
                level.StaticObjects.Add(rightWall);
                Console.WriteLine($"Добавлен коллайдер правой стены: {rightWall.Bounds}");
            }

            // Добавляем проем для двери
            if (roomName.Contains("room_exit_up"))
            {
                Collider topDoor = new Collider(roomPosition.X + (roomWidth - doorWidth) / 2, roomPosition.Y, doorWidth, doorHeight);
                level.StaticObjects.Add(topDoor);
                Console.WriteLine($"Добавлен коллайдер верхней двери: {topDoor.Bounds}");
            }
            else if (roomName.Contains("room_exit_down"))
            {
                Collider bottomDoor = new Collider(roomPosition.X + (roomWidth - doorWidth) / 2, roomPosition.Y + roomHeight - doorHeight, doorWidth, doorHeight);
                level.StaticObjects.Add(bottomDoor);
                Console.WriteLine($"Добавлен коллайдер нижней двери: {bottomDoor.Bounds}");
            }
            else if (roomName.Contains("room_exit_left"))
            {
                Collider leftDoor = new Collider(roomPosition.X, roomPosition.Y + (roomHeight - doorHeight) / 2, doorWidth, doorHeight);
                level.StaticObjects.Add(leftDoor);
                Console.WriteLine($"Добавлен коллайдер левой двери: {leftDoor.Bounds}");
            }
            else if (roomName.Contains("room_exit_right"))
            {
                Collider rightDoor = new Collider(roomPosition.X + roomWidth - doorWidth, roomPosition.Y + (roomHeight - doorHeight) / 2, doorWidth, doorHeight);
                level.StaticObjects.Add(rightDoor);
                Console.WriteLine($"Добавлен коллайдер правой двери: {rightDoor.Bounds}");
            }
        }

        private Point GetValidRoomPosition(GameObject room, List<GameObject> placedRooms)
        {
            double roomWidth = room.Sprite.PixelWidth;
            double roomHeight = room.Sprite.PixelHeight;
            double margin = 64;
            double roomCenterX = roomWidth / 2;
            double roomCenterY = roomHeight / 2;

            List<Point> validPositions = new();

            for (double x = margin; x < width - roomWidth - margin; x += 64)
            {
                for (double y = margin; y < height - roomHeight - margin; y += 64)
                {
                    Point position = new(x + roomCenterX, y + roomCenterY);
                    if (IsValidRoomPosition(room, position, placedRooms))
                    {
                        validPositions.Add(new Point(x, y)); // Возвращаем левый верхний угол
                    }
                }
            }

            if (validPositions.Count > 0)
            {
                return validPositions[random.Next(validPositions.Count)];
            }

            return new Point(-1, -1);
        }

        private bool IsValidRoomPosition(GameObject room, Point position, List<GameObject> placedRooms)
        {
            string roomName = room.Sprite.UriSource.ToString().Split('/').Last().Split('.').First();
            double roomWidth = room.Sprite.PixelWidth;
            double roomHeight = room.Sprite.PixelHeight;
            double roomCenterX = roomWidth / 2;
            double roomCenterY = roomHeight / 2;

            if (position.X - roomCenterX < 64 || position.X + roomCenterX > width - 64 || position.Y - roomCenterY < 64 || position.Y + roomCenterY > height - 64)
            {
                return false;
            }

            if ((roomName.Contains("room_exit_left") && position.X - roomCenterX < roomWidth + 64) ||
                (roomName.Contains("room_exit_right") && position.X + roomCenterX > width - roomWidth - 64) ||
                (roomName.Contains("room_exit_up") && position.Y - roomCenterY < roomHeight + 64) ||
                (roomName.Contains("room_exit_down") && position.Y + roomCenterY > height - roomHeight - 64))
            {
                return false;
            }

            foreach (var placedRoom in placedRooms)
            {
                string placedRoomName = placedRoom.Sprite.UriSource.ToString().Split('/').Last().Split('.').First();
                double placedRoomWidth = placedRoom.Sprite.PixelWidth;
                double placedRoomHeight = placedRoom.Sprite.PixelHeight;
                double placedRoomCenterX = placedRoom.Position.X + placedRoomWidth / 2;
                double placedRoomCenterY = placedRoom.Position.Y + placedRoomHeight / 2;

                if ((roomName.Contains("room_exit_left") && placedRoomCenterX > position.X - roomCenterX && placedRoomCenterX < position.X + roomCenterX) ||
                    (roomName.Contains("room_exit_right") && placedRoomCenterX < position.X + roomCenterX && placedRoomCenterX + placedRoomWidth > position.X - roomCenterX) ||
                    (roomName.Contains("room_exit_up") && placedRoomCenterY > position.Y - roomCenterY && placedRoomCenterY < position.Y + roomCenterY) ||
                    (roomName.Contains("room_exit_down") && placedRoomCenterY < position.Y + roomCenterY && placedRoomCenterY + placedRoomHeight > position.Y - roomCenterY))
                {
                    return false;
                }
            }

            return true;
        }

        private void GenerateStaticObjects(Level level)
        {
            Debug.WriteLine("Generating static objects...");
            GenerateFloor(level);
            GenerateRooms(level);
            Debug.WriteLine("Static objects generated.");
        }

        private void GenerateDynamicObjects(Level level)
        {
            Debug.WriteLine("Generating dynamic objects...");
            GameData.InitializeEntities();
            SpawnEnemies(level);
            SpawnCats(MAX_CAT_COUNT);
            Debug.WriteLine("Dynamic objects generated.");
        }

        public Level GenerateLevel(int levelIndex)
        {
            try
            {
                Level level = new(width, height, new Point(0, 0), new Point(width, height));
                GenerateStaticObjects(level);
                GenerateDynamicObjects(level);
                GameData.Levels.Add(level);
                return level;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации уровня {levelIndex}: {ex.Message}");
                return null;
            }
        }

        private void SpawnEnemies(Level level)
        {
            if (GameData.Enemies.Count <= 0 || GameData.Enemies == null)
            {
                MessageBox.Show("Ошибка при спавне врагов");
                return;
            }

            foreach (Enemy enemy in level.DynamicObjects)
            {
                enemy.enemyImage = GameData.CreateTexture(enemy.position, enemy.enemySprites[0]);
                if (!canvas.Children.Contains(enemy.enemyImage))
                    canvas.Children.Add(enemy.enemyImage);

                if (!canvas.Children.Contains(enemy.hpText))
                {
                    enemy.hpText = GameData.CreateEnemyHpText(enemy);
                    canvas.Children.Add(enemy.hpText);
                }

                DispatcherTimer attackTimer = new()
                {
                    Interval = TimeSpan.FromMilliseconds(enemy.attackInterval)
                };

                attackTimer.Tick += (s, args) =>
                {
                    double distanceToPlayer = Player.CalculateDistance(Player.playerImage, enemy.enemyImage);
                    if (distanceToPlayer <= GameData.MAX_ENEMY_ATTACK_DISTANCE)
                    {
                        enemy.Attack(GameData.Player, distanceToPlayer, GameData.MAX_ATTACK_DISTANCE);
                    }
                };

                attackTimer.Start();
                GameData.enemyAttackTimers[enemy] = attackTimer;

                level.DynamicObjects.Add(enemy);
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
                            Cat cat = new("cat", Character.Character.CharacterClass.Cat, spawnPoint);
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
            return Math.Max(0, 1 - (distance / PLAYER_VISIBILITY_RADIUS));
        }

        public void RenderLevel(int levelNumber)
        {
            try
            {
                Debug.WriteLine($"Rendering level {levelNumber}...");
                if (levelNumber < 0 || levelNumber >= GameData.Levels.Count)
                {
                    MessageBox.Show($"Ошибка: уровень {levelNumber} не существует");
                    return;
                }

                Level level = GameData.Levels[levelNumber];
                Point playerPosition = GameData.Player.position;
                int startX = (int)Math.Max(0, playerPosition.X - PLAYER_VISIBILITY_RADIUS);
                int startY = (int)Math.Max(0, playerPosition.Y - PLAYER_VISIBILITY_RADIUS);
                int endX = (int)(playerPosition.X + PLAYER_VISIBILITY_RADIUS);
                int endY = (int)(playerPosition.Y + PLAYER_VISIBILITY_RADIUS);

                foreach (var obj in level.StaticObjects)
                {
                    double distance = Math.Sqrt(Math.Pow(obj.Position.X - playerPosition.X, 2) + Math.Pow(obj.Position.Y - playerPosition.Y, 2));
                    double opacity = GetOpacity(distance);

                    if (obj.Position.X >= startX && obj.Position.X <= endX && obj.Position.Y >= startY && obj.Position.Y <= endY)
                    {
                        if (!canvas.Children.Contains(obj.RenderedElement))
                        {
                            obj.Render(canvas);
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

                foreach (var entity in level.DynamicObjects)
                {
                    if (entity is Enemy enemy)
                    {
                        if (enemy.position.X >= startX && enemy.position.X <= endX && enemy.position.Y >= startY && enemy.position.Y <= endY)
                        {
                            if (enemy.enemyImage != null)
                            {
                                Canvas.SetLeft(enemy.enemyImage, enemy.position.X);
                                Canvas.SetTop(enemy.enemyImage, enemy.position.Y);
                                Canvas.SetZIndex(enemy.enemyImage, int.MaxValue);
                                enemy.enemyImage.Visibility = Visibility.Visible;
                            }

                            if (enemy.hpText != null)
                            {
                                Canvas.SetLeft(enemy.hpText, enemy.position.X + 50);
                                Canvas.SetTop(enemy.hpText, enemy.position.Y - 20);
                                Canvas.SetZIndex(enemy.hpText, int.MaxValue);
                                enemy.hpText.Visibility = Visibility.Visible;
                                enemy.hpText.Text = $"{enemy.hp}";
                            }
                        }
                        else
                        {
                            if (enemy.enemyImage != null)
                            {
                                enemy.enemyImage.Visibility = Visibility.Hidden;
                            }

                            if (enemy.hpText != null)
                            {
                                enemy.hpText.Visibility = Visibility.Hidden;
                            }
                        }
                    }
                    else if (entity is Cat cat)
                    {
                        if (cat.catImage != null)
                        {
                            Canvas.SetLeft(cat.catImage, cat.position.X);
                            Canvas.SetTop(cat.catImage, cat.position.Y);
                            Canvas.SetZIndex(cat.catImage, 1);
                        }
                    }
                }
                Debug.WriteLine($"Level {levelNumber} rendered.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при рендеринге уровня {levelNumber}: {ex.Message}");
            }
        }

        private void RenderFirstMap()
        {
            Level level = GameData.Levels[0];
            Point playerPosition = GameData.Player.position;
            int startX = (int)Math.Max(0, playerPosition.X - PLAYER_VISIBILITY_RADIUS);
            int startY = (int)Math.Max(0, playerPosition.Y - PLAYER_VISIBILITY_RADIUS);
            int endX = (int)(playerPosition.X + PLAYER_VISIBILITY_RADIUS);
            int endY = (int)(playerPosition.Y + PLAYER_VISIBILITY_RADIUS);


            if (level.StaticObjects != null)
            {
                foreach (var obj in level.StaticObjects)
                {
                    double distance = Math.Sqrt(Math.Pow(obj.Position.X - playerPosition.X, 2) + Math.Pow(obj.Position.Y - playerPosition.Y, 2));
                    double opacity = GetOpacity(distance);

                    if (obj.Position.X >= startX && obj.Position.X <= endX && obj.Position.Y >= startY && obj.Position.Y <= endY)
                    {
                        if (!canvas.Children.Contains(obj.RenderedElement))
                        {
                            obj.Render(canvas);
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
            }

            foreach (var entity in level.DynamicObjects)
            {
                if (entity is Enemy enemy)
                {
                    if (enemy.position.X >= startX && enemy.position.X <= endX && enemy.position.Y >= startY && enemy.position.Y <= endY)
                    {
                        if (enemy.enemyImage != null)
                        {
                            Canvas.SetLeft(enemy.enemyImage, enemy.position.X);
                            Canvas.SetTop(enemy.enemyImage, enemy.position.Y);
                            Canvas.SetZIndex(enemy.enemyImage, int.MaxValue);
                            enemy.enemyImage.Visibility = Visibility.Visible;
                        }

                        if (enemy.hpText != null)
                        {
                            Canvas.SetLeft(enemy.hpText, enemy.position.X + 50);
                            Canvas.SetTop(enemy.hpText, enemy.position.Y - 20);
                            Canvas.SetZIndex(enemy.hpText, int.MaxValue);
                            enemy.hpText.Visibility = Visibility.Visible;
                            enemy.hpText.Text = $"{enemy.hp}";
                        }
                    }
                    else
                    {
                        if (enemy.enemyImage != null)
                        {
                            enemy.enemyImage.Visibility = Visibility.Hidden;
                        }

                        if (enemy.hpText != null)
                        {
                            enemy.hpText.Visibility = Visibility.Hidden;
                        }
                    }
                }
                else if (entity is Cat cat)
                {
                    if (cat.catImage != null)
                    {
                        Canvas.SetLeft(cat.catImage, cat.position.X);
                        Canvas.SetTop(cat.catImage, cat.position.Y);
                        Canvas.SetZIndex(cat.catImage, 1);
                    }
                }
            }
        }
    }
}