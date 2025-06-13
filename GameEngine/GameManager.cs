using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GunVault.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace GunVault.GameEngine
{
    public class GameManager
    {
        private Canvas _gameCanvas;
        public Player _player;
        private List<Enemy> _enemies;
        private List<Bullet> _bullets;
        private List<Explosion> _explosions;
        private List<LaserBeam> _lasers;
        private List<BulletImpactEffect> _bulletImpactEffects;
        private List<HealthKit> _healthKits;
        private List<WeaponDrop> _weaponDrops;
        private List<ArmorKit> _armorKits;
        public LevelGenerator _levelGenerator;
        private double _worldWidth;
        private double _worldHeight;
        private Canvas _worldContainer;
        private Camera _camera;
        private bool _showChunkBoundaries = false;
        private double _enemyDespawnCheckTimer = 0;
        private const double ENEMY_DESPAWN_CHECK_INTERVAL = 5.0;
        private double _gameWidth;
        private double _gameHeight;
        private Random _random;
        private int _score;
        private double _enemySpawnTimer;
        private double _enemySpawnRate = 2.0;
        private WeaponType _lastWeaponType;
        private const double INITIAL_SPAWN_RATE = 2.0;
        private const double MIN_SPAWN_RATE = 0.3;
        private const int SCORE_PER_SPAWN_RATE_DECREASE = 50;
        private const double SPAWN_RATE_DECREASE_STEP = 0.1;
        private const int SCORE_PER_MULTI_SPAWN = 200;
        private const int MAX_ENEMIES_ON_SCREEN = 20;
        private const double EXPLOSION_EXPANSION_SPEED = 150.0;
        public SpriteManager _spriteManager;
        public event EventHandler<int> ScoreChanged;
        public event EventHandler<string> WeaponChanged;
        public event EventHandler EnemyKilled;
        public event EventHandler<double> HealthKitCollected;
        public event EventHandler<WeaponType> WeaponPickedUp;
        public event EventHandler<double> ArmorKitCollected;
        public event EventHandler<int> SkillSelectionAvailable;

        private ChunkManager _chunkManager;

        private const double WORLD_SIZE_MULTIPLIER = 3.0;

        private const double ENEMY_DESPAWN_TIME = 3.0;
        
        private bool _useMultithreading = true;
        private CancellationTokenSource _enemyProcessingCancellation;
        private Task _enemyProcessingTask;
        private ConcurrentQueue<Enemy> _enemyUpdateQueue;
        private object _enemiesLock = new object();
        private int _maxEnemiesPerThread = 20;
        private int _processingThreadCount = 0;

        private int _enemyKillCounter = 0;
        private const int HEALTH_KIT_DROP_FREQUENCY = 10;
        private const int ARMOR_KIT_DROP_FREQUENCY = 13;

        // Пороги очков для выпадения оружия
        private static readonly Dictionary<WeaponType, int> WeaponScoreThresholds = new Dictionary<WeaponType, int>
        {
            { WeaponType.Shotgun, 100 },
            { WeaponType.AssaultRifle, 300 },
            { WeaponType.MachineGun, 500 },
            { WeaponType.RocketLauncher, 700 },
            { WeaponType.Laser, 1000 }
        };
        
        // Отслеживание выпавшего оружия
        private HashSet<WeaponType> _droppedWeapons = new HashSet<WeaponType>();

        // Константа для очков, необходимых для выбора навыка
        private const int SCORE_PER_SKILL_SELECTION = 500;
        // Последний порог очков, при котором был выбран навык
        private int _lastSkillSelectionScore = 0;

        private List<Mine> _mines;
        private const double MINE_SPAWN_INTERVAL = 8.0;
        private double _mineSpawnTimer = 0;

        private List<Barrel> _barrels;
        private const int BARREL_COUNT = 15;

        public GameManager(Canvas gameCanvas, Player player, double gameWidth, double gameHeight, SpriteManager spriteManager = null)
        {
            _gameCanvas = gameCanvas;
            _player = player;
            _gameWidth = gameWidth;
            _gameHeight = gameHeight;
            _spriteManager = spriteManager;
            _enemies = new List<Enemy>();
            _bullets = new List<Bullet>();
            _explosions = new List<Explosion>();
            _lasers = new List<LaserBeam>();
            _bulletImpactEffects = new List<BulletImpactEffect>();
            _healthKits = new List<HealthKit>();
            _weaponDrops = new List<WeaponDrop>();
            _armorKits = new List<ArmorKit>();
            _random = new Random();
            _score = 0;
            _enemySpawnTimer = 0;
            _enemySpawnRate = INITIAL_SPAWN_RATE;
            _lastWeaponType = _player.GetWeaponType();
            
            _enemyUpdateQueue = new ConcurrentQueue<Enemy>();
            _enemyProcessingCancellation = new CancellationTokenSource();
            
            _worldContainer = new Canvas
            {
                Width = _gameWidth * WORLD_SIZE_MULTIPLIER,
                Height = _gameHeight * WORLD_SIZE_MULTIPLIER
            };
            
            _worldWidth = _gameWidth * WORLD_SIZE_MULTIPLIER;
            _worldHeight = _gameHeight * WORLD_SIZE_MULTIPLIER;
            
            _gameCanvas.Children.Add(_worldContainer);
            
            _camera = new Camera(_gameWidth, _gameHeight, _worldWidth, _worldHeight);
            
            _camera.CenterOn(_player.X, _player.Y);
            
            Canvas.SetLeft(_worldContainer, -_camera.X);
            Canvas.SetTop(_worldContainer, -_camera.Y);
            
            _gameCanvas.Children.Remove(_player.PlayerShape);
            _worldContainer.Children.Add(_player.PlayerShape);
            
            _player.AddWeaponToCanvas(_worldContainer);
            
            _chunkManager = new ChunkManager(_worldContainer);
            
            _levelGenerator = new LevelGenerator(_worldContainer, _worldWidth, _worldHeight, _spriteManager);
            _levelGenerator.GenerateLevel();
            
            InitializeChunks();
            
            _chunkManager.EnemiesReadyToRestore += OnEnemiesReadyToRestore;

            if (_useMultithreading)
            {
                StartEnemyProcessingTask();
                Console.WriteLine("Многопоточная обработка врагов активирована");
            }

            _mines = new List<Mine>();
            _barrels = new List<Barrel>();
            SpawnBarrels();
        }
        
        private void InitializeChunks()
        {
            Dictionary<string, RectCollider> tileColliders = _levelGenerator.GetTileColliders();
            
            foreach (var colliderPair in tileColliders)
            {
                _chunkManager.AddTileCollider(colliderPair.Key, colliderPair.Value);
            }
            
            _chunkManager.UpdateActiveChunks(_player.X, _player.Y);
            
            Console.WriteLine($"Инициализированы чанки и распределены тайлы");
        }

        public void Update(double deltaTime)
        {
            _player.Move();
            
            _player.ConstrainToWorldBounds(0, 0, _worldWidth, _worldHeight);
            
            _camera.FollowTarget(_player.X, _player.Y);
            
            Canvas.SetLeft(_worldContainer, -_camera.X);
            Canvas.SetTop(_worldContainer, -_camera.Y);
            
            _chunkManager.UpdateActiveChunks(_player.X, _player.Y, _player.VelocityX, _player.VelocityY);
            _chunkManager.UpdateChunkMarkers();
            
            _enemyDespawnCheckTimer -= deltaTime;
            if (_enemyDespawnCheckTimer <= 0)
            {
                RemoveEnemiesInStaleChunks();
                _enemyDespawnCheckTimer = ENEMY_DESPAWN_CHECK_INTERVAL;
            }
            
            _enemySpawnTimer -= deltaTime;
            if (_enemySpawnTimer <= 0)
            {
                UpdateSpawnRate();
                if (_enemies.Count < MAX_ENEMIES_ON_SCREEN)
                {
                    int enemiesToSpawn = CalculateEnemiesToSpawn();
                    enemiesToSpawn = Math.Min(enemiesToSpawn, MAX_ENEMIES_ON_SCREEN - _enemies.Count);
                    for (int i = 0; i < enemiesToSpawn; i++)
                    {
                        SpawnEnemy();
                    }
                }
                _enemySpawnTimer = _enemySpawnRate;
            }
            
            foreach (var enemy in _enemies)
            {
                bool isInViewOrNear = _camera.IsInView(
                    enemy.X - 50, enemy.Y - 50, 
                    100, 100
                );
                
                if (isInViewOrNear)
                {
                    enemy.MoveTowardsPlayer(_player.X, _player.Y, deltaTime);
                }
            }
            
            Point mousePosition = Mouse.GetPosition(_gameCanvas);
            Point worldMousePosition = _camera.ScreenToWorld(mousePosition.X, mousePosition.Y);
            _player.UpdateWeapon(deltaTime, worldMousePosition);
            
            if (_player.GetCurrentWeapon().IsLaser)
            {
                LaserBeam newLaser = _player.ShootLaser(worldMousePosition);
                if (newLaser != null)
                {
                    _lasers.Add(newLaser);
                    _worldContainer.Children.Add(newLaser.LaserLine);
                    _worldContainer.Children.Add(newLaser.LaserDot);
                    
                    ProcessLaserCollisions(newLaser);
                }
            }
            else
            {
                List<Bullet> newBullets = _player.Shoot(worldMousePosition);
                if (newBullets != null && newBullets.Count > 0)
                {
                    foreach (Bullet bullet in newBullets)
                    {
                        _bullets.Add(bullet);
                        _worldContainer.Children.Add(bullet.BulletShape);
                    }
                }
            }
            
            UpdateBullets(deltaTime);
            UpdateLasers(deltaTime);
            UpdateExplosions(deltaTime);
            UpdateBulletImpacts(deltaTime);
            UpdateHealthKits(deltaTime);
            UpdateWeaponDrops(deltaTime);
            UpdateArmorKits(deltaTime);
            UpdateMines(deltaTime);
            UpdateBarrels(deltaTime);
            CheckCollisions();
            
            // Проверяем, нужно ли создать выпад оружия
            CheckWeaponDrops();

            _mineSpawnTimer -= deltaTime;
            if (_mineSpawnTimer <= 0)
            {
                SpawnMineRandom();
                _mineSpawnTimer = MINE_SPAWN_INTERVAL + _random.NextDouble() * 4.0;
            }
        }

        public void HandleKeyPress(KeyEventArgs e)
        {
            if (e.Key == Key.F3)
            {
                _showChunkBoundaries = !_showChunkBoundaries;
                _chunkManager.ToggleChunkBoundaries(_showChunkBoundaries);
                Console.WriteLine($"Отображение границ чанков: {(_showChunkBoundaries ? "включено" : "выключено")}");
            }
        }

        private Enemy FindNearestEnemy()
        {
            if (_enemies.Count == 0)
                return null;
            Enemy nearest = null;
            double minDistance = double.MaxValue;
            foreach (var enemy in _enemies)
            {
                double dx = enemy.X - _player.X;
                double dy = enemy.Y - _player.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
            return nearest;
        }

        private void CreateExplosion(double x, double y, double damage, double radius)
        {
            Explosion explosion = new Explosion(x, y, radius, EXPLOSION_EXPANSION_SPEED, damage);
            _explosions.Add(explosion);
            _worldContainer.Children.Add(explosion.ExplosionShape);
        }

        private void UpdateExplosions(double deltaTime)
        {
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                bool isActive = _explosions[i].Update(deltaTime);
                if (!isActive)
                {
                    _worldContainer.Children.Remove(_explosions[i].ExplosionShape);
                    _explosions.RemoveAt(i);
                }
            }
        }

        private void SpawnEnemy()
        {
            double spawnX = 0, spawnY = 0;
            bool foundValidSpawn = false;
            
            int maxAttempts = 30;
            int attempts = 0;
            
            double cameraLeft = _camera.X;
            double cameraTop = _camera.Y;
            double cameraRight = _camera.X + _camera.ViewportWidth;
            double cameraBottom = _camera.Y + _camera.ViewportHeight;
            
            double buffer = 100;
            
            double enemyRadius = 20;
            
            RectCollider tempCollider = null;
            
            List<Chunk> activeChunks = _chunkManager.GetActiveChunks();
            
            if (activeChunks.Count == 0)
            {
                Console.WriteLine("Не удалось создать врага: нет активных чанков");
                return;
            }
            
            Chunk? spawnChunk = null;
            
            while (!foundValidSpawn && attempts < maxAttempts)
            {
                attempts++;
                
                spawnChunk = activeChunks[_random.Next(activeChunks.Count)];
                
                var (playerChunkX, playerChunkY) = Chunk.WorldToChunk(_player.X, _player.Y);
                if (spawnChunk.ChunkX == playerChunkX && spawnChunk.ChunkY == playerChunkY)
                {
                    continue;
                }
                
                double chunkLeft = spawnChunk.WorldX;
                double chunkTop = spawnChunk.WorldY;
                double spawnAreaWidth = spawnChunk.PixelSize;
                double spawnAreaHeight = spawnChunk.PixelSize;
                
                double margin = 20;
                
                spawnX = chunkLeft + margin + _random.NextDouble() * (spawnAreaWidth - 2 * margin);
                spawnY = chunkTop + margin + _random.NextDouble() * (spawnAreaHeight - 2 * margin);
                
                if (tempCollider == null)
                {
                    double colliderSize = enemyRadius * 2 * 0.8;
                    tempCollider = new RectCollider(spawnX - colliderSize/2, spawnY - colliderSize/2, colliderSize, colliderSize);
                }
                else
                {
                    tempCollider.UpdatePosition(spawnX - tempCollider.Width/2, spawnY - tempCollider.Height/2);
                }
                
                if (_levelGenerator != null)
                {
                    bool centerWalkable = _levelGenerator.IsTileWalkable(spawnX, spawnY);
                    
                    bool allPointsWalkable = true;
                    double checkRadius = enemyRadius * 0.8;
                    
                    for (int i = 0; i < 8 && allPointsWalkable; i++)
                    {
                        double angle = i * Math.PI / 4;
                        double checkX = spawnX + Math.Cos(angle) * checkRadius;
                        double checkY = spawnY + Math.Sin(angle) * checkRadius;
                        
                        if (!_levelGenerator.IsTileWalkable(checkX, checkY))
                        {
                            allPointsWalkable = false;
                        }
                    }
                    
                    bool areaWalkable = _levelGenerator.IsAreaWalkable(tempCollider);
                    
                    if (centerWalkable && allPointsWalkable && areaWalkable)
                    {
                        foundValidSpawn = true;
                        Console.WriteLine($"Найдена валидная позиция для спавна в чанке {spawnChunk.ChunkX}:{spawnChunk.ChunkY} на попытке {attempts}: ({spawnX:F1}, {spawnY:F1})");
                    }
                }
            }
            
            if (!foundValidSpawn)
            {
                Console.WriteLine("Не удалось найти проходимую позицию для спавна, ищем безопасное место вне чанка игрока");
                
                var (playerChunkX, playerChunkY) = Chunk.WorldToChunk(_player.X, _player.Y);
                
                for (int y = playerChunkY - ChunkManager.ACTIVATION_DISTANCE; y <= playerChunkY + ChunkManager.ACTIVATION_DISTANCE; y++)
                {
                    for (int x = playerChunkX - ChunkManager.ACTIVATION_DISTANCE; x <= playerChunkX + ChunkManager.ACTIVATION_DISTANCE; x++)
                    {
                        if (x == playerChunkX && y == playerChunkY)
                            continue;
                            
                        var (worldX, worldY) = Chunk.ChunkToWorld(x, y);
                        worldX += Chunk.CHUNK_SIZE * TileSettings.TILE_SIZE / 2;
                        worldY += Chunk.CHUNK_SIZE * TileSettings.TILE_SIZE / 2;
                        
                        tempCollider.UpdatePosition(worldX - tempCollider.Width/2, worldY - tempCollider.Height/2);
                        
                        if (_levelGenerator.IsTileWalkable(worldX, worldY) && _levelGenerator.IsAreaWalkable(tempCollider))
                        {
                            spawnX = worldX;
                            spawnY = worldY;
                            foundValidSpawn = true;
                            spawnChunk = _chunkManager.GetOrCreateChunk(x, y);
                            Console.WriteLine($"Найдено безопасное место в чанке {x}:{y}: ({spawnX:F1}, {spawnY:F1})");
                            break;
                        }
                    }
                    
                    if (foundValidSpawn) break;
                }
                
                if (!foundValidSpawn)
                {
                    if (_random.NextDouble() < 0.5)
                    {
                        spawnX = _random.NextDouble() < 0.5 ? 
                            Math.Max(enemyRadius, cameraLeft - buffer) : 
                            Math.Min(_worldWidth - enemyRadius, cameraRight + buffer);
                        
                        spawnY = _random.NextDouble() * (_camera.ViewportHeight + buffer * 2) + 
                            Math.Max(enemyRadius, cameraTop - buffer);
                        spawnY = Math.Min(spawnY, _worldHeight - enemyRadius);
                    }
                    else
                    {
                        spawnX = _random.NextDouble() * (_camera.ViewportWidth + buffer * 2) + 
                            Math.Max(enemyRadius, cameraLeft - buffer);
                        spawnX = Math.Min(spawnX, _worldWidth - enemyRadius);
                        
                        spawnY = _random.NextDouble() < 0.5 ? 
                            Math.Max(enemyRadius, cameraTop - buffer) : 
                            Math.Min(_worldHeight - enemyRadius, cameraBottom + buffer);
                    }
                    
                    Console.WriteLine($"Крайний случай: спавн за пределами экрана: ({spawnX:F1}, {spawnY:F1})");
                    
                    var (spawnChunkX, spawnChunkY) = Chunk.WorldToChunk(spawnX, spawnY);
                    spawnChunk = _chunkManager.GetOrCreateChunk(spawnChunkX, spawnChunkY);
                }
            }
            
            if (spawnChunk != null)
            {
                spawnChunk.IsActive = true;
            }
            
            EnemyType enemyType = EnemyFactory.GetRandomEnemyTypeForScore(_score, _random);
            Enemy enemy = EnemyFactory.CreateEnemy(enemyType, spawnX, spawnY, _score, _spriteManager);
            
            _worldContainer.Children.Add(enemy.EnemyShape);
            _worldContainer.Children.Add(enemy.HealthBar);
            
            _enemies.Add(enemy);
        }

        private void UpdateSpawnRate()
        {
            double rateDecrease = Math.Min(
                (_score / SCORE_PER_SPAWN_RATE_DECREASE) * SPAWN_RATE_DECREASE_STEP,
                INITIAL_SPAWN_RATE - MIN_SPAWN_RATE
            );
            _enemySpawnRate = Math.Max(INITIAL_SPAWN_RATE - rateDecrease, MIN_SPAWN_RATE);
        }

        private int CalculateEnemiesToSpawn()
        {
            int baseEnemies = 1;
            int additionalEnemies = _score / SCORE_PER_MULTI_SPAWN;
            return Math.Min(baseEnemies + additionalEnemies, 5);
        }

        private void UpdateBullets(double deltaTime)
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                bool isActive = _bullets[i].Move(deltaTime);
                if (!isActive)
                {
                    _worldContainer.Children.Remove(_bullets[i].BulletShape);
                    _bullets.RemoveAt(i);
                }
            }
        }

        private void UpdateEnemies(double deltaTime)
        {
            foreach (var enemy in _enemies)
            {
                enemy.MoveTowardsPlayer(_player.X, _player.Y, deltaTime);
            }
        }

        private void CheckCollisions()
        {
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                bool bulletHitTile = false;
                if (_levelGenerator != null)
                {
                    var nearbyTileColliders = _levelGenerator.GetNearbyTileColliders(_bullets[i].X, _bullets[i].Y);
                    
                    foreach (var tileCollider in nearbyTileColliders)
                    {
                        TileType tileType = _levelGenerator.GetTileTypeAt(tileCollider.Key);
                        if (_bullets[i].CollidesWithTile(tileCollider.Value, tileType))
                        {
                            BulletImpactEffect effect = new BulletImpactEffect(
                                _bullets[i].X, 
                                _bullets[i].Y, 
                                Math.Atan2(_bullets[i].Y - _bullets[i].PrevY, _bullets[i].X - _bullets[i].PrevX),
                                tileType,
                                _worldContainer
                            );
                            _bulletImpactEffects.Add(effect);
                            
                            _worldContainer.Children.Remove(_bullets[i].BulletShape);
                            _bullets.RemoveAt(i);
                            bulletHitTile = true;
                            break;
                        }
                    }
                }
                
                if (bulletHitTile)
                    continue;
                
                bool bulletHit = false;
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    if (_bullets[i].Collides(_enemies[j]))
                    {
                        Weapon currentWeapon = _player.GetCurrentWeapon();
                        double damage = _bullets[i].Damage;
                        bool isEnemyAlive = _enemies[j].TakeDamage(damage);
                        if (currentWeapon.IsExplosive)
                        {
                            CreateExplosion(
                                _enemies[j].X, 
                                _enemies[j].Y, 
                                damage * currentWeapon.ExplosionDamageMultiplier, 
                                currentWeapon.ExplosionRadius
                            );
                        }
                        _worldContainer.Children.Remove(_bullets[i].BulletShape);
                        _bullets.RemoveAt(i);
                        bulletHit = true;
                        if (!isEnemyAlive)
                        {
                            int scoreToAdd = _enemies[j].ScoreValue;
                            UpdateScore(_score + scoreToAdd);
                            
                            _enemyKillCounter++;
                            
                            if (_enemyKillCounter % HEALTH_KIT_DROP_FREQUENCY == 0)
                            {
                                SpawnHealthKit(_enemies[j].X, _enemies[j].Y);
                            }
                            
                            if (_enemyKillCounter % ARMOR_KIT_DROP_FREQUENCY == 0)
                            {
                                SpawnArmorKit(_enemies[j].X, _enemies[j].Y);
                            }
                            
                            // Проверяем, нужно ли создать выпадение оружия с этого врага
                            TryDropWeaponFromEnemy(_enemies[j].X, _enemies[j].Y);
                            
                            _worldContainer.Children.Remove(_enemies[j].EnemyShape);
                            _worldContainer.Children.Remove(_enemies[j].HealthBar);
                            _enemies.RemoveAt(j);
                            EnemyKilled?.Invoke(this, EventArgs.Empty);
                        }
                        break;
                    }
                }
                if (bulletHit)
                    continue;
            }
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                for (int j = _enemies.Count - 1; j >= 0; j--)
                {
                    if (_explosions[i].AffectsEnemy(_enemies[j]))
                    {
                        bool isEnemyAlive = _enemies[j].TakeDamage(_explosions[i].Damage);
                        if (!isEnemyAlive)
                        {
                            int scoreToAdd = _enemies[j].ScoreValue;
                            UpdateScore(_score + scoreToAdd);
                            _enemyKillCounter++;
                            if (_enemyKillCounter % HEALTH_KIT_DROP_FREQUENCY == 0)
                            {
                                SpawnHealthKit(_enemies[j].X, _enemies[j].Y);
                            }
                            if (_enemyKillCounter % ARMOR_KIT_DROP_FREQUENCY == 0)
                            {
                                SpawnArmorKit(_enemies[j].X, _enemies[j].Y);
                            }
                            TryDropWeaponFromEnemy(_enemies[j].X, _enemies[j].Y);
                            _worldContainer.Children.Remove(_enemies[j].EnemyShape);
                            _worldContainer.Children.Remove(_enemies[j].HealthBar);
                            _enemies.RemoveAt(j);
                            EnemyKilled?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
                if (_explosions[i].Damage > 0 && _explosions[i].AffectsPlayer(_player))
                {
                    _player.TakeDamage(_explosions[i].Damage);
                }
            }
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                if (_enemies[i].CollidesWithPlayer(_player))
                {
                    _player.TakeDamage(_enemies[i].DamageOnCollision);
                    if (_enemies[i].Type == EnemyType.Bomber)
                    {
                        CreateExplosion(
                            _enemies[i].X,
                            _enemies[i].Y,
                            _enemies[i].DamageOnCollision * 2,
                            100
                        );
                    }
                    
                    // Проверяем, нужно ли создать выпадение оружия с этого врага
                    TryDropWeaponFromEnemy(_enemies[i].X, _enemies[i].Y);
                    
                    _worldContainer.Children.Remove(_enemies[i].EnemyShape);
                    _worldContainer.Children.Remove(_enemies[i].HealthBar);
                    _enemies.RemoveAt(i);
                    EnemyKilled?.Invoke(this, EventArgs.Empty);
                }
            }
            
            for (int i = _healthKits.Count - 1; i >= 0; i--)
            {
                if (_healthKits[i].CollidesWithPlayer(_player))
                {
                    _player.Heal(_healthKits[i].HealAmount);
                    
                    HealthKitCollected?.Invoke(this, _healthKits[i].HealAmount);
                    
                    _worldContainer.Children.Remove(_healthKits[i].VisualElement);
                    _healthKits.RemoveAt(i);
                }
            }
            
            // Проверка коллизий с выпадающим оружием
            for (int i = _weaponDrops.Count - 1; i >= 0; i--)
            {
                if (_weaponDrops[i].CollidesWithPlayer(_player))
                {
                    Weapon newWeapon = WeaponFactory.CreateWeapon(_weaponDrops[i].WeaponType);
                    _player.ChangeWeapon(newWeapon, _worldContainer);
                    
                    WeaponPickedUp?.Invoke(this, _weaponDrops[i].WeaponType);
                    WeaponChanged?.Invoke(this, newWeapon.Name);
                    
                    _worldContainer.Children.Remove(_weaponDrops[i].VisualElement);
                    _weaponDrops.RemoveAt(i);
                }
            }
            
            // Проверка коллизий с бронежилетами
            for (int i = _armorKits.Count - 1; i >= 0; i--)
            {
                if (_armorKits[i].CollidesWithPlayer(_player))
                {
                    _player.AddArmor(_armorKits[i].ArmorAmount);
                    
                    ArmorKitCollected?.Invoke(this, _armorKits[i].ArmorAmount);
                    
                    _worldContainer.Children.Remove(_armorKits[i].VisualElement);
                    _armorKits.RemoveAt(i);
                }
            }

            // Проверка мин
            for (int i = _mines.Count - 1; i >= 0; i--)
            {
                if (_mines[i].IsActive && _mines[i].CheckPlayerProximity(_player))
                {
                    _player.TakeDamage(_mines[i].Damage);
                    // Визуальный эффект взрыва
                    Explosion explosion = new Explosion(_mines[i].X, _mines[i].Y, 60, 200, 0); // 60 радиус, 200 скорость, 0 урона
                    _explosions.Add(explosion);
                    _worldContainer.Children.Add(explosion.ExplosionShape);
                    _mines[i].Explode();
                    _worldContainer.Children.Remove(_mines[i].VisualElement);
                    _mines.RemoveAt(i);
                }
            }

            // Проверка попадания пуль по бочкам
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                bool hitBarrel = false;
                for (int j = _barrels.Count - 1; j >= 0; j--)
                {
                    if (_barrels[j].Collider.Intersects(_bullets[i].Collider))
                    {
                        _barrels[j].TakeDamage(_bullets[i].Damage);
                        _worldContainer.Children.Remove(_bullets[i].BulletShape);
                        _bullets.RemoveAt(i);
                        hitBarrel = true;
                        break;
                    }
                }
                if (hitBarrel) break;
            }
        }

        public void ResizeGameArea(double width, double height)
        {
            _gameWidth = width;
            _gameHeight = height;
            
            _camera.UpdateViewport(width, height);
            
            if (_levelGenerator != null && (_worldWidth != width * WORLD_SIZE_MULTIPLIER || 
                                           _worldHeight != height * WORLD_SIZE_MULTIPLIER))
            {
                _worldWidth = width * WORLD_SIZE_MULTIPLIER;
                _worldHeight = height * WORLD_SIZE_MULTIPLIER;
                
                _worldContainer.Width = _worldWidth;
                _worldContainer.Height = _worldHeight;
                
                _camera.UpdateWorldSize(_worldWidth, _worldHeight);
                
                _player.ConstrainToWorldBounds(0, 0, _worldWidth, _worldHeight);
                
                _levelGenerator.ResizeLevel(_worldWidth, _worldHeight);
            }
        }

        public string GetAmmoInfo()
        {
            return _player.GetAmmoInfo();
        }

        public bool IsTileWalkable(double x, double y)
        {
            if (_levelGenerator == null)
            {
                return true;
            }
            
            return _levelGenerator.IsTileWalkable(x, y);
        }

        public Dictionary<string, RectCollider> GetNearbyTileColliders(double x, double y)
        {
            return _chunkManager.GetActiveChunkColliders();
        }
        
        public bool IsAreaWalkable(RectCollider playerCollider)
        {
            return _levelGenerator.IsAreaWalkable(playerCollider);
        }

        private void ProcessLaserCollisions(LaserBeam laser)
        {
            Dictionary<Enemy, double> hitEnemies = new Dictionary<Enemy, double>();
            foreach (var enemy in _enemies)
            {
                double distance;
                if (laser.IntersectsWithEnemy(enemy, out distance))
                {
                    hitEnemies.Add(enemy, distance);
                }
            }
            var sortedEnemies = new List<KeyValuePair<Enemy, double>>(hitEnemies);
            sortedEnemies.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            foreach (var pair in sortedEnemies)
            {
                Enemy enemy = pair.Key;
                bool isEnemyAlive = enemy.TakeDamage(laser.Damage);
                if (!isEnemyAlive)
                {
                    int scoreToAdd = enemy.ScoreValue;
                    UpdateScore(_score + scoreToAdd);
                    
                    _enemyKillCounter++;
                    
                    if (_enemyKillCounter % HEALTH_KIT_DROP_FREQUENCY == 0)
                    {
                        SpawnHealthKit(enemy.X, enemy.Y);
                    }
                    
                    if (_enemyKillCounter % ARMOR_KIT_DROP_FREQUENCY == 0)
                    {
                        SpawnArmorKit(enemy.X, enemy.Y);
                    }
                    
                    _worldContainer.Children.Remove(enemy.EnemyShape);
                    _worldContainer.Children.Remove(enemy.HealthBar);
                    _enemies.Remove(enemy);
                    EnemyKilled?.Invoke(this, EventArgs.Empty);
                }
            }
            if (sortedEnemies.Count > 0)
            {
                double maxDistance = laser.MaxLength;
                double dx = Math.Cos(laser.Angle);
                double dy = Math.Sin(laser.Angle);
                double newEndX = laser.StartX + dx * maxDistance;
                double newEndY = laser.StartY + dy * maxDistance;
                laser.SetEndPoint(newEndX, newEndY);
            }
        }

        private void UpdateLasers(double deltaTime)
        {
            for (int i = _lasers.Count - 1; i >= 0; i--)
            {
                bool isActive = _lasers[i].Update(deltaTime);
                if (!isActive)
                {
                    _worldContainer.Children.Remove(_lasers[i].LaserLine);
                    _worldContainer.Children.Remove(_lasers[i].LaserDot);
                    _lasers.RemoveAt(i);
                }
            }
        }

        private void UpdateBulletImpacts(double deltaTime)
        {
            for (int i = _bulletImpactEffects.Count - 1; i >= 0; i--)
            {
                bool isActive = _bulletImpactEffects[i].Update(deltaTime);
                if (!isActive)
                {
                    _bulletImpactEffects.RemoveAt(i);
                }
            }
        }

        public ChunkManager GetChunkManager()
        {
            return _chunkManager;
        }

        public void ToggleChunkBoundaries()
        {
            _showChunkBoundaries = !_showChunkBoundaries;
            _chunkManager.ToggleChunkBoundaries(_showChunkBoundaries);
        }

        private void RemoveEnemiesInStaleChunks()
        {
            if (_enemies.Count == 0) return;
            
            TimeSpan minimumEnemyLifetime = TimeSpan.FromSeconds(3.0);
            
            TimeSpan staleTime = TimeSpan.FromSeconds(ENEMY_DESPAWN_TIME);
            
            List<Enemy> enemiesToRemove = new List<Enemy>();
            DateTime now = DateTime.Now;
            
            foreach (var enemy in _enemies)
            {
                if (enemy.CreationTime.Add(minimumEnemyLifetime) < now && 
                    _chunkManager.IsInInactiveStaleChunk(enemy.X, enemy.Y, staleTime))
                {
                    enemiesToRemove.Add(enemy);
                }
            }
            
            if (enemiesToRemove.Count > 0)
            {
                foreach (var enemy in enemiesToRemove)
                {
                    string spriteName = GetEnemySpriteName(enemy.Type);
                    EnemyState enemyState = EnemyState.CreateFromEnemy(enemy, spriteName);
                    
                    _chunkManager.CacheEnemyState(enemyState);
                    
                    _worldContainer.Children.Remove(enemy.EnemyShape);
                    _worldContainer.Children.Remove(enemy.HealthBar);
                    _enemies.Remove(enemy);
                    EnemyKilled?.Invoke(this, EventArgs.Empty);
                }
                
                Console.WriteLine($"Кэшировано {enemiesToRemove.Count} врагов из устаревших неактивных чанков.");
            }
        }
        
        private string GetEnemySpriteName(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Basic:
                    return "enemy1";
                case EnemyType.Runner:
                    return "enemy2";
                case EnemyType.Tank:
                    return "enemy1";
                case EnemyType.Bomber:
                    return "enemy1";
                case EnemyType.Boss:
                    return "enemy1";
                default:
                    return "enemy1";
            }
        }
        
        private void RestoreEnemiesFromState(List<EnemyState> enemyStates)
        {
            foreach (var state in enemyStates)
            {
                Enemy enemy = EnemyFactory.CreateEnemy(
                    type: state.Type,
                    x: state.X,
                    y: state.Y,
                    scoreLevel: _score,
                    spriteManager: _spriteManager
                );
                
                if (enemy.TakeDamage(enemy.MaxHealth - state.Health))
                {
                    _enemies.Add(enemy);
                    
                    _worldContainer.Children.Add(enemy.EnemyShape);
                    _worldContainer.Children.Add(enemy.HealthBar);
                    
                    enemy.UpdatePosition();
                    
                    Console.WriteLine($"Восстановлен враг типа {state.Type} на позиции {state.X}, {state.Y}");
                }
            }
        }

        private void StartEnemyProcessingTask()
        {
            if (_enemyProcessingTask != null && !_enemyProcessingTask.IsCompleted)
            {
                return;
            }

            _enemyProcessingCancellation = new CancellationTokenSource();
            
            _enemyProcessingTask = Task.Run(() => 
            {
                Console.WriteLine("Запущена многопоточная обработка врагов");
                
                try
                {
                    while (!_enemyProcessingCancellation.Token.IsCancellationRequested)
                    {
                        ProcessEnemiesInThreads();
                        
                        Thread.Sleep(10);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Многопоточная обработка врагов отменена");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в обработке врагов: {ex.Message}");
                }
            }, _enemyProcessingCancellation.Token);
        }

        private void ProcessEnemiesInThreads()
        {
            if (Interlocked.CompareExchange(ref _processingThreadCount, 0, 0) > 0)
                return;
        
            List<Enemy> enemiesCopy;
        
            lock (_enemiesLock)
            {
                if (_enemies.Count == 0)
                    return;
            
                enemiesCopy = new List<Enemy>(_enemies);
            }
        
            int optimalThreadCount = Math.Max(1, Math.Min(
                Environment.ProcessorCount - 1, 
                (int)Math.Ceiling((double)enemiesCopy.Count / _maxEnemiesPerThread)
            ));
        
            List<List<Enemy>> enemyGroups = new List<List<Enemy>>();
            int groupSize = (int)Math.Ceiling((double)enemiesCopy.Count / optimalThreadCount);
        
            for (int i = 0; i < enemiesCopy.Count; i += groupSize)
            {
                int count = Math.Min(groupSize, enemiesCopy.Count - i);
                enemyGroups.Add(enemiesCopy.GetRange(i, count));
            }
        
            Interlocked.Exchange(ref _processingThreadCount, enemyGroups.Count);
        
            foreach (var group in enemyGroups)
            {
                Task.Run(() => 
                {
                    try
                    {
                        double playerX = _player.X;
                        double playerY = _player.Y;
                    
                        foreach (var enemy in group)
                        {
                            if (_camera.IsInViewExtended(enemy.X, enemy.Y, 200))
                            {
                                enemy.MoveTowardsPlayer(playerX, playerY, 1.0 / 60.0);
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _processingThreadCount);
                    }
                });
            }
        }

        private void UpdateHealthKits(double deltaTime)
        {
            for (int i = _healthKits.Count - 1; i >= 0; i--)
            {
                bool isActive = _healthKits[i].Update(deltaTime);
                if (!isActive)
                {
                    _worldContainer.Children.Remove(_healthKits[i].VisualElement);
                    _healthKits.RemoveAt(i);
                }
            }
        }

        private void SpawnHealthKit(double x, double y)
        {
            HealthKit healthKit = new HealthKit(x, y, 20, _spriteManager);
            _healthKits.Add(healthKit);
            _worldContainer.Children.Add(healthKit.VisualElement);
            Console.WriteLine($"Создана аптечка на позиции ({x}, {y})");
        }

        private void SpawnArmorKit(double x, double y)
        {
            ArmorKit armorKit = new ArmorKit(x, y, 20, _spriteManager);
            _armorKits.Add(armorKit);
            _worldContainer.Children.Add(armorKit.VisualElement);
            Console.WriteLine($"Создан бронежилет на позиции ({x}, {y})");
        }

        public void Dispose()
        {
            if (_enemyProcessingCancellation != null)
            {
                _enemyProcessingCancellation.Cancel();
                
                try
                {
                    if (_enemyProcessingTask != null)
                        _enemyProcessingTask.Wait(1000);
                }
                catch { }
                
                _enemyProcessingCancellation.Dispose();
                _enemyProcessingCancellation = null;
            }
            
            if (_chunkManager != null)
            {
                _chunkManager.EnemiesReadyToRestore -= OnEnemiesReadyToRestore;
                _chunkManager.Dispose();
            }
            
            Console.WriteLine("GameManager ресурсы освобождены");
        }

        private void OnEnemiesReadyToRestore(object sender, ChunkEnemyRestoreEventArgs e)
        {
            RestoreEnemiesFromState(e.EnemiesToRestore);
        }

        // Проверяет, нужно ли создать выпадение оружия
        private void CheckWeaponDrops()
        {
            // Оружие больше не выпадает автоматически при достижении порога очков
            // а выпадает только с убитых врагов
        }

        // Проверяет, нужно ли создать выпадение оружия с врага
        private void TryDropWeaponFromEnemy(double x, double y)
        {
            // Определяем, какое оружие может выпасть при текущем счете
            WeaponType weaponToDrop = WeaponType.Pistol;
            bool shouldDrop = false;
            
            foreach (var weaponPair in WeaponScoreThresholds)
            {
                // Если достигнут порог очков и оружие еще не выпадало
                if (_score >= weaponPair.Value && !_droppedWeapons.Contains(weaponPair.Key))
                {
                    weaponToDrop = weaponPair.Key;
                    shouldDrop = true;
                    break;
                }
            }
            
            if (shouldDrop)
            {
                // Создаем выпадение оружия на месте убитого врага
                SpawnWeaponDrop(x, y, weaponToDrop);
                
                // Отмечаем, что это оружие уже выпало
                _droppedWeapons.Add(weaponToDrop);
                
                Console.WriteLine($"Выпало оружие {WeaponFactory.GetWeaponName(weaponToDrop)} при достижении {WeaponScoreThresholds[weaponToDrop]} очков");
            }
        }

        private void UpdateWeaponDrops(double deltaTime)
        {
            for (int i = _weaponDrops.Count - 1; i >= 0; i--)
            {
                bool isActive = _weaponDrops[i].Update(deltaTime);
                if (!isActive)
                {
                    _worldContainer.Children.Remove(_weaponDrops[i].VisualElement);
                    _weaponDrops.RemoveAt(i);
                }
            }
        }

        private void SpawnWeaponDrop(double x, double y, WeaponType weaponType)
        {
            WeaponDrop weaponDrop = new WeaponDrop(x, y, weaponType, _spriteManager);
            _weaponDrops.Add(weaponDrop);
            _worldContainer.Children.Add(weaponDrop.VisualElement);
            Console.WriteLine($"Создано выпадающее оружие '{WeaponFactory.GetWeaponName(weaponType)}' на позиции ({x}, {y})");
        }

        // Сбросить отслеживание выпавшего оружия (для перезапуска игры)
        public void ResetDroppedWeapons()
        {
            _droppedWeapons.Clear();
        }

        private void UpdateArmorKits(double deltaTime)
        {
            for (int i = _armorKits.Count - 1; i >= 0; i--)
            {
                bool isActive = _armorKits[i].Update(deltaTime);
                if (!isActive)
                {
                    _worldContainer.Children.Remove(_armorKits[i].VisualElement);
                    _armorKits.RemoveAt(i);
                }
            }
        }

        // Обработка изменения счета
        private void UpdateScore(int newScore)
        {
            int oldScore = _score;
            _score = newScore;
            
            // Вызываем событие изменения счета
            ScoreChanged?.Invoke(this, _score);
            
            // Проверяем, нужно ли показать окно выбора навыка
            CheckSkillSelection(oldScore, newScore);
        }

        // Проверяет, нужно ли показать окно выбора навыка
        private void CheckSkillSelection(int oldScore, int newScore)
        {
            // Проверяем, пересекли ли мы порог очков для выбора навыка
            int oldThreshold = oldScore / SCORE_PER_SKILL_SELECTION;
            int newThreshold = newScore / SCORE_PER_SKILL_SELECTION;
            
            if (newThreshold > oldThreshold && newScore > _lastSkillSelectionScore)
            {
                // Запоминаем текущий порог очков
                _lastSkillSelectionScore = newThreshold * SCORE_PER_SKILL_SELECTION;
                
                // Вызываем событие доступности выбора навыка
                SkillSelectionAvailable?.Invoke(this, _score);
                
                Console.WriteLine($"Доступен выбор навыка при {_score} очках");
            }
        }

        private void UpdateMines(double deltaTime)
        {
            for (int i = _mines.Count - 1; i >= 0; i--)
            {
                if (!_mines[i].IsActive)
                {
                    _worldContainer.Children.Remove(_mines[i].VisualElement);
                    _mines.RemoveAt(i);
                    continue;
                }
                _mines[i].UpdatePosition();
            }
        }

        private void SpawnMineRandom()
        {
            // Пытаемся найти свободное место для мины
            for (int attempt = 0; attempt < 10; attempt++)
            {
                double x = _random.NextDouble() * _worldWidth;
                double y = _random.NextDouble() * _worldHeight;
                if (_levelGenerator.IsTileWalkable(x, y))
                {
                    Mine mine = new Mine(x, y, _spriteManager);
                    _mines.Add(mine);
                    _worldContainer.Children.Add(mine.VisualElement);
                    break;
                }
            }
        }

        private void SpawnBarrels()
        {
            for (int i = 0; i < BARREL_COUNT; i++)
            {
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    double x = _random.NextDouble() * _worldWidth;
                    double y = _random.NextDouble() * _worldHeight;
                    if (_levelGenerator.IsTileWalkable(x, y))
                    {
                        Barrel barrel = new Barrel(x, y, _spriteManager);
                        _barrels.Add(barrel);
                        _worldContainer.Children.Add(barrel.VisualElement);
                        break;
                    }
                }
            }
        }

        private void UpdateBarrels(double deltaTime)
        {
            for (int i = _barrels.Count - 1; i >= 0; i--)
            {
                if (_barrels[i].IsDestroyed)
                {
                    // Взрыв бочки
                    Explosion explosion = new Explosion(_barrels[i].X, _barrels[i].Y, 90, 200, 30); // 30 урона по области
                    _explosions.Add(explosion);
                    _worldContainer.Children.Add(explosion.ExplosionShape);
                    _worldContainer.Children.Remove(_barrels[i].VisualElement);
                    _barrels.RemoveAt(i);
                }
                else
                {
                    _barrels[i].UpdatePosition();
                }
            }
        }
    }
} 