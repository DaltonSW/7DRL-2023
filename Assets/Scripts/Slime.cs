using System;
using Godot;

namespace Cowball
{
    public partial class Slime : Area2D
    {
        private Random _random;

        private Sprite2D _sprite;

        [Export] public float Damage = 7.5F;

        [Export] private int _speed = 700;
        [Export] private int _spread = 5;
        [Export] private int _distanceAllowed = 500;
        [Export] private double _distanceTravelled;
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("Sprite");

            _random = new Random();
            RotationDegrees += _random.Next(-15, 15);
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            var movement = new Vector2((float)(_speed * delta * Mathf.Cos(Rotation)),
                (float)(_speed * delta * Mathf.Sin(Rotation)));

            Position += movement;
            _distanceTravelled += _speed * delta;
            _sprite.GlobalRotation = 0;
            if (_distanceTravelled > _distanceAllowed)
            {
                QueueFree();
            }
        }


    }
}