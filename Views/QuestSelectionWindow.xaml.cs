using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GunVault.Models;

namespace GunVault.Views
{
    public partial class QuestSelectionWindow : Window
    {
        public QuestType? SelectedQuest { get; private set; }
        private QuestPoint _questPoint;

        public QuestSelectionWindow(QuestPoint questPoint)
        {
            InitializeComponent();
            
            _questPoint = questPoint;
            SelectedQuest = null;
            
            // Настраиваем информацию о задании
            SetupQuestInfo();
        }
        
        private void SetupQuestInfo()
        {
            // Устанавливаем иконку задания
            switch (_questPoint.Type)
            {
                case QuestType.KillEnemies:
                    QuestIconText.Text = "K";
                    QuestIconBackground.Color = Colors.Red;
                    QuestIconShadow.Color = Colors.Red;
                    break;
                    
                case QuestType.CollectHealthKits:
                    QuestIconText.Text = "H";
                    QuestIconBackground.Color = Colors.Green;
                    QuestIconShadow.Color = Colors.Green;
                    break;
                    
                case QuestType.GetScore:
                    QuestIconText.Text = "S";
                    QuestIconBackground.Color = Color.FromRgb(255, 170, 0); // Orange
                    QuestIconShadow.Color = Color.FromRgb(255, 170, 0);
                    break;
            }
            
            // Устанавливаем информацию о задании
            QuestNameText.Text = _questPoint.QuestName;
            QuestObjectiveText.Text = _questPoint.QuestObjective;
            QuestDescriptionText.Text = _questPoint.QuestDescription;
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedQuest = _questPoint.Type;
            DialogResult = true;
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedQuest = null;
            DialogResult = false;
        }
    }
} 