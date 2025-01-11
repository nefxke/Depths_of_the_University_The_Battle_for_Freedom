using Character;
using GameManager;
using Interface;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

public class Movement
{
    private readonly InventoryManager _inventoryManager;
    private readonly IPlayerUI _playerUI;
    private Level _currentLevel;

    // Свойства для управления состоянием кошки
    public bool runningRight { get; set; }
    public bool isRunning { get; set; }
    public int runFrame { get; set; }
    public Action onRunAwayComplete { get; set; }

    public Movement(InventoryManager inventoryManager, IPlayerUI playerUI)
    {
        if (GameData.Levels.Count > 0 && GameData.Levels != null)
        {
            Level currentLevel = GameData.Levels[0];
            _currentLevel = currentLevel;
        }
        _inventoryManager = inventoryManager;
        _playerUI = playerUI;
    }

    public void InitializeEnemyMovements()
    {
        DispatcherTimer enemyMovementTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(25)
        };
        enemyMovementTimer.Tick += GameManager.EventManager.UpdateEnemyMovements;
        enemyMovementTimer.Start();
    }

    public static void MoveEnemy(Enemy enemy, Canvas canvas)
    {
        if (enemy == null || GameData.Player == null)
            return;

        var currentTime = DateTime.Now;
        Point enemyPos = enemy.position;
        Point playerPos = GameData.Player.position;

        double dx = playerPos.X - enemyPos.X;
        double dy = playerPos.Y - enemyPos.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance <= GameData.Player.playerVisibilityRange && distance >= GameData.SAFE_DISTANCE)
        {
            dx /= distance;
            dy /= distance;

            dx *= enemy.speed;
            dy *= enemy.speed;

            enemyPos.X += dx;
            enemyPos.Y += dy;

            enemy.IsWaiting = false;
            enemy.IsMovingRandomly = false;
            enemy.LastSeenPlayerTime = DateTime.Now;
        }
        else
        {
            if ((currentTime - enemy.LastSeenPlayerTime).TotalSeconds <= enemy.TimeToWaitBeforeMove
                && enemy.LastSeenPlayerTime != DateTime.MinValue)
            {
                return;
            }

            // Начинаем новое случайное движение
            if (!enemy.IsMovingRandomly && !enemy.IsWaiting)
            {
                enemy.RandomDirection = GetRandomDirection();
                enemy.MoveStartTime = currentTime;
                enemy.IsMovingRandomly = true;
                enemy.MoveDuration = Character.Character.Random.Next(1, 6);
            }
            else if (enemy.IsMovingRandomly)
            {
                var moveTime = (currentTime - enemy.MoveStartTime).TotalSeconds;
                if (moveTime >= enemy.MoveDuration)
                {
                    enemy.IsMovingRandomly = false;
                    enemy.IsWaiting = true;
                    enemy.WaitStartTime = currentTime;
                    enemy.WaitDuration = new Random().Next(2, 6);
                }
                else
                {
                    // Продолжаем движение в текущем направлении
                    enemyPos.X += enemy.RandomDirection.X * enemy.speed;
                    enemyPos.Y += enemy.RandomDirection.Y * enemy.speed;
                }
            }
            else if (enemy.IsWaiting)
            {
                var waitTime = (currentTime - enemy.WaitStartTime).TotalSeconds;
                if (waitTime >= enemy.WaitDuration)
                {
                    enemy.IsWaiting = false;
                    enemy.IsMovingRandomly = false; // Позволяет начать новое движение
                }
            }
        }

        // Проверяем границы канваса
        if (enemyPos.X <= 0 || enemyPos.X + Character.Character.character_size >= canvas.ActualWidth)
        {
            enemyPos.X = enemy.position.X;
            enemy.RandomDirection = GetRandomDirection(); // Новое направление при столкновении
        }
        if (enemyPos.Y <= 0 || enemyPos.Y + Character.Character.character_size >= canvas.ActualHeight)
        {
            enemyPos.Y = enemy.position.Y;
            enemy.RandomDirection = GetRandomDirection(); // Новое направление при столкновении
        }

        // Обновляем позицию врага
        enemy.position = enemyPos;

        // Синхронизация текста HP врага
        if (enemy.hpText != null)
        {
            Canvas.SetLeft(enemy.hpText, enemyPos.X + 50);
            Canvas.SetTop(enemy.hpText, enemyPos.Y - 20);
            enemy.hpText.Visibility = enemy.enemyImage.Visibility; // Синхронизация видимости текста HP с изображением врага
        }
    }
    private static Point GetRandomDirection()
    {
        double x = Character.Character.Random.NextDouble() * 2 - 1; // Случайное значение от -1 до 1
        double y = Character.Character.Random.NextDouble() * 2 - 1; // Случайное значение от -1 до 1

        // Нормализуем вектор
        double length = Math.Sqrt(x * x + y * y);
        x /= length;
        y /= length;

        return new Point(x, y);
    }

    public void StartEnemyMovement(Enemy enemy, Canvas GameCanvas)
    {
        DispatcherTimer enemyMoveTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };

        enemyMoveTimer.Tick += (sender, e) =>
        {
            MoveEnemy(enemy, GameCanvas);
            int spriteIndex = enemy.currentSpriteIndex % 2;
            enemy.enemyImage.Source = enemy.enemySprites[spriteIndex];
            enemy.currentSpriteIndex++;
        };

        enemyMoveTimer.Start();
    }

    public void HandlePlayerMovement(List<Enemy> enemies, Canvas GameCanvas)
    {
        try
        {
            if (GameData.Player.playerImage == null)
            {
                MessageBox.Show("Ошибка: playerImage не инициализирован.");
                return;
            }

            double dx = 0, dy = 0;
            bool isMoving = false;

            // Проверяем флаги для движения по осям X и Y
            if (GameData.isWPressed)
            {
                dy = -GameData.Player.speed; // Вверх
                isMoving = true;
            }
            if (GameData.isSPressed)
            {
                dy = GameData.Player.speed; // Вниз
                isMoving = true;
            }
            if (GameData.isAPressed)
            {
                dx = -GameData.Player.speed; // Влево
                isMoving = true;
            }
            if (GameData.isDPressed)
            {
                dx = GameData.Player.speed; // Вправо
                isMoving = true;
            }
            if (isMoving)
            {
                Point direction = new(dx, dy);

                MovePlayer(GameData.Player, enemies, direction, GameCanvas);

                if (GameData.animationTimer != null && !GameData.animationTimer.IsEnabled)
                {
                    GameData.Player.currentSpriteIndex = 0; // Сброс индекса спрайта
                    GameData.animationTimer.Start();
                }

                if (GameData.isAPressed) // Влево
                {
                    Player.SetSprite(GameData.Player, GameData.Player.currentSpriteIndex % 2 == 0 ? 4 : 5); // Чередуем между player_walk_left_1 и player_walk_left_2
                }
                else if (GameData.isDPressed) // Вправо
                {
                    Player.SetSprite(GameData.Player, GameData.Player.currentSpriteIndex % 2 == 0 ? 6 : 7); // Чередуем между player_walk_right_1 и player_walk_right_2
                }
                else if (GameData.isWPressed || GameData.isSPressed) // Только вертикальное движение
                {
                    // Если двигаемся вверх, сохраняем "вверх" как последнее направление
                    GameData.lastDirection = GameData.isWPressed ? 0 : 1;
                }
            }
            else
            {
                // Останавливаем анимацию, если не движемся
                if (GameData.animationTimer != null)
                {
                    GameData.animationTimer.Stop();
                }

                if (GameData.lastDirection == 2)        // Влево
                    Player.SetSprite(GameData.Player, 0);                // player_stand_left
                else if (GameData.lastDirection == 3)   // Вправо
                    Player.SetSprite(GameData.Player, 1);                // player_stand_right
                else if (GameData.lastDirection == 0)   // Вверх
                    Player.SetSprite(GameData.Player, 2);                // player_stand_back_left
                else if (GameData.lastDirection == 1)   // Вниз
                    Player.SetSprite(GameData.Player, 3);                // player_stand_back_right
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при обработке движения игрока: {ex.Message}");
        }
    }

    public void MovePlayer(Player player, List<Enemy> enemies, Point direction, Canvas gameCanvas)
    {
        try
        {
            double newX = GameData.Player.position.X + direction.X;
            double newY = GameData.Player.position.Y + direction.Y;

            double canvasWidth = gameCanvas.ActualWidth;
            double canvasHeight = gameCanvas.ActualHeight;

            double playerSize = Player.character_size;

            if (newX < 0 || newX + playerSize > canvasWidth)
                newX = GameData.Player.position.X; 

            if (newY < 0 || newY + playerSize > canvasHeight)
                newY = GameData.Player.position.Y;

            foreach (var enemy in enemies)
            {
                double enemyX = enemy.position.X;
                double enemyY = enemy.position.Y;

                double diffX = enemyX - newX;
                double diffY = enemyY - newY;

                double distance = Math.Sqrt(diffX * diffX + diffY * diffY);

                if (distance < GameData.SAFE_DISTANCE)
                {
                    // Проецируем вектор движения на вектор врага
                    double dotProduct = diffX * direction.X + diffY * direction.Y;

                    if (dotProduct > 0) 
                    {
                        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
                            newX = GameData.Player.position.X; 
                        else
                            newY = GameData.Player.position.Y;
                    }
                }
            }

            // Проверяем коллайдеры
            if (_currentLevel != null && _currentLevel.StaticObjects != null)
            {
                foreach (var collider in _currentLevel.StaticObjects.OfType<Collider>())
                {
                    if (collider.Intersects(new Collider(newX, newY, playerSize, playerSize)))
                    {
                        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
                            newX = GameData.Player.position.X; 
                        else
                            newY = GameData.Player.position.Y; 
                        break;
                    }
                }
            }

            GameData.Player.position = new Point(newX, newY);
            if (GameData.Player.playerImage != null)
            {
                Canvas.SetLeft(GameData.Player.playerImage, newX);
                Canvas.SetTop(GameData.Player.playerImage, newY);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при движении игрока: {ex.Message}");
        }
    }
    public void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.W:
            case Key.Up:
                GameData.isWPressed = true;
                GameData.lastDirection = 0; // Вверх
                break;
            case Key.S:
            case Key.Down:
                GameData.isSPressed = true;
                GameData.lastDirection = 1; // Вниз
                break;
            case Key.A:
            case Key.Left:
                GameData.isAPressed = true;
                GameData.lastDirection = 2; // Влево
                break;
            case Key.D:
            case Key.Right:
                GameData.isDPressed = true;
                GameData.lastDirection = 3; // Вправо
                break;
            case Key.E:
                GameManager.EventManager.PlayerFeedCat(_inventoryManager, _currentLevel);
                break;
            case Key.F:
                _inventoryManager.ToggleInventory();
                break;
            case Key.NumPad5:
                BattleSystem.HandlePlayerAttack(GameData.Enemies, _inventoryManager, _playerUI);
                break;
        }
    }

    public void Window_KeyUp(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.W:
            case Key.Up:
                GameData.isWPressed = false;
                break;
            case Key.S:
            case Key.Down:
                GameData.isSPressed = false;
                break;
            case Key.A:
            case Key.Left:
                GameData.isAPressed = false;
                break;
            case Key.D:
            case Key.Right:
                GameData.isDPressed = false;
                break;
        }
    }

    public void AnimationTimer_Tick(object sender, EventArgs e)
    {
        GameData.Player.currentSpriteIndex++;
        if (GameData.Player.currentSpriteIndex >= 2)
            GameData.Player.currentSpriteIndex = 0;

        Player.SetSprite(GameData.Player, GameData.Player.currentSpriteIndex);
    }

    public void InitializePlayerAnimationTimer()
    {
        GameData.animationTimer = new();
        GameData.animationTimer.Interval = TimeSpan.FromMilliseconds(GameData.ANIMATION_INTERVAL);
        GameData.animationTimer.Tick += AnimationTimer_Tick;
    }
    public void CatRunningAway(Cat cat, Action onComplete = null)
    {
        if (isRunning)
            return;

        isRunning = true;
        runFrame = 0;
        onRunAwayComplete = onComplete;

        double distanceToLeft = Canvas.GetLeft(cat.catImage);
        double distanceToRight = GameData.GameCanvas.ActualWidth - Canvas.GetLeft(cat.catImage);

        runningRight = distanceToRight > distanceToLeft;

        DispatcherTimer runAnimationTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(60)
        };
        runAnimationTimer.Tick += (sender, e) => CatRunAnimationStep(sender, e, cat);
        runAnimationTimer.Start();
    }


    private void MoveCat(Cat cat)
    {
        double currentLeft = Canvas.GetLeft(cat.catImage);
        double newLeft = currentLeft + (runningRight ? cat.speed : -cat.speed);
        cat.position.X = newLeft;
        cat.catImage.SetValue(Canvas.LeftProperty, newLeft);
    }



    private void UpdateCatAnimation(Cat cat)
    {
        int[] runFrames = runningRight
            ? new[] { 2, 4, 6 }
            : new[] { 3, 5, 7 };

        cat.catImage.Source = cat.catSprites[runFrames[runFrame % runFrames.Length]];
        runFrame++;
    }
    private void CatRunAnimationStep(object sender, EventArgs e, Cat cat)
    {
        MoveCat(cat);
        UpdateCatAnimation(cat);

        if (Canvas.GetLeft(cat.catImage) > GameData.GameCanvas.ActualWidth + cat.catImage.Width || Canvas.GetLeft(cat.catImage) < -cat.catImage.Width)
        {
            StopCatRunning(cat);
            ((DispatcherTimer)sender).Stop();
        }
    }

    public void StopCatRunning(Cat cat)
    {
        if (!isRunning)
            return;

        isRunning = false;
        onRunAwayComplete?.Invoke();
        GameData.Cats.Remove(cat);
        GameData.GameCanvas.Children.Remove(cat.catImage);
    }

    public void OnCatRemoved(Cat cat)
    {
        GameData.Cats.Remove(cat);
        GameData.GameCanvas.Children.Remove(cat.catImage);
    }
}