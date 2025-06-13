using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GunVault.GameEngine;

namespace GunVault.Models
{
    /// <summary>
    /// Класс разрушаемой бочки
    /// </summary>
    public class Barrel
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Width { get; private set; } = 48;
        public double Height { get; private set; } = 64;
        public double Health { get; private set; } = 40;
        public bool IsDestroyed { get; private set; } = false;
        public UIElement VisualElement { get; private set; }
        public RectCollider Collider { get; private set; }

        public Barrel(double x, double y, SpriteManager spriteManager = null)
        {
            X = x;
            Y = y;
            Collider = new RectCollider(X - Width / 2, Y - Height / 2, Width, Height);

            if (spriteManager != null && spriteManager.HasSprite("barrel"))
            {
                VisualElement = spriteManager.CreateSpriteImage("barrel", Width, Height);
            }
            else
            {
                VisualElement = new Rectangle
                {
                    Width = Width,
                    Height = Height,
                    Fill = Brushes.SaddleBrown,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
            }
            UpdatePosition();
        }

        public void UpdatePosition()
        {
            if (VisualElement != null)
            {
                Canvas.SetLeft(VisualElement, X - Width / 2);
                Canvas.SetTop(VisualElement, Y - Height / 2);
                Collider.UpdatePosition(X - Width / 2, Y - Height / 2);
            }
        }

        public void TakeDamage(double damage)
        {
            if (IsDestroyed) return;
            Health -= damage;
            if (Health <= 0)
            {
                IsDestroyed = true;
            }
        }
    }
} 