using Godot;
using System;

namespace Cowball
{
    public partial class Armadillo : CharacterBody2D
    {
        #region Signals
        [Signal] public delegate void DiedEventHandler();
        #endregion

        private float _speed = 50.0f;
        private bool _onLedge;
        private bool _isFacingLeft;

        private float _health = 10f;

        private RayCast2D _rayLeft;
        private RayCast2D _rayRight;
        private Sprite2D _sprite;

        public override void _Ready()
        {
            _rayLeft = GetNode<RayCast2D>("RayLeft");
            _rayRight = GetNode<RayCast2D>("RayRight");
            _sprite = GetNode<Sprite2D>("Sprite");
        }

        public override void _PhysicsProcess(double delta)
        {
            var velocity = Velocity;

            velocity.Y = IsOnFloor() ? 0 : 20;
            velocity.X = _speed;

            if (!_rayLeft.IsColliding() && _rayLeft.Enabled)
            {
                _speed *= -1;
                _sprite.FlipH = !_sprite.FlipH;
                _isFacingLeft = !_isFacingLeft;
                _rayLeft.Enabled = false;
                _rayRight.Enabled = true;
            }

            else if (!_rayRight.IsColliding() && _rayRight.Enabled)
            {
                _speed *= -1;
                _sprite.FlipH = !_sprite.FlipH;
                _isFacingLeft = !_isFacingLeft;
                _rayRight.Enabled = false;
                _rayLeft.Enabled = true;
            }

            Velocity = velocity;
            MoveAndSlide();
        }

        private void OnAreaEntered(Node area)
        {
            if (!area.IsInGroup("playerBullet")) return;
            var bullet = (Bullet)area;
            TakeDamage(bullet.Damage);
            area.QueueFree();
        }

        private void TakeDamage(float damage)
        {
            _health -= damage;
            if (_health <= 0) {
                EmitSignal(SignalName.Died);
                QueueFree();
            }
        }
    }
}
