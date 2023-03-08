#nullable enable

using Godot;
using System;

namespace Cowball {
    public partial class Level : Node2D
    {

        private CameraBounds? _cameraBounds;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _cameraBounds = GetNodeOrNull<CameraBounds>("CameraBounds");
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

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
