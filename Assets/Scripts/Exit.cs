using Godot;
using System;

namespace Cowball
{
    public partial class Exit : Node2D
    {
        public string NextLevelFilename { get; private set; }
        public PackedScene NextLevelScene { get; private set; }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public void Initialize(string nextLevelFilename, PackedScene nextLevelScene)
        {
            NextLevelFilename = nextLevelFilename;
            NextLevelScene = nextLevelScene;
        }
    }
}
