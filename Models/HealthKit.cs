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
    /// Класс, представляющий аптечку, которая может быть подобрана игроком для восстановления здоровья
    /// </summary>
    public class HealthKit
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Radius { get; private set; }
        public double HealAmount { get; private set; }
        public UIElement VisualElement { get; private set; }
        public DateTime CreationTime { get; private set; }
        
        // Коллайдер для определения столкновений
        public CircleCollider Collider { get; private set; }
        
        // Время жизни аптечки в секундах
        private const double LIFETIME = 15.0;
        
        // Анимация пульсации
        private double _pulsePhase = 0;
        private const double PULSE_SPEED = 2.0;
        
        /// <summary>
        /// Создает новую аптечку
        /// </summary>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <param name="healAmount">Количество восстанавливаемого здоровья</param>
        /// <param name="spriteManager">Менеджер спрайтов (опционально)</param>
        public HealthKit(double x, double y, double healAmount = 20, SpriteManager spriteManager = null)
        {
            X = x;
            Y = y;
            Radius = 15;
            HealAmount = healAmount;
            CreationTime = DateTime.Now;
            
            Collider = new CircleCollider(X, Y, Radius * 0.8);
            
            if (spriteManager != null && spriteManager.HasSprite("healthkit"))
            {
                // Если есть спрайт аптечки в менеджере спрайтов, используем его
                VisualElement = spriteManager.CreateSpriteImage("healthkit", Radius * 2, Radius * 2);
            }
            else
            {
                // Создаем визуальное представление аптечки (красный крест на белом фоне)
                var grid = new Grid
                {
                    Width = Radius * 2,
                    Height = Radius * 2
                };
                
                // Фон (белый круг)
                var background = new Ellipse
                {
                    Width = Radius * 2,
                    Height = Radius * 2,
                    Fill = Brushes.White,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 2
                };
                
                // Вертикальная линия креста
                var verticalLine = new Rectangle
                {
                    Width = Radius * 0.6,
                    Height = Radius * 1.6,
                    Fill = Brushes.Red,
                    RadiusX = 2,
                    RadiusY = 2
                };
                
                // Горизонтальная линия креста
                var horizontalLine = new Rectangle
                {
                    Width = Radius * 1.6,
                    Height = Radius * 0.6,
                    Fill = Brushes.Red,
                    RadiusX = 2,
                    RadiusY = 2
                };
                
                grid.Children.Add(background);
                grid.Children.Add(verticalLine);
                grid.Children.Add(horizontalLine);
                
                VisualElement = grid;
            }
            
            UpdatePosition();
        }
        
        /// <summary>
        /// Обновляет позицию аптечки и её визуального представления
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
        /// Обновляет состояние аптечки
        /// </summary>
        /// <param name="deltaTime">Прошедшее время</param>
        /// <returns>true, если аптечка все еще активна</returns>
        public bool Update(double deltaTime)
        {
            // Проверяем время жизни аптечки
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
        /// Проверяет, пересекается ли аптечка с игроком
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