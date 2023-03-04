using Godot;
using System;

public partial class Player : CharacterBody2D
{
    #region Properties
    // Jump properties
    [Export] public float JumpHeight = 145; //pixels
    [Export] public float TimeInAir = 0.2F; //honestly no idea
    [Export] public float JumpSpeed;
    [Export] public float Gravity;
    [Export] private float _jumpLockout = 10; //frames
    [Export] private float _currentJumpBuffer;

    // Move properties
    [Export] public float Speed = 300f; //pixels per second
    [Export] public float GroundSpeedCap = 500; //pixels per second
    [Export] public float Friction = 40; //no idea
    [Export] public float BaseWallJumpAway = 350;
    [Export] public float WallJumpScale = 2;

    // Dash properties
    [Export] public float DashSpeed = 400;
    [Export] public float DashDistance = 200;
    private float _currentDashDistance;

    // Slide properties
    [Export] public float SlideSpeed = 600;
    [Export] public float SlideDistance = 200;
    private float _currentSlideDistance;

    // State properties
    private bool _isFacingLeft;
    private bool _isJumping;
    private bool _isDashing;
    private bool _isCrouching;
    private bool _isSliding;
    private bool _isDying;
    private bool _canDash;
    private bool _canSlide;
    #endregion

    public override void _Ready()
    {
        base._Ready();
        // Load PackedScenes
        // Load interaction area 
        // Load sounds
        // Set current stats?

        // Calculations from https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558
        Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);

        // Set Project gravity at runtime
        PhysicsServer2D.AreaSetParam(GetViewport().FindWorld2D().Space, PhysicsServer2D.AreaParameter.Gravity, Gravity);
    }

    public override void _PhysicsProcess(double delta) // Movement
    {
        var velocity = Velocity;

        var right = Input.IsActionPressed("PlayerRight");
        var left = Input.IsActionPressed("PlayerLeft");
        var down = Input.IsActionPressed("PlayerDown");
        var jump = Input.IsActionJustPressed("PlayerJump");

        // var dash = Input.IsActionJustPressed("PlayerDash");
        var shoot = Input.IsActionJustPressed("PlayerShoot");

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        if (jump)
            velocity = StartJump(velocity);

        if (right)
            velocity.X = Math.Max(velocity.X - Speed, GroundSpeedCap);

        if (left)
            velocity.X = Math.Min(velocity.X + Speed, -GroundSpeedCap);

        velocity.X = Mathf.MoveToward(velocity.X, 0, Friction);

        Velocity = velocity;
        MoveAndSlide();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Animation checking / changing
        // Timer countdowns and resets (invincibility, shooting cooldown, etc)
    }

    #region Movement Methods
    private Vector2 StartJump(Vector2 velocity)
    {
        if (IsOnFloor())
        {
            velocity.Y -= JumpSpeed;
        }

        else if (IsOnWall())
        {
            // Do wall jump
        }

        _isJumping = true;
        _currentJumpBuffer += 1;

        return velocity;
    }

    private void StopJump()
    {
        _isJumping = false;
        _currentJumpBuffer = 0;
    }

    private void StartCrouch()
    {
        _isCrouching = true;
    }

    private void StopCrouch()
    {
        _isCrouching = false;
    }

    private void StartSlide()
    {
        _isSliding = true;
    }

    private void StopSlide()
    {
        _isSliding = false;
    }

    private void StartDash()
    {

    }
    #endregion
}
