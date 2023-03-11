using Godot;
using System;

namespace Cowball
{
    public partial class Bat : Area2D
    {
        private Player _player;
        [Export] public float Speed = 100F;
        private float _health = 10F;
        private AnimatedSprite2D _sprite;
        public Color GetHurtColor = new Color("66FFFF");

        public override void _Ready()
        {
            _player = GetNode<Player>("../../Player");
            _sprite = GetNode<AnimatedSprite2D>("Sprite");
        }

        public override void _Process(double delta)
        {
            if (_sprite.Modulate != Colors.White) _sprite.Modulate = Colors.White;
            if (_player == null) return;
            try
            {
                var dir = _player.Position - Position;
                Position += dir.Normalized() * Speed * (float)delta;
            }
            catch (ObjectDisposedException e)
            {
                Console.WriteLine(e);
                _player = null;
            }
        }

        private void OnAreaEntered(Node area)
        {
            if (!area.IsInGroup("playerBullet")) return;
            if (area.IsInGroup("enemy")) return;
            var bullet = (Bullet)area;
            TakeDamage(bullet.Damage);
            area.QueueFree();
        }

        private void TakeDamage(float damage)
        {
            _sprite.Modulate = GetHurtColor;
            _health -= damage;
            if (_health <= 0) QueueFree();
        }
    }
}
