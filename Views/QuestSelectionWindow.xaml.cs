using System;
using System.Windows;
using System.Windows.Input;
using GunVault.Models;

namespace GunVault.Views
{
    public partial class QuestSelectionWindow : Window
    {
        public QuestType? SelectedQuest { get; private set; }

        public QuestSelectionWindow()
        {
            InitializeComponent();
        }

        private void KillEnemies_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedQuest = QuestType.KillEnemies;
            DialogResult = true;
        }

        private void CollectHealthKits_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedQuest = QuestType.CollectHealthKits;
            DialogResult = true;
        }

        private void GetScore_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SelectedQuest = QuestType.GetScore;
            DialogResult = true;
        }
    }
} 