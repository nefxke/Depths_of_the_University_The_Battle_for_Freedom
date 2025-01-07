using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GameManager
{
    public class Cat : Character.Character
    {
        public Image catImage;
        public BitmapImage[] catSprites;
        public event Action<Cat> CatRemoved;

        public Cat(string name, CharacterClass charClass, Point initialPosition) : base(name, charClass, 100, 100, 0, 0, 0, 0, 40)
        {
            catSprites = new BitmapImage[]
            {
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_sit_right.png")),        //  0
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_sit_left.png")),         //  1
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_jump_start_right.png")), //  2
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_jump_start_left.png")),  //  3
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_jump_right.png")),       //  4
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_jump_left.png")),        //  5
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_jump_end_right.png")),   //  6
                new(new Uri("C:\\Users\\nefxk\\OneDrive\\Desktop\\STUDY\\ИТИП\\Курсовая\\Depths_of_the_University_The_Battle_for_Freedom\\images\\cat\\cat_jump_end_left.png")),    //  7
            };

            catImage = new Image
            {
                Width = character_size,
                Height = character_size,
                Source = catSprites[0]
            };

            position = initialPosition;
        }

        public static void SetSprite(Cat cat, int spriteIndex)
        {
            if (cat.catSprites != null && spriteIndex >= 0 && spriteIndex < cat.catSprites.Length)
                cat.catImage.Source = cat.catSprites[spriteIndex];
        }
    }
}
