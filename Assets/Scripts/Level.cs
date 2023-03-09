#nullable enable

using Godot;
using System.Linq;
using System.Collections.Generic;

namespace Cowball {
    public partial class Level : Node2D
    {

        private CameraBounds? _cameraBounds;
        private Marker2D _playerSpawnPoint;
        private List<Vector2> _itemSpawnPoints;
        private List<Vector2> _exitSpawnPoints;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _cameraBounds = GetNodeOrNull<CameraBounds>("CameraBounds");
            _playerSpawnPoint = GetNode<Marker2D>("PlayerSpawnPoint");
            _itemSpawnPoints = GetNode2DGroupPositions("ItemSpawnPoints");
            _exitSpawnPoints = GetNode2DGroupPositions("ExitSpawnPoints");
        }

        private List<Vector2> GetNode2DGroupPositions(string spawnPointGroupName)
        {
            return GetTree().GetNodesInGroup(spawnPointGroupName)
                .Select(node => ((Node2D) node).Position)
                .ToList();
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public Vector2 PlayerSpawnPosition() => _playerSpawnPoint.Position;

        public RectIntBounds CameraBounds() 
        {
            if (_cameraBounds is not null)
            {
               return _cameraBounds.Bounds();
            }
            else
            {
                Rect2 viewportRect = GetViewportRect();
                return new RectIntBounds(viewportRect);
            }
        }
    }
}
