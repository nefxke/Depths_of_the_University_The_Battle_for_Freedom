using Interface;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace GameManager
{
    public class Item : IItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public int Durability { get; set; }
        public BitmapImage Image { get; set; }
        public float HealthBonus { get; set; }
        public float EnergyBonus { get; set; }
        public float EnergyRegenerationBonus { get; set; }
        public float HealthRegenerationBonus { get; set; }
        public byte DamageBonus { get; set; }
        public int IntellectBonus { get; set; }
        public int DexterityBonus { get; set; }
        public int ExpBonus { get; set; }

        public enum AllItems
        {
            Water,
            Coffee,
            EnergyDrink,
            Tea,
            ChocolateBar,
            InstantNoodles,
            Shawarma,
            Pizza,
            SimplePen,
            UpgradedPen,
            BestStudentPen,
            LectureNotes,
            PracticeNotes,
            ExcellentStudentNotebook,
            Phone
        }
    }
    public class Water : Item
    {
        public Water()
        {
            Name = "Вода 'Прохлада'";
            Durability = 1;
            HealthBonus = 20;
            Duration = 0;
            Description = $"Вода из местного магазина. Придаст жизненных сил.\nЗдоровье: +{HealthBonus} единиц\nИспользований:{Durability}";
        }
    }
    public class Coffee : Item
    {
        public Coffee()
        {
            Name = "Кофе 'Don't sleep'";
            Duration = 30;
            Durability = 1;
            EnergyRegenerationBonus = 4;
            Description = $"Относительно недорогой кофе собственного бренда вашего университета. Скорость восстановления энергии: +{EnergyRegenerationBonus}/сек\n" +
                $"Использований:{Durability}\nВремя действия: {Duration}с";
        }
    }
    public class EnergyDrink : Item
    {
        public EnergyDrink()
        {
            Name = "Энергетик 'Boost'";
            Duration = 15;
            Durability = 1;
            EnergyBonus = 100;
            DamageBonus = 2;
            Description = "Тайком пронесенный в здание универсистета энергетик. Тысячи студентов уже оценили его эффективность и готовы поклясться, что более бодрящим, чем это, может быть только осознание того, что ты проспал экзамен.\n" +
                $"Энергия: +{EnergyBonus}\nУрон: +{DamageBonus}\nИспользований: {Durability}\nВремя действия: {Duration}с";
        }
    }
    public class Tea : Item
    {
        public Tea()
        {
            Name = "Чай 'Забористый'";
            Durability = 1;
            Duration = 60;
            EnergyRegenerationBonus = 4;
            HealthBonus = 10;
            Description = $"Поможет расслабиться и восстановить силы.\nЗдоровье:+{HealthBonus} единиц\nСкорость восстановления энергии: +{EnergyRegenerationBonus}/сек\nВремя действия: {Duration}c";
        }
    }
    public class ChocolateBar : Item
    {
        public ChocolateBar()
        {
            Name = "Шоколадный батончик 'Сладко и точка'";
            Durability = 1;
            Duration = 0;
            HealthBonus = 30;
            Description = $"Постарайтесь не увлекаться этим батончиком. 400 калорий в одной штуке.\nЗдоровье: +{HealthBonus} единиц\nИспользований:{Durability}";
        }
    }
    public class InstantNoodles : Item
    {
        public InstantNoodles()
        {
            Name = "Лапша быстрого приготовления 'Дырокол'";
            Durability = 1;
            Duration = 15;
            HealthBonus = 50;
            HealthRegenerationBonus = -1;
            Description = "Популярный среди студентов завтрак, обед и ужин. К сожалению, из-за цены и ненадлежащего качества, оправдывает свое название\n" +
                $"Здоровье: +{HealthBonus} единиц\nВосстановление здоровья: {HealthRegenerationBonus}/сек\nИспользований:{Durability}\nВремя действия: {Duration}c";
        }
    }
    public class Shawarma : Item
    {
        public Shawarma()
        {
            Name = "Шаурма 'Аппетитная'";
            Description = "На удивление большая и сытная шаурма. Производитель по непонятной причине не указал состав на упаковке.\n" +
                "Здоровье: +5/с (+150 всего)\nВремя действия: 30с";
            Durability = 1;
            Duration = 30;
            HealthRegenerationBonus = 5;
        }
    }
    public class Pizza : Item
    {
        public Pizza()
        {
            Name = "Пицца 'Сочная'";
            Durability = 1;
            Duration = 0;
            HealthBonus = 100;
            DexterityBonus = -1;
            Description = $"Упаковка пиццы самого большого радиуса, который вы видели. После нее вам будет труднее двигаться.\nЗдоровье: +{HealthBonus} единиц\nЛовкость: {DexterityBonus}\nИспользований:{Durability}";
        }
    }
    public class SimplePen : Item
    {
        public SimplePen()
        {
            Name = "Синяя ручка";
            Durability = 50;
            Duration = -1;
            DamageBonus = 2;
            Description = $"Дешевая долговечная ручка\nУрон: +{DamageBonus} (всего урона: +200)\nОсталось использований: {Durability}";
        }
    }
    public class UpgradedPen : Item
    {
        public UpgradedPen()
        {
            Name = "Черная гелевая ручка";
            Durability = 10;
            Duration = -1;
            DamageBonus = 5;
            Description = $"Любимая когда-то вами ручка черного цвета. Намного эффективнее, но быстро заканчивается\nУрон: +{DamageBonus} (всего урона: +{DamageBonus*Durability})\nОсталось использований: {Durability}";
        }
    }
    public class BestStudentPen : Item
    {
        public BestStudentPen()
        {
            Name = "Ручка лучшего студента";
            Durability = 75;
            Duration = -1;
            DamageBonus = 20;
            Description = "Ручка с особой силой. Ходят слухи, что благодаря ей удается идеально написать любой экзамен, даже если ты ни разу не появлялся на парах и не знаешь, как называется этот предмет.\n" +
                $"Урон: +{DamageBonus} (всего +{DamageBonus*Durability})\nИспользований: {Durability}";
        }
    }
    public class LectureNotes : Item
    {
        public LectureNotes()
        {
            Name = "Конспект лекции";
            Duration = 0;
            IntellectBonus = 1;
            ExpBonus = 10;
            Durability = 1;
            Description = $"Несколько листов материала по одной из ваших дисциплин. Вряд ли сильно поможет, но прочитать все же стоит\nИнтеллект: +{IntellectBonus}\nОпыт: +{ExpBonus}";
        }
    }
    public class PracticeNotes : Item
    {
        public PracticeNotes()
        {
            Name = "Конспект практики";
            Durability = 1;
            IntellectBonus = 2;
            Duration = 0;
            ExpBonus = 25;
            Description = $"Несколько листов разбора каких-то заданий. Может быть полезно.\nИнтеллект: +{IntellectBonus}\nОпыт: +{ExpBonus}";
        }
    }
    public class ExcellentStudentNotebook : Item
    {
        public ExcellentStudentNotebook()
        {
            Name = "Тетрадь отличника";
            Durability = 1;
            Duration = -1;
            IntellectBonus = 3;
            ExpBonus = 100;
            Description = "Единственное, что вас удивляет в этой тетради, - наличие, судя по всему, абсолютно всех конспектов по одной из дисциплин за последний семестр. Вам казалось это невозможным...\n" +
                $"Интеллект: +{IntellectBonus}\nОпыт: +{ExpBonus}\nИспользований:{Durability}";
        }
    }
    public class Phone : Item
    {
        public Phone()
        {
            Name = "Смартфон 'Vivo'";
            Durability = 3;
            Duration = 0;
            ExpBonus = 50;
            IntellectBonus = -1;
            Description = $"Теперь вы можете смотреть мемы. Или в очередной раз погуглить что-то или спросить у chatJPT\nОпыт: +{ExpBonus} (всего +{ExpBonus*Durability})\n" +
                $"Интеллект:{IntellectBonus}\nИспользований: {Durability}";
        }
    }
    public class Backpack : Item { }
    public class InventorySlots : Item { }
    public class PlayerInventory : IPlayerInventory
    {
        public int MaxSlots { get; private set; }
        private readonly Dictionary<IItem, int> _items;

        public PlayerInventory(int maxSlots)
        {
            MaxSlots = maxSlots;
            _items = new Dictionary<IItem, int>();
        }

        public bool AddItem(IItem item, int count = 1)
        {
            if (_items.Count >= MaxSlots)
                return false;

            if (_items.ContainsKey(item))
                _items[item] += count;
            else
                _items[item] = count;
            return true;
        }

        public bool RemoveItem(IItem item)
        {
            if (_items.ContainsKey(item))
            {
                if (_items[item] > 1)
                    _items[item]--;
                else
                    _items.Remove(item);
                return true;
            }
            return false;
        }

        public int GetItemCount(IItem item) => _items.TryGetValue(item, out var count) ? count : 0;
        public Dictionary<IItem, int> GetAllItems() => new(_items);
    }



    public class InventoryManager
    {
        private static Dictionary<Type, string> itemImagePaths = new Dictionary<Type, string>
{
    { typeof(Water), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\water.png" },
    { typeof(Coffee), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\coffee.png" },
    { typeof(EnergyDrink), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\energy_drink.png" },
    { typeof(Tea), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\tea.png" },
    { typeof(ChocolateBar), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\chocolate_bar.png" },
    { typeof(InstantNoodles), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\instant_noodles.png" },
    { typeof(Shawarma), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\shawarma.png" },
    { typeof(Pizza), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\food_drink\pizza.png" },
    { typeof(SimplePen), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\pens\simple_pen.png"},
    { typeof(UpgradedPen), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\pens\upgraded_pen.png" },
    { typeof(BestStudentPen), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\pens\best_student_pen.png" },
    { typeof(LectureNotes), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\player_upgrade\lecture_notes.png" },
    { typeof(PracticeNotes), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\player_upgrade\practice_notes.png" },
    { typeof(ExcellentStudentNotebook), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\player_upgrade\excellent_student_notebook.png" },
    { typeof(Phone), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\player_upgrade\phone.png" },
    { typeof(InventorySlots), @"C:\Users\nefxk\OneDrive\Desktop\STUDY\ИТИП\Курсовая\Depths_of_the_University_The_Battle_for_Freedom\images\items\other\inventory_slots.png" }
};

        public static BitmapImage LoadImage(string path)
        {
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Файл не найден: {path}");

                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                return null;
            }
        }


        public static string GetItemImagePath(Type itemType)
        {
            if (itemImagePaths.ContainsKey(itemType))
                return itemImagePaths[itemType];
            return null;
        }


        public const int MAX_SLOTS = 4;
        public PlayerInventory Inventory { get; private set; }

        public static Dictionary<Type, Item> itemDictionary = new();
        public static Grid InventoryGridContainer { get; private set; }
        public static Grid InventoryGrid { get; private set; }
        public static Image InventoryBackground { get; private set; }

        public static event EventHandler IsInventoryOpenChanged;
        private static bool _isInventoryOpen;

        public InventoryManager(Grid inventoryGridContainer)
        {
            Inventory = new(MAX_SLOTS);
            InventoryGridContainer = inventoryGridContainer;

            LoadItemsSprites();
        }

        public static bool IsInventoryOpen
        {
            get => _isInventoryOpen;
            set
            {
                _isInventoryOpen = value;
                IsInventoryOpenChanged?.Invoke(null, EventArgs.Empty);
                InventoryGridContainer.Visibility = _isInventoryOpen ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public static void LoadItemsSprites()
        {
            foreach (var entry in itemImagePaths)
            {
                if (!File.Exists(entry.Value))
                {
                    MessageBox.Show($"Файл изображения для {entry.Key} не найден.");
                    continue;
                }

                try
                {
                    var image = LoadImage(entry.Value);
                    var item = CreateItem(entry.Key);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения для {entry.Key}: {ex.Message}");
                }
            }
        }

        private static Item CreateItem(Type itemType)
        {
            Item item = null;

            try
            {
                if (itemDictionary.ContainsKey(itemType))
                {
                    item = itemDictionary[itemType];
                }
                else
                {
                    if (itemType == typeof(Water))
                    {
                        item = new Water();
                    }
                    else if (itemType == typeof(Coffee))
                    {
                        item = new Coffee();
                    }
                    else if (itemType == typeof(EnergyDrink))
                    {
                        item = new EnergyDrink();
                    }
                    else if (itemType == typeof(Tea))
                    {
                        item = new Tea();
                    }
                    else if (itemType == typeof(ChocolateBar))
                    {
                        item = new ChocolateBar();
                    }
                    else if (itemType == typeof(InstantNoodles))
                    {
                        item = new InstantNoodles();
                    }
                    else if (itemType == typeof(Shawarma))
                    {
                        item = new Shawarma();
                    }
                    else if (itemType == typeof(Pizza))
                    {
                        item = new Pizza();
                    }
                    else if (itemType == typeof(SimplePen))
                    {
                        item = new SimplePen();
                    }
                    else if (itemType == typeof(UpgradedPen))
                    {
                        item = new UpgradedPen();
                    }
                    else if (itemType == typeof(BestStudentPen))
                    {
                        item = new BestStudentPen();
                    }
                    else if (itemType == typeof(LectureNotes))
                    {
                        item = new LectureNotes();
                    }
                    else if (itemType == typeof(PracticeNotes))
                    {
                        item = new PracticeNotes();
                    }
                    else if (itemType == typeof(ExcellentStudentNotebook))
                    {
                        item = new ExcellentStudentNotebook();
                    }
                    else if (itemType == typeof(Phone))
                    {
                        item = new Phone();
                    }
                    else if (itemType == typeof(InventorySlots))
                    {
                        item = new InventorySlots();
                    }

                    if (item != null)
                    {
                        itemDictionary.Add(itemType, item);
                        var imagePath = itemImagePaths[itemType];
                        var image = LoadImage(imagePath);
                        if (image != null)
                        {
                            item.Image = image;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при создании предмета {itemType.Name}: {ex.Message}");
            }
            return item;
        }

        public static List<Item> GetInventoryItems(PlayerInventory inventory)
        {
            var inventoryItems = new List<Item>();
            foreach (var item in inventory.GetAllItems().Keys)
                inventoryItems.Add((Item)item);

            return inventoryItems;
        }

        public static List<Item> GetInventoryItems()
        {
            var inventoryItems = new List<Item>();
            foreach (var item in itemDictionary.Values)
                inventoryItems.Add(item);

            return inventoryItems;
        }

        public void ToggleInventory()
        {
            IsInventoryOpen = !IsInventoryOpen;
            if (IsInventoryOpen)
                UpdateInventoryDisplay();
        }
        public static FrameworkElement CreateItemControl(Item item, int itemSize, InventoryManager inventoryManager)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Width = itemSize,
                Height = itemSize,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            var image = new Image
            {
                Source = item.Image,
                Stretch = Stretch.Uniform,
                Width = itemSize,
                Height = itemSize,
            };

            var textBlock = new TextBlock
            {
                Text = inventoryManager.Inventory.GetItemCount(item).ToString(),
                Foreground = Brushes.White,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 5, 5)
            };

            var grid = new Grid();
            grid.Children.Add(image);
            grid.Children.Add(textBlock);

            if (image.Source == null)
            {
                Debug.WriteLine($"Изображение для предмета {item.Name} не загружено.");
            }

            border.Child = grid;
            border.MouseEnter += (sender, e) => ShowItemDescription(item);
            border.MouseLeave += (sender, e) => HideItemDescription();
            border.MouseDown += (sender, e) => UseItem(item);

            return border;
        }


        private static void ShowItemDescription(Item item)
        {
            if (Application.Current.MainWindow is IItemDescription mainWindow)
                mainWindow.ShowItemDescription(item.Description);
        }

        private static void HideItemDescription()
        {
            if (Application.Current.MainWindow is IItemDescription mainWindow)
                mainWindow.HideItemDescription();
        }


        private static void UseItem(Item item)
        {
            if (Application.Current.MainWindow is IUseItem mainWindow)
                mainWindow.UseItem(item.Name);
        }
        public static void CreateInventoryUI(PlayerInventory inventory, int rows, int columns, InventoryManager manager)
        {
            if (InventoryGrid != null)
                return;

            InventoryGrid = new Grid
            {
                Background = new SolidColorBrush(Colors.Transparent),
            };

            InventoryBackground = new Image
            {
                Source = itemDictionary[typeof(InventorySlots)].Image,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            InventoryGrid.Children.Add(InventoryBackground);
            Grid.SetRow(InventoryBackground, 0);
            Grid.SetColumn(InventoryBackground, 0);
            Grid.SetRowSpan(InventoryBackground, rows);
            Grid.SetColumnSpan(InventoryBackground, columns);

            var inventoryTable = new Grid
            {
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            int itemSize = 120; 
            int margin = 10; 

            for (int i = 0; i < rows; i++)
            {
                inventoryTable.RowDefinitions.Add(new RowDefinition { Height = new GridLength(itemSize + margin) });
            }
            for (int i = 0; i < columns; i++)
            {
                inventoryTable.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(itemSize + margin) });
            }

            int itemIndex = 0;
            foreach (var item in inventory.GetAllItems().Keys)
            {
                if (itemIndex >= rows * columns)
                    break;

                var itemControl = CreateItemControl((Item)item, itemSize, manager);
                Grid.SetRow(itemControl, itemIndex / columns);
                Grid.SetColumn(itemControl, itemIndex % columns);
                inventoryTable.Children.Add(itemControl);
                itemIndex++;
            }

            InventoryGrid.Children.Add(inventoryTable);
            Grid.SetRow(inventoryTable, 0);
            Grid.SetColumn(inventoryTable, 0);
            Grid.SetRowSpan(inventoryTable, rows);
            Grid.SetColumnSpan(inventoryTable, columns);

            InventoryGridContainer.Children.Add(InventoryGrid);
        }



        public PlayerInventory GetInventory()
        {
            return Inventory;
        }

        public void UpdateInventoryDisplay()
        {
            try
            {
                if (InventoryGrid != null)
                {
                    var inventoryTable = (Grid)InventoryGrid.Children[1];
                    inventoryTable.Children.Clear();

                    int itemIndex = 0;
                    foreach (var item in Inventory.GetAllItems().Keys)
                    {
                        if (itemIndex >= 2 * 2)
                            break;

                        int itemSize = 100;
                        var itemControl = CreateItemControl((Item)item, itemSize, this);
                        Grid.SetRow(itemControl, itemIndex / 2);
                        Grid.SetColumn(itemControl, itemIndex % 2);
                        inventoryTable.Children.Add(itemControl);
                        itemIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обновлении отображения инвентаря: {ex.Message}");
            }
        }


    }
}