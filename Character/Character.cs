using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Character
{
    public abstract class Character : INotifyPropertyChanged
    {
        public Point position;                                                      // Текущая позиция персонажа на Canvas
        public const int character_size = 100;
        public int currentSpriteIndex;                                              // Индекс текущего спрайта
        public int SpriteAnimationInterval;                                         // Интервал смены спрайтов в миллисекундах

        public static readonly Random Random = new();
        public CharacterClass character_class { get; set; }
        public string character_name { get; set; }

        public int exp { get; set; }


        //////////////////////////////////////////////////////////////////////////
        //  для вывода на канвас
        private int _hp;
        public virtual int hp
        {
            get => _hp;
            set
            {
                if (_hp != value)
                {
                    _hp = value;
                    OnPropertyChanged(nameof(hp));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //////////////////////////////////////////////////////////////////////////

        public double MAX_HP { get; set; }
        public int energy { get; set; }
        public byte strength { get; set; }
        public int dexterity { get; set; }
        public int intellect { get; set; }
        public double speed { get; set; }

        public enum CharacterClass
        {
            // Игрок
            Technician,
            Humanitarian,
            Cadet,

            // Враги
            ExhaustedStudent,
            ExcellentStudent,
            GhostOfProfessor,
            GhostOfDean,
            SpiritOfUniversity,

            //  кот
            Cat
        }
        public bool CanMove { get; set; } = true;

        public Character(string name, CharacterClass charClass, int experience, int health, int energy, byte str, byte dex, byte intel, int speed)
        {
            character_name = name;
            character_class = charClass;
            exp = experience;
            this.energy = energy;
            strength = str;
            dexterity = dex;
            intellect = intel;
            hp = health;
            MAX_HP = health * 1.5;
            this.speed = speed;
        }
        public static double CalculateDistance(Image img1, Image img2)
        {
            double x1 = Canvas.GetLeft(img1) + img1.Width / 2;
            double y1 = Canvas.GetTop(img1) + img1.Height / 2;
            double x2 = Canvas.GetLeft(img2) + img2.Width / 2;
            double y2 = Canvas.GetTop(img2) + img2.Height / 2;

            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        public virtual byte CalculateDamage()
        {
            return (byte)(strength / 2 + (dexterity + intellect) / 4);
        }
    }
}