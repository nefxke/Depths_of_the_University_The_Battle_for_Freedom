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
        }
    }

    public abstract class GameObject
    {
        public Point Position { get; set; }
        public BitmapImage Sprite { get; set; }
        public UIElement RenderedElement { get; set; }
        public double Opacity { get; set; } = 1.0;

        protected GameObject() { }
        protected GameObject(BitmapImage sprite)
        {
            Sprite = sprite;
        }
        public abstract void Render(Canvas canvas);
    }

    public class FloorTile : GameObject
    {
        public FloorTile(BitmapImage sprite) : base(sprite) { }

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
            else
            {
                Canvas.SetLeft(RenderedElement, Position.X);
                Canvas.SetTop(RenderedElement, Position.Y);
            }
        }
    }
    public class RoomTile : GameObject
    {
        public RoomTile(BitmapImage sprite) : base(sprite) { }

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
                Canvas.SetZIndex(RenderedElement, 1); // Устанавливаем z-index для комнат
                Console.WriteLine($"Отрендерён объект комнаты: {this.GetType().Name} на позиции {Position}");
            }
            else
            {
                Canvas.SetLeft(RenderedElement, Position.X);
                Canvas.SetTop(RenderedElement, Position.Y);
            }
        }
    }
    public class RoomTileChunk : GameObject
    {
        public RoomTileChunk(BitmapImage sprite, Point position, double chunkWidth, double chunkHeight) : base(sprite)
        {
            Position = position;
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
}