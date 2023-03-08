using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
        [Export] public float JumpHeight = 40; //pixels
        [Export] public float TimeInAir = 0.18F; //honestly no idea
        [Export] public float JumpSpeed;
        [Export] public float Gravity;
        [Export] private float _jumpLockout = 10; //frames
        [Export] private float _currentJumpBuffer;
        [Export] public float MaxVertSpeed = 450F; // pixels above ground (360 will cap you at half of a 720p window)
        [Export] public float JumpBrakeDamping = 1F;

        // Move properties
        [Export] public float Speed = 150F; //pixels per second
        [Export] public float GroundSpeedCap = 375; //pixels per second
        [Export] public float Friction = 60; //no idea
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

        // Drop properties
        [Export] public float DropGravMult = 10;
        [Export] public float SoftDropInitBoost = 600;
        [Export] public float HardDropInitBoost = 1000;
        [Export] public float DropBounceMult = 0.4F;
        private Vector2 _dropInitPos;

        // State properties
        private bool _isFacingLeft;
        private bool _isJumping;
        private bool _isDashing;
        private bool _isCrouching;
        private bool _isSliding;
        private bool _isDying;
        private bool _isDropping;
        private bool _isHardDropping;
        private bool _canDash;
        private bool _canSlide;

        // Health
        private float _currentHealth;
        private float _currentInvincibility;

        // Sprites
        private AnimatedSprite2D _healthSprite;
        private AnimatedSprite2D _animatedSprite;
        private AnimatedSprite2D _ballSprite;
        private Sprite2D _hatSprite;
        private Node2D _armGunNode;

        // Player bounding
        private CollisionPolygon2D _collisionArea; // Physics collisions
        private CollisionPolygon2D _interactionArea; // Non-physics interactions

        // Sounds
        private AudioStreamPlayer _audioPlayer;
        private AudioStreamWav _jumpSound;
        private AudioStreamWav _shootSound;
        private AudioStreamWav _hurtSound;

        // Scenes
        private PackedScene _bulletScene;
        private PackedScene _itemScene;

        // Misc
        private List<Item> _items;

        #endregion

        public override void _Ready()
        {
            base._Ready();

            _bulletScene = GD.Load<PackedScene>("res://Assets/Scenes/Bullet.tscn");
            _itemScene = GD.Load<PackedScene>("res://Assets/Scenes/Item.tscn");

            _collisionArea = GetNode<CollisionPolygon2D>("CollisionPolygon");
            _interactionArea = GetNode<CollisionPolygon2D>("Area2D/InteractionPolygon");

            // _ballSprite = GetNode<Sprite2D>("BallSprite");
            _ballSprite = GetNode<AnimatedSprite2D>("AnimatedBallSprite");
            _hatSprite = GetNode<Sprite2D>("HatSprite");
            _armGunNode = GetNode<Node2D>("ArmGun");

            _ballSprite.Animation = "normal";

            // Calculations from https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558
            Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
            JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);

            _items = new List<Item>();

            // Set Project gravity at runtime
            PhysicsServer2D.AreaSetParam(GetViewport().FindWorld2D().Space, PhysicsServer2D.AreaParameter.Gravity, Gravity);
        }

        public override void _PhysicsProcess(double delta) // Movement
        {
            var velocity = Velocity;

            var right = Input.IsActionPressed("PlayerRight");
            var left = Input.IsActionPressed("PlayerLeft");

            var softDrop = Input.IsActionJustPressed("SoftBounce");
            var hardDrop = Input.IsActionJustPressed("HardBounce");

            var shoot = Input.IsActionJustPressed("PlayerShoot");

            if (right) // Move right
            {
                velocity.X = Math.Min(velocity.X + Speed, GroundSpeedCap);
                FaceRight();
            }

            if (left) // Move left
            {
                velocity.X = Math.Max(velocity.X - Speed, -GroundSpeedCap);
                FaceLeft();
            }

            if (!IsOnFloor()) // If in the air, check for bounce start and apply gravity
            {
                if (softDrop || hardDrop) // If either drop is pressed...
                {
                    _isDropping = true;
                    velocity.Y = softDrop ? SoftDropInitBoost : HardDropInitBoost; // Set velocity based on which
                    _dropInitPos = GlobalPosition;
                    _isHardDropping = hardDrop;
                }
                velocity.Y += Gravity * (float)delta;
            }

            else // If on floor...
            {
                if (!_isDropping) // ... and not dropping...
                {
                    if (_ballSprite.Frame == 4)
                    {
                        velocity = StartJump(velocity, JumpSpeed); // ...normal jump
                        _ballSprite.Animation = "normal";
                    }

                    else if (_ballSprite.Animation == "normal")
                    {
                        // _ballSprite.Animation = "squish";
                        _ballSprite.Play("squish");
                    }
                }

                else // If on floor and IS dropping...
                {
                    if (_isHardDropping) // If hard dropping, do dampened jump
                    {
                        velocity = StartJump(velocity, (JumpSpeed * JumpBrakeDamping));
                    }

                    else // Handle bouncy stuff
                    {
                        var totalDistFallen = Mathf.Abs(_dropInitPos.Y - GlobalPosition.Y);
                        var speed = JumpSpeed;
                        if (totalDistFallen > JumpHeight * 0.6) // Arbitrary value
                        {
                            speed += (float)Math.Sqrt(2 * totalDistFallen * DropBounceMult * Gravity);
                        }
                        velocity = StartJump(velocity, Mathf.Min(speed, MaxVertSpeed));
                    }
                    _isDropping = false;
                    _isHardDropping = false;
                }
            }


            if (shoot)
            {
                //SpawnItem();
                Shoot();
            }

            velocity.X = Mathf.MoveToward(velocity.X, 0, Friction);

            Velocity = velocity;
            MoveAndSlide();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            _armGunNode.Rotation = _ballSprite.GetAngleTo(GetGlobalMousePosition());

            // Animation checking / changing
            // Timer countdowns and resets (invincibility, shooting cooldown, etc)
        }

        #region Movement Methods
        private Vector2 StartJump(Vector2 velocity, float jumpSpeed)
        {
            if (IsOnFloor())
            {
                velocity.Y = -jumpSpeed;
            }

            else if (IsOnWall())
            {
                // Do wall jump, if we even have one
            }

            _isJumping = true;
            _currentJumpBuffer += 1;

            return velocity;
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
            _isFacingLeft = left;
            _ballSprite.FlipH = left;
            _hatSprite.FlipH = left;
        }

        public void FaceLeft() { Face(true); }
        public void FaceRight() { Face(false); }

        #endregion

        private void Shoot()
        {
            var bullet = (Bullet)_bulletScene.Instantiate();
            var bulletSpawn = GetNode<Marker2D>("ArmGun/BulletSpawn");
            bullet.Position = bulletSpawn.GlobalPosition;
            bullet.Rotation = _armGunNode.Rotation;
            GetParent().AddChild(bullet);
        }

        private void SpawnItem()
        {
            var item = (Item)_itemScene.Instantiate();
            var itemParams = new ItemParams("Heart", "heart", StatToChange.Health, 1);
            item.Initialize(itemParams);
            item.Position = GetGlobalMousePosition();
            GetParent().AddChild(item);
        }

        private void AddItem(Item newItem)
        {
            _items.Add(newItem);

            if (newItem.StatToChange == StatToChange.None)
            {
                // newItem.ThingToDo()
                return;
            }

            switch (newItem.StatToChange)
            {
                case StatToChange.Health:
                    _currentHealth += (int)newItem.AmountToChange;
                    break;

                case StatToChange.Speed:
                    Speed += (float)newItem.AmountToChange;
                    break;

                case StatToChange.FireRate:
                    // We'll need a shot cooldown
                    break;

                case StatToChange.JumpSpeed:
                    JumpSpeed += (float)newItem.AmountToChange;
                    break;

                case StatToChange.None:
                default:
                    break;
            }
        }

        public void OnAreaEntered(Area2D area)
        {
            if (!area.IsInGroup("items")) return;
            var item = area.GetParent<Item>();
            item.QueueFree();
        }


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



