using Godot;
using System;

namespace Cowball {
    public partial class CameraBounds : Node2D
    {
        private Marker2D _topLeft;
        private Marker2D _bottomRight;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _topLeft = GetNode<Marker2D>("TopLeft");
            _bottomRight = GetNode<Marker2D>("BottomRight");
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public RectIntBounds Bounds() 
        {
            return new RectIntBounds(
                (int) _topLeft.Position.X,
                (int) _bottomRight.Position.X,
                (int) _topLeft.Position.Y,
                (int) _bottomRight.Position.Y);
        }
    }

    public readonly record struct RectIntBounds
    {
        public int left { get; init; }
        public int right { get; init; }
        public int top { get; init; }
        public int bottom { get; init; }

        public RectIntBounds(int left, int right, int top, int bottom) 
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public RectIntBounds(Rect2 rect) 
        {
            Rect2 normalizedRect = rect.Abs();
            left = (int) normalizedRect.Position.X;
            right = (int) normalizedRect.Size.X;
            top = (int) normalizedRect.Position.Y;
            bottom = (int) normalizedRect.Size.Y;
        }
    }

}
