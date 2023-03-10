using Godot;
using System;
using System.Collections.Generic;

public partial class SlimeBoss : CharacterBody2D
{
    private const string IdleAnim = "idle";
    private const string SquishAnim = "squish";
    private const string JumpAnim = "jump";

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

    // Collisions
    private CollisionPolygon2D _currentCollision;
    private List<CollisionPolygon2D> _idleCollisions;
    private List<CollisionPolygon2D> _squishCollisions;
    private List<CollisionPolygon2D> _jumpCollisions;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _sprite.Play(IdleAnim);

        _random = new Random();

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

        _idleCollisions = new List<CollisionPolygon2D>()
        {
            allZero,
            idleOneFive,
            idleTwoFour,
            idleThree,
            idleTwoFour,
            idleOneFive
        };

        _jumpCollisions = new List<CollisionPolygon2D>()
        {
            allZero,
            jumpOne,
            jumpTwo,
            jumpThree,
            jumpFour
        };

        _squishCollisions = new List<CollisionPolygon2D>()
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

        foreach (var col in _idleCollisions)
        {
            col.Disabled = true;
        }

        foreach (var col in _jumpCollisions)
        {
            col.Disabled = true;
        }

        foreach (var col in _squishCollisions)
        {
            col.Disabled = true;
        }
    }

    public override void _Process(double delta)
    {
        if (!_sprite.IsPlaying()) return;

        _currentCollision.Disabled = true;
        var frame = _sprite.Frame;
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
