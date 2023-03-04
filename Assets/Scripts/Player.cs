using Godot;
using System;

public partial class Player : CharacterBody2D
{

    #region Signals
    [Signal] public delegate void PlayerKilledEventHandler();
    #endregion

    #region Properties
    // Constants
    [Export] private float MAX_HEALTH = 5;
    private const float INVINCIBILITY_BUFFER = 0.5F;
    private const int SPRITE_SCALE = 1;

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

    private float _currentInvincibility = 0;

    // Health
    private float _currentHealth;

    // Sprites
    private AnimatedSprite2D healthSprite;
    private AnimatedSprite2D animatedSprite;
    private Sprite2D crouchingSprite;
    private Sprite2D slidingSprite;

    // Physics collision boxes
    private CollisionShape2D[] normalCollisionBoxes;
    private CollisionShape2D crouchingCollision;
    private CollisionShape2D crouchingArrowUpShape;
    private Area2D crouchingArrowUp;
    private CollisionShape2D slidingCollision;
    
    private Area2D meleeCollision;
    private CollisionShape2D meleeCollisionShape;

    // Interaction (non-physics) collision boxes
    private CollisionShape2D normalInteraction;
    private CollisionShape2D crouchingInteraction;
    private CollisionShape2D slidingInteraction;

    // Sounds
    private AudioStreamPlayer audioPlayer;
    private AudioStreamWav jumpSound;
    private AudioStreamWav shootSound;
    private AudioStreamWav hurtSound;
    private AudioStreamWav guitarHitSound;
    private AudioStreamWav guitarMissSound;


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

    // TODO: Probably rework the physics to be unique, these so far are the default Godot template

    public override void _PhysicsProcess(double delta) // Movement
    {
        var velocity = Velocity;

        var right = Input.IsActionPressed("PlayerRight");
        var left = Input.IsActionPressed("PlayerLeft");

        var crouch = Input.IsActionPressed("PlayerDown");
        var jump = Input.IsActionJustPressed("PlayerJump");

        // var dash = Input.IsActionJustPressed("PlayerDash");
        var shoot = Input.IsActionJustPressed("PlayerShoot");

        // Add the gravity
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        // Handle Jump
        if (jump)
            velocity = StartJump(velocity);

        // Get the input direction and handle the movement/deceleration
        // As good practice, you should replace UI actions with custom gameplay actions
        var direction = Input.GetVector("PlayerLeft", "PlayerRight", "PlayerDown", "PlayerUp");
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

    #region Visual Methods
    private void ClearSpritesAndHitboxes()
    {
        animatedSprite.Visible = false;
        crouchingSprite.Visible = false;
        slidingSprite.Visible = false;

        foreach (CollisionShape2D normalCollisionBox in normalCollisionBoxes)
        {
            normalCollisionBox.SetDeferred("disabled", true);
        }
        crouchingCollision.SetDeferred("disabled", true);
        crouchingArrowUpShape.SetDeferred("disabled", true);
        slidingCollision.SetDeferred("disabled", true);

        normalInteraction.SetDeferred("disabled", true);
        crouchingInteraction.SetDeferred("disabled", true);
        slidingInteraction.SetDeferred("disabled", true);
    }

    private void ActivateNormalSpriteAndHitboxes()
    {
        animatedSprite.Visible = true;
        foreach (CollisionShape2D normalCollisionBox in normalCollisionBoxes)
        {
            normalCollisionBox.SetDeferred("disabled", false);
        }
        normalInteraction.SetDeferred("disabled", false);
    }

    private void SwitchToNormalSpriteAndHitboxes()
    {
        ClearSpritesAndHitboxes();
        ActivateNormalSpriteAndHitboxes();
    }

    private void ActivateCrouchSpriteAndHitboxes()
    {
        crouchingSprite.Visible = true;
        crouchingCollision.SetDeferred("disabled", false);
        crouchingArrowUpShape.SetDeferred("disabled", false);
        crouchingInteraction.SetDeferred("disabled", false);
    }

    private void SwitchToCrouchSpriteAndHitboxes()
    {
        ClearSpritesAndHitboxes();
        ActivateCrouchSpriteAndHitboxes();
    }

    private void ActivateSlideSpriteAndHitboxes()
    {
        slidingSprite.Visible = true;
        slidingCollision.SetDeferred("disabled", false);
        crouchingArrowUpShape.SetDeferred("disabled", false);
        slidingInteraction.SetDeferred("disabled", false);
    }

    private void SwitchToSlideSpriteAndHitboxes()
    {
        ClearSpritesAndHitboxes();
        ActivateSlideSpriteAndHitboxes();
    }

    private void CycleTransparency(bool lighten)
    {
        Color tempNormal = animatedSprite.Modulate;
        Color tempCrouch = crouchingSprite.Modulate;
        Color tempSlide = slidingSprite.Modulate;

        tempNormal.A = lighten ? 1 : 0.5F;	
        tempCrouch.A = lighten ? 1 : 0.5F;	
        tempSlide.A = lighten ? 1 : 0.5F;

        animatedSprite.Modulate = tempNormal;
        crouchingSprite.Modulate = tempCrouch;
        slidingSprite.Modulate = tempSlide;
    }
    
    private void Face(bool left)
    {
        int xMultiplier = left ? -1 : 1;
        GlobalTransform = new Transform2D(new Vector2(xMultiplier * SPRITE_SCALE, 0), new Vector2(0, SPRITE_SCALE), new Vector2(Position.X, Position.Y));
        _isFacingLeft = left;
    }

    private void FaceRight() { Face(false); }
    private void FaceLeft() { Face(true); }
    #endregion
    
    public void RecalcPhysics()
    {
        Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);
    }

    public void KillPlayer()
    {
        SwitchToNormalSpriteAndHitboxes();
        CycleTransparency(true);
        animatedSprite.Play("health_death");
        Die();
    }

    private void Die()
    {
        healthSprite.Frame = 0;
        _isDying = true;
        ProcessMode = ProcessModeEnum.Always;
        GetTree().Paused = true;
    }

    public void FallAndDie()
    {
        animatedSprite.Play("fall_death");
        Die();
        Tween tween = GetTree().CreateTween();
        var endPosition = new Vector2(Position.X, Position.Y + 200);
        tween.TweenProperty(this, "position", endPosition, .3f)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In)
            .SetDelay(.3f);
        tween.TweenCallback(Callable.From(OnTweenCompleted));
    }

    public void OnTweenCompleted()
    {
        EmitSignal(SignalName.PlayerKilled);
    }

    public void HealPlayer()
    {
        _currentHealth = MAX_HEALTH;
        healthSprite.Frame = 5;
    }

    public void HurtPlayer()
    {
        if (_currentInvincibility == 0)
        {
            _currentInvincibility = 0.016667F;
            CycleTransparency(false);
            _currentHealth--;
            healthSprite.Frame = (int)_currentHealth;
            audioPlayer.Stream = hurtSound;
            audioPlayer.Play();
            if(_currentHealth == 0)
            {
                KillPlayer();
            }
        }

    }

    public void ResetPlayer()
    {
        GD.Print("resetting");
        HealPlayer();
        ClearSpritesAndHitboxes();
        ActivateNormalSpriteAndHitboxes();
        animatedSprite.Play("idle");
        ProcessMode = ProcessModeEnum.Inherit;
        _isDying = false;
    }

    private void LoadSounds()
    {
        audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
        audioPlayer.VolumeDb = -18;

        jumpSound = GD.Load<AudioStreamWav>("res://Sounds/SFX/jump.wav");
        shootSound = GD.Load<AudioStreamWav>("res://Sounds/SFX/shoot.wav");
        hurtSound = GD.Load<AudioStreamWav>("res://Sounds/SFX/hurt.wav");
        //guitarHitSound = GD.Load<AudioStreamSample>("res://Sounds/SFX/guitar_hit.wav");
        //guitarMissSound = GD.Load<AudioStreamSample>("res://Sounds/SFX/guitar_miss.wav");
        GD.Print("Sounds");
    }
}
