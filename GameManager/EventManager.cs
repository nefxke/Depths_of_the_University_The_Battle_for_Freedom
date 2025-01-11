using Character;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace GameManager
{
    public class EventManager
    {
        public static event Action<string> ItemReceived;

        public static readonly byte CAT_DROP_CHANCE_PERCENT = 50;
        public static readonly byte EXHAUSTED_STUDENT_DROP_CHANCE_PERCENT = 5;
        public static readonly byte EXCELLENT_STUDENT_DROP_CHANCE_PERCENT = 15;
        public static readonly byte CAT_BONUS_EXP = 10;
        public static event Action<InventoryManager, Level> CatFedAndRunningAway;

        public static void PlayerFeedCat(InventoryManager manager, Level level)
        {
            const double feedDistance = Character.Character.character_size * 3 / 4;
            Cat nearestCat = null;
            double minDistance = double.MaxValue;

            foreach (var cat in GameData.Cats)
            {
                var distance = Character.Character.CalculateDistance(GameData.Player.playerImage, cat.catImage);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestCat = cat;
                }
            }

            if (nearestCat != null && minDistance <= feedDistance)
            {
                Player.AddExp(GameData.Player, CAT_BONUS_EXP);
                GiveRandomItemToPlayer(manager, CAT_DROP_CHANCE_PERCENT);
                CatFedAndRunningAway?.Invoke(manager, level);
            }
        }
        public static void GiveRandomItemToPlayer(InventoryManager manager, byte chance)
        {
            try
            {
                var allItems = Enum.GetNames(typeof(Item.AllItems));
                var allItemCount = allItems.Length;

                if (allItemCount == 0)
                    return;

                var randomNumber = new Random().Next(0, 100);

                if (randomNumber < chance)
                {
                    var randomIndex = new Random().Next(0, allItemCount - 1);
                    var randomItemName = allItems[randomIndex];
                    var randomItemType = Type.GetType($"GameManager.{randomItemName}");

                    if (randomItemType != null)
                    {
                        var randomItem = (Item)Activator.CreateInstance(randomItemType);

                        if (randomItem != null)
                        {
                            var count = new Random().Next(1, 3);
                            var imagePath = InventoryManager.GetItemImagePath(randomItemType);
                            if (imagePath != null)
                                randomItem.Image = InventoryManager.LoadImage(imagePath);

                            manager.Inventory.AddItem(randomItem);

                            if (randomItem.Image == null)
                                Debug.WriteLine($"Изображение для предмета {randomItem.Name} не загружено.");

                            ItemReceived?.Invoke(randomItem.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при добавлении случайного предмета: {ex.Message}");
            }
        }
        public static byte GetDropChanceFrom(Character.Character entity)
        {
            byte chance = 0;
            if (entity.GetType() == typeof(ExhaustedStudent))
                chance = EXHAUSTED_STUDENT_DROP_CHANCE_PERCENT;

            if (entity.GetType() == typeof(ExcellentStudent))
                chance = EXCELLENT_STUDENT_DROP_CHANCE_PERCENT;

            //  если будут другие враги, дописать


            if (entity.GetType() == typeof(Cat))
                chance = CAT_DROP_CHANCE_PERCENT;

            return chance;
        }

        public static void UpdateEnemyMovementsCore()
        {
            foreach (var enemy in GameData.Enemies)
                Movement.MoveEnemy(enemy, GameData.GameCanvas);
        }

        public static void UpdateEnemyMovements(object sender, EventArgs e)
        {
            UpdateEnemyMovementsCore();
        }
        public static void UpdatePlayerPositionOnCanvas()
        {
            if (GameData.Player.playerImage != null)
            {
                Canvas.SetLeft(GameData.Player.playerImage, GameData.Player.position.X);
                Canvas.SetTop(GameData.Player.playerImage, GameData.Player.position.Y);
            }
        }
        public static void UpdateEnemyPositionsOnCanvas()
        {
            foreach (var enemy in GameData.Enemies)
            {
                if (enemy.enemyImage != null)
                {
                    Canvas.SetLeft(enemy.enemyImage, enemy.position.X);
                    Canvas.SetTop(enemy.enemyImage, enemy.position.Y);
                }
            }
        }




        
    }
}