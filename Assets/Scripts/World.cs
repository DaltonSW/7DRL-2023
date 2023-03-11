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
        }
        private State _state;
        private AudioStreamPlayer _audioPlayer;
        private AudioStream pauseSound;
        private AudioStream unpauseSound;

        private Player _player;
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

            _player = GetNode<Player>("Player");
            _audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
            _audioPlayer.Autoplay = false;

            pauseSound = GD.Load<AudioStream>("res://Assets/Sounds/Pause.wav");
            unpauseSound = GD.Load<AudioStream>("res://Assets/Sounds/Unpause.wav");

            // TODO: remove, load instead
            _level = GetNode<Level>("Level");

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

        public Exit CreateExit(string nextLevelFilename, PackedScene nextLevelScene)
        {
            Exit exit = _exitScene.Instantiate<Exit>();
            exit.Initialize(nextLevelFilename, nextLevelScene);
            return exit;
        }

        public void SetUpLevel(Level level)
        {
            _player.SetCameraLimits(level.CameraBounds());
            _player.Position = level.PlayerSpawnPosition();

            Queue<ItemParams> itemPool = CopyToShuffledQueue(ITEM_POOL);
            SpawnNodes(level.ItemSpawnPoints, () => CreateItem(itemPool.Dequeue()));

            _nextLevels = TakeAndLoadLevelsFromPool(level.ExitSpawnPoints.Count);
            // TODO: only spawn after enemies are all dead
            SpawnNodes(level.ExitSpawnPoints, () =>
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
                PackedScene levelScene = ResourceLoader.Load<PackedScene>($"res://Assets/Levels/{levelFilename}.tscn");
                randomLevels.Enqueue((levelFilename, levelScene));
            }
            return randomLevels;
        }

        private void SpawnNodes(List<Vector2> spawnPoints, Func<Node2D> constructNode)
        {
            foreach (Vector2 spawnPoint in spawnPoints)
            {
                Node2D node = constructNode();
                GetParent().AddChild(node);
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

    }

}
