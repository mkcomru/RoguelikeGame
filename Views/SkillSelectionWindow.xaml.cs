using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GunVault.Models;

namespace GunVault.Views
{
    public partial class SkillSelectionWindow : Window
    {
        private List<PlayerSkill> _availableSkills;
        private List<PlayerSkill> _selectedSkills;
        private Player _player;
        private Random _random;
        
        // Событие, которое будет вызвано при выборе навыка
        public event EventHandler<PlayerSkill> SkillSelected;
        
        public SkillSelectionWindow(Player player)
        {
            InitializeComponent();
            
            _player = player;
            _random = new Random();
            _availableSkills = new List<PlayerSkill>
            {
                new PlayerSkill(SkillType.HealthBoost),
                new PlayerSkill(SkillType.SpeedBoost),
                new PlayerSkill(SkillType.InstantHeal),
                new PlayerSkill(SkillType.InstantArmor),
                new PlayerSkill(SkillType.OrbitalShield),
                new PlayerSkill(SkillType.DamageShield)
            };
            
            // Выбираем 3 случайных навыка
            _selectedSkills = SelectRandomSkills(3);
            
            // Отображаем выбранные навыки
            DisplaySkills();
        }
        
        /// <summary>
        /// Выбирает указанное количество случайных навыков из доступных
        /// </summary>
        private List<PlayerSkill> SelectRandomSkills(int count)
        {
            List<PlayerSkill> skills = new List<PlayerSkill>();
            List<PlayerSkill> availableSkills = new List<PlayerSkill>(_availableSkills);
            
            for (int i = 0; i < count && availableSkills.Count > 0; i++)
            {
                int index = _random.Next(availableSkills.Count);
                skills.Add(availableSkills[index]);
                availableSkills.RemoveAt(index);
            }
            
            return skills;
        }
        
        /// <summary>
        /// Отображает выбранные навыки в окне
        /// </summary>
        private void DisplaySkills()
        {
            if (_selectedSkills.Count >= 1)
            {
                Skill1Name.Text = _selectedSkills[0].Name;
                Skill1Description.Text = _selectedSkills[0].Description;
                Skill1Icon.Child = _selectedSkills[0].Icon;
            }
            
            if (_selectedSkills.Count >= 2)
            {
                Skill2Name.Text = _selectedSkills[1].Name;
                Skill2Description.Text = _selectedSkills[1].Description;
                Skill2Icon.Child = _selectedSkills[1].Icon;
            }
            
            if (_selectedSkills.Count >= 3)
            {
                Skill3Name.Text = _selectedSkills[2].Name;
                Skill3Description.Text = _selectedSkills[2].Description;
                Skill3Icon.Child = _selectedSkills[2].Icon;
            }
        }
        
        /// <summary>
        /// Обработчик наведения мыши на навык
        /// </summary>
        private void Skill_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // Анимация увеличения
                ScaleTransform scale = new ScaleTransform(1.0, 1.0);
                border.RenderTransform = scale;
                border.RenderTransformOrigin = new Point(0.5, 0.5);
                
                DoubleAnimation scaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.05,
                    Duration = TimeSpan.FromSeconds(0.2),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                
                // Изменение цвета границы
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(200, 255, 82, 82));
            }
        }
        
        /// <summary>
        /// Обработчик ухода мыши с навыка
        /// </summary>
        private void Skill_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // Анимация уменьшения
                ScaleTransform scale = border.RenderTransform as ScaleTransform;
                if (scale != null)
                {
                    DoubleAnimation scaleAnimation = new DoubleAnimation
                    {
                        From = scale.ScaleX,
                        To = 1.0,
                        Duration = TimeSpan.FromSeconds(0.2),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                }
                
                // Восстановление цвета границы
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)); // #33FFFFFF
            }
        }
        
        /// <summary>
        /// Обработчик выбора первого навыка
        /// </summary>
        private void Skill1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedSkills.Count >= 1)
            {
                SelectSkill(_selectedSkills[0]);
            }
        }
        
        /// <summary>
        /// Обработчик выбора второго навыка
        /// </summary>
        private void Skill2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedSkills.Count >= 2)
            {
                SelectSkill(_selectedSkills[1]);
            }
        }
        
        /// <summary>
        /// Обработчик выбора третьего навыка
        /// </summary>
        private void Skill3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_selectedSkills.Count >= 3)
            {
                SelectSkill(_selectedSkills[2]);
            }
        }
        
        /// <summary>
        /// Выбирает навык и закрывает окно
        /// </summary>
        private void SelectSkill(PlayerSkill skill)
        {
            // Применяем навык к игроку
            string message = skill.ApplySkill(_player);
            
            // Вызываем событие выбора навыка
            SkillSelected?.Invoke(this, skill);
            
            // Закрываем окно
            DialogResult = true;
        }
    }
} 