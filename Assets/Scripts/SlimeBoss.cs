using Godot;
using System;

namespace Cowball
{
    public partial class SlimeBoss : CharacterBody2D
    {
        private const string IdleAnim = "idle";
        private const string SquishAnim = "squish";
        private const string JumpAnim = "jump";
        private const string DeathAnim = "death";

        private Random _random;
        private PackedScene _slimeScene;
        private AnimatedSprite2D _sprite;

        // Health properties
        [Export] private float _maxHealth = 100;
        private float _currentHealth;

        // Attack properties
        [Export] public int MinIdleLoopsBeforeJump = 4;
        [Export] public int PercentChanceJump = 80;

        // State properties
        private bool _isJumping;
        private bool _isFacingLeft;
        private bool _canFlip;
        private int _idleLoopsCompleted;
        private int _prevSpriteFrame;

        // Jump properties
        [Export] public float JumpHeight = 120; //pixels
        [Export] public float TimeInAir = 0.18F; //honestly no idea
        [Export] public float JumpSpeedHoriz = 350F;
        [Export] public float JumpSpeed;
        [Export] public float Gravity;

        // Collisions
        private CollisionPolygon2D _currentCollision;
        private CollisionPolygon2D[] _idleCollisions;
        private CollisionPolygon2D[] _squishCollisions;
        private CollisionPolygon2D[] _jumpCollisions;

        public override void _Ready()
        {
            _slimeScene = GD.Load<PackedScene>("res://Assets/Scenes/Enemies/Slime.tscn");

            _sprite = GetNode<AnimatedSprite2D>("Sprite");
            _sprite.Play(IdleAnim);

            _random = new Random();

            _currentHealth = _maxHealth;

            // Calculations from https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558
            Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
            JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);

            LoadCollisions();
        }

        private void LoadCollisions()
        {
            var allZero = GetNode<CollisionPolygon2D>("allZero");
            var idleOneFive = GetNode<CollisionPolygon2D>("idleOneFive");
            var idleTwoFour = GetNode<CollisionPolygon2D>("idleTwoFour");
            var idleThree = GetNode<CollisionPolygon2D>("idleThree");
            var jumpOne = GetNode<CollisionPolygon2D>("jumpOne");
            var jumpTwo = GetNode<CollisionPolygon2D>("jumpTwo");
            var jumpThree = GetNode<CollisionPolygon2D>("jumpThree");
            var jumpFour = GetNode<CollisionPolygon2D>("jumpFour");
            var squishOneEleven = GetNode<CollisionPolygon2D>("squishOneEleven");
            var squishTwoTen = GetNode<CollisionPolygon2D>("squishTwoTen");
            var squishThreeNine = GetNode<CollisionPolygon2D>("squishThreeNine");
            var squishFourEight = GetNode<CollisionPolygon2D>("squishFourEight");
            var squishFiveSeven = GetNode<CollisionPolygon2D>("squishFiveSeven");
            var squishSix = GetNode<CollisionPolygon2D>("squishSix");

            _currentCollision = allZero;

            CollisionPolygon2D[] idle =
            {
                allZero,
                idleOneFive,
                idleTwoFour,
                idleThree,
                idleTwoFour,
                idleOneFive
            };

            CollisionPolygon2D[] jump =
            {
                allZero,
                jumpOne,
                jumpTwo,
                jumpThree,
                jumpFour
            };

            CollisionPolygon2D[] squish =
            {
                allZero,
                squishOneEleven,
                squishTwoTen,
                squishThreeNine,
                squishFourEight,
                squishFiveSeven,
                squishSix,
                squishFiveSeven,
                squishFourEight,
                squishThreeNine,
                squishTwoTen,
                squishOneEleven
            };

            CollisionPolygon2D[][] allCollisions = {
                idle,
                jump,
                squish
            };

            _idleCollisions = idle;
            _jumpCollisions = jump;
            _squishCollisions = squish;

            foreach (var list in allCollisions)
            {
                foreach (var col in list)
                {
                    col.Disabled = true;
                }
            }
        }

        public override void _Process(double delta)
        {
            if (!_sprite.IsPlaying())
            {
                if (_sprite.Animation == DeathAnim)
                {
                    QueueFree();
                }
                return;
            }
            if (_sprite.Animation == DeathAnim)
            {
                return;
            }
            var frame = _sprite.Frame;
            if (frame == _prevSpriteFrame) return;

            _currentCollision.Disabled = true;

            _currentCollision.Visible = false;

            // ReSharper disable once ConvertSwitchStatementToSwitchExpression
            switch (_sprite.Animation)
            {
                case IdleAnim:
                    _currentCollision = _idleCollisions[frame];
                    break;

                case JumpAnim:
                    _currentCollision = _jumpCollisions[frame];
                    break;

                case SquishAnim:
                    _currentCollision = _squishCollisions[frame];
                    break;
            }
            _currentCollision.Disabled = false;

            _currentCollision.Visible = true;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_sprite.Animation == DeathAnim)
            {
                return;
            }
            var velocity = Velocity;
            velocity.Y += Gravity * (float)delta;

            if (!IsOnFloor())
            {
                if (IsOnWall() && _canFlip)
                {
                    GD.Print("WALL");
                    // velocity.X *= -1;
                    _canFlip = false;
                    _isFacingLeft = !_isFacingLeft;
                    _sprite.FlipH = !_sprite.FlipH;
                }
            }

            else
            {
                if (_isJumping)
                {
                    _isJumping = false;
                    _sprite.Play(IdleAnim);
                    _idleLoopsCompleted = 0;
                }
            }

            if (_sprite.Animation == IdleAnim) // if idle anim...
            {
                if (!_sprite.IsPlaying()) // check if anim is done. If yes...
                {
                    if (_idleLoopsCompleted >= MinIdleLoopsBeforeJump) //check if "min idle loops has passed". If yes...
                    {
                        if (_random.Next(0, 100) < PercentChanceJump + _idleLoopsCompleted - MinIdleLoopsBeforeJump)
                        {
                            _idleLoopsCompleted = 0;
                            _sprite.Play(SquishAnim);
                        }

                        else
                        {
                            _idleLoopsCompleted = 0;
                            _sprite.Play(IdleAnim);
                        }
                    }

                    else // if not, increment loopsDone, start idle again
                    {
                        _idleLoopsCompleted += 1;
                        _sprite.Play(IdleAnim);
                        GD.Print("loop!");
                    }
                }
            }

            else if (_sprite.Animation == SquishAnim && !_sprite.IsPlaying())
            {
                _sprite.Play(JumpAnim);
                _idleLoopsCompleted = 0;
            }

            else if (_sprite.Animation == JumpAnim && !_sprite.IsPlaying() && !_isJumping)
            {
                velocity = Jump(velocity);
                Shoot();
            }

            Velocity = velocity;

            var collision = MoveAndCollide(Velocity * (float)delta);
            if (collision == null) return;
            //if (collision.GetCollider().IsClass("Slime")) return;
            var normal = collision.GetNormal();
            if (normal == Vector2.Up)
            {
                if (_isJumping)
                {
                    _isJumping = false;
                    _sprite.Play(IdleAnim);
                    _idleLoopsCompleted = 0;
                    // Shoot(); // If we want a "hard mode" thing where he shoots twice per boing
                }
                Velocity = new Vector2(0, 0);
                _canFlip = true;
            }

            else if (normal == Vector2.Left || normal == Vector2.Right)
            {
                if (!_canFlip) return;
                _isFacingLeft = !_isFacingLeft;
                _sprite.FlipH = !_sprite.FlipH;
                _canFlip = false;
                Velocity = Velocity.Bounce(normal);
            }

            else
            {
                Velocity = Velocity.Bounce(normal);
            }

        }

        private Vector2 Jump(Vector2 velocity)
        {
            _isJumping = true;
            _canFlip = true;
            var dir = _isFacingLeft ? -1 : 1;
            return new Vector2(velocity.X + JumpSpeedHoriz * dir, velocity.Y - JumpSpeed);
        }

        private void Shoot()
        {
            foreach (var node in GetTree().GetNodesInGroup("slimeSpawns"))
            {
                var point = (Marker2D)node;
                var newSlime = (Slime)_slimeScene.Instantiate();
                newSlime.GlobalPosition = point.GlobalPosition;
                newSlime.GlobalRotation = point.GlobalRotation;
                GetParent().AddChild(newSlime);
            }
        }

        public float GetHealthPercent() { return _currentHealth / _maxHealth * 100; }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;

            if (_currentHealth > 0) return;
            _sprite.Play(DeathAnim);
            _currentCollision.Disabled = true;
        }
    }
}
