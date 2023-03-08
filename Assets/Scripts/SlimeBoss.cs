using Godot;
using System;

namespace Cowball
{
    public partial class SlimeBoss : RigidBody2D
    {
        private Random _random;

        private AnimatedSprite2D _sprite;
        [Export] public int MinIdleLoopsBeforeJump = 4;
        private int _idleLoopsCompleted;
        [Export] public int PercentChanceJump = 80;
        private bool _isJumping;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sprite = GetNode<AnimatedSprite2D>("Sprite");
            _sprite.Play("idle");

            _random = new Random();
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
            if (_sprite.Animation == "idle") // if idle anim...
            {
                if (!_sprite.IsPlaying()) // check if anim is done. If yes...
                {
                    if (_idleLoopsCompleted >= MinIdleLoopsBeforeJump) //check if "min idle loops has passed". If yes...
                    {
                        if (_random.Next(0, 100) < PercentChanceJump + _idleLoopsCompleted - MinIdleLoopsBeforeJump)
                        {
                            _idleLoopsCompleted = 0;
                            _sprite.Play("squish");
                        }

                        else
                        {
                            _idleLoopsCompleted = 0;
                            _sprite.Play("idle");
                        }
                    }

                    else // if not, increment loopsDone, start idle again
                    {
                        _idleLoopsCompleted += 1;
                        _sprite.Play("idle");
                        GD.Print("loop!");
                    }
                }
            }

            else if (_sprite.Animation == "squish" && !_sprite.IsPlaying())
            {
                //Jump();
                _sprite.Play("jump");
                _idleLoopsCompleted = 0;
            }

            else if (_sprite.Animation == "jump" && !_sprite.IsPlaying())
            {
                //_sprite.Play("idle");
                //Jump();
            }
        }

        public void Jump()
        {
            _sprite.Play("idle");
            _isJumping = true;
            // Check 2 raycasts to see which wall is further away
            // Jump toward that wall (via some impulse application, I think?)
            // Shoot out projectiles in a circle around you
            // Maybe shoot projectiles when you land as well, as a difficulty setting?
        }
    }
}

