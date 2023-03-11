using Godot;
using System;
using System.Collections.Generic;

namespace Cowball
{
    public partial class Bat : Area2D
    {
        private Player _player;
        [Export] public float Speed = 100F;
        private float _health = 10F;

        public override void _Ready()
        {
            _player = GetNode<Player>("../../Player");
        }

        public override void _Process(double delta)
        {
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
            _health -= damage;
            if (_health <= 0) QueueFree();
        }
    }
}
