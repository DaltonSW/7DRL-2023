using Godot;
using System;

namespace Cowball
{
    public partial class CitySlicker : CharacterBody2D
    {
        private PackedScene _bulletScene;
        private Sprite2D _sprite;
        private Sprite2D _hatSprite;
        private Node2D _armGunNode;
        private Player _player;

        // Shooting
        [Export] public double FireRate = 1;
        [Export] public double Range = 250;
        private double _shotCooldown;

        public override void _Ready()
        {
            _armGunNode = GetNode<Node2D>("ArmGun");
            _sprite = GetNode<Sprite2D>("Sprite");
            _hatSprite = GetNode<Sprite2D>("HatSprite");
            _player = GetNode<Player>("../Player");

            _bulletScene = GD.Load<PackedScene>("res://Assets/Scenes/Enemies/EnemyBullet.tscn");
        }

        public override void _Process(double delta)
        {
            _armGunNode.Rotation = _sprite.GetAngleTo(_player.GlobalPosition);
            var left = Mathf.Abs(_armGunNode.RotationDegrees) > 90 && Mathf.Abs(_armGunNode.RotationDegrees) < 180;
            _sprite.FlipH = left;
            _hatSprite.FlipH = left;

            var dir = _player.Position - Position;
            var distToPlayer = Mathf.Sqrt(dir.X * dir.X + dir.Y * dir.Y);

            if (_shotCooldown == 0 && distToPlayer < Range)
            {
                Shoot();
                _shotCooldown += delta;
            }

            else
            {
                _shotCooldown += delta;
                if (_shotCooldown >= FireRate) _shotCooldown = 0;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsOnFloor()) return;
            var velocity = Velocity;
            velocity.Y += 200 * (float)delta;
            Velocity = velocity;
            MoveAndSlide();
        }

        private void Shoot()
        {
            var bullet = (EnemyBullet)_bulletScene.Instantiate();
            var bulletSpawn = GetNode<Marker2D>("ArmGun/BulletSpawn");
            bullet.Position = bulletSpawn.GlobalPosition;
            bullet.Rotation = _armGunNode.Rotation;
            GetParent().AddChild(bullet);
        }

        private void OnAreaEntered(Node area)
        {
            if (!area.IsInGroup("playerBullet")) return;
            QueueFree();
            area.QueueFree();
        }
    }
}
