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
    /// Класс, представляющий бронежилет, который может быть подобран игроком для получения брони
    /// </summary>
    public class ArmorKit
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Radius { get; private set; }
        public double ArmorAmount { get; private set; }
        public UIElement VisualElement { get; private set; }
        public DateTime CreationTime { get; private set; }
        
        // Коллайдер для определения столкновений
        public CircleCollider Collider { get; private set; }
        
        // Время жизни бронежилета в секундах
        private const double LIFETIME = 15.0;
        
        // Анимация пульсации
        private double _pulsePhase = 0;
        private const double PULSE_SPEED = 2.0;
        
        /// <summary>
        /// Создает новый бронежилет
        /// </summary>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <param name="armorAmount">Количество добавляемой брони</param>
        /// <param name="spriteManager">Менеджер спрайтов (опционально)</param>
        public ArmorKit(double x, double y, double armorAmount = 20, SpriteManager spriteManager = null)
        {
            X = x;
            Y = y;
            Radius = 40;
            ArmorAmount = armorAmount;
            CreationTime = DateTime.Now;
            
            Collider = new CircleCollider(X, Y, Radius * 0.8);
            
            if (spriteManager != null && spriteManager.HasSprite("armor"))
            {
                // Если есть спрайт бронежилета в менеджере спрайтов, используем его
                VisualElement = spriteManager.CreateSpriteImage("armor", Radius * 2, Radius * 2);
            }
            else
            {
                // Создаем визуальное представление бронежилета (синий щит)
                var grid = new Grid
                {
                    Width = Radius * 2,
                    Height = Radius * 2
                };
                
                // Фон (синий круг)
                var background = new Ellipse
                {
                    Width = Radius * 2,
                    Height = Radius * 2,
                    Fill = Brushes.DarkBlue,
                    Stroke = Brushes.LightBlue,
                    StrokeThickness = 2
                };
                
                // Щит (белый символ)
                var shield = new Path
                {
                    Data = Geometry.Parse("M10,3 L20,3 L20,10 C20,15 15,18 10,20 C5,18 0,15 0,10 L0,3 Z"),
                    Fill = Brushes.White,
                    Width = Radius * 1.5,
                    Height = Radius * 1.5,
                    Stretch = Stretch.Uniform
                };
                
                grid.Children.Add(background);
                grid.Children.Add(shield);
                
                VisualElement = grid;
            }
            
            UpdatePosition();
        }
        
        /// <summary>
        /// Обновляет позицию бронежилета и его визуального представления
        /// </summary>
        public void UpdatePosition()
        {
            if (VisualElement != null)
            {
                Canvas.SetLeft(VisualElement, X - Radius);
                Canvas.SetTop(VisualElement, Y - Radius);
                
                // Обновляем позицию коллайдера
                Collider.UpdatePosition(X, Y);
            }
        }
        
        /// <summary>
        /// Обновляет состояние бронежилета
        /// </summary>
        /// <param name="deltaTime">Прошедшее время</param>
        /// <returns>true, если бронежилет все еще активен</returns>
        public bool Update(double deltaTime)
        {
            // Проверяем время жизни бронежилета
            if ((DateTime.Now - CreationTime).TotalSeconds > LIFETIME)
            {
                return false;
            }
            
            // Обновляем фазу пульсации
            _pulsePhase += deltaTime * PULSE_SPEED;
            if (_pulsePhase > Math.PI * 2)
            {
                _pulsePhase -= Math.PI * 2;
            }
            
            // Применяем эффект пульсации
            double scale = 1.0 + Math.Sin(_pulsePhase) * 0.1;
            
            if (VisualElement is FrameworkElement element)
            {
                ScaleTransform scaleTransform = new ScaleTransform(scale, scale);
                element.RenderTransform = scaleTransform;
                
                // Устанавливаем центр трансформации в центр элемента
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            
            return true;
        }
        
        /// <summary>
        /// Проверяет, пересекается ли бронежилет с игроком
        /// </summary>
        /// <param name="player">Игрок</param>
        /// <returns>true, если происходит пересечение</returns>
        public bool CollidesWithPlayer(Player player)
        {
            if (Collider != null && player.Collider != null)
            {
                return Collider.Intersects(player.Collider);
            }
            
            // Запасной вариант, если коллайдеры не инициализированы
            double dx = X - player.X;
            double dy = Y - player.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            // Используем константное значение для радиуса игрока, если коллайдер недоступен
            double playerRadius = 20; // Примерный радиус игрока
            return distance < Radius + playerRadius;
        }
    }
} 