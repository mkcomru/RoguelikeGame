using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GunVault.Models
{
    // Типы квестов, которые могут быть доступны
    public enum QuestType
    {
        KillEnemies,
        CollectHealthKits,
        GetScore
    }

    // Класс, представляющий точку квеста на карте
    public class QuestPoint
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public QuestType Type { get; private set; }
        public bool IsActive { get; set; } = true;
        public UIElement VisualElement { get; private set; }

        // Название квеста для отображения
        public string QuestName { get; private set; }
        
        // Описание квеста для отображения
        public string QuestDescription { get; private set; }
        
        // Цель квеста для отображения
        public string QuestObjective { get; private set; }
        
        // Цвет квеста для отображения
        public Color QuestColor { get; private set; }

        // Конструктор
        public QuestPoint(double x, double y, QuestType type)
        {
            X = x;
            Y = y;
            Type = type;
            
            // Устанавливаем информацию о квесте в зависимости от его типа
            SetQuestInfo();
            
            CreateVisual();
        }
        
        // Устанавливаем информацию о квесте
        private void SetQuestInfo()
        {
            switch (Type)
            {
                case QuestType.KillEnemies:
                    QuestName = "ОХОТА";
                    QuestDescription = "Уничтожьте указанное количество врагов в ограниченное время";
                    QuestObjective = "Убить 7 врагов за 40 сек";
                    QuestColor = Colors.Red;
                    break;
                    
                case QuestType.CollectHealthKits:
                    QuestName = "СБОР";
                    QuestDescription = "Найдите и соберите указанное количество аптечек";
                    QuestObjective = "Собрать 4 аптечки";
                    QuestColor = Colors.Green;
                    break;
                    
                case QuestType.GetScore:
                    QuestName = "ОЧКИ";
                    QuestDescription = "Наберите указанное количество очков, уничтожая врагов";
                    QuestObjective = "Набрать 2000 очков";
                    QuestColor = Color.FromRgb(255, 170, 0); // Orange
                    break;
            }
        }

        // Создание визуального представления точки квеста
        private void CreateVisual()
        {
            // Создаем контейнер для элементов
            Canvas container = new Canvas
            {
                Width = 40,
                Height = 40
            };

            // Создаем круг для точки квеста
            Ellipse questCircle = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = new SolidColorBrush(Colors.Gold),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2
            };

            // Добавляем эффект свечения
            questCircle.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Gold,
                BlurRadius = 15,
                ShadowDepth = 0
            };

            // Добавляем значок в зависимости от типа квеста
            TextBlock questIcon = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Выбираем символ для типа квеста
            switch (Type)
            {
                case QuestType.KillEnemies:
                    questIcon.Text = "K";
                    break;
                case QuestType.CollectHealthKits:
                    questIcon.Text = "H";
                    break;
                case QuestType.GetScore:
                    questIcon.Text = "S";
                    break;
            }

            // Размещаем элементы в контейнере
            Canvas.SetLeft(questCircle, 5);
            Canvas.SetTop(questCircle, 5);
            container.Children.Add(questCircle);

            // Центрируем текст в круге
            Canvas.SetLeft(questIcon, 14);
            Canvas.SetTop(questIcon, 7);
            container.Children.Add(questIcon);

            // Устанавливаем позицию контейнера
            Canvas.SetLeft(container, X - 20);
            Canvas.SetTop(container, Y - 20);

            // Сохраняем визуальный элемент
            VisualElement = container;
        }

        // Обновление позиции визуального элемента
        public void UpdatePosition()
        {
            Canvas.SetLeft(VisualElement, X - 20);
            Canvas.SetTop(VisualElement, Y - 20);
        }
    }
} 