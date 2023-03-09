using Godot;
using System;

public partial class SlimeBoss : CharacterBody2D
{
    private Random _random;

    private AnimatedSprite2D _sprite;
    [Export] public int MinIdleLoopsBeforeJump = 4;
    private int _idleLoopsCompleted;
    [Export] public int PercentChanceJump = 80;
    private bool _isJumping;
    private bool _isFacingLeft;
    private bool _canFlip;

    // Jump properties
    [Export] public float JumpHeight = 120; //pixels
    [Export] public float TimeInAir = 0.18F; //honestly no idea
    [Export] public float JumpSpeedHoriz = 350F;
    [Export] public float JumpSpeed;
    [Export] public float Gravity;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _sprite.Play("idle");

        _random = new Random();

        // Calculations from https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558
        Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);
    }



    public override void _PhysicsProcess(double delta)
    {
        var velocity = Velocity;
        velocity.Y += Gravity * (float)delta;

        // Add the gravity.
        if (!IsOnFloor())
        {
            if (IsOnWall() && _canFlip)
            {
                GD.Print("WALL");
                velocity.X *= -1;
                _canFlip = false;
                _isFacingLeft = !_isFacingLeft;
                _sprite.FlipH = !_sprite.FlipH;
            }
        }

        else
        {
            if (_isJumping)
            {
                velocity.X = 0;
                _isJumping = false;
                _sprite.Play("idle");
                _idleLoopsCompleted = 0;
            }

        }

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
            _sprite.Play("jump");
            _idleLoopsCompleted = 0;
        }

        else if (_sprite.Animation == "jump" && !_sprite.IsPlaying() && !_isJumping)
        {
            velocity = Jump(velocity);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private Vector2 Jump(Vector2 velocity)
    {
        _isJumping = true;
        _canFlip = true;
        var dir = _isFacingLeft ? -1 : 1;
        return new Vector2(velocity.X + JumpSpeedHoriz * dir, velocity.Y - JumpSpeed);

        // Shoot out projectiles in a circle around you
        // Maybe shoot projectiles when you land as well, as a difficulty setting?
    }
}
