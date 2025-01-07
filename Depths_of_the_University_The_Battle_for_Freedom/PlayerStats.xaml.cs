using Character;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Depths_of_the_University_The_Battle_for_Freedom
{
    public partial class PlayerStats : UserControl
    {
        public PlayerStats()
        {
            InitializeComponent();
        }

        public void BindToPlayer(Player player)
        {
            DataContext = player;
            player.PropertyChanged += Player_PropertyChanged;
            UpdateHealthBar(player.hp);
        }

        private void Player_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var player = DataContext as Player;
            if (player != null)
            {
                switch (e.PropertyName)
                {
                    case nameof(Player.hp):
                        UpdateHealthBar(player.hp);
                        break;
                }
            }
        }

        private void UpdateHealthBar(int currentHp)
        {
            double healthWidth;

            if (currentHp <= 100)
            {
                healthWidth = (currentHp / 100.0) * 200;
            }
            else
            {
                healthWidth = 200 + ((currentHp - 100) / 50.0) * 50;
            }

            var healthAnimation = new DoubleAnimation
            {
                To = healthWidth,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            HealthRectangle.BeginAnimation(Rectangle.WidthProperty, healthAnimation);
            HealthText.Text = currentHp.ToString();
        }
    }
}
