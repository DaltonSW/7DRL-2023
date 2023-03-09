using Godot;
using System;

namespace Cowball {
    public partial class World : Node2D
    {
        private Player _player;
        private Level _level;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _player = GetNode<Player>("Player");
            _level = GetNode<Level>("Level");

            SetUpLevel(_level);
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public void SetUpLevel(Level level)
        {
            _player.SetCameraLimits(level.CameraBounds());
            _player.Position = level.PlayerSpawnPosition();
        }
    }

}
