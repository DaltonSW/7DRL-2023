using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Cowball {
    public partial class World : Node2D
    {
        private enum State
        {
            LoadingLevel,
            Playing,
        }
        private State _state;

        private Player _player;
        private Level _level;

        private PackedScene _itemScene;
        private PackedScene _exitScene;

        private Random _rng;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _player = GetNode<Player>("Player");
            _level = GetNode<Level>("Level");

            _itemScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Item.tscn");
            _exitScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Exit.tscn");

            _rng = new Random();

            _state = State.LoadingLevel;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            switch (_state)
            {
                case State.LoadingLevel: {
                    SetUpLevel(_level);
                    _state = State.Playing;
                    break;
                }
                case State.Playing: { 
                    // Do nothing 
                    break;
                }
            }
        }

        public void SetUpLevel(Level level)
        {
            _player.SetCameraLimits(level.CameraBounds());
            _player.Position = level.PlayerSpawnPosition();

            Stack<ItemParams> itemPool = CreateItemPoolShuffled();
            SpawnNodes(level.ItemSpawnPoints, () => CreateItem(itemPool.Pop()));
            SpawnNodes(level.ExitSpawnPoints, () => _exitScene.Instantiate<Node2D>());
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

        private Stack<ItemParams> CreateItemPoolShuffled()
        {
            List<ItemParams> shuffledList = 
                CreateItemPool()
                    .OrderBy(_ => _rng.Next())
                    .ToList();
            return new Stack<ItemParams>(shuffledList);
        }

        private static List<ItemParams> CreateItemPool()
        {
            // Items TODO:
            // Lead Underwear - Butt stomp does more damage
            // Bike Pump - Higher bounce
            // Campfire - Flaming bullets
            // Bigger bullets - Bigger bullets
            // Hardhat - Can't take damage on your head
            return new List<ItemParams> {
                new ItemParams("Soylent", "Soylent", StatToChange.Health, 1),
                new ItemParams("Hotdog", "Hot Dog", StatToChange.Health, 1),
                new ItemParams("Itchy Finger", "Poison Ivy", StatToChange.FireRate, 0.3),
                new ItemParams("Coffee", "Coffee", StatToChange.Speed, 25),
            };
        }

        private Item CreateItem(ItemParams itemParams)
        {
            Item item = _itemScene.Instantiate<Item>();
            item.Initialize(itemParams);
            return item;
        }

    }

}
