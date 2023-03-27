using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Cowball
{
    /// <summary>
    /// A top-level node for managing a single run of the game.
    /// <para/>
    /// Manages:
    /// <list type="number">
    ///   <item>
    ///     Loading and populating levels as specified by the level scenes.
    ///     This includes loading the set of possible items and populating 
    ///     the levels with items.
    ///   </item>
    ///   <item>
    ///     Loading and playing all audio.
    ///   </item>
    ///   <item>
    ///     The run's state: loading the next level, playing, paused, won, or lost.
    ///     These states mainly change whether the player has control over the game.
    ///   </item>
    ///   <item>
    ///     Random number generation.
    ///   </item>
    /// </list>
    /// </summary>
    public partial class World : Node2D
    {
        /// <summary>
        /// A state of a run.
        /// </summary>
        private enum State
        {
            /// <summary>
            /// The game is loading the next level.
            /// </summary>
            LoadingLevel,

            /// <summary>
            /// The player is in control of the player character and playing a level.
            /// </summary>
            Playing,

            /// <summary>
            /// The run is over and the player can no longer control the player character.
            /// The player has won or lost the run.
            /// </summary>
            GameOver,
        }

        /// <summary>
        /// The state of the run.
        /// </summary>
        private State _state;

        /// <summary>
        /// The audio player for music.
        /// </summary>
        private AudioStreamPlayer _musicPlayer;

        /// <summary>
        // TODO: I think each player can only play one sound at once.
        //       We need a different audio player for each sound that
        //       we want to play simultaneously.
        //       Should consider just defaulting to one player per sound effect.
        /// The audio player for sound effects.
        /// </summary>
        private AudioStreamPlayer _audioPlayer;

        /// <summary>
        /// The game music.
        /// </summary>
        private AudioStream _gameMusic;
        
        /// <summary>
        /// The sound played when the run becomes paused.
        /// </summary>
        private AudioStream _pauseSound;

        /// <summary>
        /// The sound played when the run stops being paused.
        /// </summary>
        private AudioStream _unpauseSound;

        /// <summary>
        /// The sound played when the slime boss dies.
        /// </summary>
        private AudioStream _bossDeath;

        /// <summary>
        /// The player character.
        /// </summary>
        private Player _player;

        /// <summary>
        /// The slime boss, if it is present in the level, otherwise null.
        /// </summary>
        private SlimeBoss _boss;

        /// <summary>
        /// The overlay displayed when the player wins the run.
        /// </summary>
        private Sprite2D _youWin;

        /// <summary>
        /// The overlay displayed when the player loses the run.
        /// </summary>
        private Sprite2D _youLose;

        /// <summary>
        /// Whether the level must be instantiated from the packed scene
        /// `_levelScene` in order to load it.
        /// If false, the level is already present as a child of this node.
        /// </summary>
        private bool _needToInstantiateLevelScene;

        /// <summary>
        /// Stores a packed level scene before it is instantiated.
        /// If `_state` is `LoadingLevel` AND `_needToInstantiateLevelScene`,
        /// this scene is instantiated and added as a child.
        /// </summary>
        private PackedScene _levelScene;

        /// <summary>
        /// The current level, at least if `_state` is `Playing`.
        /// </summary>
        private Level _level;

        /// <summary>
        /// The generic Item scene.
        /// Must be instantiated then initialized to create items.
        /// </summary>
        private PackedScene _itemScene;

        /// <summary>
        /// The generic Exit scene.
        /// Must be instantiated then initialized to create exits.
        /// </summary>
        private PackedScene _exitScene;

        /// <summary>
        /// The random number generator for the run.
        /// </summary>
        private Random _rng;

        /// <summary>
        /// The boss level scene. 
        /// </summary>
        private PackedScene _bossLevelScene;
        
        /// <summary>
        /// A queue of all the levels which can be used in the run, shuffled.
        /// Used to draw random levels to populate the exits in each level.
        /// </summary>
        private Queue<string> _randomLevelFilenames;

        /// <summary>
        /// A queue of all the levels which have been drawn for the exits
        /// in the current level.
        /// </summary>
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
            _player.Connect("PlayerKilled", new Callable(this, nameof(OnPlayerDeath)));

            _youWin = GetNode<Sprite2D>("YouWin");
            _youLose = GetNode<Sprite2D>("YouLose");

            _pauseSound = GD.Load<AudioStream>("res://Assets/Sounds/Pause.wav");
            _unpauseSound = GD.Load<AudioStream>("res://Assets/Sounds/Unpause.wav");
            _bossDeath = GD.Load<AudioStream>("res://Assets/Sounds/BossDefeated.wav");
            _gameMusic = GD.Load<AudioStream>("res://Assets/Sounds/Game.wav");

            _musicPlayer = GetNode<AudioStreamPlayer>("MusicPlayer");
            _musicPlayer.Autoplay = true;
            _musicPlayer.Stream = _gameMusic;
            _musicPlayer.VolumeDb = -5f;
            _musicPlayer.Play();

            _audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
            _audioPlayer.Autoplay = false;
            _audioPlayer.VolumeDb = -5f;

            _itemScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Item.tscn");
            _exitScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Exit.tscn");
            _bossLevelScene = LoadLevelScene("Boss Level");

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
                    GetTree().ChangeSceneToFile("res://Assets/Scenes/MainMenu.tscn");
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
                            MoveChild(_level, 1);
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

        /// <summary>
        /// A "pseudo-constructor" for an Exit.
        /// Instantiates a node from `_exitScene`
        /// and initializes it with additional data it requires.
        /// Does not add the node as a child,
        /// so if you consider `_Ready` as part of the construction,
        /// this doesn't cover that part.
        /// </summary>
        /// <param name="nextLevelFilename">The filename of the level to associate with the Exit.</param>
        /// <param name="nextLevelScene">The packed scene of the level to associate with the Exit.</param>
        /// <returns>The new Exit.</returns>
        private Exit CreateExit(string nextLevelFilename, PackedScene nextLevelScene)
        {
            Exit exit = _exitScene.Instantiate<Exit>();
            exit.Initialize(this, nextLevelFilename, nextLevelScene);
            return exit;
        }

        /// <summary>
        /// Spawn random elements of the level such as items and exits,
        /// set up the player based on the nodes present in the given Level,
        /// and connect signals from nodes present in the level.
        /// </summary>
        /// <param name="level">The level to set up.</param>
        private void SetUpLevel(Level level)
        {
            // Set up the player based on the level's specifications.
            _player.SetCameraLimits(level.CameraBounds());
            _player.Position = level.PlayerSpawnPosition();
            _player.SetSpawn(level.PlayerSpawnPosition());

            // Spawn random items at each spawn point.
            Queue<ItemParams> itemPool = CopyToShuffledQueue(ITEM_POOL);
            SpawnNodesInLevel(level, level.ItemSpawnPoints, () => CreateItem(itemPool.Dequeue()));

            // Set `_nextLevels` based on number of exits.
            // If low on levels, use the Boss level for all `_nextLevels`.
            if (_randomLevelFilenames.Count - 4 < level.ExitSpawnPoints.Count)
            {
                var bossLevels =
                    Enumerable.Range(0, level.ExitSpawnPoints.Count)
                        .Select(i => ("Boss Level", _bossLevelScene));
                _nextLevels = new Queue<(string, PackedScene)>(bossLevels);
            }
            else
            {
                // Otherwise, load random levels.
                _nextLevels = TakeAndLoadLevelsFromPool(level.ExitSpawnPoints.Count);
            }

            // Connect enemy signals.
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

            // If there are no enemies in the level, spawn the exits immediately.
            SpawnExitsIfNoEnemiesRemain(null);
        }

        // TODO: cast to Enemy once we have a common interface for them?
        /// <summary>
        /// Get all the current enemy Nodes.
        /// </summary>
        /// <returns>An Array of the current enemy Nodes.</returns>
        private Godot.Collections.Array<Node> FindEnemies()
        {
            return GetTree().GetNodesInGroup("root_enemy");
        }

        /// <summary>
        /// Spawn the exits to the current level if no enemies remain.
        /// <para/>
        /// This method is set to be called on every enemy's death (see `OnEnemyDied`).
        /// An enemy is not immediately removed from the tree when it dies,
        /// so `OnEnemyDied` passes the enemy that just died to this method.
        /// If that enemy is the only one remaining in the tree, the exits still spawn.
        /// </summary>
        /// <param name="enemy">An enemy that is in the process of dying, if any.</param>
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

        /// <summary>
        /// Should be called every time an enemy dies.
        /// </summary>
        /// <param name="enemy">The enemy that died.</param>
        private void OnEnemyDied(Node enemy)
        {
            SpawnExitsIfNoEnemiesRemain(enemy);
        }

        /// <summary>
        /// Remove levels from the level pool, then load their corresponding scenes.
        /// Return the levels and scenes that were removed and loaded.
        /// </summary>
        /// <param name="n">The number of levels to remove and load.</param>
        /// <returns>A Queue of corresponding levels and loaded scenes.</returns>
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

        /// <summary>
        /// Load the scene for a level.
        /// </summary>
        /// <param name="levelFilename">The filename of the level within the level folder.</param>
        /// <returns>The scene.</returns>
        private static PackedScene LoadLevelScene(string levelFilename)
        {
            return ResourceLoader.Load<PackedScene>($"res://Assets/Levels/{levelFilename}.tscn");
        }

        /// <summary>
        /// Given a level and its spawn points, at each spawn point
        /// generates a new node with the given constructor and adds
        /// the node as a child of the level.
        /// </summary>
        /// <param name="level">The level to populate.</param>
        /// <param name="spawnPoints">The positions where nodes will be spawned.</param>
        /// <param name="constructNode">A function which returns unique nodes each time it is called.</param>
        private void SpawnNodesInLevel(Level level, List<Vector2> spawnPoints, Func<Node2D> constructNode)
        {
            foreach (Vector2 spawnPoint in spawnPoints)
            {
                Node2D node = constructNode();
                level.AddChild(node);
                node.Position = spawnPoint;
            }
        }

        /// <summary>
        /// Should be called when a boss dies.
        /// </summary>
        /// <param name="boss">The boss that died.</param>
        private void OnBossDeath(Node boss)
        {
            _audioPlayer.Stream = _bossDeath;
            _audioPlayer.Play();
            GetTree().Paused = true;
            _youWin.Visible = true;
            _state = State.GameOver;
        }

        /// <summary>
        /// Should be called when the player dies.
        /// </summary>
        private void OnPlayerDeath()
        {
            GetTree().Paused = true;
            _youLose.Visible = true;
            _state = State.GameOver;
        }

        /// <summary>
        /// The unique parameters of every item in the game.
        /// Specifies which items will be used to populate the levels.
        /// </summary>
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

        /// <summary>
        /// Load the list of all level filenames.
        /// </summary>
        /// <returns>The list of level filenames.</returns>
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

        /// <summary>
        /// Copy and shuffle all the elements of the given enumerable
        /// and return them in a `Queue`. The given enumerable is not modified.
        /// </summary>
        /// <typeparam name="T">The enumerable's elements' type.</typeparam>
        /// <param name="enumerable">An enumerable.</param>
        /// <returns>A new queue containing copies of all the elements of `enumerable`, shuffled.</returns>
        private Queue<T> CopyToShuffledQueue<T>(IEnumerable<T> enumerable)
        {
            // TODO: try just passing the result of `OrderBy` to the `Queue`
            //       constructor. Pretty sure there's no reason to collect in a `List`.
            List<T> shuffledList = enumerable.OrderBy(_ => _rng.Next()).ToList();
            return new Queue<T>(shuffledList);
        }

        /// <summary>
        /// A "pseudo-constructor" for an Item.
        /// Instantiates a node from `_itemScene`
        /// and initializes it with additional data it requires.
        /// Does not add the node as a child,
        /// so if you consider `_Ready` as part of the construction,
        /// this doesn't cover that part.
        /// </summary>
        /// <param name="itemParams">The argument to `Item::initialize`.</param>
        /// <returns>The new item.</returns>
        private Item CreateItem(ItemParams itemParams)
        {
            Item item = _itemScene.Instantiate<Item>();
            item.Initialize(itemParams);
            return item;
        }

        /// <summary>
        /// Load the next level from the given exit.
        /// </summary>
        /// <param name="exit">An exit.</param>
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

            // Queue the current level to be freed at the next tick.
            _level.QueueFree();
            
            // Prepare the next level to be loaded at the next tick.
            _state = State.LoadingLevel;
            _levelScene = exit.NextLevelScene;
            _needToInstantiateLevelScene = true;
        }
    }

}
