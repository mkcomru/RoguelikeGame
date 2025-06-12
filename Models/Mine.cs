using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GunVault.GameEngine;

namespace GunVault.Models
{
    /// <summary>
    /// Класс, представляющий мину, которая наносит урон игроку при приближении
    /// </summary>
    public class Mine
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Size { get; private set; } = 40;
        public double ActivationRadius { get; private set; } = 100;
        public double Damage { get; private set; } = 25;
        public UIElement VisualElement { get; private set; }
        public bool IsActive { get; private set; } = true;
        public CircleCollider Collider { get; private set; }

        public Mine(double x, double y, SpriteManager spriteManager = null)
        {
            X = x;
            Y = y;
            Collider = new CircleCollider(X, Y, ActivationRadius);

            if (spriteManager != null && spriteManager.HasSprite("mine"))
            {
                VisualElement = spriteManager.CreateSpriteImage("mine", Size, Size);
            }
            else
            {
                // Запасной вариант: простая серая окружность
                VisualElement = new Ellipse
                {
                    Width = Size,
                    Height = Size,
                    Fill = Brushes.Gray,
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
                Canvas.SetLeft(VisualElement, X - Size / 2);
                Canvas.SetTop(VisualElement, Y - Size / 2);
                Collider.UpdatePosition(X, Y);
            }
        }

        public bool CheckPlayerProximity(Player player)
        {
            if (!IsActive) return false;
            double dx = X - player.X;
            double dy = Y - player.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            return distance <= ActivationRadius;
        }

        public void Explode()
        {
            IsActive = false;
        }
    }
} 