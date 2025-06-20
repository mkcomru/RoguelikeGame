using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GunVault.Models
{
    /// <summary>
    /// Типы навыков, которые может выбрать игрок
    /// </summary>
    public enum SkillType
    {
        // Навыки за счет
        HealthBoost,     // +20 к максимальному здоровью
        SpeedBoost,      // +15% к скорости
        InstantHeal,     // Мгновенная аптечка
        InstantArmor,    // Мгновенный бронежилет
        OrbitalShield,   // Сферы вокруг игрока
        DamageShield,    // Щит, поглощающий один удар
        
        // Навыки за выполнение заданий
        QuestHealthBoost,    // +10 к максимальному здоровью
        QuestDamageBoost,    // +5% к урону
        QuestSpeedBoost      // +2% к скорости
    }

    /// <summary>
    /// Класс, представляющий навык игрока
    /// </summary>
    public class PlayerSkill
    {
        public SkillType Type { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string IconPath { get; private set; }
        public UIElement Icon { get; private set; }
        public bool IsQuestSkill { get; private set; }

        public PlayerSkill(SkillType type)
        {
            Type = type;
            IsQuestSkill = type == SkillType.QuestHealthBoost || 
                           type == SkillType.QuestDamageBoost || 
                           type == SkillType.QuestSpeedBoost;
            
            switch (type)
            {
                case SkillType.HealthBoost:
                    Name = "Усиление здоровья";
                    Description = "+20 к максимальному здоровью";
                    IconPath = "heart.png";
                    break;
                case SkillType.SpeedBoost:
                    Name = "Ускорение";
                    Description = "+3% к скорости передвижения";
                    IconPath = "speed.png";
                    break;
                case SkillType.InstantHeal:
                    Name = "Аптечка";
                    Description = "Мгновенно восстанавливает здоровье";
                    IconPath = "medkit.png";
                    break;
                case SkillType.InstantArmor:
                    Name = "Бронежилет";
                    Description = "Мгновенно добавляет броню";
                    IconPath = "armor.png";
                    break;
                case SkillType.OrbitalShield:
                    Name = "Орбитальный щит";
                    Description = "Сферы вращаются вокруг игрока, блокируя урон";
                    IconPath = "orbital.png";
                    break;
                case SkillType.DamageShield:
                    Name = "Защитный барьер";
                    Description = "Блокирует 100% урона от одной атаки";
                    IconPath = "shield.png";
                    break;
                    
                // Навыки за задания
                case SkillType.QuestHealthBoost:
                    Name = "Здоровье героя";
                    Description = "+10 к максимальному здоровью";
                    IconPath = "quest_health.png";
                    break;
                case SkillType.QuestDamageBoost:
                    Name = "Сила удара";
                    Description = "+5% к урону от оружия";
                    IconPath = "quest_damage.png";
                    break;
                case SkillType.QuestSpeedBoost:
                    Name = "Ловкость";
                    Description = "+2% к скорости передвижения";
                    IconPath = "quest_speed.png";
                    break;
            }
            
            // Создаем иконку по умолчанию (цветной прямоугольник с текстом)
            CreateDefaultIcon();
        }

        /// <summary>
        /// Создает иконку по умолчанию, если изображение недоступно
        /// </summary>
        private void CreateDefaultIcon()
        {
            Border border = new Border
            {
                Width = 64,
                Height = 64,
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(2)
            };

            // Устанавливаем цвет в зависимости от типа навыка
            switch (Type)
            {
                case SkillType.HealthBoost:
                case SkillType.QuestHealthBoost:
                    border.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Красный
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 120, 120));
                    break;
                case SkillType.SpeedBoost:
                case SkillType.QuestSpeedBoost:
                    border.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Зеленый
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(120, 255, 120));
                    break;
                case SkillType.InstantHeal:
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Желтый
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 230, 120));
                    break;
                case SkillType.InstantArmor:
                    border.Background = new SolidColorBrush(Color.FromRgb(0, 123, 255)); // Синий
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(120, 170, 255));
                    break;
                case SkillType.OrbitalShield:
                    border.Background = new SolidColorBrush(Color.FromRgb(111, 66, 193)); // Фиолетовый
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(170, 120, 255));
                    break;
                case SkillType.DamageShield:
                    border.Background = new SolidColorBrush(Color.FromRgb(23, 162, 184)); // Бирюзовый
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(120, 220, 255));
                    break;
                case SkillType.QuestDamageBoost:
                    border.Background = new SolidColorBrush(Color.FromRgb(255, 99, 71)); // Томатный
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 150, 130));
                    break;
            }

            // Добавляем текст с первой буквой названия навыка
            TextBlock textBlock = new TextBlock
            {
                Text = Name[0].ToString(),
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = textBlock;
            Icon = border;
        }

        /// <summary>
        /// Применяет навык к игроку
        /// </summary>
        /// <param name="player">Игрок</param>
        /// <returns>Сообщение о результате применения навыка</returns>
        public string ApplySkill(Player player)
        {
            if (player == null)
                return "Ошибка: игрок не найден";

            switch (Type)
            {
                case SkillType.HealthBoost:
                    player.IncreaseMaxHealth(20);
                    return "Максимальное здоровье увеличено на 20";
                
                case SkillType.SpeedBoost:
                    player.IncreaseSpeed(0.03); // Изменено с 0.15 на 0.03 (3%)
                    return "Скорость увеличена на 3%";
                
                case SkillType.InstantHeal:
                    double healAmount = player.MaxHealth * 0.5; // 50% от максимального здоровья
                    player.Heal(healAmount);
                    return $"Восстановлено {healAmount:F0} единиц здоровья";
                
                case SkillType.InstantArmor:
                    double armorAmount = player.MaxArmor * 0.5; // 50% от максимальной брони
                    player.AddArmor(armorAmount);
                    return $"Добавлено {armorAmount:F0} единиц брони";
                
                case SkillType.OrbitalShield:
                    player.ActivateOrbitalShield();
                    return "Активирован орбитальный щит";
                
                case SkillType.DamageShield:
                    player.ActivateDamageShield();
                    return "Активирован защитный барьер";
                
                // Навыки за задания
                case SkillType.QuestHealthBoost:
                    player.IncreaseMaxHealth(10);
                    return "Максимальное здоровье увеличено на 10";
                    
                case SkillType.QuestDamageBoost:
                    player.IncreaseDamage(0.05f); // +5% к урону
                    return "Урон от оружия увеличен на 5%";
                    
                case SkillType.QuestSpeedBoost:
                    player.IncreaseSpeed(0.02); // +2% к скорости
                    return "Скорость увеличена на 2%";
                
                default:
                    return "Неизвестный навык";
            }
        }
    }
} 