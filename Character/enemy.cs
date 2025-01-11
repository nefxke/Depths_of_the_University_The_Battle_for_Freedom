using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Character
{
    public class Enemy : Character
    {
        private Player player;

        public static List<Type> EnemyTypes = new() {typeof(ExcellentStudent), typeof(ExhaustedStudent) };

        public Image enemyImage;
        public BitmapImage[] enemySprites;

        public DateTime lastAttackTime = DateTime.MinValue;         // Время последней атаки
        private DispatcherTimer attackTimer;                        // Таймер для периодических атак
        public int attackInterval { get; set; } = 1000;             // Интервал между атаками (в миллисекундах)

        public TextBlock hpText { get; set; }

        public virtual int CalculateDamage() { return strength; }

        public DateTime timeLostPlayer = DateTime.Now;
        public DateTime LastDirectionChangeTime { get; set; } = DateTime.Now;
        public DateTime movementStartTime { get; set; } // Время начала текущего движения
        public bool isMoving { get; set; } = true; // Флаг движения

        public double maxAttackDistance = character_size * 1.25;

        //  движение
        public bool IsMovingRandomly { get; set; }
        public bool IsWaiting { get; set; }
        public DateTime MoveStartTime { get; set; }
        public DateTime WaitStartTime { get; set; }
        public DateTime LastSeenPlayerTime { get; set; }
        public double MoveDuration { get; set; }
        public double WaitDuration { get; set; }
        public double TimeToWaitBeforeMove { get; set; } 
        public Point RandomDirection { get; set; }

        public Enemy(string name, CharacterClass charClass, int expReward, Point initialPosition, int updatePositionInterval, int _attackInterval)
            : base(name, charClass, expReward, 100, 20, 1, 1, 1, 1)
        {
            attackInterval = _attackInterval;
            position = initialPosition;

            //  движение
            IsMovingRandomly = false;
            IsWaiting = false;
            MoveStartTime = DateTime.MinValue;
            WaitStartTime = DateTime.MinValue;
            LastSeenPlayerTime = DateTime.MinValue;
            MoveDuration = 0;
            WaitDuration = 0;
            TimeToWaitBeforeMove = new Random().Next(1, 4); // от 1 до 3 секунд
            RandomDirection = new Point(0, 0);

            // Инициализация таймера для атак
            attackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            attackTimer.Tick += AttackTick;
            attackTimer.Start();

            // Инициализация текста HP
            hpText = new TextBlock
            {
                Text = $"HP: {hp}",
                Foreground = Brushes.Red,
                FontSize = 12,
                Visibility = Visibility.Hidden 
            };
            Canvas.SetLeft(hpText, position.X + 50);
            Canvas.SetTop(hpText, position.Y - 20); 
        }




        // Таймер атаки, который срабатывает каждую секунду
        private void AttackTick(object sender, EventArgs e)
        {
            if (player != null)
            {
                double dx = player.position.X - position.X;
                double dy = player.position.Y - position.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                // Если игрок находится в радиусе атаки, то атакуем
                Attack(player, distance, maxAttackDistance);
            }
        }
        public void Attack(Player player, double currentDistance, double maxAttackDistance)
        {
            if ((DateTime.Now - lastAttackTime).TotalMilliseconds >= attackInterval)
            {
                int damage = CalculateDamage();
                player.TakeDamage(damage);
                lastAttackTime = DateTime.Now;
            }
        }

        // Метод для установки ссылки на игрока
        public void SetPlayer(Player player)
        {
            this.player = player;
        }


        // Установка спрайта
        public void SetSprite(int spriteIndex)
        {
            if (spriteIndex >= 0 && spriteIndex < enemySprites.Length)
                enemyImage.Source = enemySprites[spriteIndex];
        }

        // Свойство HP
        private int _hp;
        public override int hp
        {
            get => _hp;
            set
            {
                _hp = value;
                OnPropertyChanged(nameof(hp));
            }
        }
    }


    public class ExhaustedStudent : Enemy
    {
        public ExhaustedStudent(string name, Point initialPosition)
            : base("Exhausted Student", CharacterClass.ExhaustedStudent, 20, initialPosition, 1, 750)
        {
            hp = 25; // Здоровье
            speed *= 0.75;

            strength = 2;
            dexterity = 0;
            intellect = 2;

            // Инициализация спрайтов
            enemySprites = new BitmapImage[]
            {
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\exhausted_student\\exhausted_student_stand_right.png")),
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\exhausted_student\\exhausted_student_stand_left.png")),
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\exhausted_student\\exhausted_student_walk_right.png")),
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\exhausted_student\\exhausted_student_walk_left.png"))
            };

            // Инициализация изображения
            enemyImage = new Image
            {
                Width = character_size,
                Height = character_size,
                Source = enemySprites[0]
            };
            Canvas.SetLeft(enemyImage, initialPosition.X);
            Canvas.SetTop(enemyImage, initialPosition.Y);
        }

        public override int CalculateDamage()
        {
            return (strength + dexterity / 10);
        }
    }
    public class ExcellentStudent : Enemy
    {
        public ExcellentStudent(string name, Point initialPosition)
            : base("Student on Scholarship", CharacterClass.ExcellentStudent, 50, initialPosition, 1, 500)
        {
            hp = 50;
            speed *= 0.9;

            strength = 1;
            dexterity = 2;
            intellect = 5;

            // Инициализация спрайтов
            enemySprites = new BitmapImage[]
            {
            new BitmapImage(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\excellent_student\\excellent_student_stand_left.png")),
            new BitmapImage(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\excellent_student\\excellent_student_stand_right.png")),
            new BitmapImage(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\excellent_student\\excellent_student_walk_left.png")),
            new BitmapImage(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\enemy\\excellent_student\\excellent_student_walk_right.png"))
            };

            // Инициализация текущего спрайта 
            enemyImage = new Image
            {
                Width = character_size,
                Height = character_size,
                Source = enemySprites[0]
            };
            Canvas.SetLeft(enemyImage, initialPosition.X);
            Canvas.SetTop(enemyImage, initialPosition.Y);
        }

        public override int CalculateDamage()
        {
            return (int)(strength / 2 + intellect * 2);
        }
    }



    //public class DeanGhost : Enemy{}
    //public class HonorStudent : Enemy {}
    //public class UniversitySpirit : Enemy{}
}