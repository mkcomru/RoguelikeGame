using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Shapes;
using System.Windows;

namespace GunVault.GameEngine
{
    public class SpriteManager
    {
        private Dictionary<string, BitmapImage> _spriteCache;
        private string _spritesFolder;
        private bool _initialized;
        
        public SpriteManager(string spritesFolder = "Sprites")
        {
            _spriteCache = new Dictionary<string, BitmapImage>();
            _initialized = false;
            
            try
            {
                string projectDir = Directory.GetCurrentDirectory();
                _spritesFolder = System.IO.Path.Combine(projectDir, spritesFolder);
                
                if (!Directory.Exists(_spritesFolder))
                {
                    string exePath = AppDomain.CurrentDomain.BaseDirectory;
                    _spritesFolder = System.IO.Path.Combine(exePath, spritesFolder);
                    
                    if (!Directory.Exists(_spritesFolder))
                    {
                        string parentDir = Directory.GetParent(exePath)?.FullName ?? exePath;
                        _spritesFolder = System.IO.Path.Combine(parentDir, spritesFolder);
                        
                        if (!Directory.Exists(_spritesFolder))
                        {
                            string grandParentDir = Directory.GetParent(parentDir)?.FullName ?? parentDir;
                            _spritesFolder = System.IO.Path.Combine(grandParentDir, spritesFolder);
                            
                            if (!Directory.Exists(_spritesFolder))
                            {
                                _spritesFolder = System.IO.Path.Combine(exePath, spritesFolder);
                                Console.WriteLine($"Предупреждение: Папка со спрайтами не найдена: {spritesFolder}");
                                return;
                            }
                        }
                    }
                }
                
                _initialized = true;
                Console.WriteLine($"Папка со спрайтами найдена: {_spritesFolder}");

                if (_initialized)
                {
                    string minePath = System.IO.Path.Combine(_spritesFolder, "mine.png");
                    if (File.Exists(minePath))
                    {
                        RegisterSprite("mine", minePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации менеджера спрайтов: {ex.Message}");
            }
        }
        
        public BitmapImage LoadSprite(string spriteName)
        {
            try
            {
                if (_spriteCache.ContainsKey(spriteName))
                {
                    Console.WriteLine($"Спрайт {spriteName} загружен из кэша");
                    return _spriteCache[spriteName];
                }
                
                if (!_initialized)
                {
                    Console.WriteLine($"Менеджер спрайтов не инициализирован, невозможно загрузить: {spriteName}");
                    return null;
                }
                
                // Сначала ищем в корневой папке спрайтов
                string filePath = System.IO.Path.Combine(_spritesFolder, $"{spriteName}.png");
                
                // Если файл не найден в корневой папке, ищем в подпапках
                if (!File.Exists(filePath))
                {
                    // Проверяем в подпапке player
                    string playerPath = System.IO.Path.Combine(_spritesFolder, "player", $"{spriteName}.png");
                    if (File.Exists(playerPath))
                    {
                        filePath = playerPath;
                    }
                    else
                    {
                        // Проверяем в подпапке enemyes
                        string enemyPath = System.IO.Path.Combine(_spritesFolder, "enemyes", $"{spriteName}.png");
                        if (File.Exists(enemyPath))
                        {
                            filePath = enemyPath;
                        }
                        else
                        {
                            // Проверяем в подпапке guns
                            string gunPath = System.IO.Path.Combine(_spritesFolder, "guns", $"{spriteName}.png");
                            if (File.Exists(gunPath))
                            {
                                filePath = gunPath;
                            }
                        }
                    }
                }
                
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл спрайта не найден: {filePath}");
                    return null;
                }
                
                Console.WriteLine($"Загружаю спрайт: {filePath}");
                
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                _spriteCache[spriteName] = bitmap;
                Console.WriteLine($"Спрайт {spriteName} успешно загружен, размер: {bitmap.PixelWidth}x{bitmap.PixelHeight}");
                
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке спрайта {spriteName}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Проверяет, доступен ли спрайт с указанным именем
        /// </summary>
        /// <param name="spriteName">Имя спрайта</param>
        /// <returns>true, если спрайт доступен</returns>
        public bool HasSprite(string spriteName)
        {
            try
            {
                if (_spriteCache.ContainsKey(spriteName))
                {
                    return true;
                }
                
                if (!_initialized)
                {
                    Console.WriteLine($"Менеджер спрайтов не инициализирован, невозможно проверить наличие: {spriteName}");
                    return false;
                }
                
                // Проверяем в корневой папке
                string filePath = System.IO.Path.Combine(_spritesFolder, $"{spriteName}.png");
                bool exists = File.Exists(filePath);
                
                // Если не найден в корневой папке, проверяем в подпапках
                if (!exists)
                {
                    // Проверяем в подпапке player
                    string playerPath = System.IO.Path.Combine(_spritesFolder, "player", $"{spriteName}.png");
                    exists = File.Exists(playerPath);
                    
                    if (!exists)
                    {
                        // Проверяем в подпапке enemyes
                        string enemyPath = System.IO.Path.Combine(_spritesFolder, "enemyes", $"{spriteName}.png");
                        exists = File.Exists(enemyPath);
                        
                        if (!exists)
                        {
                            // Проверяем в подпапке guns
                            string gunPath = System.IO.Path.Combine(_spritesFolder, "guns", $"{spriteName}.png");
                            exists = File.Exists(gunPath);
                        }
                    }
                }
                
                Console.WriteLine($"Проверка спрайта {spriteName}: {(exists ? "найден" : "не найден")}");
                
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке наличия спрайта {spriteName}: {ex.Message}");
                return false;
            }
        }
        
        public UIElement CreateSpriteImage(string spriteName, double width, double height)
        {
            try
            {
                BitmapImage sprite = LoadSprite(spriteName);
                
                if (sprite == null)
                {
                    Console.WriteLine($"Создаю запасную форму для спрайта {spriteName} (размеры: {width}x{height})");
                    return CreateFallbackShape(width, height, spriteName);
                }
                
                Image image = new Image
                {
                    Source = sprite,
                    Width = width,
                    Height = height,
                    Stretch = Stretch.Uniform
                };
                
                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании спрайта {spriteName}: {ex.Message}");
                return CreateFallbackShape(width, height, spriteName);
            }
        }
        
        private UIElement CreateFallbackShape(double width, double height, string spriteName = null)
        {
            Brush fillBrush = Brushes.DarkGray;
            
            if (spriteName != null)
            {
                switch (spriteName.ToLower())
                {
                    // Оружие
                    case "pistol": fillBrush = Brushes.DarkGray; break;
                    case "shotgun": fillBrush = Brushes.Brown; break;
                    case "assault_rifle": fillBrush = Brushes.Green; break;
                    case "sniper": fillBrush = Brushes.DarkBlue; break;
                    case "machine_gun": fillBrush = Brushes.DarkGreen; break;
                    case "rocket_launcher": fillBrush = Brushes.Red; break;
                    case "laser": fillBrush = Brushes.Purple; break;
                    
                    // Персонажи
                    case "enemy1": fillBrush = Brushes.Red; break;
                    case "player": fillBrush = Brushes.Blue; break;
                    
                    // Тайлы
                    case "grass1": fillBrush = Brushes.ForestGreen; break;
                    case "dirt1": fillBrush = Brushes.SaddleBrown; break;
                    case "water1": fillBrush = Brushes.DodgerBlue; break;
                    case "stone1": fillBrush = Brushes.Gray; break;
                    case "sand1": fillBrush = Brushes.Khaki; break;
                    
                    // Предметы
                    case "healthkit": fillBrush = Brushes.Red; break;
                }
            }
            
            return new Rectangle
            {
                Width = width,
                Height = height,
                Fill = fillBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
        }

        public void Initialize(string spritesFolder)
        {
            _spritesFolder = spritesFolder;
            _initialized = true;
            _spriteCache = new Dictionary<string, BitmapImage>();
            
            Console.WriteLine($"SpriteManager инициализирован. Путь к папке спрайтов: {_spritesFolder}");
            Console.WriteLine($"Полный путь: {System.IO.Path.GetFullPath(_spritesFolder)}");
            
            if (!Directory.Exists(_spritesFolder))
            {
                Console.WriteLine($"ВНИМАНИЕ: Папка спрайтов не существует: {_spritesFolder}");
            }
            else
            {
                // Выводим информацию о файлах в корневой папке
                var rootFiles = Directory.GetFiles(_spritesFolder, "*.png");
                Console.WriteLine($"Найдено {rootFiles.Length} PNG файлов в корневой папке спрайтов:");
                foreach (var file in rootFiles)
                {
                    Console.WriteLine($"  - {System.IO.Path.GetFileName(file)}");
                }
                
                // Проверяем подпапку player
                string playerFolder = System.IO.Path.Combine(_spritesFolder, "player");
                if (Directory.Exists(playerFolder))
                {
                    var playerFiles = Directory.GetFiles(playerFolder, "*.png");
                    Console.WriteLine($"Найдено {playerFiles.Length} PNG файлов в папке player:");
                    foreach (var file in playerFiles)
                    {
                        Console.WriteLine($"  - player/{System.IO.Path.GetFileName(file)}");
                    }
                }
                else
                {
                    Console.WriteLine("Папка player не найдена");
                }
                
                // Проверяем подпапку enemyes
                string enemyFolder = System.IO.Path.Combine(_spritesFolder, "enemyes");
                if (Directory.Exists(enemyFolder))
                {
                    var enemyFiles = Directory.GetFiles(enemyFolder, "*.png");
                    Console.WriteLine($"Найдено {enemyFiles.Length} PNG файлов в папке enemyes:");
                    foreach (var file in enemyFiles)
                    {
                        Console.WriteLine($"  - enemyes/{System.IO.Path.GetFileName(file)}");
                    }
                }
                else
                {
                    Console.WriteLine("Папка enemyes не найдена");
                }
                
                // Проверяем подпапку guns
                string gunsFolder = System.IO.Path.Combine(_spritesFolder, "guns");
                if (Directory.Exists(gunsFolder))
                {
                    var gunsFiles = Directory.GetFiles(gunsFolder, "*.png");
                    Console.WriteLine($"Найдено {gunsFiles.Length} PNG файлов в папке guns:");
                    foreach (var file in gunsFiles)
                    {
                        Console.WriteLine($"  - guns/{System.IO.Path.GetFileName(file)}");
                    }
                }
                else
                {
                    Console.WriteLine("Папка guns не найдена");
                }
            }
        }

        public void RegisterSprite(string spriteName, string filePath)
        {
            try
            {
                if (_spriteCache.ContainsKey(spriteName))
                {
                    Console.WriteLine($"Спрайт {spriteName} уже зарегистрирован");
                    return;
                }
                
                if (!_initialized)
                {
                    Console.WriteLine($"Менеджер спрайтов не инициализирован, невозможно зарегистрировать: {spriteName}");
                    return;
                }
                
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл спрайта не найден: {filePath}");
                    return;
                }
                
                Console.WriteLine($"Загружаю спрайт: {filePath}");
                
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                _spriteCache[spriteName] = bitmap;
                Console.WriteLine($"Спрайт {spriteName} успешно загружен, размер: {bitmap.PixelWidth}x{bitmap.PixelHeight}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при регистрации спрайта {spriteName}: {ex.Message}");
            }
        }
    }
} 