using Character;
using GameManager;
using Interface;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Depths_of_the_University_The_Battle_for_Freedom
{
    public partial class MainWindow : Window, IPlayerUI, IItemDescription, IUseItem
    {
        public GameData gameData;
        public Movement movement;
        public InventoryManager inventoryManager;
        public LevelManager levelManager;
        public LevelGenerator levelGenerator;
        private int currentLevelIndex = 0;

        public static bool IsInventoryOpen { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                InitializeGame();
                InitializePlayer();

                CompositionTarget.Rendering += UpdateGame;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при инициализации: " + ex.Message);
            }
        }




        private void InitializeSubscriptions()
        {
            InventoryEvents.ItemAdded += OnItemAdded;
            GameManager.EventManager.ItemReceived += ShowItemReceivedMessage;

            KeyDown += movement.Window_KeyDown;
            KeyUp += movement.Window_KeyUp;

            levelGenerator.PlayerSpawned += OnPlayerSpawned;
            levelGenerator.PlayerInitialized += OnPlayerInitialized;
            GameManager.EventManager.CatFedAndRunningAway += HandleCatRunningAway;

            SizeChanged += MainWindow_SizeChanged;
        }

        public void InitializeGame()
        {
            GameData.GameCanvas = GameCanvas;
            GameCanvas.Loaded += GameCanvas_Loaded;

            inventoryManager = new InventoryManager(InventoryGridContainer);
            movement = new Movement(inventoryManager, this);

            InventoryManager.LoadItemsSprites();

            levelManager = new LevelManager();

            var startPoint = new Point(0, 0);
            var endPoint = new Point(GameData.SCREEN_WIDTH, GameData.SCREEN_HEIGHT);

            levelGenerator = new LevelGenerator(GameData.SCREEN_WIDTH, GameData.SCREEN_HEIGHT, GameData.GameCanvas, startPoint, endPoint);

            for (int levelNumber = 0; levelNumber < 3; levelNumber++)
            {
                Level level = levelGenerator.GenerateLevel(levelNumber);
                LevelManager.AddLevel(level);
            }

            InitializeSubscriptions();
            levelGenerator.RenderLevel(0);
        }

        private void HandleCatRunningAway(InventoryManager manager, Level level)
        {
            foreach (var cat in GameData.Cats)
            {
                movement.CatRunningAway(cat, () => movement.StopCatRunning(cat));
                cat.CatRemoved += movement.OnCatRemoved;
            }
        }

        public void OnCatRemoved(Cat cat)
        {
            GameData.Cats.Remove(cat);
            GameData.GameCanvas.Children.Remove(cat.catImage);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (levelManager != null)
            {
                levelManager.UpdateLevelSize(new Point(0, 0), new Point(GameCanvas.ActualWidth, GameCanvas.ActualHeight));
                ScaleMap(new Size(this.ActualWidth, this.ActualHeight));
            }
        }
        private void ScaleMap(Size windowSize)
        {
            double scaleX = windowSize.Width / GameCanvas.ActualWidth;
            double scaleY = windowSize.Height / GameCanvas.ActualHeight;

            ScaleTransform scaleTransform = new ScaleTransform(scaleX, scaleY);
            GameCanvas.RenderTransform = scaleTransform;
        }

        public void UpdateGame(object sender, EventArgs e)
        {
            try
            {
                levelGenerator.RenderLevel(currentLevelIndex);

                if (InventoryManager.IsInventoryOpen)
                    UpdateInventoryDisplay(inventoryManager);
                else
                {
                    movement.HandlePlayerMovement(GameData.Enemies, GameCanvas);
                    GameManager.EventManager.UpdatePlayerPositionOnCanvas();
                    GameManager.EventManager.UpdateEnemyMovementsCore();
                    GameManager.EventManager.UpdateEnemyPositionsOnCanvas();

                    BattleSystem.CheckForEnemyDeath(GameCanvas, inventoryManager);
                    BattleSystem.CheckForPlayerDeath();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении игры: " + ex.Message);
            }
        }


        private void OnPlayerSpawned(Player player)
        {
            if (GameData.Player != null)
            {
                GameData.Player.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == "hp")
                        UpdateHealthBar(GameData.Player.hp);
                    if (e.PropertyName == "energy")
                        UpdateEnergyBar(GameData.Player.energy);
                    if (e.PropertyName == "exp")
                        UpdateExpBar(GameData.Player.exp);
                };

                Player.ExpGained += OnExpGained;
            }
        }

        private void OnPlayerInitialized(Player player)
        {
            InitializePlayerAnimationTimer();
        }

        private void OnItemAdded(object sender, ItemAddedEventArgs e)
        {
            for (int i = 0; i < e.Count; ++i)
            {
                IItem item = e.Item;
                inventoryManager.Inventory.AddItem(item);
            }

            inventoryManager.UpdateInventoryDisplay();
        }

        public void LoadPlayerTexture()
        {
            try
            {
                if (Player.playerSprites != null && Player.playerSprites.Length > 0)
                {
                    var playerImage = GameData.CreateTexture(GameData.Player.position, Player.playerSprites[0]);
                    GameCanvas.Children.Add(GameData.Player.playerImage);
                }
                else
                {
                    MessageBox.Show("Ошибка: Спрайты игрока не инициализированы.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке текстуры игрока: " + ex.Message);
            }
        }

        public void LoadEnemyTextures()
        {
            try
            {
                foreach (Enemy enemy in GameData.Enemies)
                {
                    enemy.enemyImage = GameData.CreateTexture(enemy.position, enemy.enemySprites[0]);
                    if (!GameCanvas.Children.Contains(enemy.enemyImage))
                        GameCanvas.Children.Add(enemy.enemyImage);

                    if (!GameCanvas.Children.Contains(enemy.hpText))
                    {
                        enemy.hpText = GameData.CreateEnemyHpText(enemy);
                        GameCanvas.Children.Add(enemy.hpText);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке текстур врагов: " + ex.Message);
            }
        }

        public void OnExpGained(string message)
        {
            ShowEnergyMessage(message);
        }

        private void UpdateHealthBar(int currentHp)
        {
            double healthWidth = (currentHp / 100.0) * 150;

            var healthAnimation = new DoubleAnimation
            {
                To = healthWidth,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            HealthRectangle.BeginAnimation(Rectangle.WidthProperty, healthAnimation);
        }

        public void InitializePlayer()
        {
            try
            {
                GameData.Player = new Technician("Игрок", GameData.playerInitPosition);
                GameData.GameCanvas.DataContext = GameData.Player;

                LoadPlayerTexture();
                InitializePlayerAnimationTimer();

                GameData.Player.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == "hp")
                        UpdateHealthBar(GameData.Player.hp);
                    if (e.PropertyName == "energy")
                        UpdateEnergyBar(GameData.Player.energy);
                    if (e.PropertyName == "exp")
                        UpdateExpBar(GameData.Player.exp);
                };

                Player.ExpGained += OnExpGained;


                OnPlayerInitialized(GameData.Player);
                OnPlayerSpawned(GameData.Player);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при инициализации игрока: " + ex.Message);
            }
        }

        private void UpdateEnergyBar(int currentEnergy)
        {
            double energyWidth = (currentEnergy / 100.0) * 150;

            var energyAnimation = new DoubleAnimation
            {
                To = energyWidth,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            EnergyRectangle.BeginAnimation(Rectangle.WidthProperty, energyAnimation);
        }

        private void UpdateExpBar(int currentExp)
        {
            double expWidth = (currentExp / 100.0) * 150;

            var expAnimation = new DoubleAnimation
            {
                To = expWidth,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            ExpRectangle.BeginAnimation(Rectangle.WidthProperty, expAnimation);
        }

        public void InitializeLevel()
        {
            try
            {
                if (GameCanvas == null && GameCanvas.ActualWidth <= 0 || GameCanvas.ActualHeight <= 0)
                {
                    MessageBox.Show("Ошибка: GameCanvas имеет нулевые или отрицательные размеры.");
                    return;
                }

                Point startPoint = new Point(0, 0);
                Point endPoint = new Point(GameCanvas.ActualWidth, GameCanvas.ActualHeight);

                levelGenerator = new LevelGenerator(GameCanvas.ActualWidth, GameCanvas.ActualHeight, GameCanvas, startPoint, endPoint);
                Level current_level = levelGenerator.GenerateLevel(0);
                levelGenerator.RenderLevel(0);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void CreateInventory(InventoryManager manager)
        {
            InventoryManager.IsInventoryOpenChanged += (sender, e) => OnIsInventoryOpenChanged(sender, e, manager);
        }

        private void GameCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeLevel();
                CreateInventory(inventoryManager);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при инициализации канваса: " + ex.Message);
            }
        }

        public void OnIsInventoryOpenChanged(object sender, EventArgs e, InventoryManager manager)
        {
            if (!InventoryManager.IsInventoryOpen)
                InventoryManager.InventoryGridContainer.Visibility = Visibility.Collapsed;
            else
            {
                if (InventoryManager.InventoryGrid == null)
                {
                    int rows = 2;
                    int columns = 2;
                    InventoryManager.CreateInventoryUI(manager.Inventory, rows, columns, inventoryManager);
                }
                InventoryManager.InventoryGridContainer.Visibility = Visibility.Visible;
                manager.UpdateInventoryDisplay();
            }
        }

        private void UpdateInventoryDisplay(InventoryManager manager)
        {
            if (InventoryManager.InventoryGrid != null)
                manager.UpdateInventoryDisplay();
        }

        public void ShowItemDescription(string description)
        {
            ItemDescriptionTextBlock.Text = description;
            ItemDescriptionTextBlock.Visibility = Visibility.Visible;
        }

        public void HideItemDescription()
        {
            ItemDescriptionTextBlock.Visibility = Visibility.Collapsed;
        }

        public void UseItem(string itemName)
        {
            IsInventoryOpen = false;
            IItem item = null;
            switch (itemName)
            {
                case "Вода 'Прохлада'":
                    GameData.Player.hp += 20;
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Кофе 'Don't sleep'":
                    GameData.Player.ApplyEnergyRegenerationBonus(4, 30);
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Энергетик 'Boost'":
                    GameData.Player.energy += 100;
                    GameData.Player.ApplyDamageBonus(2, 15);
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Чай 'Забористый'":
                    GameData.Player.hp += 10;
                    GameData.Player.ApplyEnergyRegenerationBonus(4, 60);
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Шоколадный батончик 'Сладко и точка'":
                    GameData.Player.hp += 30;
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Лапша быстрого приготовления 'Дырокол'":
                    GameData.Player.hp += 50;
                    GameData.Player.ApplyHealthRegenerationBonus(-1, 15);
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Шаурма 'Аппетитная'":
                    GameData.Player.hp += 30;
                    GameData.Player.ApplyHealthRegenerationBonus(5, 30);
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Пицца 'Сочная'":
                    GameData.Player.hp += 100;
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Конспект лекции":
                    GameData.Player.exp += InventoryManager.itemDictionary[typeof(LectureNotes)].ExpBonus;
                    GameData.Player.intellect += InventoryManager.itemDictionary[typeof(LectureNotes)].IntellectBonus;
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Конспект практики":
                    GameData.Player.exp += InventoryManager.itemDictionary[typeof(PracticeNotes)].ExpBonus;
                    GameData.Player.intellect += InventoryManager.itemDictionary[typeof(PracticeNotes)].IntellectBonus;
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Тетрадь отличника":
                    Player.AddExp(GameData.Player, InventoryManager.itemDictionary[typeof(ExcellentStudentNotebook)].IntellectBonus);

                    GameData.Player.intellect += InventoryManager.itemDictionary[typeof(ExcellentStudentNotebook)].IntellectBonus;
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Смартфон 'Vivo'":
                    GameData.Player.exp += InventoryManager.itemDictionary[typeof(Phone)].ExpBonus;
                    GameData.Player.intellect += InventoryManager.itemDictionary[typeof(Phone)].IntellectBonus;
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
                case "Корм для кошек 'Dreaming'":
                    item = inventoryManager.Inventory.GetAllItems().Keys.FirstOrDefault(i => i.Name == itemName);
                    break;
            }

            if (item != null)
            {
                var itemType = item.GetType();
                if (--item.Durability < 1)
                {
                    if (inventoryManager.Inventory.RemoveItem(item))
                    {
                        ShowEnergyMessage("Использовано: " + itemName);
                        UpdateInventoryDisplay(inventoryManager);
                    }
                    else
                        MessageBox.Show("Не удалось удалить предмет: " + itemName);
                }
            }
        }

        public void ShowEnergyMessage(string message)
        {
            EnergyMessageTextBlock.Text = message;
            EnergyMessageTextBlock.Visibility = Visibility.Visible;

            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                BeginTime = TimeSpan.FromSeconds(0.5)
            };

            EnergyMessageTextBlock.BeginAnimation(TextBlock.OpacityProperty, fadeOutAnimation);

            fadeOutAnimation.Completed += (sender, e) =>
            {
                EnergyMessageTextBlock.Visibility = Visibility.Collapsed;
            };
        }

        public void FlashEnergyValue()
        {
            var storyboard = new Storyboard();

            var flashAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.5,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };

            var colorAnimation = new ObjectAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                RepeatBehavior = new RepeatBehavior(1),
                AutoReverse = false
            };

            colorAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Brushes.Red, KeyTime.FromPercent(0)));
            colorAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Brushes.White, KeyTime.FromPercent(0.2)));
            colorAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Brushes.Red, KeyTime.FromPercent(0.4)));
            colorAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Brushes.White, KeyTime.FromPercent(0.6)));
            colorAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Brushes.Red, KeyTime.FromPercent(0.8)));
            colorAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame(Brushes.White, KeyTime.FromPercent(1)));

            Storyboard.SetTargetProperty(flashAnimation, new PropertyPath(TextBlock.OpacityProperty));
            storyboard.Children.Add(flashAnimation);

            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath(TextBlock.ForegroundProperty));
            storyboard.Children.Add(colorAnimation);

            storyboard.Begin();
        }

        private void ShowItemReceivedMessage(string itemName)
        {
            ItemReceivedMessageTextBlock.Text = "Получен предмет: " + itemName;
            ItemReceivedMessageTextBlock.Visibility = Visibility.Visible;

            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(2)),
                BeginTime = TimeSpan.FromSeconds(0.5)
            };

            ItemReceivedMessageTextBlock.BeginAnimation(OpacityProperty, fadeOutAnimation);

            fadeOutAnimation.Completed += (sender, e) =>
            {
                ItemReceivedMessageTextBlock.Visibility = Visibility.Collapsed;
            };
        }

        public void InitializePlayerAnimationTimer()
        {
            GameData.animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(GameData.ANIMATION_INTERVAL)
            };
            GameData.animationTimer.Tick += AnimationTimer_Tick;
            GameData.animationTimer.Start();
        }

        public void AnimationTimer_Tick(object sender, EventArgs e)
        {
            GameData.Player.currentSpriteIndex++;
            if (GameData.Player.currentSpriteIndex >= 2)
                GameData.Player.currentSpriteIndex = 0;

            Player.SetSprite(GameData.Player, GameData.Player.currentSpriteIndex);
        }
    }
}