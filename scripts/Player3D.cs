using Godot;
using System.Collections.Generic;

public partial class Player3D : CharacterBody3D
{
	// MOVEMENT
	[Export] public float MaxGroundSpeed = 5.0f;
	[Export] public float MaxRunSpeed = 8.5f;
	[Export] public float GroundAcceleration = 20.0f;
	[Export] public float RunAcceleration = 30.0f;
	[Export] public float JumpForce = 4.5f;
	[Export] public float MouseSensitivity = 0.3f;
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float CoyoteTime = 0.18f;
	[Export] public float JumpBufferTime = 0.18f;

	// COMBAT - LIGHT ATTACK (base values before stat bonuses)
	[Export] public float LightSlashDamage = 15.0f;
	[Export] public float LightSlashKnockback = 5.0f;
	[Export] public float LightAttackCooldown = 0.5f;

	// COMBAT - HEAVY ATTACK
	[Export] public float HeavySlashDamage = 35.0f;
	[Export] public float HeavySlashKnockback = 15.0f;
	[Export] public float HeavyAttackCooldown = 1.2f;
	[Export] public float HeavyStaminaCost = 30.0f;

	// COMBAT - SPECIAL ATTACK
	[Export] public float SpecialDamage = 50.0f;
	[Export] public float SpecialKnockback = 20.0f;
	[Export] public float SpecialStaminaCost = 60.0f;
	[Export] public float SpecialCooldown = 2.0f;

	// COMBAT - GENERAL (base values before stat bonuses)
	[Export] public float CriticalChance = 0.25f;
	[Export] public float CriticalMultiplier = 1.5f;
	[Export] public float InvincibilityDuration = 0.3f;
	[Export] public float AttackModeWindowTime = 3.0f;

	// DODGE ROLL
	[Export] public float DodgeRollSpeed = 4.5f;
	[Export] public float DodgeRollDuration = 0.48f;
	[Export] public float DodgeRollCooldown = 0.70f;
	[Export] public float DodgeRollStaminaCost = 20.0f;

	// HEALTH & STAMINA (base values before stat bonuses)
	[Export] public float MaxPlayerHealth = 100.0f;
	[Export] public float MaxStamina = 100.0f;
	[Export] public float StaminaDrainRateRun = 30.0f;
	[Export] public float StaminaRegenRate = 15.0f;
	[Export] public float StaminaRegenDelay = 0.5f;

	// SMOOTHING TUNING
	[Export] public float CameraFollowSpeed = 12.0f;
	[Export] public float InputSmoothSpeed = 16.0f;
	[Export] public float FrictionSpeed = 10.0f;
	[Export] public float AnimBlendTime = 0.15f;
	[Export] public float KnockbackDecaySpeed = 8.0f;

	// ─── EFFECTIVE STATS (base + leveling bonuses) ───────────────
	// These are what the game actually uses for all calculations.
	// They read from LevelingSystem every time they're accessed.
	public float EffectiveMaxHealth => MaxPlayerHealth + (LevelingSystem?.GetHealthBonus() ?? 0f);
	public float EffectiveMaxStamina => MaxStamina + (LevelingSystem?.GetStaminaBonus() ?? 0f);
	public float EffectiveCritChance => CriticalChance + (LevelingSystem?.GetCritChanceBonus() ?? 0f);
	public float EffectiveDodgeChance => LevelingSystem?.GetDodgeChanceBonus() ?? 0f;
	public float EffectiveDamageMultiplier => LevelingSystem?.GetDamageMultiplier() ?? 1f;
	public float EffectiveAttackSpeedMult => LevelingSystem?.GetAttackSpeedMultiplier() ?? 1f;

	// PLAYER STATE
	public float _playerHealth = 100.0f;
	public float _stamina = 100.0f;
	private float _staminaEmptyTimer = 0.0f;
	private float _invincibilityTimer = 0.0f;

	private ProgressBar _playerHealthBar, _staminaBar;
	private Label _playerHealthLabel, _staminaLabel;

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

	// LEVELING SYSTEM
	public LevelingSystem LevelingSystem { get; private set; }
	public StatAllocationUI StatsUI { get; private set; }

	private Dictionary<string, string> _animationCache = new Dictionary<string, string>();
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

	// Attack mode system
	public enum AttackMode { None, Light, Heavy, Special }
	public AttackMode _currentAttackMode = AttackMode.None;
	public float _attackModeTimer = 0.0f;

	// FIXED: Track which attack was actually performed for damage calculation
	private AttackMode _lastPerformedAttack = AttackMode.Light;

	// Auto battle system
	public bool _isAutoBattle = false;
	private float _autoAttackTimer = 0.0f;
	private float _autoAttackDelay = 0.8f;
	private float _lastDamageTime = 0.0f;

	private bool _key1WasPressed = false;
	private bool _key2WasPressed = false;
	private bool _key3WasPressed = false;
	private bool _dodgeKeyWasPressed = false;

	private Vector3 _velocity = Vector3.Zero;
	private Vector3 _smoothInputDirection = Vector3.Zero;
	private Vector3 _lastInputDirection = Vector3.Zero;
	private Vector3 _dodgeRollDirection = Vector3.Zero;
	private Vector3 _knockbackVelocity = Vector3.Zero;
	private float _dodgeRollTimer = 0.0f;
	private float _coyoteCounter = 0.0f;
	private float _jumpBufferCounter = 0.0f;
	private bool _isJumping = false;
	private bool _wasInAir = false;

	private HashSet<Enemy> _hitEnemiesThisAttack = new HashSet<Enemy>();
	private float _cameraYaw;
	private float _cameraPitch;
	private float _cameraHeightOffset = 1.5f;
	private Vector3 _smoothCameraPos = Vector3.Zero;
	private string _currentAnimName = "";

	// Slash effect colors per attack type
	private static readonly Color LightSlashColor = new Color(0.8f, 0.9f, 1.0f, 0.6f);   // White-blue
	private static readonly Color HeavySlashColor = new Color(1.0f, 0.6f, 0.1f, 0.7f);   // Orange
	private static readonly Color SpecialSlashColor = new Color(0.9f, 0.1f, 0.2f, 0.8f); // Red

	/// <summary>
	/// Frame-rate independent exponential decay factor.
	/// Returns a value between 0..1 that produces identical smoothing at any FPS.
	/// Usage: value = Lerp(value, target, ExpDecay(speed, delta))
	/// </summary>
	private static float ExpDecay(float speed, float delta)
	{
		return 1.0f - Mathf.Exp(-speed * delta);
	}

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

		// Initialize leveling system
		LevelingSystem = new LevelingSystem();
		AddChild(LevelingSystem);
		LevelingSystem.LevelUp += OnPlayerLevelUp;

		// Create and add StatAllocationUI
		var statUI = new StatAllocationUI();
		StatsUI = statUI;
		AddChild(statUI);
		statUI.CallDeferred(nameof(StatAllocationUI.InitializeDirectly), LevelingSystem);

		// Hide sword at start
		if (_swordRoot != null)
			_swordRoot.Visible = false;

		InitializeAnimations();
		PlayAnimationSmooth("Idle");
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

		bool isRunning = inputDir != Vector2.Zero && Input.IsKeyPressed(Key.Shift) && CanRun();
		Vector3 moveDirection = _isDodgeRolling ? _dodgeRollDirection : GetCameraRelativeDirection(inputDir);

		if (Input.IsActionJustPressed("ui_accept") && !_isSitting && !_isAttacking && IsOnFloor())
			_jumpBufferCounter = JumpBufferTime;

		bool isGrounded = IsOnFloor();
		bool justLanded = isGrounded && _wasInAir;
		_wasInAir = !isGrounded;

		if (isGrounded)
		{
			_coyoteCounter = CoyoteTime;
			// Smooth ground settle instead of hard snap
			_velocity.Y = Mathf.Lerp(_velocity.Y, 0.0f, ExpDecay(20.0f, dt));
		}

		ApplyGravity(ref _velocity, dt);
		HandleMovement(moveDirection, isRunning, isGrounded, dt);
		HandleJump();

		// Decay knockback smoothly (frame-rate independent)
		_knockbackVelocity = _knockbackVelocity.Lerp(Vector3.Zero, ExpDecay(KnockbackDecaySpeed, dt));
		if (_knockbackVelocity.LengthSquared() < 0.01f)
			_knockbackVelocity = Vector3.Zero;

		// Add knockback to final velocity
		_velocity.X += _knockbackVelocity.X;
		_velocity.Z += _knockbackVelocity.Z;

		if (_isAttacking)
			CheckAttackHits();

		_velocity.Y = Mathf.Clamp(_velocity.Y, -20.0f, float.MaxValue);

		Velocity = _velocity;
		MoveAndSlide();

		// Remove knockback component after MoveAndSlide so it doesn't accumulate
		_velocity.X -= _knockbackVelocity.X;
		_velocity.Z -= _knockbackVelocity.Z;

		UpdateStamina(isRunning, dt);
		UpdateCameraRig();
		UpdateAnimationState(moveDirection, isRunning, _isJumping, justLanded);

		_isJumping = false;
	}

	// ─── ATTACK MODE SELECTION ───────────────────────────────────
	private void HandleAttackModeSelection()
	{
		// Press 1 for Light Attack
		if (Input.IsKeyPressed(Key.Key1) && !_key1WasPressed)
		{
			_key1WasPressed = true;
			_currentAttackMode = AttackMode.Light;
			_attackModeTimer = AttackModeWindowTime;
		}
		if (!Input.IsKeyPressed(Key.Key1))
			_key1WasPressed = false;

		// Press 2 for Heavy Attack
		if (Input.IsKeyPressed(Key.Key2) && !_key2WasPressed)
		{
			_key2WasPressed = true;
			_currentAttackMode = AttackMode.Heavy;
			_attackModeTimer = AttackModeWindowTime;
		}
		if (!Input.IsKeyPressed(Key.Key2))
			_key2WasPressed = false;

		// Press 3 for Special Attack
		if (Input.IsKeyPressed(Key.Key3) && !_key3WasPressed)
		{
			_key3WasPressed = true;
			_currentAttackMode = AttackMode.Special;
			_attackModeTimer = AttackModeWindowTime;
		}
		if (!Input.IsKeyPressed(Key.Key3))
			_key3WasPressed = false;
	}

	// ─── AUTO BATTLE ─────────────────────────────────────────────
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

		// Use effective stats for auto-battle decisions
		float healthPercent = _playerHealth / EffectiveMaxHealth;
		float staminaPercent = _stamina / EffectiveMaxStamina;
		float enemyHealthPercent = nearestEnemy.CurrentHealth / nearestEnemy.MaxHealth;

		float braveryLevel = CalculateBravery(healthPercent, staminaPercent, nearestEnemy);

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
				AttackMode selectedAttack = SelectBraveAttack(healthPercent, staminaPercent, braveryLevel, enemyHealthPercent, distanceToEnemy);
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

	private float CalculateBravery(float healthPercent, float staminaPercent, Enemy enemy)
	{
		float bravery = 0.5f;

		// HEALTH-BASED BRAVERY
		if (healthPercent > 0.9f) { bravery += 0.4f; }
		else if (healthPercent > 0.8f) { bravery += 0.35f; }
		else if (healthPercent > 0.65f) { bravery += 0.2f; }
		else if (healthPercent > 0.5f) { bravery += 0.08f; }
		else if (healthPercent > 0.35f) { bravery -= 0.05f; }
		else if (healthPercent > 0.2f) { bravery -= 0.2f; }
		else if (healthPercent > 0.1f) { bravery -= 0.4f; }
		else if (healthPercent <= 0.08f) { bravery -= 0.6f; }

		// STAMINA-BASED BRAVERY
		if (staminaPercent > 0.85f) { bravery += 0.25f; }
		else if (staminaPercent > 0.65f) { bravery += 0.15f; }
		else if (staminaPercent > 0.45f) { bravery += 0.05f; }
		else if (staminaPercent < 0.1f) { bravery -= 0.2f; }

		// ENEMY STATE
		if (enemy != null && GodotObject.IsInstanceValid(enemy))
		{
			float enemyHealthPercent = enemy.CurrentHealth / enemy.MaxHealth;

			if (enemyHealthPercent < 0.15f) { bravery += 0.5f; }
			else if (enemyHealthPercent < 0.3f) { bravery += 0.35f; }
			else if (enemyHealthPercent < 0.5f) { bravery += 0.2f; }
			else if (enemyHealthPercent > 0.8f) { bravery -= 0.1f; }
		}

		return Mathf.Clamp(bravery, 0.0f, 1.0f);
	}

	private void TacticalMovement(Vector3 dirToEnemy, float distance, float healthPercent, float braveryLevel, bool enemyClosing, float delta)
	{
		Vector3 moveDirection = Vector3.Zero;

		// FLEE: Only when REALLY critical
		if (healthPercent < 0.08f && distance < 5.0f && braveryLevel < 0.15f)
		{
			moveDirection = -dirToEnemy;
		}
		// FAR AWAY: Run straight to enemy
		else if (distance > 4.5f)
		{
			moveDirection = dirToEnemy;
		}
		// CIRCLE: Strafe around enemy for positioning
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
		// AGGRESSIVE CHARGE: Get closer when winning
		else if (distance > 2.5f && braveryLevel > 0.75f && healthPercent > 0.6f)
		{
			moveDirection = dirToEnemy * 1.2f;
		}
		// HUNT: Normal approach
		else if (distance > 2.5f)
		{
			moveDirection = dirToEnemy;
		}
		// ATTACK STANCE: Stop and attack
		else
		{
			moveDirection = Vector3.Zero;
		}

		// Apply movement with frame-rate independent smoothing
		if (moveDirection != Vector3.Zero)
		{
			moveDirection = moveDirection.Normalized();
			Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);
			float targetSpeed = (braveryLevel > 0.7f) ? MaxRunSpeed * 1.1f : MaxRunSpeed;
			Vector3 desiredVelocity = moveDirection * targetSpeed;

			horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, ExpDecay(RunAcceleration * 0.5f, delta));

			_velocity.X = horizontalVelocity.X;
			_velocity.Z = horizontalVelocity.Z;
		}
		else
		{
			Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);
			horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, ExpDecay(FrictionSpeed * 2.0f, delta));
			_velocity.X = horizontalVelocity.X;
			_velocity.Z = horizontalVelocity.Z;
		}
	}

	private void PredictiveDodging(float distanceToEnemy, float healthPercent, float braveryLevel, float delta)
	{
		// REACTIVE DODGE: When recently hit, dodge MORE
		if (_lastDamageTime > 0 && IsOnFloor())
		{
			if (GD.Randf() < 0.90f)
			{
				_jumpBufferCounter = JumpBufferTime;
				return;
			}
		}

		// CRITICAL: When enemy is very close, ALWAYS dodge
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

	private AttackMode SelectBraveAttack(float healthPercent, float staminaPercent, float braveryLevel, float enemyHealthPercent, float distance)
	{
		// CRITICAL HEALTH: Survival mode
		if (healthPercent < 0.08f)
			return AttackMode.Light;

		// VERY LOW STAMINA: Can't afford heavy
		if (staminaPercent < 0.12f)
			return AttackMode.Light;

		// ENEMY ALMOST DEAD: Finish them!
		if (enemyHealthPercent < 0.2f && staminaPercent > 0.4f && braveryLevel > 0.5f)
		{
			if (GD.Randf() < 0.8f) return AttackMode.Special;
			if (GD.Randf() < 0.7f) return AttackMode.Heavy;
		}

		// ENEMY HALF HEALTH: Push harder
		if (enemyHealthPercent < 0.5f && staminaPercent > 0.45f)
		{
			if (GD.Randf() < 0.65f) return AttackMode.Heavy;
			if (staminaPercent > 0.7f && GD.Randf() < 0.4f) return AttackMode.Special;
		}

		// VERY BRAVE: Go all out
		if (braveryLevel > 0.85f)
		{
			if (staminaPercent > 0.65f && GD.Randf() < 0.65f) return AttackMode.Special;
			if (staminaPercent > 0.35f && GD.Randf() < 0.8f) return AttackMode.Heavy;
		}
		else if (braveryLevel > 0.70f)
		{
			if (staminaPercent > 0.5f && GD.Randf() < 0.7f) return AttackMode.Heavy;
			if (staminaPercent > 0.75f && GD.Randf() < 0.45f) return AttackMode.Special;
		}
		else if (braveryLevel > 0.55f)
		{
			if (staminaPercent > 0.45f && GD.Randf() < 0.6f) return AttackMode.Heavy;
			if (staminaPercent > 0.8f && GD.Randf() < 0.3f) return AttackMode.Special;
		}
		else if (braveryLevel > 0.40f)
		{
			if (staminaPercent > 0.5f && GD.Randf() < 0.45f) return AttackMode.Heavy;
			if (staminaPercent > 0.8f && GD.Randf() < 0.2f) return AttackMode.Special;
		}
		else
		{
			if (staminaPercent > 0.6f && healthPercent > 0.6f && GD.Randf() < 0.25f) return AttackMode.Heavy;
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

	// ─── PHYSICS HELPERS ─────────────────────────────────────────

	private void ApplyGravity(ref Vector3 velocity, float delta)
	{
		if (!IsOnFloor())
		{
			// Asymmetric gravity: lighter on the way up, heavier on the way down
			float multiplier = velocity.Y > 0 ? 0.55f : 1.3f;
			velocity.Y -= _gravity * multiplier * delta;
		}
	}

	private void HandleMovement(Vector3 desiredDirection, bool isRunning, bool isGrounded, float delta)
	{
		float maxSpeed = isRunning ? MaxRunSpeed : MaxGroundSpeed;
		float acceleration = isRunning ? RunAcceleration : GroundAcceleration;

		if (_isDodgeRolling)
		{
			// Eased dodge speed: ramp up then ease out over the dodge duration
			float dodgeProgress = 1.0f - (_dodgeRollTimer / DodgeRollDuration);
			float dodgeEase = 1.0f - (dodgeProgress * dodgeProgress); // Quadratic ease-out
			float currentDodgeSpeed = DodgeRollSpeed * Mathf.Max(dodgeEase, 0.3f);

			Vector3 horizontalVelocity = _dodgeRollDirection * currentDodgeSpeed;
			_velocity.X = horizontalVelocity.X;
			_velocity.Z = horizontalVelocity.Z;
			return;
		}

		if (desiredDirection != Vector3.Zero)
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

		Vector3 horizontalVel = new Vector3(_velocity.X, 0, _velocity.Z);

		if (desiredDirection != Vector3.Zero)
		{
			// Smooth the raw input direction (frame-rate independent)
			_smoothInputDirection = _smoothInputDirection.Lerp(desiredDirection, ExpDecay(InputSmoothSpeed, delta));
			_lastInputDirection = _smoothInputDirection;

			Vector3 desiredVelocity = _smoothInputDirection * maxSpeed;
			float speed = horizontalVel.Length();
			float desiredSpeed = desiredVelocity.Length();

			// Separate accel/decel curves for better game feel
			float lerpFactor;
			if (speed < desiredSpeed)
				lerpFactor = ExpDecay(acceleration * 0.5f, delta);  // Accelerating
			else
				lerpFactor = ExpDecay(acceleration * 0.25f, delta); // Decelerating (gentler)

			horizontalVel = horizontalVel.Lerp(desiredVelocity, lerpFactor);

			RotateTowardDirection(desiredDirection, delta);
		}
		else
		{
			_smoothInputDirection = Vector3.Zero;
			_lastInputDirection = _lastInputDirection.Lerp(Vector3.Zero, ExpDecay(InputSmoothSpeed * 0.5f, delta));

			// Frame-rate independent friction
			float frictionFactor = isGrounded ? FrictionSpeed : FrictionSpeed * 0.6f;
			horizontalVel = horizontalVel.Lerp(Vector3.Zero, ExpDecay(frictionFactor, delta));
		}

		_velocity.X = horizontalVel.X;
		_velocity.Z = horizontalVel.Z;
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

		// Variable jump height: gentler cut for smoother arc
		if (_isJumping && !Input.IsActionPressed("ui_accept") && _velocity.Y > 0)
		{
			_velocity.Y *= 0.65f;
			_isJumping = false;
		}
	}

	private void HandleDodgeRoll(float delta)
	{
		// Track dodge timer while rolling
		if (_isDodgeRolling)
		{
			_dodgeRollTimer -= delta;
			if (_dodgeRollTimer <= 0)
			{
				_isDodgeRolling = false;
				_dodgeRollCooldownTimer = DodgeRollCooldown;
				// Preserve some momentum after dodge (smooth exit)
				_velocity.X *= 0.6f;
				_velocity.Z *= 0.6f;
				_animPlayer.SpeedScale = 1.0f;
			}
			return;
		}

		if (Input.IsActionPressed("dodge") && !_dodgeKeyWasPressed)
		{
			_dodgeKeyWasPressed = true;

			if (!IsOnFloor() || _dodgeRollCooldownTimer > 0 || !HasStaminaForAction(DodgeRollStaminaCost))
			{
				_dodgeKeyWasPressed = false;
				return;
			}

			_isDodgeRolling = true;
			_dodgeRollDirection = _lastInputDirection.LengthSquared() > 0.01f
				? _lastInputDirection.Normalized()
				: (-_camera.GlobalTransform.Basis.Z with { Y = 0 }).Normalized();
			_dodgeRollTimer = DodgeRollDuration;

			_stamina -= DodgeRollStaminaCost;
			_staminaEmptyTimer = StaminaRegenDelay;
			_invincibilityTimer = DodgeRollDuration;

			if (_animationCache.TryGetValue("DodgeSlide", out string dodgeAnimPath))
			{
				PlayAnimationSmooth("DodgeSlide");
				_animPlayer.SpeedScale = 1.2f;
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
		// Frame-rate independent rotation
		float newYaw = Mathf.LerpAngle(Rotation.Y, targetYaw, ExpDecay(RotationSpeed, delta));
		Rotation = new Vector3(Rotation.X, newYaw, Rotation.Z);
	}

	private void UpdateCameraRig()
	{
		// Frame-rate independent camera follow
		Vector3 targetCamPos = GlobalPosition + Vector3.Up * _cameraHeightOffset;
		float camDt = (float)GetProcessDeltaTime();
		if (camDt <= 0) camDt = (float)GetPhysicsProcessDeltaTime();
		_smoothCameraPos = _smoothCameraPos.Lerp(targetCamPos, ExpDecay(CameraFollowSpeed, camDt));
		_springArm.GlobalPosition = _smoothCameraPos;

		_springArm.GlobalRotation = new Vector3(_cameraPitch, _cameraYaw, 0.0f);
	}

	// ─── ANIMATIONS ──────────────────────────────────────────────

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

	/// <summary>
	/// Cross-fade to animation with blend time. Prevents popping between states.
	/// </summary>
	private void PlayAnimationSmooth(string name, float customBlend = -1.0f)
	{
		if (!_animationCache.TryGetValue(name, out string path)) return;
		if (_currentAnimName == name) return;

		_currentAnimName = name;
		float blend = customBlend >= 0 ? customBlend : AnimBlendTime;

		// Use cross-fade for smooth transitions
		if (_animPlayer.CurrentAnimation != "" && blend > 0)
		{
			_animPlayer.Play(path, blend);
		}
		else
		{
			_animPlayer.Play(path);
		}

		// Default speed — attack-specific speed is set in the Perform methods
		_animPlayer.SpeedScale = 1.0f;
	}

	// Keep old name for compatibility but route through smooth version
	private void PlayAnimation(string name)
	{
		PlayAnimationSmooth(name);
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

	// ─── ATTACK SYSTEM (FIXED: uses _lastPerformedAttack) ────────

	private void HandleAttack()
	{
		if (_isAttacking || _isDodgeRolling)
		{
			if (!_animPlayer.IsPlaying())
			{
				_isAttacking = false;
				_hitEnemiesThisAttack.Clear();
				_currentAnimName = "";  // Reset so next anim can cross-fade
				_animPlayer.SpeedScale = 1.0f;
			}
			return;
		}

		if (!Input.IsActionJustPressed("attack"))
			return;

		if (!_isSwordEquipped)
			return;

		switch (_currentAttackMode)
		{
			case AttackMode.Light:
				if (CanAttack()) PerformLightAttack();
				break;
			case AttackMode.Heavy:
				if (CanHeavyAttack()) PerformHeavyAttack();
				break;
			case AttackMode.Special:
				if (CanSpecialAttack()) PerformSpecialAttack();
				break;
			case AttackMode.None:
				if (CanAttack()) PerformLightAttack();
				break;
		}
	}

	private bool CanAttack() => !_isSitting && _lightAttackCooldownTimer <= 0 && HasStaminaForAction(5) && IsOnFloor() && _isSwordEquipped;
	private bool CanHeavyAttack() => !_isSitting && _heavyAttackCooldownTimer <= 0 && HasStaminaForAction(HeavyStaminaCost) && IsOnFloor() && _isSwordEquipped;
	private bool CanSpecialAttack() => !_isSitting && _specialAttackCooldownTimer <= 0 && HasStaminaForAction(SpecialStaminaCost) && IsOnFloor() && _isSwordEquipped;

	private void PerformLightAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_lastPerformedAttack = AttackMode.Light;

		// Apply attack speed bonus from stat points
		float cooldown = LightAttackCooldown * EffectiveAttackSpeedMult;
		_lightAttackCooldownTimer = cooldown;
		_currentAttackMode = AttackMode.None;

		if (_comboTimer > 0)
			_comboCount++;
		else
			_comboCount = 1;
		_comboTimer = 0.5f;

		PlayAnimationSmooth("SwordSlash", 0.08f);  // Fast blend into attack
		_animPlayer.SpeedScale = 1.3f;  // Light = fastest swing
		CreateSlashEffect(LightSlashColor, 80f);
	}

	private void PerformHeavyAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_lastPerformedAttack = AttackMode.Heavy;

		float cooldown = HeavyAttackCooldown * EffectiveAttackSpeedMult;
		_heavyAttackCooldownTimer = cooldown;
		_stamina -= HeavyStaminaCost;
		_staminaEmptyTimer = StaminaRegenDelay;
		_currentAttackMode = AttackMode.None;

		PlayAnimationSmooth("SwordSlash", 0.08f);
		_animPlayer.SpeedScale = 0.85f;  // Heavy = slow powerful windup
		CreateSlashEffect(HeavySlashColor, 120f);
		ShakeScreen(0.1f, 0.1f);  // Slight shake on heavy windup
	}

	private void PerformSpecialAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_lastPerformedAttack = AttackMode.Special;

		float cooldown = SpecialCooldown * EffectiveAttackSpeedMult;
		_specialAttackCooldownTimer = cooldown;
		_stamina -= SpecialStaminaCost;
		_staminaEmptyTimer = StaminaRegenDelay;
		_currentAttackMode = AttackMode.None;

		PlayAnimationSmooth("SwordSlash", 0.08f);
		_animPlayer.SpeedScale = 0.7f;  // Special = slowest, most dramatic
		CreateSlashEffect(SpecialSlashColor, 160f);
		ShakeScreen(0.18f, 0.25f);  // Bigger shake for special
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

				// FIXED: Pass the actual attack type for correct damage/knockback
				float damage = CalculateDamage(_lastPerformedAttack, out bool isCritical);
				float knockback = GetKnockbackForAttack(_lastPerformedAttack);

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

	/// <summary>
	/// FIXED: Calculate damage based on the actual attack type performed,
	/// then apply stat bonuses from LevelingSystem.
	/// </summary>
	private float CalculateDamage(AttackMode attackType, out bool isCritical)
	{
		// Pick base damage from the attack type that was actually performed
		float baseDamage = attackType switch
		{
			AttackMode.Light => LightSlashDamage,
			AttackMode.Heavy => HeavySlashDamage,
			AttackMode.Special => SpecialDamage,
			_ => LightSlashDamage
		};

		// Apply damage multiplier from stat points
		baseDamage *= EffectiveDamageMultiplier;

		// Apply combo bonus
		if (_comboCount >= 2)
		{
			baseDamage *= (1.0f + (_comboCount * 0.5f));
		}

		// Apply crit chance from stat points
		isCritical = GD.Randf() < EffectiveCritChance;
		if (isCritical)
		{
			baseDamage *= CriticalMultiplier;
		}

		return baseDamage;
	}

	/// <summary>
	/// FIXED: Return knockback based on the actual attack type performed.
	/// </summary>
	private float GetKnockbackForAttack(AttackMode attackType)
	{
		return attackType switch
		{
			AttackMode.Light => LightSlashKnockback,
			AttackMode.Heavy => HeavySlashKnockback,
			AttackMode.Special => SpecialKnockback,
			_ => LightSlashKnockback
		};
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

		PlayAnimationSmooth(anim);
	}

	// ─── HEALTH & STAMINA (now uses Effective values) ────────────

	private void CreatePlayerHealthBar()
	{
		_playerHealth = EffectiveMaxHealth;
		_stamina = EffectiveMaxStamina;

		(_playerHealthBar, _playerHealthLabel) = HealthBarFactory.CreateScreenBar(
			this, EffectiveMaxHealth, 50, "Health", new Color(0.2f, 1.0f, 0.2f, 0.8f));

		(_staminaBar, _staminaLabel) = HealthBarFactory.CreateScreenBar(
			this, EffectiveMaxStamina, 90, "Stamina", new Color(1.0f, 1.0f, 0.2f, 0.8f));
	}

	public void PlayerTakeDamage(float damage)
	{
		if (_isGameOver || _invincibilityTimer > 0.0f) return;

		// Apply dodge chance from stat points — chance to completely avoid the hit
		if (GD.Randf() < EffectiveDodgeChance)
		{
			ShowDodgeText();
			return;
		}

		_playerHealth = Mathf.Max(0, _playerHealth - damage);
		_invincibilityTimer = InvincibilityDuration;
		_lastDamageTime = 0.3f;

		if (_playerHealthBar != null) _playerHealthBar.Value = _playerHealth;
		if (_playerHealthLabel != null) _playerHealthLabel.Text = $"Health: {_playerHealth:F0} / {EffectiveMaxHealth:F0}";

		ShowDamageNumber(damage);

		// Smooth knockback instead of instant velocity add
		Node3D enemy = GetTree().GetFirstNodeInGroup("enemy") as Node3D;
		if (enemy != null)
		{
			Vector3 knockbackDir = (GlobalPosition - enemy.GlobalPosition).Normalized();
			knockbackDir.Y = 0;
			_knockbackVelocity += knockbackDir.Normalized() * 5.0f;
		}

		if (_playerHealth <= 0.0f)
			GameOver();
	}

	/// <summary>
	/// Called when player levels up — restore HP/Stamina to new effective max
	/// </summary>
	private void OnPlayerLevelUp()
	{
		_playerHealth = EffectiveMaxHealth;
		_stamina = EffectiveMaxStamina;

		if (_playerHealthBar != null)
		{
			_playerHealthBar.MaxValue = EffectiveMaxHealth;
			_playerHealthBar.Value = _playerHealth;
		}
		if (_staminaBar != null)
		{
			_staminaBar.MaxValue = EffectiveMaxStamina;
			_staminaBar.Value = _stamina;
		}
		if (_playerHealthLabel != null) _playerHealthLabel.Text = $"Health: {_playerHealth:F0} / {EffectiveMaxHealth:F0}";
		if (_staminaLabel != null) _staminaLabel.Text = $"Stamina: {_stamina:F0} / {EffectiveMaxStamina:F0}";
	}

	/// <summary>
	/// Show a cyan "DODGE!" popup when a hit is avoided via dodge stat
	/// </summary>
	private void ShowDodgeText()
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Label label = new Label();
		label.Text = "DODGE!";
		label.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 1f, 1));
		label.AddThemeFontSizeOverride("font_size", 30);
		label.ZIndex = 100;

		Vector3 worldPos = GlobalPosition + Vector3.Up * 2.0f;
		label.GlobalPosition = camera.UnprojectPosition(worldPos);

		GetTree().Root.AddChild(label);

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);
		Vector2 startPos = label.GlobalPosition;
		tween.Parallel().TweenProperty(label, "global_position", startPos - Vector2.Up * 50f, 0.8f);
		tween.Parallel().TweenProperty(label, "modulate", new Color(1, 1, 1, 0), 0.8f);
		tween.TweenCallback(Callable.From(() => {
			if (IsInstanceValid(label)) label.QueueFree();
		}));

		GetTree().CreateTimer(1.5f).Timeout += () => {
			if (IsInstanceValid(label)) label.QueueFree();
		};
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
		label.GlobalPosition = camera.UnprojectPosition(worldPos);

		GetTree().Root.AddChild(label);

		// Smooth ease-out float + fade
		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);
		Vector2 startPos = label.GlobalPosition;

		float moveDistance = isCritical ? 80.0f : 60.0f;
		tween.Parallel().TweenProperty(label, "global_position", startPos - Vector2.Up * moveDistance, 1.0f);
		tween.Parallel().TweenProperty(label, "modulate", new Color(1, 1, 1, 0), 1.0f);

		tween.TweenCallback(Callable.From(() => {
			if (IsInstanceValid(label)) label.QueueFree();
		}));

		GetTree().CreateTimer(2.0f).Timeout += () => {
			if (IsInstanceValid(label)) label.QueueFree();
		};
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

	/// <summary>
	/// Slash effect with color and size that varies per attack type.
	/// Light = small white-blue, Heavy = medium orange, Special = large red.
	/// </summary>
	private void CreateSlashEffect(Color slashColor, float finalSize)
	{
		if (_swordRoot == null) return;

		Camera3D cam = GetViewport().GetCamera3D();
		if (cam == null) return;

		var slashEffect = new Panel();
		slashEffect.ZIndex = -1;

		var slashStyle = new StyleBoxFlat { BgColor = slashColor };
		slashEffect.AddThemeStyleboxOverride("panel", slashStyle);

		Vector2 screenPos = cam.UnprojectPosition(_swordRoot.GlobalPosition);
		float startSize = finalSize * 0.35f;

		slashEffect.GlobalPosition = screenPos - Vector2.One * (startSize * 0.5f);
		slashEffect.CustomMinimumSize = Vector2.One * startSize;

		GetTree().Root.AddChild(slashEffect);

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.Out);

		tween.Parallel().TweenProperty(slashEffect, "custom_minimum_size", Vector2.One * finalSize, 0.35f);
		tween.Parallel().TweenProperty(slashEffect, "modulate", new Color(1, 1, 1, 0), 0.35f);

		tween.TweenCallback(Callable.From(() => {
			if (IsInstanceValid(slashEffect)) slashEffect.QueueFree();
		}));
	}

	private void UpdateStamina(bool isRunning, float delta)
	{
		float maxStam = EffectiveMaxStamina;

		if (_isAutoBattle && _lastInputDirection != Vector3.Zero && IsOnFloor())
		{
			_stamina = Mathf.Max(0, _stamina - StaminaDrainRateRun * 0.5f * delta);
			_staminaEmptyTimer = 0.3f;
		}
		else if (isRunning && IsOnFloor())
		{
			_stamina = Mathf.Max(0, _stamina - StaminaDrainRateRun * delta);
			_staminaEmptyTimer = 0.5f;
		}
		else if (_isAttacking)
		{
			_staminaEmptyTimer = 0.2f;
		}
		else
		{
			_staminaEmptyTimer = Mathf.Max(0, _staminaEmptyTimer - delta);
			if (_staminaEmptyTimer <= 0 && !isRunning && !_isAttacking)
				_stamina = Mathf.Min(maxStam, _stamina + StaminaRegenRate * delta);
		}

		_stamina = Mathf.Clamp(_stamina, 0, maxStam);

		if (_staminaBar != null) _staminaBar.Value = _stamina;
		if (_staminaLabel != null) _staminaLabel.Text = $"Stamina: {_stamina:F0} / {maxStam:F0}";
	}

	private bool HasStaminaForAction(float cost) => _stamina >= cost;
	public float GetStamina() => _stamina;
	public bool CanRun() => _stamina > 0.0f;
	public float GetPlayerHealth() => _playerHealth;
	public bool IsPlayerAlive() => _playerHealth > 0.0f;

	// ─── GAME OVER ───────────────────────────────────────────────

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
}
