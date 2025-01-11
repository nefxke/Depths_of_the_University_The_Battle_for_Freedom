using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Character;

namespace GameManager
{
    public class Collider : GameObject
    {
        public Rect Bounds { get; set; }

        public Collider(double x, double y, double width, double height)
            : base(null, new Point(x, y)) 
        {
            Bounds = new Rect(x, y, width, height);
            Console.WriteLine($"Создан коллайдер: {Bounds}");
        }

        public bool Intersects(Collider other)
        {
            return Bounds.IntersectsWith(other.Bounds);
        }

        public override void Render(Canvas canvas)
        {
            // Коллайдеры не рендерятся визуально
        }
    }

    public abstract class GameObject
    {
        public Point Position { get; set; }
        public BitmapImage Sprite { get; set; }
        public UIElement RenderedElement { get; set; }

        public GameObject(BitmapImage sprite, Point position)
        {
            Sprite = sprite ?? throw new ArgumentNullException(nameof(sprite), "Sprite не может быть null");
            Position = position;
        }

        public virtual void Render(Canvas canvas)
        {
            if (Sprite == null)
            {
                MessageBox.Show($"Ошибка: у объекта {GetType().Name} нет текстуры!");
                return;
            }

            if (RenderedElement == null)
            {
                Image image = new Image
                {
                    Source = Sprite,
                    Width = Sprite.PixelWidth,
                    Height = Sprite.PixelHeight
                };

                Canvas.SetLeft(image, Position.X);
                Canvas.SetTop(image, Position.Y);
                canvas.Children.Add(image);
                RenderedElement = image;

                Console.WriteLine($"Объект {GetType().Name} ({this}) нарисован на {Position}");
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name} at {Position}";
        }
    }

    public class FloorTile : GameObject
    {
        public FloorTile(BitmapImage sprite, Point position) : base(sprite, position) { }

        public override void Render(Canvas canvas)
        {
            if (RenderedElement == null)
            {
                RenderedElement = new Image
                {
                    Source = Sprite,
                    Width = Sprite.PixelWidth,
                    Height = Sprite.PixelHeight
                };
                Canvas.SetLeft(RenderedElement, Position.X);
                Canvas.SetTop(RenderedElement, Position.Y);
                canvas.Children.Add(RenderedElement);
                Canvas.SetZIndex(RenderedElement, 0);
            }
        }
    }

    public class RoomTile : GameObject
    {
        public RoomTile(BitmapImage sprite, Point position) : base(sprite, position) { }

        public override void Render(Canvas canvas)
        {
            if (RenderedElement == null)
            {
                RenderedElement = new Image
                {
                    Source = Sprite,
                    Width = Sprite.PixelWidth,
                    Height = Sprite.PixelHeight
                };
                Canvas.SetLeft(RenderedElement, Position.X);
                Canvas.SetTop(RenderedElement, Position.Y);
                canvas.Children.Add(RenderedElement);
                Canvas.SetZIndex(RenderedElement, 1);
                Console.WriteLine($"Отрендерён {this}");
            }
        }
    }

    public class RoomTileChunk : GameObject
    {
        public RoomTileChunk(BitmapImage sprite, Point position, double chunkWidth, double chunkHeight)
            : base(sprite, position)
        {
            RenderedElement = new Image
            {
                Source = sprite,
                Width = chunkWidth,
                Height = chunkHeight
            };
            Canvas.SetLeft(RenderedElement, position.X);
            Canvas.SetTop(RenderedElement, position.Y);
        }

        public override void Render(Canvas canvas)
        {
            if (!canvas.Children.Contains(RenderedElement))
            {
                canvas.Children.Add(RenderedElement);
            }
        }
    }

    public class WallTile : GameObject 
    {
        public WallTile(BitmapImage sprite, Point position) : base(sprite, position) { }

        public override void Render(Canvas canvas)
        {
            if (RenderedElement == null)
            {
                RenderedElement = new Image
                {
                    Source = Sprite,
                    Width = Sprite.PixelWidth,
                    Height = Sprite.PixelHeight
                };
                Canvas.SetLeft(RenderedElement, Position.X);
                Canvas.SetTop(RenderedElement, Position.Y);
                canvas.Children.Add(RenderedElement);
                Canvas.SetZIndex(RenderedElement, 2);
            }
        }
    }

    public class Door : GameObject
    {
        public Door(BitmapImage sprite, Point position) : base(sprite, position) { }

        public override void Render(Canvas canvas)
        {
            if (RenderedElement == null)
            {
                RenderedElement = new Image
                {
                    Source = Sprite,
                    Width = Sprite.PixelWidth,
                    Height = Sprite.PixelHeight
                };
                Canvas.SetLeft(RenderedElement, Position.X);
                Canvas.SetTop(RenderedElement, Position.Y);
                canvas.Children.Add(RenderedElement);
                Canvas.SetZIndex(RenderedElement, 2);
            }
        }
    }
}
