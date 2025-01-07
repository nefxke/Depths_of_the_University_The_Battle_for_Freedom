using Character;
using Interface;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GameManager
{
    public class BattleSystem
    {
        const double GAME_DIFFICULTY_FACTOR = 1; //  0.75 - легкий, 1 - нормальный, 1.25 - повышенный, 2 - невозможный

        public static void InitializeEnemyAttackTimers()
        {
            foreach (var enemy in GameData.Enemies)
            {
                var attackTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(enemy.attackInterval)
                };

                if (!GameData.enemyTypes.Contains(enemy.GetType()))
                    GameData.enemyTypes.Add(enemy.GetType());

                attackTimer.Start();
                GameData.enemyAttackTimers[enemy] = attackTimer;
            }
        }

        public static bool IsPositionFree(Point position, double minDistance = 50)
        {
            foreach (var enemy in GameData.Enemies)
            {
                double distance = Player.CalculateDistance(Player.playerImage, enemy.enemyImage);
                if (distance < minDistance)
                    return false;
            }
            return true;
        }

        public static int CalculateDamageFrom(Character.Character person, InventoryManager manager)
        {
            if (person != null)
            {
                double damage = person.CalculateDamage();
                if (person is Player)
                {
                    byte maxPenDamage = 0;
                    foreach (var item in manager.Inventory.GetAllItems().Keys)
                    {
                        if (item is SimplePen or UpgradedPen or BestStudentPen)
                        {
                            if (item.DamageBonus > maxPenDamage)
                                maxPenDamage = item.DamageBonus;
                        }
                    }
                    damage += maxPenDamage;
                }
                else if (person is Enemy && GAME_DIFFICULTY_FACTOR != 1)
                    damage *= GAME_DIFFICULTY_FACTOR;

                return (int)damage;
            }
            return 0;
        }

        public static void AttackEnemy(Enemy enemy, double currentDistance, double maxDistance, InventoryManager manager, IPlayerUI playerUI)
        {
            if (currentDistance <= maxDistance)
            {
                if ((DateTime.Now - GameData.Player.lastAttackTime).TotalMilliseconds >= GameData.Player.attackInterval)
                {
                    int damage = CalculateDamageFrom(GameData.Player, manager);
                    if (GameData.Player.energy >= damage / 1.5)
                    {
                        enemy.hp -= damage;
                        GameData.Player.energy -= (int)(damage / 1.5);
                        GameData.Player.lastAttackTime = DateTime.Now;
                    }
                    else
                    {
                        playerUI.ShowEnergyMessage("Недостаточно энергии для нанесения удара!");
                        playerUI.FlashEnergyValue();
                    }
                }
            }
        }

        public static void HandlePlayerAttack(List<Enemy> enemies, InventoryManager manager, IPlayerUI playerUI)
        {
            if (GameData.Enemies.Count == 0)
                return;

            var currentDamage = CalculateDamageFrom(GameData.Player, manager);
            if (GameData.Player.energy < currentDamage)
            {
                playerUI.ShowEnergyMessage("Недостаточно энергии для нанесения удара!");
                playerUI.FlashEnergyValue();
                return;
            }

            foreach (var enemy in enemies)
            {
                double distance = Player.CalculateDistance(Player.playerImage, enemy.enemyImage);
                AttackEnemy(enemy, distance, GameData.MAX_ATTACK_DISTANCE, manager, playerUI);
            }
        }

        public static void EnemyAttackTick(object sender, EventArgs e)
        {
            HandleEnemyAttack();
        }

        public static void HandleEnemyAttack()
        {
            foreach (var enemy in GameData.Enemies)
            {
                if (GameData.Player != null)
                {
                    double distanceToPlayer = Player.CalculateDistance(Player.playerImage, enemy.enemyImage);
                    if (distanceToPlayer <= GameData.MAX_ENEMY_ATTACK_DISTANCE)
                    {
                        enemy.Attack(GameData.Player, distanceToPlayer, GameData.MAX_ENEMY_ATTACK_DISTANCE);
                    }
                }
            }
        }

        public static Enemy SpawnEnemy(Enemy enemy, Canvas gameCanvas)
        {
            double canvasWidth = gameCanvas.ActualWidth;
            double canvasHeight = gameCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                MessageBox.Show("Canvas not ready!");
                return null;
            }

            Point spawnPoint;
            do
            {
                double x = GameData.random.NextDouble() * (canvasWidth - Enemy.character_size);
                double y = GameData.random.NextDouble() * (canvasHeight - Enemy.character_size);
                spawnPoint = new Point(x, y);
            } while (!IsPositionFree(spawnPoint));

            enemy.position = spawnPoint;
            enemy.enemyImage = GameData.CreateTexture(enemy.position, enemy.enemySprites[0]);
            gameCanvas.Children.Add(enemy.enemyImage);

            enemy.hpText = GameData.CreateEnemyHpText(enemy);
            gameCanvas.Children.Add(enemy.hpText);

            var attackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(enemy.attackInterval) // интервал атаки
            };

            attackTimer.Tick += (s, args) => HandleEnemyAttack();

            attackTimer.Start();
            GameData.enemyAttackTimers[enemy] = attackTimer;

            GameData.Enemies.Add(enemy);
            return enemy;
        }

        public static void SpawnRandomEnemies(int count, Canvas gameCanvas)
        {
            Random random = new();

            for (int i = 0; i < count; ++i)
            {
                if (GameData.enemyTypes != null && GameData.enemyTypes.Count > 0)
                {
                    int randomIndex = random.Next(GameData.enemyTypes.Count);
                    Type enemyType = GameData.enemyTypes[randomIndex];

                    string name = "Enemy" + (i + 1);
                    Point initialPosition = new(random.Next(0, 1280), random.Next(0, 720));       // изменить на текущее разрешение

                    Enemy newEnemy = null;
                    if (enemyType == typeof(ExhaustedStudent)) newEnemy = new ExhaustedStudent(name, initialPosition);
                    else if (enemyType == typeof(ExcellentStudent)) newEnemy = new ExcellentStudent(name, initialPosition);

                    if (newEnemy != null)
                        SpawnEnemy(newEnemy, gameCanvas);
                }
            }
        }

        public static Type GetEnemyTypeForDrop(Enemy enemy, InventoryManager manager)
        {
            foreach (var type in Enemy.EnemyTypes)
                if (enemy.GetType() == type)
                    return type;
            return null;
        }

        public static void CheckForEnemyDeath(Canvas gameCanvas, InventoryManager manager)
        {
            if (GameData.Enemies.Count <= 0)
                return;

            List<Enemy> enemiesToRemove = new();

            foreach (var enemy in GameData.Enemies)
            {
                if (enemy != null)
                {
                    if (enemy.hpText != null)
                        enemy.hpText.Text = enemy.hp.ToString();

                    if (enemy.hp <= 0)
                    {
                        if (enemy.hpText != null)
                            gameCanvas.Children.Remove(enemy.hpText);

                        if (enemy.enemyImage != null)
                            gameCanvas.Children.Remove(enemy.enemyImage);

                        // Останавливаем таймер атаки врага
                        if (GameData.enemyAttackTimers.ContainsKey(enemy))
                        {
                            GameData.enemyAttackTimers[enemy].Stop();
                            GameData.enemyAttackTimers.Remove(enemy);
                        }

                        enemiesToRemove.Add(enemy);
                        Player.AddExp(GameData.Player, enemy.exp);

                        var chance = EventManager.GetDropChanceFrom(enemy);
                        EventManager.GiveRandomItemToPlayer(manager, chance);   //  со случайным шансом выдает какой-то предмет после устранения врага
                    }
                }
            }

            //  удаляем врага с канваса
            foreach (var deadEnemy in enemiesToRemove)
            {
                if (GameData.enemyAttackTimers.ContainsKey(deadEnemy))
                {
                    GameData.enemyAttackTimers[deadEnemy].Stop();
                    GameData.enemyAttackTimers.Remove(deadEnemy);
                }

                GameData.Enemies.Remove(deadEnemy);
            }

            if (GameData.Enemies.Count == 0)
                SpawnRandomEnemies(new Random().Next(1, 4), gameCanvas);
        }




        public static void CheckForPlayerDeath()
        {
            if (GameData.Player != null && GameData.Player.hp <= 0)
            {
                Canvas.SetLeft(Player.playerImage, 0);
                Canvas.SetTop(Player.playerImage, 0);

                GameData.Player.ResetStats();
            }
        }
    }
}
