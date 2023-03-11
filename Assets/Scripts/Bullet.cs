using Godot;

namespace Cowball
{
    public partial class Bullet : Area2D
    {

        [Export] public float Damage = 4F;

        [Export] private int _speed = 700;
        [Export] private int _spread = 5;
        [Export] private int _distanceAllowed = 200;
        [Export] private float disappearTime = 0.15F;
        private double _distanceTravelled;
        private bool _isDisappearing;
        private double _disappearTimeRemaining;
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _isDisappearing = false;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            if (_isDisappearing)
            {
                _disappearTimeRemaining -= delta;
                Modulate = new Color(1, 1, 1, (float)(_disappearTimeRemaining / disappearTime));
                if (_disappearTimeRemaining < 0)
                {
                    QueueFree();
                }
            }
            else
            {
                var movement = new Vector2((float)(_speed * delta * Mathf.Cos(Rotation)),
                    (float)(_speed * delta * Mathf.Sin(Rotation)));

                Position += movement;
                _distanceTravelled += _speed * delta;
                if (_distanceTravelled > _distanceAllowed)
                {
                     FreeBullet();
                }
            }
        }

        public void SetDamage(float damage)
        {
            Damage = damage;
        }

        private void OnAreaEntered(Area2D area)
        {
            if (_isDisappearing) return;
            if (!area.IsInGroup("boss")) return;
            var boss = (SlimeBoss)(area.GetParent());
            boss.TakeDamage(Damage);
            FreeBullet();
        }

        private void FreeBullet()
        {
            _isDisappearing = true;
            _disappearTimeRemaining = disappearTime;
        }
    }
}
