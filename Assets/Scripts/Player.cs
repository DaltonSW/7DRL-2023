using Godot;
using System;

namespace Cowball
{
    public partial class Player : CharacterBody2D
    {
        #region Signals
        [Signal] public delegate void PlayerKilledEventHandler();
        #endregion

        #region Properties
        // Constants
        [Export] private float _maxHealth = 5;
        private const float InvincibilityBuffer = 0.5F;
        private const int SpriteScale = 1;

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
        private AnimatedSprite2D _healthSprite;
        private AnimatedSprite2D _animatedSprite;
        private Sprite2D _ballSprite;
        private Sprite2D _hatSprite;

        private CollisionPolygon2D _collisionArea; // Physics collisions
        private CollisionPolygon2D _interactionArea; // Non-physics interactions

        // Sounds
        private AudioStreamPlayer _audioPlayer;
        private AudioStreamWav _jumpSound;
        private AudioStreamWav _shootSound;
        private AudioStreamWav _hurtSound;


        #endregion

        public override void _Ready()
        {
            base._Ready();
            // Load PackedScenes
            // Load interaction area 
            // Load sounds
            // Set current stats?

            _collisionArea = GetNode<CollisionPolygon2D>("CollisionPolygon");
            _interactionArea = GetNode<CollisionPolygon2D>("InteractionPolygon");

            _ballSprite = GetNode<Sprite2D>("Ball");
            _hatSprite = GetNode<Sprite2D>("Hat");

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
            {
                velocity.X = Math.Max(velocity.X - Speed, GroundSpeedCap);
                FaceRight();
            }

            if (left)
            {
                velocity.X = Math.Min(velocity.X + Speed, -GroundSpeedCap);
                FaceLeft();
            }

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

        #region Visual Methods
        private void ClearSpritesAndHitboxes()
        {
            _animatedSprite.Visible = false;
            _collisionArea.SetDeferred("disabled", true);
            _interactionArea.SetDeferred("disabled", true);
        }

        private void ActivateNormalSpriteAndHitboxes()
        {
            _animatedSprite.Visible = true;
            _collisionArea.SetDeferred("disabled", false);
            _interactionArea.SetDeferred("disabled", false);
        }

        private void SwitchToNormalSpriteAndHitboxes()
        {
            ClearSpritesAndHitboxes();
            ActivateNormalSpriteAndHitboxes();
        }

        private void CycleTransparency(bool lighten)
        {
            var tempNormal = _animatedSprite.Modulate;

            tempNormal.A = lighten ? 1 : 0.5F;

            _animatedSprite.Modulate = tempNormal;
        }

        private void Face(bool left)
        {
            // var xMultiplier = left ? -1 : 1;
            // GlobalTransform = new Transform2D(new Vector2(xMultiplier * SpriteScale, 0), new Vector2(0, SpriteScale), new Vector2(Position.X, Position.Y));
            _isFacingLeft = left;
            _ballSprite.FlipH = left;
            _hatSprite.FlipH = left;
        }

        public void FaceLeft() { Face(true); }
        public void FaceRight() { Face(false); }

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
            _animatedSprite.Play("health_death");
            Die();
        }

        private void Die()
        {
            _healthSprite.Frame = 0;
            _isDying = true;
            ProcessMode = ProcessModeEnum.Always;
            GetTree().Paused = true;
        }

        public void FallAndDie()
        {
            _animatedSprite.Play("fall_death");
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
            _currentHealth = _maxHealth;
            _healthSprite.Frame = 5;
        }

        public void HurtPlayer()
        {
            if (_currentInvincibility == 0)
            {
                _currentInvincibility = 0.016667F;
                CycleTransparency(false);
                _currentHealth--;
                _healthSprite.Frame = (int)_currentHealth;
                _audioPlayer.Stream = _hurtSound;
                _audioPlayer.Play();
                if (_currentHealth == 0)
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
            _animatedSprite.Play("idle");
            ProcessMode = ProcessModeEnum.Inherit;
            _isDying = false;
        }

        private void LoadSounds()
        {
            _audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
            _audioPlayer.VolumeDb = -18;

            _jumpSound = GD.Load<AudioStreamWav>("res://Sounds/SFX/jump.wav");
            _shootSound = GD.Load<AudioStreamWav>("res://Sounds/SFX/shoot.wav");
            _hurtSound = GD.Load<AudioStreamWav>("res://Sounds/SFX/hurt.wav");
            //guitarHitSound = GD.Load<AudioStreamSample>("res://Sounds/SFX/guitar_hit.wav");
            //guitarMissSound = GD.Load<AudioStreamSample>("res://Sounds/SFX/guitar_miss.wav");
            GD.Print("Sounds");
        }
    }

}
