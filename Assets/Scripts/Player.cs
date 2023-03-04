using Godot;
using System;

public partial class Player : CharacterBody2D
{
    public const float Speed = 300.0f;
    public const float JumpVelocity = -400.0f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    // Set Gravity at runtime
    // PhysicsServer2D.AreaSetParam(GetViewport().FindWorld2D().Space, PhysicsServer2D.AreaParameter.Gravity, <VAL>);

    public override void _Ready()
    {
        base._Ready();
        // Load PackedScenes
        // Load interaction area 
        // Load sounds
        // Set current stats?

        // Calculations from https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558
        // GRAVITY = (float)(JUMP_HEIGHT / (2 * Math.Pow(TIME_IN_AIR, 2)));
        // JUMP_SPEED = (float)Math.Sqrt(2 * JUMP_HEIGHT * GRAVITY);
    }

    // TODO: Probably rework the physics to be unique, these so far are the default Godot template

    public override void _PhysicsProcess(double delta) // Movement
    {
        Vector2 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (direction != Vector2.Zero)
        {
            velocity.X = direction.X * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed); // Applying friction
        }

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
    private void StartJump()
    {

    }

    private void StopJump()
    {

    }

    private void StartCrouch()
    {

    }

    private void StopCrouch()
    {

    }

    private void StartSlide()
    {

    }

    private void StopSlide()
    {

    }

    private void StartDash()
    {

    }
    #endregion
}
