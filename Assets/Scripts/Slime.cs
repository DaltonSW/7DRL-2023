using System;
using Godot;

namespace Cowball
{
    public partial class Slime : CharacterBody2D
    {
        private Random _random;

        private Sprite2D _sprite;

        [Export] public float Damage = 3F;

        [Export] private int _speed = 800;
        [Export] private int _spread = 15;
        [Export] private int _allowedBounces = 5;
        private int _currentBounces;

        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("Sprite");

            _random = new Random();
            RotationDegrees += _random.Next(-_spread, _spread);
            Velocity = new Vector2(_speed, 0).Rotated(Rotation);
        }

        public override void _Process(double delta)
        {
            var collision = MoveAndCollide(Velocity * (float)delta);


            if (collision != null)
            {
                if (!collision.GetCollider().IsClass("SlimeBoss"))
                {
                    Velocity = Velocity.Bounce(collision.GetNormal());
                    _currentBounces += 1;
                }
            }
            _sprite.GlobalRotation = 0;
            if (_currentBounces >= _allowedBounces)
            {
                QueueFree();
            }
        }


    }
}