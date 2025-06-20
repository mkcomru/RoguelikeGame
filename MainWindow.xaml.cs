﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GunVault.Models;
using GunVault.GameEngine;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media;

namespace GunVault;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Player? _player;
    private GameLoop? _gameLoop;
    private InputHandler? _inputHandler;
    private GameManager? _gameManager;
    private SpriteManager? _spriteManager; // Менеджер спрайтов
    private int _score = 0;
    
    // Таймер для автоматического скрытия уведомления
    private System.Windows.Threading.DispatcherTimer? _notificationTimer;
    
    // Флаг для отображения информации о размерах мира
    private bool _showDebugInfo = false;
    
    // Переменные для экрана загрузки
    private Task? _preloadTask;
    private CancellationTokenSource? _preloadCancellation;
    private int _totalChunksToLoad = 0;
    private int _loadedChunksCount = 0;
    private const int PRELOAD_RADIUS = 3; // Радиус предзагрузки чанков вокруг игрока (в чанках)
    private const int INITIAL_BUFFER_SIZE = 500; // Размер буферной зоны вокруг игрока (в пикселях)
    private System.Windows.Threading.DispatcherTimer? _loadingAnimationTimer; // Таймер для анимации текста загрузки
    
    // Переменные для отслеживания статистики игры
    private DateTime _gameStartTime;
    private int _enemiesKilled = 0;
    private bool _isGameOver = false;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Инициализируем игру после загрузки окна
        Loaded += MainWindow_Loaded;
        
        // Добавляем обработчик закрытия окна
        Closing += MainWindow_Closing;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Инициализируем менеджер спрайтов
            try
            {
                _spriteManager = new SpriteManager();
                Console.WriteLine("SpriteManager успешно инициализирован");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации SpriteManager: {ex.Message}");
                // Продолжаем работу без менеджера спрайтов
                _spriteManager = null;
            }
            
            // Показываем экран загрузки
            LoadingScreen.Visibility = Visibility.Visible;
            LoadingStatusText.Text = "Инициализация игры...";
            UpdateLoadingProgress(0);
            
            // Создаем анимированные частицы
            CreateLoadingScreenParticles();
            
            // Инициализируем и запускаем таймер анимации загрузки
            InitializeLoadingAnimation();
            
            // Запускаем инициализацию игры с предзагрузкой в отдельном потоке
            _preloadCancellation = new CancellationTokenSource();
            _preloadTask = Task.Run(() => PreloadGame(_preloadCancellation.Token), _preloadCancellation.Token);
            
            // Инициализируем таймер для уведомлений
            _notificationTimer = new System.Windows.Threading.DispatcherTimer();
            _notificationTimer.Tick += NotificationTimer_Tick;
            _notificationTimer.Interval = TimeSpan.FromSeconds(4); // Уведомление исчезнет через 4 секунды
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при запуске игры: {ex.Message}\n\n{ex.StackTrace}", 
                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void InitializeGame()
    {
        try
        {
            // Рассчитываем размеры мира (в 3 раза больше экрана)
            double worldWidth = GameCanvas.ActualWidth * 3.0; // WORLD_SIZE_MULTIPLIER из GameManager
            double worldHeight = GameCanvas.ActualHeight * 3.0;

            // Проверяем наличие спрайтов игрока
            Console.WriteLine("Проверка наличия спрайтов игрока:");
            string[] playerSprites = new string[] {
                "player_pistol_stop", "player_pistol_run_v1", "player_pistol_run_v2",
                "player_shotgun_stop", "player_shotgun_run_v1", "player_shotgun_run_v2"
            };
            
            foreach (var spriteName in playerSprites)
            {
                bool exists = _spriteManager.HasSprite(spriteName);
                Console.WriteLine($"Спрайт {spriteName}: {(exists ? "найден" : "НЕ НАЙДЕН")}");
            }

            // Инициализируем игрока в центре МИРА, а не экрана
            double centerX = worldWidth / 2;
            double centerY = worldHeight / 2;
            _player = new Player(centerX, centerY, _spriteManager);
            
            // Инициализируем обработчик ввода
            _inputHandler = new InputHandler(_player);
            
            // Добавляем игрока на канвас
            GameCanvas.Children.Add(_player.PlayerShape);
            
            // Устанавливаем _parentCanvas для игрока
            _player.AddWeaponToCanvas(GameCanvas);
            
            // Метод AddColliderVisualToCanvas все еще существует, но уже ничего не делает
            _player.AddColliderVisualToCanvas(GameCanvas);
            
            // Инициализируем менеджер игры, передаем менеджер спрайтов
            _gameManager = new GameManager(GameCanvas, _player, GameCanvas.ActualWidth, GameCanvas.ActualHeight, _spriteManager);
            _gameManager.ScoreChanged += GameManager_ScoreChanged;
            _gameManager.WeaponChanged += GameManager_WeaponChanged;
            _gameManager.EnemyKilled += GameManager_EnemyKilled;
            _gameManager.HealthKitCollected += GameManager_HealthKitCollected;
            _gameManager.WeaponPickedUp += GameManager_WeaponPickedUp;
            _gameManager.ArmorKitCollected += GameManager_ArmorKitCollected;
            _gameManager.SkillSelectionAvailable += GameManager_SkillSelectionAvailable;
            
            // Инициализируем игровой цикл
            _gameLoop = new GameLoop(_gameManager, GameCanvas.ActualWidth, GameCanvas.ActualHeight);
            _gameLoop.GameTick += GameLoop_GameTick;
            
            // Запускаем игровой цикл
            _gameLoop.Start();
            
            // Фокус на канвас для обработки ввода
            GameCanvas.Focus();
            
            // Обновляем информацию об игроке
            UpdatePlayerInfo();
            
            // Запоминаем время начала игры
            _gameStartTime = DateTime.Now;
            _isGameOver = false;
            
            Console.WriteLine("Игра успешно инициализирована");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при инициализации игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            Console.WriteLine($"Ошибка при инициализации игры: {ex}");
        }
    }
    
    // Обработка изменения счета
    private void GameManager_ScoreChanged(object sender, int newScore)
    {
        _score = newScore;
        UpdatePlayerInfo();
    }
    
    // Обработка изменения оружия
    private void GameManager_WeaponChanged(object sender, string weaponName)
    {
        // Вместо MessageBox показываем внутриигровое уведомление
        ShowWeaponNotification(weaponName);
    }
    
    // Обработка уничтожения врага
    private void GameManager_EnemyKilled(object sender, EventArgs e)
    {
        _enemiesKilled++;
    }
    
    // Обработка подбора аптечки
    private void GameManager_HealthKitCollected(object sender, double healAmount)
    {
        ShowNotification($"Подобрана аптечка (+{healAmount} HP)");
        UpdatePlayerInfo();
    }
    
    // Обработка подбора оружия
    private void GameManager_WeaponPickedUp(object sender, WeaponType weaponType)
    {
        // Показываем уведомление о подборе оружия
        ShowNotification($"Подобрано: {WeaponFactory.GetWeaponName(weaponType)}");
    }
    
    // Обработка подбора брони
    private void GameManager_ArmorKitCollected(object sender, double amount)
    {
        ShowMessage($"Подобран бронежилет (+{amount} брони)", Colors.Blue);
        UpdatePlayerInfo();
    }
    
    // Показывает уведомление о получении нового оружия
    private void ShowWeaponNotification(string weaponName)
    {
        // Остановим таймер, если он уже запущен
        if (_notificationTimer!.IsEnabled)
        {
            _notificationTimer.Stop();
        }
        
        // Устанавливаем название оружия
        NotificationWeaponName.Text = weaponName;
        
        // Показываем уведомление с анимацией появления
        WeaponNotification.Opacity = 0;
        WeaponNotification.Visibility = Visibility.Visible;
        
        // Сначала создаем анимацию "выдвижения" сверху
        ThicknessAnimation slideDownAnimation = new ThicknessAnimation
        {
            From = new Thickness(0, -100, 0, 0),
            To = new Thickness(0, 0, 0, 0),
            Duration = TimeSpan.FromSeconds(0.5),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        
        // Затем создаем анимацию появления
        DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        
        // Запускаем анимации
        WeaponNotification.BeginAnimation(Border.MarginProperty, slideDownAnimation);
        WeaponNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        
        // Запускаем таймер для автоматического скрытия
        _notificationTimer.Start();
    }
    
    // Автоматически скрывает уведомление по таймеру
    private void NotificationTimer_Tick(object sender, EventArgs e)
    {
        _notificationTimer!.Stop();
        HideNotification();
    }
    
    // Скрывает уведомление с анимацией
    private void HideNotification()
    {
        // Анимация скрытия вверх
        ThicknessAnimation slideUpAnimation = new ThicknessAnimation
        {
            From = new Thickness(0, 0, 0, 0),
            To = new Thickness(0, -100, 0, 0),
            Duration = TimeSpan.FromSeconds(0.5),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        
        // Анимация прозрачности
        DoubleAnimation fadeOutAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.5)
        };
        
        // По завершении анимации скрываем элемент полностью
        fadeOutAnimation.Completed += (s, e) => WeaponNotification.Visibility = Visibility.Collapsed;
        
        // Запускаем анимации
        WeaponNotification.BeginAnimation(Border.MarginProperty, slideUpAnimation);
        WeaponNotification.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
    }
    
    // Обновление информации об игроке
    private void GameLoop_GameTick(object sender, EventArgs e)
    {
        UpdatePlayerInfo();
        
        // Проверяем, жив ли игрок
        if (_player != null && _player.Health <= 0 && !_isGameOver)
        {
            PlayerDeath();
        }
    }
    
    // Обработка смерти игрока
    private void PlayerDeath()
    {
        _isGameOver = true;
        
        // Останавливаем игровой цикл
        _gameLoop?.Stop();
        
        // Вычисляем время игры
        TimeSpan gameTime = DateTime.Now - _gameStartTime;
        string timeString = $"{gameTime.Minutes:D2}:{gameTime.Seconds:D2}";
        
        // Обновляем статистику на экране смерти
        DeathScoreText.Text = $"Счёт: {_score}";
        DeathTimeText.Text = $"Время игры: {timeString}";
        DeathKillsText.Text = $"Убито врагов: {_enemiesKilled}";
        
        // Показываем экран смерти с анимацией
        DeathScreen.Opacity = 0;
        DeathScreen.Visibility = Visibility.Visible;
        
        // Анимация появления экрана смерти
        DoubleAnimation fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(1)
        };
        
        DeathScreen.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }
    
    // Обновление отображаемой информации
    private void UpdatePlayerInfo()
    {
        if (_gameManager != null && _gameManager._player != null)
        {
            double healthPercent = (_gameManager._player.Health / _gameManager._player.MaxHealth) * 100;
            HealthBar.Value = healthPercent;
            HealthText.Text = $"Здоровье: {_gameManager._player.Health:F0}/{_gameManager._player.MaxHealth:F0}";
            
            double armorPercent = (_gameManager._player.Armor / _gameManager._player.MaxArmor) * 100;
            ArmorBar.Value = armorPercent;
            ArmorText.Text = $"Броня: {_gameManager._player.Armor:F0}/{_gameManager._player.MaxArmor:F0}";
            
            AmmoText.Text = _gameManager.GetAmmoInfo();
        }
        
        // Обновляем информацию об оружии
        WeaponText.Text = $"Оружие: {_player?.GetWeaponName()}";
        
        // Обновляем счет
        ScoreText.Text = $"Счёт: {_score}";
        
        // Отображаем отладочную информацию, если включено
        if (_showDebugInfo)
        {
            DebugInfoText.Text = $"Поз. игрока: ({_player?.X:F0}, {_player?.Y:F0})";
            DebugInfoText.Visibility = Visibility.Visible;
        }
        else
        {
            DebugInfoText.Visibility = Visibility.Collapsed;
        }
    }
    
    // Отображение обычного уведомления
    private void ShowNotification(string message)
    {
        // Останавливаем таймер, если он активен
        if (_notificationTimer!.IsEnabled)
        {
            _notificationTimer.Stop();
        }
        
        // Устанавливаем текст уведомления
        NotificationWeaponName.Text = message;
        
        // Показываем уведомление с анимацией
        WeaponNotification.Opacity = 0;
        WeaponNotification.Visibility = Visibility.Visible;
        
        // Анимация появления
        DoubleAnimation fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        
        // Запускаем анимацию
        WeaponNotification.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        
        // Запускаем таймер для автоматического скрытия
        _notificationTimer.Start();
    }
    
    // Обработка изменения размера окна
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Игнорируем изменения размера, когда ширина или высота canvas равны 0 (при минимизации)
        if (GameCanvas.ActualWidth <= 1 || GameCanvas.ActualHeight <= 1)
        {
            return;
        }
            
        // Также игнорируем, если размер изменился незначительно (может быть вызвано восстановлением из минимизации)
        if (Math.Abs(e.PreviousSize.Width - e.NewSize.Width) < 10 && 
            Math.Abs(e.PreviousSize.Height - e.NewSize.Height) < 10)
        {
            return;
        }
            
        if (_gameLoop != null)
        {
            _gameLoop.ResizeGameArea(GameCanvas.ActualWidth, GameCanvas.ActualHeight);
        }
    }
    
    // Обработка нажатия клавиш
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        Console.WriteLine($"Нажата клавиша: {e.Key}");
        
        if (_inputHandler != null)
        {
            _inputHandler.HandleKeyDown(e);
        }
        
        // Обрабатываем клавиши только если игра инициализирована
        if (_gameManager != null && _player != null)
        {
            // Обработка специальных клавиш
            if (e.Key == Key.R)
            {
                _player.StartReload();
            }
            // Переключаем отображение границ чанков
            else if (e.Key == Key.F3)
            {
                _gameManager.ToggleChunkBoundaries();
                _showDebugInfo = !_showDebugInfo;
            }
            
            // Передаем событие нажатия клавиши в GameManager
            _gameManager.HandleKeyPress(e);
        }
    }
    
    // Обработка отпускания клавиш
    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        Console.WriteLine($"Отпущена клавиша: {e.Key}");
        
        if (_inputHandler != null)
        {
            _inputHandler.HandleKeyUp(e);
        }
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Останавливаем таймер анимации
        _loadingAnimationTimer?.Stop();
        _loadingAnimationTimer = null;
        
        // Отменяем задачу предзагрузки, если она выполняется
        if (_preloadTask != null && !_preloadTask.IsCompleted)
        {
            _preloadCancellation?.Cancel();
            try
            {
                _preloadTask.Wait(500); // Ждем не более 500 мс
            }
            catch { }
        }
        
        // Освобождаем ресурсы при закрытии приложения
        if (_gameManager != null)
        {
            _gameManager.Dispose();
        }
        
        // Останавливаем игровой цикл
        if (_gameLoop != null)
        {
            _gameLoop.Stop();
        }
        
        // Освобождаем ресурсы менеджера чанков
        if (_gameManager?._levelGenerator != null)
        {
            var chunkManager = _gameManager.GetChunkManager();
            if (chunkManager != null)
            {
                (chunkManager as IDisposable)?.Dispose();
            }
        }
        
        // Очищаем ресурсы предзагрузки
        _preloadCancellation?.Dispose();
        _preloadCancellation = null;
        
        Console.WriteLine("Ресурсы игры освобождены при закрытии приложения.");
    }

    /// <summary>
    /// Предзагружает игру и чанки вокруг начальной позиции игрока
    /// </summary>
    private void PreloadGame(CancellationToken cancellationToken)
    {
        try
        {
            // Шаг 1: Инициализация базовых игровых компонентов
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Инициализация игровых компонентов...";
                UpdateLoadingProgress(10);
                
                // Проверяем наличие спрайтов игрока
                Console.WriteLine("Проверка наличия спрайтов игрока в PreloadGame:");
                string[] playerSprites = new string[] {
                    "player_pistol_stop", "player_pistol_run_v1", "player_pistol_run_v2",
                    "player_shotgun_stop", "player_shotgun_run_v1", "player_shotgun_run_v2"
                };
                
                foreach (var spriteName in playerSprites)
                {
                    bool exists = _spriteManager.HasSprite(spriteName);
                    Console.WriteLine($"Спрайт {spriteName}: {(exists ? "найден" : "НЕ НАЙДЕН")}");
                }
            });
            
            // Шаг 2: Инициализация базовых игровых компонентов
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Инициализация игровых компонентов...";
                UpdateLoadingProgress(5);
            });
            
            // Рассчитываем размеры мира (в 3 раза больше экрана)
            double worldWidth = 0, worldHeight = 0;
            double centerX = 0, centerY = 0;
            
            Dispatcher.Invoke(() => {
                worldWidth = GameCanvas.ActualWidth * 3.0;
                worldHeight = GameCanvas.ActualHeight * 3.0;
                centerX = worldWidth / 2;
                centerY = worldHeight / 2;
            });
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 3: Создание игрока
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Создание игрока...";
                UpdateLoadingProgress(10);
                
                _player = new Player(centerX, centerY, _spriteManager);
                _inputHandler = new InputHandler(_player);
                GameCanvas.Children.Add(_player.PlayerShape);
                _player.AddWeaponToCanvas(GameCanvas);
                _player.AddColliderVisualToCanvas(GameCanvas);
            });
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 4: Инициализация менеджера игры
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Инициализация игрового мира...";
                UpdateLoadingProgress(20);
                
                _gameManager = new GameManager(GameCanvas, _player, GameCanvas.ActualWidth, GameCanvas.ActualHeight, _spriteManager);
                _gameManager.ScoreChanged += GameManager_ScoreChanged;
                _gameManager.WeaponChanged += GameManager_WeaponChanged;
                _gameManager.EnemyKilled += GameManager_EnemyKilled;
                _gameManager.HealthKitCollected += GameManager_HealthKitCollected;
                _gameManager.WeaponPickedUp += GameManager_WeaponPickedUp;
                _gameManager.ArmorKitCollected += GameManager_ArmorKitCollected;
                _gameManager.SkillSelectionAvailable += GameManager_SkillSelectionAvailable;
                _gameManager.QuestProgressChanged += GameManager_QuestProgressChanged;
            });
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 5: Предварительная загрузка чанков вокруг игрока
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Предзагрузка мира вокруг игрока...";
                UpdateLoadingProgress(30);
            });
            
            // Получаем менеджер чанков
            ChunkManager chunkManager = _gameManager.GetChunkManager();
            
            // Определяем чанк игрока
            var (playerChunkX, playerChunkY) = Chunk.WorldToChunk(centerX, centerY);
            
            // Рассчитываем общее количество чанков для загрузки
            _totalChunksToLoad = (2 * PRELOAD_RADIUS + 1) * (2 * PRELOAD_RADIUS + 1);
            _loadedChunksCount = 0;
            
            // Предзагружаем чанки в большой зоне вокруг игрока
            for (int dy = -PRELOAD_RADIUS; dy <= PRELOAD_RADIUS; dy++)
            {
                for (int dx = -PRELOAD_RADIUS; dx <= PRELOAD_RADIUS; dx++)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    
                    int chunkX = playerChunkX + dx;
                    int chunkY = playerChunkY + dy;
                    
                    // Создаем чанк и ждем его загрузки
                    Chunk chunk = chunkManager.GetOrCreateChunk(chunkX, chunkY);
                    
                    // Определяем приоритет загрузки (ближние чанки - выше приоритет)
                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int loadPriority = 10 * distance; // 0 для центрального чанка, больше для дальних
                    
                    // Активируем чанки рядом с игроком
                    if (distance <= ChunkManager.ACTIVATION_DISTANCE)
                    {
                        chunk.IsActive = true;
                    }
                    
                    // Имитируем ожидание загрузки чанка
                    int waitTime = 10 + loadPriority; // Чем дальше чанк, тем дольше загрузка
                    Thread.Sleep(waitTime);
                    
                    // Обновляем прогресс
                    _loadedChunksCount++;
                    int progressPercent = 30 + (int)(60.0 * _loadedChunksCount / _totalChunksToLoad);
                    
                    Dispatcher.Invoke(() => {
                        UpdateLoadingProgress(progressPercent);
                    });
                }
            }
            
            if (cancellationToken.IsCancellationRequested) return;
            
            // Шаг 6: Завершение инициализации
            Dispatcher.Invoke(() => {
                LoadingStatusText.Text = "Запуск игрового цикла...";
                UpdateLoadingProgress(95);
                
                // Инициализируем игровой цикл
                _gameLoop = new GameLoop(_gameManager, GameCanvas.ActualWidth, GameCanvas.ActualHeight);
                _gameLoop.GameTick += GameLoop_GameTick;
                
                // Обновляем активные чанки вокруг игрока
                _gameManager.GetChunkManager().UpdateActiveChunks(_player.X, _player.Y, _player.VelocityX, _player.VelocityY);
                
                LoadingStatusText.Text = "Загрузка завершена!";
                UpdateLoadingProgress(100);
            });
            
            // Небольшая пауза, чтобы показать 100% загрузки
            Thread.Sleep(500);
            
            // Запускаем игру в UI потоке
            Dispatcher.Invoke(() => {
                // Останавливаем таймер анимации
                _loadingAnimationTimer?.Stop();
                
                // Скрываем экран загрузки
                LoadingScreen.Visibility = Visibility.Collapsed;
                
                // Сбрасываем флаг окончания игры
                _isGameOver = false;
                
                // Запускаем игровой цикл
                _gameLoop.Start();
                
                // Фокус на канвас для обработки ввода
                GameCanvas.Focus();
                
                // Обновляем информацию об игроке
                UpdatePlayerInfo();
                
                // Запоминаем время начала игры
                _gameStartTime = DateTime.Now;
                
                // Сбрасываем отслеживание выпавшего оружия
                _gameManager.ResetDroppedWeapons();
                
                // Сбрасываем выполненные квесты
                _gameManager.ResetCompletedQuests();
                
                Console.WriteLine("Игра успешно инициализирована");
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => {
                MessageBox.Show($"Ошибка при инициализации игры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Ошибка при инициализации игры: {ex}");
                
                // Скрываем экран загрузки при ошибке
                LoadingScreen.Visibility = Visibility.Collapsed;
            });
        }
    }

    /// <summary>
    /// Обновляет отображаемый прогресс загрузки
    /// </summary>
    private void UpdateLoadingProgress(int percent)
    {
        LoadingProgressBar.Value = percent;
        LoadingProgressText.Text = $"{percent}%";
        
        // Обновляем ширину индикатора прогресса
        double progressWidth = (percent / 100.0) * 500; // 500 - ширина контейнера
        ProgressIndicator.Width = progressWidth;
    }

    /// <summary>
    /// Инициализирует и запускает анимацию текста загрузки
    /// </summary>
    private void InitializeLoadingAnimation()
    {
        _loadingAnimationTimer = new System.Windows.Threading.DispatcherTimer();
        _loadingAnimationTimer.Tick += LoadingAnimation_Tick;
        _loadingAnimationTimer.Interval = TimeSpan.FromMilliseconds(500); // Обновление каждые 500 мс
        _loadingAnimationTimer.Start();
    }

    /// <summary>
    /// Обрабатывает тик таймера анимации загрузки
    /// </summary>
    private int _animationDotCount = 0;
    private void LoadingAnimation_Tick(object sender, EventArgs e)
    {
        if (LoadingStatusText.Text.EndsWith("..."))
        {
            // Обрезаем многоточие
            string baseText = LoadingStatusText.Text.Substring(0, LoadingStatusText.Text.Length - 3);
            LoadingStatusText.Text = baseText;
            _animationDotCount = 0;
        }
        else
        {
            _animationDotCount++;
            LoadingStatusText.Text += ".";
        }
    }

    /// <summary>
    /// Создает анимированные частицы для экрана загрузки
    /// </summary>
    private void CreateLoadingScreenParticles()
    {
        // Очищаем существующие частицы
        ParticlesCanvas.Children.Clear();
        
        // Создаем случайный генератор
        Random random = new Random();
        
        // Создаем 50 случайных частиц
        for (int i = 0; i < 50; i++)
        {
            // Создаем частицу (эллипс)
            Ellipse particle = new Ellipse();
            
            // Задаем случайный размер (от 2 до 6)
            double size = random.Next(2, 7);
            particle.Width = size;
            particle.Height = size;
            
            // Задаем случайный цвет
            string[] colors = { "#3498db", "#2ecc71", "#e74c3c", "#f1c40f", "#9b59b6", "#1abc9c", "#FFD700" };
            particle.Fill = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    colors[random.Next(colors.Length)]));
            
            // Задаем случайную позицию
            double canvasWidth = ParticlesCanvas.ActualWidth > 0 ? ParticlesCanvas.ActualWidth : 1200;
            double canvasHeight = ParticlesCanvas.ActualHeight > 0 ? ParticlesCanvas.ActualHeight : 700;
            Canvas.SetLeft(particle, random.NextDouble() * canvasWidth);
            Canvas.SetTop(particle, random.NextDouble() * canvasHeight);
            
            // Задаем случайную прозрачность
            particle.Opacity = random.NextDouble() * 0.5 + 0.2; // от 0.2 до 0.7
            
            // Добавляем частицу на канвас
            ParticlesCanvas.Children.Add(particle);
            
            // Создаем анимацию для частицы
            AnimateParticle(particle, random);
        }
    }
    
    /// <summary>
    /// Анимирует частицу на экране загрузки
    /// </summary>
    private void AnimateParticle(Ellipse particle, Random random)
    {
        // Создаем анимацию движения по вертикали
        DoubleAnimation moveAnimation = new DoubleAnimation();
        moveAnimation.From = Canvas.GetTop(particle);
        moveAnimation.To = Canvas.GetTop(particle) - random.Next(50, 200); // Движение вверх
        moveAnimation.Duration = TimeSpan.FromSeconds(random.Next(5, 15)); // Случайная длительность
        moveAnimation.RepeatBehavior = RepeatBehavior.Forever;
        
        // Создаем анимацию прозрачности
        DoubleAnimation opacityAnimation = new DoubleAnimation();
        opacityAnimation.From = particle.Opacity;
        opacityAnimation.To = random.NextDouble() * 0.3 + 0.1; // Случайная прозрачность
        opacityAnimation.Duration = TimeSpan.FromSeconds(random.Next(2, 6));
        opacityAnimation.AutoReverse = true;
        opacityAnimation.RepeatBehavior = RepeatBehavior.Forever;
        
        // Запускаем анимации
        particle.BeginAnimation(Canvas.TopProperty, moveAnimation);
        particle.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
    }
    
    /// <summary>
    /// Обработчик нажатия на кнопку "Начать сначала"
    /// </summary>
    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        // Скрываем экран смерти
        DeathScreen.Visibility = Visibility.Collapsed;
        
        // Очищаем игровой канвас
        GameCanvas.Children.Clear();
        
        // Сбрасываем счет и статистику
        _score = 0;
        _enemiesKilled = 0;
        
        // Сбрасываем флаг окончания игры
        _isGameOver = false;
        
        // Освобождаем ресурсы текущей игры
        if (_gameManager != null)
        {
            _gameManager.Dispose();
            _gameManager = null;
        }
        
        // Останавливаем игровой цикл, если он еще запущен
        if (_gameLoop != null)
        {
            _gameLoop.Stop();
            _gameLoop = null;
        }
        
        // Показываем экран загрузки
        LoadingScreen.Visibility = Visibility.Visible;
        LoadingStatusText.Text = "Перезапуск игры...";
        UpdateLoadingProgress(0);
        
        // Создаем анимированные частицы
        CreateLoadingScreenParticles();
        
        // Инициализируем и запускаем таймер анимации загрузки
        InitializeLoadingAnimation();
        
        // Запускаем инициализацию игры с предзагрузкой в отдельном потоке
        _preloadCancellation = new CancellationTokenSource();
        _preloadTask = Task.Run(() => PreloadGame(_preloadCancellation.Token), _preloadCancellation.Token);
    }
    
    /// <summary>
    /// Обработчик нажатия на кнопку "Выйти"
    /// </summary>
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        // Закрываем приложение
        Application.Current.Shutdown();
    }

    private void UpdatePlayerStats()
    {
        if (_gameManager != null && _gameManager._player != null)
        {
            double healthPercent = (_gameManager._player.Health / _gameManager._player.MaxHealth) * 100;
            HealthBar.Value = healthPercent;
            HealthText.Text = $"Здоровье: {_gameManager._player.Health:F0}/{_gameManager._player.MaxHealth:F0}";
            
            double armorPercent = (_gameManager._player.Armor / _gameManager._player.MaxArmor) * 100;
            ArmorBar.Value = armorPercent;
            ArmorText.Text = $"Броня: {_gameManager._player.Armor:F0}/{_gameManager._player.MaxArmor:F0}";
            
            AmmoText.Text = _gameManager.GetAmmoInfo();
        }
    }

    private void OnHealthKitCollected(object sender, double amount)
    {
        ShowMessage($"Подобрана аптечка (+{amount} HP)", Colors.Green);
        UpdatePlayerStats();
    }

    private void OnArmorKitCollected(object sender, double amount)
    {
        ShowMessage($"Подобран бронежилет (+{amount} брони)", Colors.Blue);
        UpdatePlayerStats();
    }

    // Показывает уведомление с указанным цветом
    private void ShowMessage(string message, Color color)
    {
        ShowNotification(message);
    }

    // Обработчик события доступности выбора навыка
    private void GameManager_SkillSelectionAvailable(object sender, int score)
    {
        // Приостанавливаем игровой цикл
        _gameLoop?.Pause();
        
        try
        {
            // Создаем и показываем окно выбора навыков
            var skillWindow = new Views.SkillSelectionWindow(_player);
            
            // Подписываемся на событие выбора навыка
            skillWindow.SkillSelected += (s, skill) => 
            {
                // Показываем уведомление о выбранном навыке
                ShowNotification($"Получен навык: {skill.Name}");
                
                // Обновляем информацию об игроке
                UpdatePlayerInfo();
            };
            
            // Показываем окно как диалог
            bool? result = skillWindow.ShowDialog();
            
            // Если окно было закрыто без выбора навыка, выбираем случайный
            if (result != true)
            {
                // В этой реализации окно всегда закрывается с DialogResult = true
                // поэтому этот код не должен выполняться
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при показе окна выбора навыков: {ex.Message}");
        }
        finally
        {
            // Возобновляем игровой цикл
            _gameLoop?.Resume();
            
            // Возвращаем фокус на игровой канвас
            GameCanvas.Focus();
        }
    }

    private void GameManager_QuestProgressChanged(object sender, string text)
    {
        Dispatcher.Invoke(() =>
        {
            if (!string.IsNullOrEmpty(text))
            {
                QuestProgressText.Text = text;
                QuestNotificationBorder.Visibility = Visibility.Visible;
                
                // Добавляем эффект появления, если уведомление было скрыто
                if (QuestNotificationBorder.Opacity < 1)
                {
                    DoubleAnimation fadeIn = new DoubleAnimation
                    {
                        From = 0.7,
                        To = 1.0,
                        Duration = TimeSpan.FromSeconds(0.3)
                    };
                    QuestNotificationBorder.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
                
                // Обновляем индикатор прогресса на основе текста
                UpdateQuestProgressIndicator(text);
                
                // Если это уведомление о выполнении или провале квеста, добавляем подсветку
                if (text.Contains("выполнен") || text.Contains("Квест выполнен"))
                {
                    QuestNotificationBorder.Background = new SolidColorBrush(Color.FromArgb(80, 50, 205, 50)); // Зеленый
                    
                    // Анимация завершения - заполняем индикатор до 100%
                    DoubleAnimation completeAnimation = new DoubleAnimation
                    {
                        To = (QuestProgressIndicator.Parent as FrameworkElement)?.ActualWidth ?? 400,
                        Duration = TimeSpan.FromSeconds(0.5),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    QuestProgressIndicator.BeginAnimation(FrameworkElement.WidthProperty, completeAnimation);
                }
                else if (text.Contains("провален"))
                {
                    QuestNotificationBorder.Background = new SolidColorBrush(Color.FromArgb(80, 220, 20, 60)); // Красный
                    
                    // Анимация провала - мигаем индикатором
                    DoubleAnimation failAnimation = new DoubleAnimation
                    {
                        From = QuestProgressIndicator.Width,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.7),
                        EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut }
                    };
                    QuestProgressIndicator.BeginAnimation(FrameworkElement.WidthProperty, failAnimation);
                }
                else if (text.Contains("начат"))
                {
                    QuestNotificationBorder.Background = new SolidColorBrush(Color.FromArgb(80, 255, 215, 0)); // Золотой
                    
                    // Анимация мигания при начале квеста
                    ColorAnimation blinkAnimation = new ColorAnimation
                    {
                        From = Color.FromArgb(120, 255, 215, 0),
                        To = Color.FromArgb(50, 255, 215, 0),
                        Duration = TimeSpan.FromSeconds(0.5),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(3)
                    };
                    
                    SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(80, 255, 215, 0));
                    QuestNotificationBorder.Background = brush;
                    brush.BeginAnimation(SolidColorBrush.ColorProperty, blinkAnimation);
                    
                    // Сбрасываем индикатор прогресса
                    QuestProgressIndicator.Width = 0;
                }
                else
                {
                    // Обычный фон для обновлений прогресса
                    QuestNotificationBorder.Background = new SolidColorBrush(Color.FromArgb(50, 255, 215, 0));
                }
            }
            else
            {
                // Плавно скрываем уведомление
                DoubleAnimation fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                fadeOut.Completed += (s, e) => QuestNotificationBorder.Visibility = Visibility.Collapsed;
                QuestNotificationBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                
                QuestProgressText.Text = "";
            }
        });
    }

    /// <summary>
    /// Обновляет индикатор прогресса задания на основе текста уведомления
    /// </summary>
    private void UpdateQuestProgressIndicator(string questText)
    {
        try
        {
            // Получаем ширину родительского контейнера
            double maxWidth = (QuestProgressIndicator.Parent as FrameworkElement)?.ActualWidth ?? 400;
            
            // Определяем тип задания и прогресс
            double progress = 0;
            
            if (questText.Contains("Убито:"))
            {
                // Формат: "Квест: Убейте 7 врагов за 40 сек | Убито: X/7 | Время: Y сек"
                int currentKills = 0;
                int totalKills = 7;
                
                // Извлекаем текущее количество убийств
                int killsIndex = questText.IndexOf("Убито:");
                if (killsIndex > 0)
                {
                    string killsPart = questText.Substring(killsIndex + 7);
                    string[] killsData = killsPart.Split('/');
                    if (killsData.Length > 0)
                    {
                        int.TryParse(killsData[0].Trim(), out currentKills);
                    }
                }
                
                progress = (double)currentKills / totalKills;
            }
            else if (questText.Contains("Собрано:"))
            {
                // Формат: "Квест: Соберите 4 аптечки | Собрано: X/4"
                int currentCollected = 0;
                int totalToCollect = 4;
                
                // Извлекаем текущее количество собранных аптечек
                int collectedIndex = questText.IndexOf("Собрано:");
                if (collectedIndex > 0)
                {
                    string collectedPart = questText.Substring(collectedIndex + 9);
                    string[] collectedData = collectedPart.Split('/');
                    if (collectedData.Length > 0)
                    {
                        int.TryParse(collectedData[0].Trim(), out currentCollected);
                    }
                }
                
                progress = (double)currentCollected / totalToCollect;
            }
            else if (questText.Contains("Очки:"))
            {
                // Формат: "Квест: Наберите 2000 очков | Очки: X/2000"
                int currentScore = 0;
                int targetScore = 2000;
                
                // Извлекаем текущее количество очков
                int scoreIndex = questText.IndexOf("Очки:");
                if (scoreIndex > 0)
                {
                    string scorePart = questText.Substring(scoreIndex + 6);
                    string[] scoreData = scorePart.Split('/');
                    if (scoreData.Length > 0)
                    {
                        int.TryParse(scoreData[0].Trim(), out currentScore);
                    }
                }
                
                progress = (double)currentScore / targetScore;
            }
            
            // Ограничиваем прогресс от 0 до 1
            progress = Math.Max(0, Math.Min(1, progress));
            
            // Обновляем ширину индикатора прогресса с анимацией
            double targetWidth = progress * maxWidth;
            
            DoubleAnimation progressAnimation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            
            QuestProgressIndicator.BeginAnimation(FrameworkElement.WidthProperty, progressAnimation);
            
            // Обновляем цвет индикатора в зависимости от прогресса
            Color indicatorColor;
            if (progress < 0.3)
                indicatorColor = Color.FromRgb(255, 215, 0); // Золотой
            else if (progress < 0.7)
                indicatorColor = Color.FromRgb(255, 165, 0); // Оранжевый
            else
                indicatorColor = Color.FromRgb(50, 205, 50); // Зеленый
            
            QuestProgressIndicator.Background = new SolidColorBrush(indicatorColor);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обновлении индикатора прогресса: {ex.Message}");
        }
    }
}