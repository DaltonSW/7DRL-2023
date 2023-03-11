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
            Paused,
        }
        private State _state;

        private Player _player;

        private bool _needToInstantiateLevelScene;
        private PackedScene _levelScene;
        private Level _level;

        private PackedScene _itemScene;
        private PackedScene _exitScene;

        private Random _rng;

        private Queue<string> _randomLevelFilenames;
        private Queue<(string, PackedScene)> _nextLevels;

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

            _itemScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Item.tscn");
            _exitScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Exit.tscn");

            _state = State.LoadingLevel;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            var pausing = Input.IsActionJustPressed("Pause");
            if (pausing) GetTree().Paused = !GetTree().Paused;
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

            Queue<ItemParams> itemPool = CopyToShuffledQueue(ITEM_POOL);
            SpawnNodesInLevel(level, level.ItemSpawnPoints, () => CreateItem(itemPool.Dequeue()));

            _nextLevels = TakeAndLoadLevelsFromPool(level.ExitSpawnPoints.Count);
            // TODO: only spawn after enemies are all dead
            SpawnNodesInLevel(level, level.ExitSpawnPoints, () =>
            {
                (string nextLevelFilename, PackedScene nextLevelScene) = _nextLevels.Dequeue();
                return CreateExit(nextLevelFilename, nextLevelScene);
            });
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

        // Items TODO:
        // Lead Underwear - Butt stomp does more damage
        // Campfire - Flaming bullets
        // Bigger bullets - Bigger bullets
        // Hardhat - Can't take damage on your head
        private static ItemParams[] ITEM_POOL =
            {
                new ItemParams("Soylent", "Soylent", StatToChange.Health, 1),
                new ItemParams("Hotdog", "Hot Dog", StatToChange.Health, 1),
                new ItemParams("Itchy Finger", "Poison Ivy", StatToChange.FireRate, 0.3),
                new ItemParams("Coffee", "Coffee", StatToChange.Speed, 25),
                new ItemParams("Bike Pump", "Bike Pump", StatToChange.JumpSpeed, 25),
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
            _level.QueueFree();
            _state = State.LoadingLevel;
            _levelScene = exit.NextLevelScene;
            _needToInstantiateLevelScene = true;

            // Return other levels to pool
            var otherLevels = GetTree().GetNodesInGroup("exits")
                .Select(node => (Exit) node)
                .Where(e => !System.Object.ReferenceEquals(e, exit))
                .Select(exit => exit.NextLevelFilename);
            foreach (var otherLevel in otherLevels)
            {
                _randomLevelFilenames.Enqueue(otherLevel);
            }
        }
    }

}
