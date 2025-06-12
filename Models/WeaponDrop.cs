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
    /// Класс, представляющий выпадающее оружие, которое может быть подобрано игроком
    /// </summary>
    public class WeaponDrop
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Radius { get; private set; }
        public WeaponType WeaponType { get; private set; }
        public UIElement VisualElement { get; private set; }
        public DateTime CreationTime { get; private set; }
        
        // Коллайдер для определения столкновений
        public CircleCollider Collider { get; private set; }
        
        // Время жизни выпадающего оружия в секундах
        private const double LIFETIME = 20.0;
        
        // Анимация пульсации
        private double _pulsePhase = 0;
        private const double PULSE_SPEED = 1.5;
        
        /// <summary>
        /// Создает новое выпадающее оружие
        /// </summary>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <param name="weaponType">Тип оружия</param>
        /// <param name="spriteManager">Менеджер спрайтов (опционально)</param>
        public WeaponDrop(double x, double y, WeaponType weaponType, SpriteManager spriteManager = null)
        {
            X = x;
            Y = y;
            Radius = 20;
            WeaponType = weaponType;
            CreationTime = DateTime.Now;
            
            Collider = new CircleCollider(X, Y, Radius * 0.8);
            
            // Создаем визуальное представление оружия
            VisualElement = CreateVisualElement(spriteManager);
            
            UpdatePosition();
        }
        
        /// <summary>
        /// Создает визуальное представление оружия
        /// </summary>
        private UIElement CreateVisualElement(SpriteManager spriteManager)
        {
            string spriteName = GetSpriteNameForWeaponType(WeaponType);
            
            if (spriteManager != null && spriteManager.HasSprite(spriteName))
            {
                // Если есть спрайт оружия в менеджере спрайтов, используем его
                return spriteManager.CreateSpriteImage(spriteName, Radius * 2, Radius * 2);
            }
            else
            {
                // Создаем визуальное представление оружия
                Grid grid = new Grid
                {
                    Width = Radius * 2,
                    Height = Radius * 2
                };
                
                // Фон (круг)
                Ellipse background = new Ellipse
                {
                    Width = Radius * 2,
                    Height = Radius * 2,
                    Fill = GetColorForWeaponType(WeaponType),
                    Stroke = Brushes.Gold,
                    StrokeThickness = 2
                };
                
                // Иконка оружия (упрощенная)
                UIElement weaponIcon = CreateWeaponIcon(WeaponType);
                
                grid.Children.Add(background);
                grid.Children.Add(weaponIcon);
                
                return grid;
            }
        }
        
        /// <summary>
        /// Возвращает имя спрайта для типа оружия
        /// </summary>
        private string GetSpriteNameForWeaponType(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Pistol: return "weapon_pistol";
                case WeaponType.Shotgun: return "weapon_shotgun";
                case WeaponType.AssaultRifle: return "weapon_assultrifle";
                case WeaponType.MachineGun: return "weapon_mashinegun";
                case WeaponType.RocketLauncher: return "weapon_rocketlauncher";
                case WeaponType.Laser: return "weapon_laser";
                case WeaponType.Sniper: return "weapon_sniper";
                default: return "weapon_pistol";
            }
        }
        
        /// <summary>
        /// Возвращает цвет для типа оружия
        /// </summary>
        private Brush GetColorForWeaponType(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Pistol: return Brushes.DarkGray;
                case WeaponType.Shotgun: return Brushes.Brown;
                case WeaponType.AssaultRifle: return Brushes.Green;
                case WeaponType.MachineGun: return Brushes.DarkGreen;
                case WeaponType.RocketLauncher: return Brushes.Red;
                case WeaponType.Laser: return Brushes.Purple;
                case WeaponType.Sniper: return Brushes.DarkBlue;
                default: return Brushes.Gray;
            }
        }
        
        /// <summary>
        /// Создает иконку оружия
        /// </summary>
        private UIElement CreateWeaponIcon(WeaponType weaponType)
        {
            // Упрощенное представление оружия
            Rectangle weaponShape = new Rectangle
            {
                Width = Radius * 1.2,
                Height = Radius * 0.6,
                Fill = Brushes.WhiteSmoke
            };
            
            // Настраиваем форму в зависимости от типа оружия
            switch (weaponType)
            {
                case WeaponType.Shotgun:
                    weaponShape.Width = Radius * 1.4;
                    weaponShape.Height = Radius * 0.8;
                    break;
                case WeaponType.AssaultRifle:
                    weaponShape.Width = Radius * 1.6;
                    weaponShape.Height = Radius * 0.5;
                    break;
                case WeaponType.MachineGun:
                    weaponShape.Width = Radius * 1.5;
                    weaponShape.Height = Radius * 0.7;
                    break;
                case WeaponType.RocketLauncher:
                    weaponShape.Width = Radius * 1.3;
                    weaponShape.Height = Radius * 0.9;
                    break;
                case WeaponType.Laser:
                    weaponShape.Width = Radius * 1.4;
                    weaponShape.Height = Radius * 0.4;
                    weaponShape.Fill = Brushes.LightCyan;
                    break;
            }
            
            return weaponShape;
        }
        
        /// <summary>
        /// Обновляет позицию оружия и его визуального представления
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
        /// Обновляет состояние выпадающего оружия
        /// </summary>
        /// <param name="deltaTime">Прошедшее время</param>
        /// <returns>true, если оружие все еще активно</returns>
        public bool Update(double deltaTime)
        {
            // Проверяем время жизни оружия
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
            double scale = 1.0 + Math.Sin(_pulsePhase) * 0.15;
            double rotation = _pulsePhase * 10; // Добавляем вращение
            
            if (VisualElement is FrameworkElement element)
            {
                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(scale, scale));
                transformGroup.Children.Add(new RotateTransform(rotation));
                
                element.RenderTransform = transformGroup;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            
            return true;
        }
        
        /// <summary>
        /// Проверяет, пересекается ли выпадающее оружие с игроком
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
        
        /// <summary>
        /// Получить название оружия
        /// </summary>
        public string GetWeaponName()
        {
            return WeaponFactory.GetWeaponName(WeaponType);
        }
    }
} 