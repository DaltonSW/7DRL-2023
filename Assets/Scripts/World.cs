using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Cowball
{
    public partial class World : Node2D
    {
        private enum State
        {
            LoadingLevel,
            Playing,
            GameOver,
        }
        private State _state;
        private AudioStreamPlayer _audioPlayer;
        private AudioStream _pauseSound;
        private AudioStream _unpauseSound;
        private AudioStream _bossDeath;

        private Player _player;
        private SlimeBoss _boss;

        private Sprite2D _youWin;
        private Sprite2D _youLose;

        private bool _needToInstantiateLevelScene;
        private PackedScene _levelScene;
        private Level _level;

        private PackedScene _itemScene;
        private PackedScene _exitScene;

        private Random _rng;

        private Queue<string> _randomLevelFilenames;
        private Queue<(string, PackedScene)> _nextLevels;

        private int _enemiesLeftInLevel;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _rng = new Random();

            List<string> levelFilenames = LoadLevelFilenames();
            _randomLevelFilenames = CopyToShuffledQueue(levelFilenames);

            // TODO: make test scenes independent of World script
            //       so we don't have to branch here.
            Level levelAlreadyInTree = GetNodeOrNull<Level>("Level");
            if (levelAlreadyInTree is not null)
            {
                _level = levelAlreadyInTree;
                _needToInstantiateLevelScene = false;
            }
            else
            {
                _levelScene = LoadLevelScene(_randomLevelFilenames.Dequeue());
                _needToInstantiateLevelScene = true;
            }
            _player = GetNode<Player>("Player");
            _player.Connect("PlayerKilled", new Callable(this, nameof(OnPlayerDeath)));

            _youWin = GetNode<Sprite2D>("YouWin");
            _youLose = GetNode<Sprite2D>("YouLose");

            _audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
            _audioPlayer.Autoplay = false;
            _audioPlayer.VolumeDb = -5f;

            _pauseSound = GD.Load<AudioStream>("res://Assets/Sounds/Pause.wav");
            _unpauseSound = GD.Load<AudioStream>("res://Assets/Sounds/Unpause.wav");
            _bossDeath = GD.Load<AudioStream>("res://Assets/Sounds/BossDefeated.wav");

            _itemScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Item.tscn");
            _exitScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Exit.tscn");

            _state = State.LoadingLevel;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            var pausing = Input.IsActionJustPressed("Pause");
            if (pausing)
            {
                if (_state == State.GameOver)
                {
                    // Load main menu
                }
                if (GetTree().Paused)
                {
                    _audioPlayer.Stream = _unpauseSound;
                    _audioPlayer.Play();
                }
                else
                {
                    _audioPlayer.Stream = _pauseSound;
                    _audioPlayer.Play();
                }

                GetTree().Paused = !GetTree().Paused;

            }

            switch (_state)
            {
                case State.LoadingLevel:
                    {
                        if (_needToInstantiateLevelScene)
                        {
                            _level = _levelScene.Instantiate<Level>();
                            AddChild(_level);
                            MoveChild(_level, 0);
                            _needToInstantiateLevelScene = false;
                        }
                        SetUpLevel(_level);
                        _state = State.Playing;
                        break;
                    }
                case State.Playing:
                    {
                        // Do nothing
                        break;
                    }
            }
        }

        private Exit CreateExit(string nextLevelFilename, PackedScene nextLevelScene)
        {
            Exit exit = _exitScene.Instantiate<Exit>();
            exit.Initialize(this, nextLevelFilename, nextLevelScene);
            return exit;
        }

        private void SetUpLevel(Level level)
        {
            _player.SetCameraLimits(level.CameraBounds());
            _player.Position = level.PlayerSpawnPosition();
            _player.SetSpawn(level.PlayerSpawnPosition());

            Queue<ItemParams> itemPool = CopyToShuffledQueue(ITEM_POOL);
            SpawnNodesInLevel(level, level.ItemSpawnPoints, () => CreateItem(itemPool.Dequeue()));

            _nextLevels = TakeAndLoadLevelsFromPool(level.ExitSpawnPoints.Count);

            var enemies = FindEnemies();
            foreach (var enemy in enemies)
            {
                enemy.Connect("Died", Callable.From(() => OnEnemyDied(enemy)));
            }
            var boss = GetTree().GetFirstNodeInGroup("boss");
            if (boss is not null)
            {
                boss.Connect("BossKilled", Callable.From(() => OnBossDeath(boss)));
            }

            SpawnExitsIfNoEnemiesRemain(null);
        }

        private Godot.Collections.Array<Node> FindEnemies()
        {
            return GetTree().GetNodesInGroup("root_enemy");
        }

        private void SpawnExitsIfNoEnemiesRemain(Node enemy)
        {
            var enemies = FindEnemies();
            if (enemies.Count == 0 || (enemies.Count == 1 && System.Object.ReferenceEquals(enemies[0], enemy)))
            {
                SpawnNodesInLevel(_level, _level.ExitSpawnPoints, () =>
                {
                    (string nextLevelFilename, PackedScene nextLevelScene) = _nextLevels.Dequeue();
                    return CreateExit(nextLevelFilename, nextLevelScene);
                });
            }
        }

        private void OnEnemyDied(Node enemy)
        {
            SpawnExitsIfNoEnemiesRemain(enemy);
        }

        private Queue<(string, PackedScene)> TakeAndLoadLevelsFromPool(int n)
        {
            var randomLevels = new Queue<(string, PackedScene)>();
            for (int i = 0; i < n; i++)
            {
                string levelFilename = _randomLevelFilenames.Dequeue();
                PackedScene levelScene = LoadLevelScene(levelFilename);
                randomLevels.Enqueue((levelFilename, levelScene));
            }
            return randomLevels;
        }

        private static PackedScene LoadLevelScene(string levelFilename)
        {
            return ResourceLoader.Load<PackedScene>($"res://Assets/Levels/{levelFilename}.tscn");
        }

        private void SpawnNodesInLevel(Level level, List<Vector2> spawnPoints, Func<Node2D> constructNode)
        {
            foreach (Vector2 spawnPoint in spawnPoints)
            {
                Node2D node = constructNode();
                level.AddChild(node);
                node.Position = spawnPoint;
            }
        }

        private void OnBossDeath(Node boss)
        {
            _audioPlayer.Stream = _bossDeath;
            _audioPlayer.Play();
            GetTree().Paused = true;
            _youWin.Visible = true;
            _state = State.GameOver;
        }

        private void OnPlayerDeath()
        {
            GetTree().Paused = true;
            _youLose.Visible = true;
            _state = State.GameOver;
        }

        private static ItemParams[] ITEM_POOL =
            {
                new ItemParams("Soylent", "Soylent", StatToChange.Health, 1),
                new ItemParams("Hot Dog", "Hot Dog", StatToChange.Health, 1),
                new ItemParams("Itchy Finger", "Poison Ivy", StatToChange.FireRate, 0.3),
                new ItemParams("Coffee", "Coffee", StatToChange.Speed, 25),
                new ItemParams("Bike Pump", "Bike Pump", StatToChange.JumpSpeed, 25),
                new ItemParams("Bigger Bullets", "Bigger Bullets", StatToChange.Damage, 1),
                new ItemParams("Campfire", "Campfire", StatToChange.Damage, 1),
                new ItemParams("Hard Hat", "Hard Hat", StatToChange.Health, 1),
                new ItemParams("Lead Underwear", "Lead Underwear", StatToChange.Health, 1),
            };

        private static List<string> LoadLevelFilenames()
        {
            // `using` closes the file when `file` goes out of scope.
            // Godot editor doesn't like ".csv" files, so this has to be a ".txt".
            using var file = FileAccess.Open("res://Assets/Levels/level_list.csv.txt", FileAccess.ModeFlags.Read);

            List<string> levelFilenames = new List<string>();
            while (file.GetPosition() < file.GetLength())
            {
                string[] line = file.GetCsvLine();
                string levelFilename = line[0];
                levelFilenames.Add(levelFilename);
            }
            return levelFilenames;
        }

        private Queue<T> CopyToShuffledQueue<T>(IEnumerable<T> list)
        {
            List<T> shuffledList = list.OrderBy(_ => _rng.Next()).ToList();
            return new Queue<T>(shuffledList);
        }

        private Item CreateItem(ItemParams itemParams)
        {
            Item item = _itemScene.Instantiate<Item>();
            item.Initialize(itemParams);
            return item;
        }

        public void LoadNextLevel(Exit exit)
        {
            // Return other levels to pool
            var otherLevels = GetTree().GetNodesInGroup("exits")
                .Select(node => (Exit)node)
                .Where(e => !System.Object.ReferenceEquals(e, exit))
                .Select(exit => exit.NextLevelFilename);
            foreach (var otherLevel in otherLevels)
            {
                _randomLevelFilenames.Enqueue(otherLevel);
            }

            _level.QueueFree();
            _state = State.LoadingLevel;
            _levelScene = exit.NextLevelScene;
            _needToInstantiateLevelScene = true;
        }
    }

}
