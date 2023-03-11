using Godot;

namespace Cowball
{
    public partial class EnemyBullet : Area2D
    {

        [Export] public float Damage = 0.5F;

        [Export] private int _speed = 450;
        [Export] private int _spread = 5;
        [Export] private int _distanceAllowed = 700;
        private double _distanceTravelled;
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
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

        private void OnBodyEntered(Node2D node)
        {
            if (!node.IsInGroup("player")) return;
            var player = (Player)node;
            player.DamagePlayer(Damage);
            QueueFree();
        }

        private void FreeBullet()
        {
            QueueFree();
        }
    }
}