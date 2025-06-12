using System;
using System.Windows;
using System.Windows.Threading;
using GunVault.Models;

namespace GunVault.GameEngine
{
    public class GameLoop
    {
        private DispatcherTimer _gameTimer;
        private GameManager _gameManager;
        private double _gameWidth;
        private double _gameHeight;
        private DateTime _lastTime;
        
        // Переменные для отслеживания FPS
        private int _frameCount = 0;
        private double _elapsedTime = 0;
        private double _fps = 0;
        
        private DateTime _lastUpdateTime;
        private bool _isPaused = false;
        
        public event EventHandler GameTick;
        
        public GameLoop(GameManager gameManager, double gameWidth, double gameHeight)
        {
            _gameManager = gameManager;
            _gameWidth = gameWidth;
            _gameHeight = gameHeight;
            _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _gameTimer.Tick += GameTimerTick;
            _lastTime = DateTime.Now;
        }
        
        public void Start()
        {
            _lastTime = DateTime.Now;
            _gameTimer.Start();
            _isPaused = false;
        }
        
        public void Stop()
        {
            _gameTimer.Stop();
            _isPaused = false;
        }
        
        /// <summary>
        /// Приостанавливает игровой цикл
        /// </summary>
        public void Pause()
        {
            if (!_isPaused)
            {
                _gameTimer.Stop();
                _isPaused = true;
                Console.WriteLine("Игровой цикл приостановлен");
            }
        }
        
        /// <summary>
        /// Возобновляет игровой цикл
        /// </summary>
        public void Resume()
        {
            if (_isPaused)
            {
                _lastTime = DateTime.Now;
                _gameTimer.Start();
                _isPaused = false;
                Console.WriteLine("Игровой цикл возобновлен");
            }
        }
        
        private void GameTimerTick(object sender, EventArgs e)
        {
            DateTime currentTime = DateTime.Now;
            double deltaTime = (currentTime - _lastTime).TotalSeconds;
            _lastTime = currentTime;
            deltaTime = Math.Min(deltaTime, 0.1); // Ограничиваем дельту времени для стабильности
            
            // Отслеживание FPS
            _frameCount++;
            _elapsedTime += deltaTime;
            
            if (_elapsedTime >= 1.0)
            {
                _fps = _frameCount / _elapsedTime;
                _frameCount = 0;
                _elapsedTime = 0;
                
                // Выводим FPS каждую секунду
                Console.WriteLine($"FPS: {_fps:F1}");
            }
            
            // Обновляем состояние игры
            _gameManager.Update(deltaTime);
            
            // Обновляем анимацию игрока
            if (_gameManager != null && _gameManager._player != null)
            {
                Console.WriteLine($"АНИМАЦИЯ: вызов UpdateAnimation с deltaTime={deltaTime:F3}");
                _gameManager._player.UpdateAnimation(deltaTime);
            }
            
            // Вызываем событие тика
            GameTick?.Invoke(this, EventArgs.Empty);
        }
        
        public void ResizeGameArea(double width, double height)
        {
            _gameWidth = width;
            _gameHeight = height;
            _gameManager.ResizeGameArea(width, height);
        }
    }
} 