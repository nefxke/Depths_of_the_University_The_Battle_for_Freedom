using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Character
{
    public class Player : Character, INotifyPropertyChanged
    {
        public static Image playerImage;
        public static BitmapImage[] playerSprites;

        public double attackInterval;

        public DateTime lastAttackTime = DateTime.MinValue;  // время последней атаки
        private DispatcherTimer healingTimer;                // интервал лечения
        private DateTime lastReceivedDamageTime;

        public int playerVisibilityRange = 400;

        private int energyRegenerationBonus;
        private DispatcherTimer energyRegenerationTimer;
        private DateTime energyRegenerationEndTime;

        private byte damageBonus;
        private DispatcherTimer damageBonusTimer;
        private DateTime damageBonusEndTime;

        private int healthRegenerationBonus;
        private DispatcherTimer healthRegenerationTimer;
        private DateTime healthRegenerationEndTime;

        private DispatcherTimer energyHealingTimer; // Таймер для восстановления энергии

        // Делегат для события
        public delegate void ExpGainedEventHandler(string message);

        // Событие, которое будет вызываться при получении опыта
        public static event ExpGainedEventHandler ExpGained;



        public Player(string name, CharacterClass charClass, Point initialPosition)
            : base(name, charClass, 100, 100, 1, 1, 1, 1, 3)
        {
            position = initialPosition;
            level = 1;

            // Инициализация спрайтов игрока
            playerSprites = new BitmapImage[]
            {
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_stand_left.png")),         //  0
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_stand_right.png")),        //  1
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_stand_back_left.png")),    //  2
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_stand_back_right.png")),   //  3
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_walk_left_1.png")),        //  4
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_walk_left_2.png")),        //  5
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_walk_right_1.png")),       //  6
        new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\player\\player_walk_right_2.png")),       //  7
            };

            // Инициализация изображения игрока
            playerImage = new Image
            {
                Width = character_size,
                Height = character_size,
                Source = playerSprites[0]
            };

            Canvas.SetLeft(playerImage, initialPosition.X);
            Canvas.SetTop(playerImage, initialPosition.Y);

            // Настройка таймера лечения
            healingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            healingTimer.Tick += HealingTick;
            healingTimer.Start();

            // Настройка таймера восстановления энергии
            energyHealingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.25)
            };
            energyHealingTimer.Tick += EnergyHealingTick;
            energyHealingTimer.Start();

            energyRegenerationBonus = 0;
            energyRegenerationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            energyRegenerationTimer.Tick += EnergyRegenerationTick;

            damageBonus = 0;
            damageBonusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            damageBonusTimer.Tick += DamageBonusTick;

            healthRegenerationBonus = 0;
            healthRegenerationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            healthRegenerationTimer.Tick += HealthRegenerationTick;
        }

        private void EnergyHealingTick(object sender, EventArgs e)
        {
            if ((DateTime.Now - lastReceivedDamageTime).TotalSeconds >= 3)
            {
                if (energy < 100)
                    energy = Math.Min(energy + 1 + energyRegenerationBonus, 100);
            }
        }

        private void EnergyRegenerationTick(object sender, EventArgs e)
        {
            if (DateTime.Now >= energyRegenerationEndTime)
            {
                energyRegenerationBonus = 0;
                energyRegenerationTimer.Stop();
            }
            else
            {
                if (energy < 100)
                    energy = Math.Min(energy + 1 + energyRegenerationBonus, 100);
            }
        }

        public void ApplyEnergyRegenerationBonus(int bonus, int duration)
        {
            energyRegenerationBonus = bonus;
            energyRegenerationEndTime = DateTime.Now.AddSeconds(duration);
            energyRegenerationTimer.Start();
        }

        public void ApplyDamageBonus(int bonus, int duration)
        {
            damageBonus = (byte)bonus;
            damageBonusEndTime = DateTime.Now.AddSeconds(duration);
            damageBonusTimer.Start();
        }

        private void DamageBonusTick(object sender, EventArgs e)
        {
            if (DateTime.Now >= damageBonusEndTime)
            {
                damageBonus = 0;
                damageBonusTimer.Stop();
            }
            else
            {
                strength += damageBonus;
            }
        }

        private void HealthRegenerationTick(object sender, EventArgs e)
        {
            if (DateTime.Now >= healthRegenerationEndTime)
            {
                healthRegenerationBonus = 0;
                healthRegenerationTimer.Stop();
            }
            else
            {
                if (hp < MAX_HP)
                    hp = Math.Min(hp + 1 + healthRegenerationBonus, (int)MAX_HP);
            }
        }

        public void ApplyHealthRegenerationBonus(int bonus, int duration)
        {
            healthRegenerationBonus = bonus;
            healthRegenerationEndTime = DateTime.Now.AddSeconds(duration);
            healthRegenerationTimer.Start();
        }

        private void HealingTick(object sender, EventArgs e)
        {
            if ((DateTime.Now - lastReceivedDamageTime).TotalSeconds >= 3)
            {
                if (hp < MAX_HP / 1.5)
                    hp = Math.Min(hp + 1 + healthRegenerationBonus, (int)MAX_HP);
            }
        }

        public static void SetSprite(int spriteIndex)
        {
            if (playerSprites != null && spriteIndex >= 0 && spriteIndex < playerSprites.Length)
            {
                playerImage.Source = playerSprites[spriteIndex];
            }
        }


        ////////////////////////////////////////////
        ///                 БОЕВКА              ////
        ///////////////////////////////////////////

        public void TakeDamage(int damage)
        {
            hp = Math.Max(0, hp - damage);
            lastReceivedDamageTime = DateTime.Now;
        }

        public static void AddExp(Player player, int exp)
        {
            player.exp += exp;
            if (player.exp >= 100)
            {
                player.exp -= 100;
                player.level++;
                player.strength++;
                player.dexterity++;
                player.intellect++;
                player.energy = 100;
                player.hp = (int)player.MAX_HP;

                ExpGained?.Invoke("Новый уровень!");
            }
        }

        public void ResetStats()
        {
            hp = 100;
            energy = 50;
            exp = exp - 25 < 0 ? 0 : exp - 25;

            if (level != 0)
            {
                if (strength > 1)
                    strength -= 1;
                if (dexterity > 1)
                    dexterity -= 1;
                if (intellect > 1)
                    intellect -= 1;
            }
        }

        ////////////////////////////////////////////
        //  ОБНОВЛЕНИЕ ХАРАКТЕРИСТИКИ НА ЭКРАНЕ  //
        ///////////////////////////////////////////

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _hp;
        public override int hp
        {
            get => _hp;
            set
            {
                if (_hp != value && !double.IsNaN(value))
                {
                    _hp = value;
                    OnPropertyChanged(nameof(hp));
                }
            }
        }

        private int _maxHp = 100;
        public int MaxHp
        {
            get => _maxHp;
            set
            {
                _maxHp = value;
                OnPropertyChanged(nameof(MaxHp));
            }
        }

        private int _energy;
        public new int energy
        {
            get => _energy;
            set
            {
                _energy = value;
                OnPropertyChanged(nameof(energy));
            }
        }

        private int _exp;
        public new int exp
        {
            get => _exp;
            set
            {
                _exp = value;
                OnPropertyChanged(nameof(exp));
            }
        }

        private int _level;
        public int level
        {
            get => _level;
            set
            {
                _level = value;
                OnPropertyChanged(nameof(level));
            }
        }
    }

    public class Technician : Player
    {
        public Technician(string name, Point initialPosition)
            : base(name, CharacterClass.Technician, initialPosition)
        {
            attackInterval = 300;

            hp = 100;
            strength = 3;
            dexterity = 2;
            intellect = 5;
            energy = 40;
            speed = 2;
        }
    }

    public class Humanitarian : Player
    {
        public Humanitarian(string name, Point initialPosition)
            : base(name, CharacterClass.Humanitarian, initialPosition)
        {
            attackInterval = 400;

            strength = 2;
            dexterity = 3;
            intellect = 5;
            hp = 85;
            energy = 60;
            speed = 2.5;
        }
    }
}
