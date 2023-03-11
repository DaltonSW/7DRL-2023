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
        [Export] public float MaxHealth = 5;
        public float CurrentHealth;
        private float _currentInvincibility;
        private PlayerHealth _healthHUD;

        // Shooting
        [Export] public double FireRate = 0.6;
        private double _shotCooldown;

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

        // Other Child Nodes
        private Camera2D _camera;

        // Scenes
        private PackedScene _bulletScene;
        private PackedScene _itemScene;

        // Misc
        private List<Item> _items;
        private Vector2 _curSpawnPoint;
        [Export] private int _bottomBound = 1500;

        #endregion

        public override void _Ready()
        {
            base._Ready();

            _bulletScene = GD.Load<PackedScene>("res://Assets/Scenes/Bullet.tscn");
            _itemScene = GD.Load<PackedScene>("res://Assets/Scenes/Item.tscn");

            _collisionArea = GetNode<CollisionPolygon2D>("CollisionPolygon");
            _interactionArea = GetNode<CollisionPolygon2D>("Area2D/InteractionPolygon");

            _camera = GetNode<Camera2D>("Camera");

            // _ballSprite = GetNode<Sprite2D>("BallSprite");
            _ballSprite = GetNode<AnimatedSprite2D>("AnimatedBallSprite");
            _hatSprite = GetNode<Sprite2D>("HatSprite");
            _armGunNode = GetNode<Node2D>("ArmGun");

            _ballSprite.Animation = "normal";

            // Calculations from https://medium.com/@brazmogu/physics-for-game-dev-a-platformer-physics-cheatsheet-f34b09064558
            Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
            JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);

            _items = new List<Item>();

            CurrentHealth = MaxHealth;
            _healthHUD = GetNode<PlayerHealth>("../PlayerHealth");

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

            var shoot = Input.IsActionPressed("PlayerShoot");

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

                    var anim = softDrop ? "softDrop" : "hardDrop";
                    _ballSprite.Play(anim);
                }
                else
                {
                    _ballSprite.Play("normal");
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
                    _ballSprite.Play("normal");
                }
            }

            if (_shotCooldown == 0)
            {
                if (shoot)
                {
                    Shoot();
                    _shotCooldown += delta;
                }
            }

            else
            {
                _shotCooldown += delta;
                if (_shotCooldown >= FireRate) _shotCooldown = 0;
            }

            velocity.X = Mathf.MoveToward(velocity.X, 0, Friction);

            if (GlobalPosition.Y > _bottomBound) Respawn();

            Velocity = velocity;
            MoveAndSlide();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            _armGunNode.Rotation = _ballSprite.GetAngleTo(GetGlobalMousePosition());
        }

        #region Movement Methods
        private Vector2 StartJump(Vector2 velocity, float jumpSpeed)
        {
            if (IsOnFloor())
            {
                velocity.Y = -jumpSpeed;
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

        private void FaceLeft() { Face(true); }
        private void FaceRight() { Face(false); }

        #endregion

        private void Shoot()
        {
            var bullet = (Bullet)_bulletScene.Instantiate();
            var bulletSpawn = GetNode<Marker2D>("ArmGun/BulletSpawn");
            bullet.Position = bulletSpawn.GlobalPosition;
            bullet.Rotation = _armGunNode.Rotation;
            GetParent().AddChild(bullet);
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
                    AddHealth();
                    break;

                case StatToChange.Speed:
                    Speed += (float)newItem.AmountToChange;
                    break;

                case StatToChange.FireRate:
                    FireRate += (float)newItem.AmountToChange;
                    break;

                case StatToChange.JumpSpeed:
                    JumpSpeed += (float)newItem.AmountToChange;
                    break;

                case StatToChange.None:
                default:
                    break;
            }
        }

        public void AddHealth()
        {
            CurrentHealth += 1;
            _healthHUD.AddHeart();
        }

        public void Respawn()
        {
            DamagePlayer(0.5F);
            Velocity = Vector2.Zero;
            GlobalPosition = _curSpawnPoint;
        }

        public void SetSpawn(Vector2 spawn)
        {
            _curSpawnPoint = spawn;
        }

        public void OnAreaEntered(Area2D area)
        {
            if (area.IsInGroup("items"))
            {
                var item = area.GetParent<Item>();
                item.QueueFree();
                return;
            }

            if (area.IsInGroup("exit_areas"))
            {
                Exit exit = area.GetParent<Exit>();
                exit.OnPlayerEntered();
                return;
            }

            if (area.IsInGroup("enemy"))
            {
                DamagePlayer(0.5F);
            }
        }

        public void OnBodyEntered(Node2D node)
        {
            if (node.IsInGroup("enemy"))
            {
                DamagePlayer(0.5F);
            }
        }

        public void DamagePlayer(float damage)
        {
            CurrentHealth -= damage;
            if (CurrentHealth <= 0)
            {
                QueueFree();
            }
        }


        // public void RecalcPhysics()
        // {
        //     Gravity = (float)(JumpHeight / (2 * Math.Pow(TimeInAir, 2)));
        //     JumpSpeed = (float)Math.Sqrt(2 * JumpHeight * Gravity);
        // }

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
            var tween = GetTree().CreateTween();
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
            CurrentHealth = MaxHealth;
            _healthSprite.Frame = 5;
        }

        public void HurtPlayer()
        {
            if (_currentInvincibility == 0)
            {
                _currentInvincibility = 0.016667F;
                CycleTransparency(false);
                CurrentHealth--;
                _healthSprite.Frame = (int)CurrentHealth;
                _audioPlayer.Stream = _hurtSound;
                _audioPlayer.Play();
                if (CurrentHealth == 0)
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

        public void SetCameraLimits(RectIntBounds limits)
        {
            _camera.SetLimits(limits);
        }
    }

    /// Adds methods to Camera2D.
    // TODO: move to separate file if we add other method extensions
    public static class Camera2DExtensions
    {
        /// Convenience method to set all Camera limits at once.
        public static void SetLimits(this Camera2D camera, RectIntBounds limits)
        {
            camera.SetLimits(limits.left, limits.right, limits.top, limits.bottom);
        }

        private static void SetLimits(this Camera2D camera, int limitLeft, int limitRight, int limitTop, int limitBottom)
        {
            camera.LimitLeft = limitLeft;
            camera.LimitRight = limitRight;
            camera.LimitTop = limitTop;
            camera.LimitBottom = limitBottom;
        }
    }
}



