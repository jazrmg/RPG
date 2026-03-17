using Godot;
using System.Collections.Generic;

public partial class Player3D : CharacterBody3D
{
	// MOVEMENT
	[Export] public float MaxGroundSpeed = 5.0f;
	[Export] public float MaxRunSpeed = 8.5f;
	[Export] public float GroundAcceleration = 20.0f;
	[Export] public float RunAcceleration = 30.0f;
	[Export] public float JumpForce = 3.0f;
	[Export] public float MouseSensitivity = 0.3f;
	[Export] public float RotationSpeed = 15.0f;
	[Export] public float CoyoteTime = 0.16f;
	[Export] public float JumpBufferTime = 0.16f;

	// COMBAT - LIGHT ATTACK
	[Export] public float LightSlashDamage = 15.0f;
	[Export] public float LightSlashKnockback = 5.0f;
	[Export] public float LightAttackCooldown = 0.5f;

	// COMBAT - HEAVY ATTACK
	[Export] public float HeavySlashDamage = 35.0f;
	[Export] public float HeavySlashKnockback = 15.0f;
	[Export] public float HeavyAttackCooldown = 1.2f;

	// COMBAT - SPECIAL ATTACK
	[Export] public float SpecialDamage = 50.0f;
	[Export] public float SpecialKnockback = 20.0f;
	[Export] public float SpecialCooldown = 2.0f;

	// COMBAT - GENERAL
	[Export] public float CriticalChance = 0.25f;
	[Export] public float CriticalMultiplier = 1.5f;
	[Export] public float InvincibilityDuration = 0.3f;
	[Export] public float AttackModeWindowTime = 3.0f;

	// DODGE ROLL
	[Export] public float DodgeRollSpeed = 4.2f;
	[Export] public float DodgeRollDuration = 0.48f;
	[Export] public float DodgeRollCooldown = 0.75f;

	// HEALTH
	[Export] public float MaxPlayerHealth = 100.0f;

	// PLAYER STATE
	public float _playerHealth = 100.0f;
	private float _invincibilityTimer = 0.0f;

	private ProgressBar _playerHealthBar;
	private Label _playerHealthLabel;

	private const string PlayerGroup = "player";
	private const string AnimationPlayerPath = "CharacterModel/AnimationPlayer";
	private const string SpringArmPath = "SpringArm3D";
	private const string CameraPath = "SpringArm3D/Camera3D";
	private const string SwordRootPath = "CharacterModel/Skeleton3D/RightHandAttachment/antique_estoc_1k";
	private const string DodgeSlideFbxPath = "res://models/player/Running_Slide.fbx";

	private readonly float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

	private Camera3D _camera;
	private SpringArm3D _springArm;
	private AnimationPlayer _animPlayer;
	private Node3D _swordRoot;

	public LevelingSystem LevelingSystem { get; private set; }
	public StatAllocationUI StatsUI { get; private set; }

	private Dictionary<string, string> _animationCache = new Dictionary<string, string>();
	private string _dodgeSlideAnim = "";
	private bool _isSwordEquipped = false;
	private bool _isSitting = false;
	private bool _isAttacking = false;
	private bool _isDodgeRolling = false;
	private bool _isGameOver = false;
	
	public float _lightAttackCooldownTimer = 0.0f;
	public float _heavyAttackCooldownTimer = 0.0f;
	private float _dodgeRollCooldownTimer = 0.0f;
	public float _specialAttackCooldownTimer = 0.0f;
	private float _comboTimer = 0.0f;
	private int _comboCount = 0;

	public enum AttackMode
	{
		None,
		Light,
		Heavy,
		Special
	}
	public AttackMode _currentAttackMode = AttackMode.None;
	public float _attackModeTimer = 0.0f;

	public bool _isAutoBattle = false;
	private float _autoAttackTimer = 0.0f;
	private float _autoAttackDelay = 0.8f;
	private float _lastDamageTime = 0.0f;

	private bool _key1WasPressed = false;
	private bool _key2WasPressed = false;
	private bool _key3WasPressed = false;
	private bool _dodgeKeyWasPressed = false;

	private Vector3 _velocity = Vector3.Zero;
	private Vector3 _lastInputDirection = Vector3.Zero;
	private Vector3 _dodgeRollDirection = Vector3.Zero;
	private float _coyoteCounter = 0.0f;
	private float _jumpBufferCounter = 0.0f;
	private bool _isJumping = false;
	private bool _wasInAir = false;

	private HashSet<Enemy> _hitEnemiesThisAttack = new HashSet<Enemy>();
	private float _cameraYaw;
	private float _cameraPitch;
	private float _cameraHeightOffset = 1.5f;
	private Vector3 _smoothCameraPos = Vector3.Zero;

	public override void _Ready()
	{
		AddToGroup(PlayerGroup);

		_springArm = GetNodeOrNull<SpringArm3D>(SpringArmPath);
		_camera = GetNodeOrNull<Camera3D>(CameraPath);
		_animPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
		_swordRoot = GetNodeOrNull<Node3D>(SwordRootPath);

		if (_springArm == null || _camera == null || _animPlayer == null)
		{
			SetPhysicsProcess(false);
			return;
		}

		Input.MouseMode = Input.MouseModeEnum.Captured;
		_animPlayer.RootNode = _animPlayer.GetParent().GetPath();
		_cameraHeightOffset = _springArm.Position.Y;
		_cameraYaw = Rotation.Y;
		_cameraPitch = _springArm.Rotation.X;
		_smoothCameraPos = GlobalPosition + Vector3.Up * _cameraHeightOffset;
		_springArm.TopLevel = true;

		LevelingSystem = new LevelingSystem();
		AddChild(LevelingSystem);
		LevelingSystem.LevelUp += OnPlayerLevelUp;

		var statUI = new StatAllocationUI();
		StatsUI = statUI;
		AddChild(statUI);
		statUI.CallDeferred(nameof(StatAllocationUI.InitializeDirectly), LevelingSystem);

		if (_swordRoot != null)
			_swordRoot.Visible = false;

		InitializeAnimations();
		PlayAnimation("Idle");
	}

	public override void _Input(InputEvent @event)
	{
		if (_isGameOver && @event.IsActionPressed("ui_accept"))
		{
			RestartGame();
			SceneTree tree = GetTree();
			tree?.Root.SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_cameraYaw += Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity);
			_cameraPitch += Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity);
			_cameraPitch = Mathf.Clamp(_cameraPitch, Mathf.DegToRad(-60), Mathf.DegToRad(30));
			UpdateCameraRig();
		}

		if (@event.IsActionPressed("ui_cancel"))
			Input.MouseMode = Input.MouseModeEnum.Visible;

		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		_invincibilityTimer = Mathf.Max(0, _invincibilityTimer - dt);
		_lightAttackCooldownTimer = Mathf.Max(0, _lightAttackCooldownTimer - dt);
		_heavyAttackCooldownTimer = Mathf.Max(0, _heavyAttackCooldownTimer - dt);
		_dodgeRollCooldownTimer = Mathf.Max(0, _dodgeRollCooldownTimer - dt);
		_specialAttackCooldownTimer = Mathf.Max(0, _specialAttackCooldownTimer - dt);
		_comboTimer = Mathf.Max(0, _comboTimer - dt);
		_jumpBufferCounter -= dt;
		_coyoteCounter -= dt;
		_lastDamageTime = Mathf.Max(0, _lastDamageTime - dt);

		if (_attackModeTimer > 0)
		{
			_attackModeTimer -= dt;
		}
		else
		{
			_currentAttackMode = AttackMode.None;
		}

		HandleAttackModeSelection();
		HandleAutoBattle(dt);
		HandleSwordToggle();
		HandleSitToggle();
		HandleDodgeRoll(dt);
		HandleAttack();

		Vector2 inputDir = (_isSitting || _isAttacking) ? Vector2.Zero : 
			Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		bool isRunning = inputDir != Vector2.Zero && Input.IsKeyPressed(Key.Shift);
		Vector3 moveDirection = _isDodgeRolling ? _dodgeRollDirection : GetCameraRelativeDirection(inputDir);

		if (Input.IsActionJustPressed("ui_accept") && !_isSitting && !_isAttacking && IsOnFloor())
			_jumpBufferCounter = JumpBufferTime;

		bool isGrounded = IsOnFloor();
		bool justLanded = isGrounded && _wasInAir;
		_wasInAir = !isGrounded;

		if (isGrounded)
		{
			_coyoteCounter = CoyoteTime;
			_velocity.Y = 0.0f;
		}

		ApplyGravity(ref _velocity, dt);
		HandleMovement(moveDirection, isRunning, isGrounded, dt);
		HandleJump();

		if (_isAttacking)
			CheckAttackHits();

		_velocity.Y = Mathf.Clamp(_velocity.Y, -20.0f, float.MaxValue);

		Velocity = _velocity;
		MoveAndSlide();

		UpdateCameraRig();
		UpdateAnimationState(moveDirection, isRunning, _isJumping, justLanded);

		_isJumping = false;
	}

	private void HandleAttackModeSelection()
	{
		if (Input.IsKeyPressed(Key.Key1) && !_key1WasPressed)
		{
			_key1WasPressed = true;
			_currentAttackMode = AttackMode.Light;
			_attackModeTimer = AttackModeWindowTime;
		}
		if (!Input.IsKeyPressed(Key.Key1))
			_key1WasPressed = false;

		if (Input.IsKeyPressed(Key.Key2) && !_key2WasPressed)
		{
			_key2WasPressed = true;
			_currentAttackMode = AttackMode.Heavy;
			_attackModeTimer = AttackModeWindowTime;
		}
		if (!Input.IsKeyPressed(Key.Key2))
			_key2WasPressed = false;

		if (Input.IsKeyPressed(Key.Key3) && !_key3WasPressed)
		{
			_key3WasPressed = true;
			_currentAttackMode = AttackMode.Special;
			_attackModeTimer = AttackModeWindowTime;
		}
		if (!Input.IsKeyPressed(Key.Key3))
			_key3WasPressed = false;
	}

	private void HandleAutoBattle(float delta)
	{
		if (!_isAutoBattle || _isGameOver || !_isSwordEquipped)
		{
			_autoAttackTimer = 0;
			return;
		}

		Enemy nearestEnemy = FindNearestEnemy();
		if (nearestEnemy == null || !GodotObject.IsInstanceValid(nearestEnemy))
		{
			_isAutoBattle = false;
			return;
		}

		Vector3 dirToEnemy = (nearestEnemy.GlobalPosition - GlobalPosition);
		float distanceToEnemy = dirToEnemy.Length();
		Vector3 dirToEnemyNorm = dirToEnemy.Normalized();
		dirToEnemyNorm.Y = 0;

		float healthPercent = _playerHealth / MaxPlayerHealth;
		float enemyHealthPercent = nearestEnemy.CurrentHealth / nearestEnemy.MaxHealth;

		float braveryLevel = CalculateBravery(healthPercent, enemyHealthPercent);

		bool enemyClosing = distanceToEnemy < 4.5f;
		
		TacticalMovement(dirToEnemyNorm, distanceToEnemy, healthPercent, braveryLevel, enemyClosing, delta);

		if (dirToEnemyNorm != Vector3.Zero)
		{
			RotateTowardDirection(dirToEnemyNorm, delta * 1.2f);
		}

		if (enemyClosing && IsOnFloor())
		{
			PredictiveDodging(distanceToEnemy, healthPercent, braveryLevel, delta);
		}

		if (distanceToEnemy <= 3.5f)
		{
			_autoAttackTimer -= delta;

			if (_autoAttackTimer <= 0 && !_isAttacking && IsOnFloor())
			{
				AttackMode selectedAttack = SelectBraveAttack(healthPercent, braveryLevel, enemyHealthPercent, distanceToEnemy);
				_currentAttackMode = selectedAttack;

				bool didAttack = false;

				if (selectedAttack == AttackMode.Special && CanSpecialAttack())
				{
					PerformSpecialAttack();
					_autoAttackTimer = _autoAttackDelay + 0.4f;
					didAttack = true;
				}
				else if (selectedAttack == AttackMode.Heavy && CanHeavyAttack())
				{
					PerformHeavyAttack();
					_autoAttackTimer = _autoAttackDelay + 0.2f;
					didAttack = true;
				}
				else if (CanAttack())
				{
					PerformLightAttack();
					_autoAttackTimer = _autoAttackDelay - 0.1f;
					didAttack = true;
				}

				if (!didAttack)
				{
					_autoAttackTimer = 0.08f;
				}
			}
		}
		else if (distanceToEnemy > 4.0f)
		{
			_autoAttackTimer = 0;
		}
	}

	private float CalculateBravery(float healthPercent, float enemyHealthPercent)
	{
		float bravery = 0.5f;

		if (healthPercent > 0.9f) { bravery += 0.4f; }
		else if (healthPercent > 0.8f) { bravery += 0.35f; }
		else if (healthPercent > 0.65f) { bravery += 0.2f; }
		else if (healthPercent > 0.5f) { bravery += 0.08f; }
		else if (healthPercent > 0.35f) { bravery -= 0.05f; }
		else if (healthPercent > 0.2f) { bravery -= 0.2f; }
		else if (healthPercent > 0.1f) { bravery -= 0.4f; }
		else if (healthPercent <= 0.08f) { bravery -= 0.6f; }

		if (enemyHealthPercent < 0.15f) { bravery += 0.5f; }
		else if (enemyHealthPercent < 0.3f) { bravery += 0.35f; }
		else if (enemyHealthPercent < 0.5f) { bravery += 0.2f; }
		else if (enemyHealthPercent > 0.8f) { bravery -= 0.1f; }

		float finalBravery = Mathf.Clamp(bravery, 0.0f, 1.0f);
		
		return finalBravery;
	}

	private void TacticalMovement(Vector3 dirToEnemy, float distance, float healthPercent, float braveryLevel, bool enemyClosing, float delta)
	{
		Vector3 moveDirection = Vector3.Zero;

		if (healthPercent < 0.08f && distance < 5.0f && braveryLevel < 0.15f)
		{
			moveDirection = -dirToEnemy;
		}
		else if (distance > 4.5f)
		{
			moveDirection = dirToEnemy;
		}
		else if (distance > 2.2f && distance <= 4.5f && enemyClosing && braveryLevel > 0.3f)
		{
			Vector3 sideDirection = dirToEnemy.Cross(Vector3.Up).Normalized();
			if (GD.Randf() < 0.6f)
			{
				if ((int)GD.Randi() % 2 == 0)
					sideDirection = -sideDirection;
				moveDirection = sideDirection;
			}
			else
			{
				moveDirection = dirToEnemy * 0.7f;
			}
		}
		else if (distance > 2.5f && braveryLevel > 0.75f && healthPercent > 0.6f)
		{
			moveDirection = dirToEnemy * 1.2f;
		}
		else if (distance > 2.5f)
		{
			moveDirection = dirToEnemy;
		}
		else
		{
			moveDirection = Vector3.Zero;
		}

		if (moveDirection != Vector3.Zero)
		{
			moveDirection = moveDirection.Normalized();
			Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);
			float targetSpeed = (braveryLevel > 0.7f) ? MaxRunSpeed * 1.1f : MaxRunSpeed;
			Vector3 desiredVelocity = moveDirection * targetSpeed;
			
			horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, RunAcceleration * delta * 1.2f);

			_velocity.X = horizontalVelocity.X;
			_velocity.Z = horizontalVelocity.Z;
		}
		else
		{
			Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);
			horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, RunAcceleration * 2.0f * delta);
			_velocity.X = horizontalVelocity.X;
			_velocity.Z = horizontalVelocity.Z;
		}
	}

	private void PredictiveDodging(float distanceToEnemy, float healthPercent, float braveryLevel, float delta)
	{
		if (_lastDamageTime > 0 && IsOnFloor())
		{
			if (GD.Randf() < 0.90f)
			{
				_jumpBufferCounter = JumpBufferTime;
				return;
			}
		}

		if (distanceToEnemy < 2.0f && IsOnFloor())
		{
			if (GD.Randf() < 0.80f)
			{
				_jumpBufferCounter = JumpBufferTime;
				return;
			}
		}

		float baseDodgeChance = 0.15f;
		
		if (healthPercent < 0.75f) baseDodgeChance = 0.20f;
		if (healthPercent < 0.60f) baseDodgeChance = 0.25f;
		if (healthPercent < 0.50f) baseDodgeChance = 0.30f;
		if (healthPercent < 0.35f) baseDodgeChance = 0.40f;
		if (healthPercent < 0.20f) baseDodgeChance = 0.50f;
		if (healthPercent < 0.10f) baseDodgeChance = 0.60f;

		float distanceMultiplier = 1.0f;
		if (distanceToEnemy < 3.5f) distanceMultiplier = 2.0f;
		if (distanceToEnemy < 2.5f) distanceMultiplier = 3.0f;
		if (distanceToEnemy < 1.8f) distanceMultiplier = 4.0f;

		float braveryModifier = 1.0f - (braveryLevel * 0.1f);
		braveryModifier = Mathf.Max(braveryModifier, 0.7f);

		float finalDodgeChance = baseDodgeChance * distanceMultiplier * braveryModifier;
		finalDodgeChance = Mathf.Min(finalDodgeChance, 0.85f);

		if (distanceToEnemy < 3.0f)
		{
			finalDodgeChance = Mathf.Max(finalDodgeChance, 0.25f);
		}

		if (GD.Randf() < finalDodgeChance && IsOnFloor())
		{
			_jumpBufferCounter = JumpBufferTime;
		}
	}

	private AttackMode SelectBraveAttack(float healthPercent, float braveryLevel, float enemyHealthPercent, float distance)
	{
		if (healthPercent < 0.08f)
		{
			return AttackMode.Light;
		}

		if (enemyHealthPercent < 0.2f && braveryLevel > 0.5f)
		{
			if (GD.Randf() < 0.8f) { return AttackMode.Special; }
			if (GD.Randf() < 0.7f) { return AttackMode.Heavy; }
		}

		if (enemyHealthPercent < 0.5f)
		{
			if (GD.Randf() < 0.65f) { return AttackMode.Heavy; }
			if (GD.Randf() < 0.4f) { return AttackMode.Special; }
		}

		if (braveryLevel > 0.85f)
		{
			if (GD.Randf() < 0.65f) { return AttackMode.Special; }
			if (GD.Randf() < 0.8f) { return AttackMode.Heavy; }
		}
		else if (braveryLevel > 0.70f)
		{
			if (GD.Randf() < 0.7f) { return AttackMode.Heavy; }
			if (GD.Randf() < 0.45f) { return AttackMode.Special; }
		}
		else if (braveryLevel > 0.55f)
		{
			if (GD.Randf() < 0.6f) { return AttackMode.Heavy; }
			if (GD.Randf() < 0.3f) { return AttackMode.Special; }
		}
		else if (braveryLevel > 0.40f)
		{
			if (GD.Randf() < 0.45f) { return AttackMode.Heavy; }
			if (GD.Randf() < 0.2f) { return AttackMode.Special; }
		}
		else
		{
			if (GD.Randf() < 0.25f && healthPercent > 0.6f) { return AttackMode.Heavy; }
		}

		return AttackMode.Light;
	}

	private Enemy FindNearestEnemy()
	{
		var enemies = GetTree().GetNodesInGroup("enemy");
		Enemy nearest = null;
		float nearestDistance = float.MaxValue;

		foreach (Node node in enemies)
		{
			if (node is Enemy enemy && GodotObject.IsInstanceValid(enemy))
			{
				float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
				if (dist < nearestDistance)
				{
					nearestDistance = dist;
					nearest = enemy;
				}
			}
		}

		return nearest;
	}

	private void ApplyGravity(ref Vector3 velocity, float delta)
	{
		if (!IsOnFloor())
		{
			float multiplier = velocity.Y > 0 ? 0.5f : 1.2f;
			velocity.Y -= _gravity * multiplier * delta;
		}
	}

	private void HandleMovement(Vector3 desiredDirection, bool isRunning, bool isGrounded, float delta)
	{
		float maxSpeed = isRunning ? MaxRunSpeed : MaxGroundSpeed;
		float acceleration = isRunning ? RunAcceleration : GroundAcceleration;

		if (_isDodgeRolling)
		{
			maxSpeed = DodgeRollSpeed;
			acceleration = DodgeRollSpeed * 2;
		}

		if (desiredDirection != Vector3.Zero && !_isDodgeRolling)
		{
			Vector2 rawInput = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
			if (rawInput.Y > 0) maxSpeed *= 0.5f;
			else if (rawInput.X != 0) maxSpeed *= 0.7f;
		}

		if (!isGrounded)
		{
			maxSpeed *= 0.5f;
			acceleration *= 0.5f;
		}

		Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);

		if (_isDodgeRolling)
		{
			horizontalVelocity = _dodgeRollDirection * DodgeRollSpeed;
		}
		else if (desiredDirection != Vector3.Zero)
		{
			_lastInputDirection = _lastInputDirection.Lerp(desiredDirection, 0.08f);
			Vector3 desiredVelocity = _lastInputDirection * maxSpeed;
			float speed = horizontalVelocity.Length();
			float desiredSpeed = desiredVelocity.Length();

			if (speed < desiredSpeed)
				horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, acceleration * 0.9f * delta);
			else
				horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, (acceleration * 0.4f) * delta);

			if (!_isDodgeRolling)
				RotateTowardDirection(desiredDirection, delta);
		}
		else
		{
			_lastInputDirection = Vector3.Zero;
			horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, 8.0f * delta);
		}

		_velocity.X = horizontalVelocity.X;
		_velocity.Z = horizontalVelocity.Z;
	}

	private void HandleJump()
	{
		if (_jumpBufferCounter > 0 && _coyoteCounter > 0 && !_isJumping)
		{
			_velocity.Y = JumpForce;
			_isJumping = true;
			_jumpBufferCounter = 0.0f;
			_coyoteCounter = 0.0f;
			
			if (_isDodgeRolling)
			{
				_isDodgeRolling = false;
				_dodgeRollCooldownTimer = DodgeRollCooldown;
			}
		}

		if (_isJumping && !Input.IsActionPressed("ui_accept") && _velocity.Y > 0)
		{
			_velocity.Y *= 0.5f;
			_isJumping = false;
		}
	}

	private void HandleDodgeRoll(float delta)
	{
		if (_isDodgeRolling)
		{
			return;
		}

		if (Input.IsActionPressed("dodge") && !_dodgeKeyWasPressed)
		{
			_dodgeKeyWasPressed = true;

			if (!IsOnFloor())
			{
				_dodgeKeyWasPressed = false;
				return;
			}

			if (_dodgeRollCooldownTimer > 0)
			{
				_dodgeKeyWasPressed = false;
				return;
			}

			_isDodgeRolling = true;
			_dodgeRollDirection = _lastInputDirection != Vector3.Zero ? _lastInputDirection : -_camera.GlobalTransform.Basis.Z;
			_dodgeRollDirection.Y = 0;
			_dodgeRollDirection = _dodgeRollDirection.Normalized();

			_invincibilityTimer = DodgeRollDuration;

			if (_animationCache.TryGetValue("DodgeSlide", out string dodgeAnimPath))
			{
				PlayAnimation("DodgeSlide");
				_animPlayer.SpeedScale = 1.2f;
				
				Animation dodgeAnim = _animPlayer.GetAnimation(dodgeAnimPath);
				float dodgeDuration = dodgeAnim != null ? (float)dodgeAnim.Length : DodgeRollDuration;
				
				GetTree().CreateTimer(dodgeDuration).Timeout += () =>
				{
					if (IsInstanceValid(this) && _isDodgeRolling)
					{
						_isDodgeRolling = false;
						_dodgeRollCooldownTimer = DodgeRollCooldown;
						
						_velocity.X *= 0.75f;
						_velocity.Z *= 0.75f;
						_animPlayer.SpeedScale = 1.0f;
					}
				};
			}
			else
			{
				_isDodgeRolling = false;
			}
		}

		if (!Input.IsActionPressed("dodge"))
			_dodgeKeyWasPressed = false;
	}

	private Vector3 GetCameraRelativeDirection(Vector2 inputDir)
	{
		if (inputDir == Vector2.Zero) return Vector3.Zero;

		Vector3 forward = -_camera.GlobalTransform.Basis.Z;
		Vector3 right = _camera.GlobalTransform.Basis.X;
		forward.Y = right.Y = 0;

		return ((forward.Normalized() * -inputDir.Y) + (right.Normalized() * inputDir.X)).Normalized();
	}

	private void RotateTowardDirection(Vector3 direction, float delta)
	{
		if (direction == Vector3.Zero) return;
		float targetYaw = Mathf.Atan2(direction.X, direction.Z);
		float newYaw = Mathf.LerpAngle(Rotation.Y, targetYaw, RotationSpeed * delta);
		Rotation = new Vector3(Rotation.X, newYaw, Rotation.Z);
	}

	private void UpdateCameraRig()
	{
		Vector3 targetCamPos = GlobalPosition + Vector3.Up * _cameraHeightOffset;
		_smoothCameraPos = _smoothCameraPos.Lerp(targetCamPos, 0.12f);
		_springArm.GlobalPosition = _smoothCameraPos;
		
		_springArm.GlobalRotation = new Vector3(_cameraPitch, _cameraYaw, 0.0f);
	}

	private void InitializeAnimations()
	{
		var library = new AnimationLibrary();
		_animPlayer.AddAnimationLibrary("Custom", library);

		var animPaths = new[] {
			("Idle", "res://models/player/Idle.fbx", true),
			("Walk", "res://models/player/Walking.fbx", true),
			("Run", "res://models/player/Running.fbx", true),
			("Jump", "res://models/player/Jump.fbx", false),
			("Sit", "res://models/player/Sitting Idle.fbx", true),
			("SwordIdle", "res://models/player/Great_Sword_Idle.fbx", true),
			("SwordSlash", "res://models/player/Great_Sword_Slash.fbx", false),
			("DodgeSlide", DodgeSlideFbxPath, false)
		};

		foreach (var (name, path, loop) in animPaths)
		{
			Animation anim = AnimationHelper.ExtractAnimationFromFbx(path, loop);
			if (anim != null)
			{
				library.AddAnimation(name, anim);
				_animationCache[name] = $"Custom/{name}";
			}
		}
	}

	private void HandleSwordToggle()
	{
		if (!Input.IsActionJustPressed("equip_sword") || !IsOnFloor() || _isSitting || _isAttacking)
			return;

		_isSwordEquipped = !_isSwordEquipped;
		if (_swordRoot != null)
			_swordRoot.Visible = _isSwordEquipped;
	}

	private void HandleSitToggle()
	{
		if (!Input.IsActionJustPressed("sit") || !IsOnFloor() || _isAttacking)
			return;

		_isSitting = !_isSitting;
	}

	private void HandleAttack()
	{
		if (_isAttacking || _isDodgeRolling)
		{
			if (!_animPlayer.IsPlaying())
			{
				_isAttacking = false;
				_hitEnemiesThisAttack.Clear();
				_animPlayer.SpeedScale = 1.0f;
			}
			return;
		}

		if (!Input.IsActionJustPressed("attack"))
			return;

		if (!_isSwordEquipped)
		{
			return;
		}

		switch (_currentAttackMode)
		{
			case AttackMode.Light:
				if (CanAttack()) 
				{
					PerformLightAttack();
				}
				break;
			case AttackMode.Heavy:
				if (CanHeavyAttack()) 
				{
					PerformHeavyAttack();
				}
				break;
			case AttackMode.Special:
				if (CanSpecialAttack()) 
				{
					PerformSpecialAttack();
				}
				break;
			case AttackMode.None:
				if (CanAttack()) 
				{
					PerformLightAttack();
				}
				break;
		}
	}

	private bool CanAttack() => !_isSitting && _lightAttackCooldownTimer <= 0 && IsOnFloor() && _isSwordEquipped;
	private bool CanHeavyAttack() => !_isSitting && _heavyAttackCooldownTimer <= 0 && IsOnFloor() && _isSwordEquipped;
	private bool CanSpecialAttack() => !_isSitting && _specialAttackCooldownTimer <= 0 && IsOnFloor() && _isSwordEquipped;

	private void PerformLightAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_lightAttackCooldownTimer = LightAttackCooldown;
		_currentAttackMode = AttackMode.None;

		if (_comboTimer > 0)
		{
			_comboCount++;
		}
		else
		{
			_comboCount = 1;
		}
		_comboTimer = 0.5f;

		PlayAnimation("SwordSlash");
	}

	private void PerformHeavyAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_heavyAttackCooldownTimer = HeavyAttackCooldown;
		_currentAttackMode = AttackMode.None;

		PlayAnimation("SwordSlash");
	}

	private void PerformSpecialAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_specialAttackCooldownTimer = SpecialCooldown;
		_currentAttackMode = AttackMode.None;

		PlayAnimation("SwordSlash");
	}

	private void CheckAttackHits()
	{
		if (_swordRoot == null) return;

		var space = GetWorld3D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters3D();
		query.Shape = new BoxShape3D { Size = Vector3.One * 2.3f };
		query.Transform = _swordRoot.GlobalTransform;

		foreach (var result in space.IntersectShape(query))
		{
			if ((Node)result["collider"] is Enemy enemy && !_hitEnemiesThisAttack.Contains(enemy))
			{
				_hitEnemiesThisAttack.Add(enemy);
				Vector3 knockbackDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
				knockbackDir.Y = 0;
				knockbackDir = knockbackDir.Normalized();

				float damage = DetermineAttackDamage(out bool isCritical);
				float knockback = DetermineAttackKnockback();

				ShakeScreen(0.15f, 0.2f);
				
				enemy.FlashHit();
				
				float enemyHealthBeforeDamage = enemy.CurrentHealth;
				enemy.TakeDamage(damage, knockbackDir, knockback, isCritical);
				
				if (enemyHealthBeforeDamage > 0 && enemy.CurrentHealth <= 0)
				{
					int xpReward = 100;
					LevelingSystem.AddXP(xpReward);
				}
			}
		}
	}

	private float DetermineAttackDamage(out bool isCritical)
	{
		float baseDamage = LightSlashDamage;

		if (_comboCount >= 2)
		{
			baseDamage *= (1.0f + (_comboCount * 0.5f));
		}

		isCritical = GD.Randf() < CriticalChance;
		if (isCritical)
		{
			baseDamage *= CriticalMultiplier;
		}

		return baseDamage;
	}

	private float DetermineAttackKnockback()
	{
		return LightSlashKnockback;
	}

	private void UpdateAnimationState(Vector3 moveDirection, bool isRunning, bool jumped, bool landed)
	{
		if (_isAttacking) return;
		if (_isDodgeRolling) return;

		string anim = _isSitting ? "Sit" :
			(landed && !jumped) ? (_isSwordEquipped ? "SwordIdle" : "Idle") :
			(jumped || !IsOnFloor()) ? "Jump" :
			(moveDirection != Vector3.Zero) ? (isRunning ? "Run" : "Walk") :
			(_isSwordEquipped ? "SwordIdle" : "Idle");

		PlayAnimation(anim);
	}

	private void PlayAnimation(string name)
	{
		if (!_animationCache.TryGetValue(name, out string path)) return;
		if (_animPlayer.CurrentAnimation != path)
		{
			_animPlayer.Play(path);
			if (name.Contains("Attack") || name.Contains("Slash"))
			{
				_animPlayer.SpeedScale = 1.15f;
			}
			else
			{
				_animPlayer.SpeedScale = 1.0f;
			}
		}
	}

	private bool IsPlayerAlive() => _playerHealth > 0.0f;

	private void GameOver()
	{
		_isGameOver = true;
		SetPhysicsProcess(false);

		CanvasLayer layer = new CanvasLayer { Layer = 200 };
		AddChild(layer);

		ColorRect bg = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
		bg.AnchorLeft = bg.AnchorTop = 0;
		bg.AnchorRight = bg.AnchorBottom = 1;
		layer.AddChild(bg);

		VBoxContainer container = new VBoxContainer();
		container.AnchorLeft = 0.25f;
		container.AnchorTop = 0.3f;
		container.AnchorRight = 0.75f;
		container.AnchorBottom = 0.7f;
		container.Alignment = BoxContainer.AlignmentMode.Center;
		layer.AddChild(container);

		var labels = new[] {
			("GAME OVER", Colors.Red, 80),
			("You died!", Colors.White, 40)
		};

		foreach (var (text, color, size) in labels)
		{
			Label lbl = new Label { Text = text };
			lbl.AddThemeColorOverride("font_color", color);
			lbl.AddThemeFontSizeOverride("font_size", size);
			container.AddChild(lbl);
		}

		Button btn = new Button { Text = "Restart Game", CustomMinimumSize = new Vector2(300, 80) };
		btn.AddThemeFontSizeOverride("font_size", 32);
		btn.Pressed += RestartGame;
		container.AddChild(btn);

		Label hint = new Label { Text = "or Press ENTER" };
		hint.AddThemeColorOverride("font_color", Colors.Yellow);
		hint.AddThemeFontSizeOverride("font_size", 24);
		container.AddChild(hint);
	}

	private void RestartGame()
	{
		GetTree().ReloadCurrentScene();
	}

	private void OnPlayerLevelUp()
	{
		_playerHealth = MaxPlayerHealth;

		if (_playerHealthBar != null) _playerHealthBar.Value = _playerHealth;
		if (_playerHealthLabel != null) _playerHealthLabel.Text = $"Health: {_playerHealth:F0} / {MaxPlayerHealth:F0}";
	}

	private void ShakeScreen(float duration, float intensity)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		
		Vector3 originalPos = camera.GlobalPosition;
		Vector3 shakeOffset = new Vector3(
			(float)GD.Randf() * intensity - intensity / 2,
			(float)GD.Randf() * intensity - intensity / 2,
			0
		);
		
		tween.TweenProperty(camera, "global_position", originalPos + shakeOffset, duration * 0.25f);
		tween.TweenProperty(camera, "global_position", originalPos, duration * 0.75f);
	}

	public void PlayerTakeDamage(float damage)
	{
		if (_isGameOver || _invincibilityTimer > 0.0f) return;

		_playerHealth = Mathf.Max(0, _playerHealth - damage);
		_invincibilityTimer = InvincibilityDuration;
		_lastDamageTime = 0.3f;

		if (_playerHealthBar != null) _playerHealthBar.Value = _playerHealth;
		if (_playerHealthLabel != null) _playerHealthLabel.Text = $"Health: {_playerHealth:F0} / {MaxPlayerHealth:F0}";

		ShowDamageNumber(damage);

		Node3D enemy = GetTree().GetFirstNodeInGroup("enemy") as Node3D;
		if (enemy != null)
		{
			Vector3 knockbackDir = (GlobalPosition - enemy.GlobalPosition).Normalized();
			knockbackDir.Y = 0;
			_velocity += knockbackDir.Normalized() * 5.0f;
		}

		if (_playerHealth <= 0.0f)
			GameOver();
	}

	private void ShowDamageNumber(float damage, bool isCritical = false)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Label label = new Label();
		label.Text = isCritical ? $"{damage:F0}!" : damage.ToString("F0");
		
		Color damageColor = isCritical ? new Color(1, 0.8f, 0, 1) : Colors.Red;
		label.AddThemeColorOverride("font_color", damageColor);
		
		int fontSize = isCritical ? 40 : 28;
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.ZIndex = 100;
		
		Vector3 worldPos = GlobalPosition + Vector3.Up * 2.0f;
		Vector2 screenPos = camera.UnprojectPosition(worldPos);
		label.GlobalPosition = screenPos;
		
		GetTree().Root.AddChild(label);

		Tween tween = CreateTween();
		Vector2 startPos = label.GlobalPosition;
		tween.SetTrans(Tween.TransitionType.Linear);
		
		tween.Parallel().TweenProperty(label, "global_position", startPos - Vector2.Up * 60.0f, 1.2f);
		tween.Parallel().TweenProperty(label, "modulate", new Color(1, 1, 1, 0), 1.2f);
		
		tween.TweenCallback(Callable.From(() => {
			if (label != null && IsInstanceValid(label))
				label.QueueFree();
		}));
		
		GetTree().CreateTimer(2.0f).Timeout += () => {
			if (label != null && IsInstanceValid(label))
			{
				label.QueueFree();
			}
		};
	}
}
