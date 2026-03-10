using Godot;
using System.Collections.Generic;

public partial class Player3D : CharacterBody3D
{
	// MOVEMENT
	[Export] public float MaxGroundSpeed = 5.0f;
	[Export] public float MaxRunSpeed = 8.5f;
	[Export] public float GroundAcceleration = 20.0f;
	[Export] public float RunAcceleration = 30.0f;
	[Export] public float JumpForce = 4.0f;
	[Export] public float MouseSensitivity = 0.3f;
	[Export] public float RotationSpeed = 12.0f;
	[Export] public float CoyoteTime = 0.12f;
	[Export] public float JumpBufferTime = 0.12f;

	// COMBAT - LIGHT ATTACK
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

	// COMBAT - GENERAL
	[Export] public float CriticalChance = 0.25f;
	[Export] public float CriticalMultiplier = 1.5f;
	[Export] public float InvincibilityDuration = 0.3f;
	[Export] public float AttackModeWindowTime = 3.0f;

	// DODGE ROLL
	[Export] public float DodgeRollSpeed = 4.0f;  // ✨ Very short slide now
	[Export] public float DodgeRollDuration = 0.5f;
	[Export] public float DodgeRollCooldown = 0.8f;
	[Export] public float DodgeRollStaminaCost = 20.0f;

	// HEALTH & STAMINA
	[Export] public float MaxPlayerHealth = 100.0f;
	[Export] public float MaxStamina = 100.0f;
	[Export] public float StaminaDrainRateRun = 30.0f;
	[Export] public float StaminaRegenRate = 15.0f;
	[Export] public float StaminaRegenDelay = 0.5f;

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
	private const string DodgeSlideFbxPath = "res://models/player/Running_Slide.fbx";  // ✨ NEW

	private readonly float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

	private Camera3D _camera;
	private SpringArm3D _springArm;
	private AnimationPlayer _animPlayer;
	private Node3D _swordRoot;

	// ✨ LEVELING SYSTEM
	public LevelingSystem LevelingSystem { get; private set; }

	private Dictionary<string, string> _animationCache = new Dictionary<string, string>();
	private string _dodgeSlideAnim = "";  // ✨ NEW: Store dodge animation name
	private bool _isSwordEquipped = false;  // ✨ SWORD NOT EQUIPPED AT START - REQUIRE MANUAL EQUIP!
	private bool _isSitting = false;
	private bool _isAttacking = false;
	private bool _isDodgeRolling = false;
	private bool _isGameOver = false;
	
	public float _lightAttackCooldownTimer = 0.0f;  // ✨ SEPARATE: Light cooldown
	public float _heavyAttackCooldownTimer = 0.0f;  // ✨ SEPARATE: Heavy cooldown
	private float _dodgeRollCooldownTimer = 0.0f;
	public float _specialAttackCooldownTimer = 0.0f;
	private float _comboTimer = 0.0f;
	private int _comboCount = 0;

	// ✨ NEW: Attack mode system
	public enum AttackMode
	{
		None,
		Light,
		Heavy,
		Special
	}
	public AttackMode _currentAttackMode = AttackMode.None;
	public float _attackModeTimer = 0.0f;

	// ✨ NEW: Auto battle system
	public bool _isAutoBattle = false;
	private float _autoAttackTimer = 0.0f;
	private float _autoAttackDelay = 0.8f;  // Time between auto attacks
	private float _lastDamageTime = 0.0f;  // ✨ Track when last hit, dodge MORE when recently hit!

	private bool _key1WasPressed = false;
	private bool _key2WasPressed = false;
	private bool _key3WasPressed = false;
	private bool _dodgeKeyWasPressed = false;  // ✨ NEW: Track dodge key state

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
		_springArm.TopLevel = true;

		// ✨ NEW: Initialize leveling system
		LevelingSystem = new LevelingSystem();
		AddChild(LevelingSystem);
		LevelingSystem.LevelUp += OnPlayerLevelUp;  // Connect level-up callback

		// ✨ NEW: Create and add StatAllocationUI (level-up popup)
		var statUI = new StatAllocationUI();
		AddChild(statUI);  // ✨ FIXED: Add to Player3D instead of Root
		// ✨ Use CallDeferred to initialize AFTER scene tree is ready
		statUI.CallDeferred(nameof(StatAllocationUI.InitializeDirectly), LevelingSystem);
		GD.Print("[Player3D] ✅ StatAllocationUI created and queued for initialization");

		// ✨ Hide sword at start (not equipped yet)
		if (_swordRoot != null)
			_swordRoot.Visible = false;

		InitializeAnimations();
		// CreatePlayerHealthBar();  // ✨ DISABLED - Using SkillUIManager instead!
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
		_lightAttackCooldownTimer = Mathf.Max(0, _lightAttackCooldownTimer - dt);  // ✨ SEPARATE
		_heavyAttackCooldownTimer = Mathf.Max(0, _heavyAttackCooldownTimer - dt);  // ✨ SEPARATE
		_dodgeRollCooldownTimer = Mathf.Max(0, _dodgeRollCooldownTimer - dt);
		_specialAttackCooldownTimer = Mathf.Max(0, _specialAttackCooldownTimer - dt);
		_comboTimer = Mathf.Max(0, _comboTimer - dt);
		_jumpBufferCounter -= dt;
		_coyoteCounter -= dt;
		_lastDamageTime = Mathf.Max(0, _lastDamageTime - dt);  // ✨ Track time since last hit

		// ✨ NEW: Update attack mode timer
		if (_attackModeTimer > 0)
		{
			_attackModeTimer -= dt;
		}
		else
		{
			_currentAttackMode = AttackMode.None;
		}

		// ✨ NEW: Handle attack mode selection
		HandleAttackModeSelection();

		// ✨ NEW: Handle auto battle
		HandleAutoBattle(dt);

		HandleSwordToggle();
		HandleSitToggle();
		HandleDodgeRoll(dt);
		HandleAttack();

		Vector2 inputDir = (_isSitting || _isAttacking) ? Vector2.Zero : 
			Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		bool isRunning = inputDir != Vector2.Zero && Input.IsKeyPressed(Key.Shift) && CanRun();
		Vector3 moveDirection = _isDodgeRolling ? _dodgeRollDirection : GetCameraRelativeDirection(inputDir);

		// ✨ IMPROVED: Allow jump input during dodge!
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

		UpdateStamina(isRunning, dt);
		UpdateCameraRig();
		UpdateAnimationState(moveDirection, isRunning, _isJumping, justLanded);

		_isJumping = false;
	}

	// ✨ NEW: Handle attack mode selection with number keys
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

	// ✨ ULTIMATE AUTO BATTLE: Smart, Brave, Aggressive, Smooth, Tactical!
	private void HandleAutoBattle(float delta)
	{
		if (!_isAutoBattle || _isGameOver || !_isSwordEquipped)
		{
			_autoAttackTimer = 0;
			return;
		}

		// Find nearest enemy
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

		// ✨ GET SMART STATS
		float healthPercent = _playerHealth / MaxPlayerHealth;
		float staminaPercent = _stamina / MaxStamina;
		float enemyHealthPercent = nearestEnemy.CurrentHealth / nearestEnemy.MaxHealth;

		// ✨ BRAVERY SYSTEM: More aggressive when winning
		float braveryLevel = CalculateBravery(healthPercent, staminaPercent, nearestEnemy);

		// ✨ DEBUG: Print main stats every 0.5 seconds
		bool enemyClosing = distanceToEnemy < 4.5f;
		
		// ✨ TACTICAL MOVEMENT WITH CIRCLING
		TacticalMovement(dirToEnemyNorm, distanceToEnemy, healthPercent, braveryLevel, enemyClosing, delta);

		// ✨ FACE ENEMY SMOOTHLY
		if (dirToEnemyNorm != Vector3.Zero)
		{
			RotateTowardDirection(dirToEnemyNorm, delta * 1.2f);  // Slightly faster turning for better responsiveness
		}

		// ✨ DODGING: Always try to dodge when enemy is close (not just when not attacking)
		if (enemyClosing && IsOnFloor())
		{
			PredictiveDodging(distanceToEnemy, healthPercent, braveryLevel, delta);
		}

		// ✨ AUTO ATTACK LOGIC - Aggressive when in range!
		if (distanceToEnemy <= 3.8f)  // Slightly larger range for more aggression
		{
			_autoAttackTimer -= delta;

			if (_autoAttackTimer <= 0 && !_isAttacking && IsOnFloor())
			{
				// Choose BEST attack based on situation
				AttackMode selectedAttack = SelectBraveAttack(healthPercent, staminaPercent, braveryLevel, enemyHealthPercent, distanceToEnemy);
				_currentAttackMode = selectedAttack;

				bool didAttack = false;

				// Execute attack - prioritize special/heavy when winning!
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
					_autoAttackTimer = _autoAttackDelay - 0.1f;  // Slightly faster light attacks
					didAttack = true;
				}

				// Faster retry on failed attack
				if (!didAttack)
				{
					_autoAttackTimer = 0.08f;  // Much faster retry
				}
			}
		}
		else if (distanceToEnemy > 4.0f)
		{
			// Reset timer when too far away
			_autoAttackTimer = 0;
		}
	}

	// ✨ ENHANCED: Calculate bravery level (0.0 = coward, 1.0 = fearless warrior!)
	private float CalculateBravery(float healthPercent, float staminaPercent, Enemy enemy)
	{
		float bravery = 0.5f;  // Start neutral

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

		// ENEMY STATE - Are we WINNING?
		if (enemy != null && GodotObject.IsInstanceValid(enemy))
		{
			float enemyHealthPercent = enemy.CurrentHealth / enemy.MaxHealth;

			// Enemy almost dead = VERY BRAVE! Go for the kill!
			if (enemyHealthPercent < 0.15f) { bravery += 0.5f; }
			else if (enemyHealthPercent < 0.3f) { bravery += 0.35f; }
			else if (enemyHealthPercent < 0.5f) { bravery += 0.2f; }
			else if (enemyHealthPercent > 0.8f) { bravery -= 0.1f; }
		}

		float finalBravery = Mathf.Clamp(bravery, 0.0f, 1.0f);
		
		return finalBravery;
	}

	// ✨ ENHANCED: Tactical movement with smart circling and positioning
	private void TacticalMovement(Vector3 dirToEnemy, float distance, float healthPercent, float braveryLevel, bool enemyClosing, float delta)
	{
		Vector3 moveDirection = Vector3.Zero;
		string movementType = "";

		// FLEE: Only when REALLY critical
		if (healthPercent < 0.08f && distance < 5.0f && braveryLevel < 0.15f)
		{
			moveDirection = -dirToEnemy;
			movementType = "🚫 FLEEING!";
		}
		// FAR AWAY (> 4.5m): Just run straight to enemy!
		else if (distance > 4.5f)
		{
			moveDirection = dirToEnemy;
			movementType = "🏃 HUNTING (far)";
		}
		// CIRCLE: When fighting - move around enemy for better positioning
		else if (distance > 2.2f && distance <= 4.5f && enemyClosing && braveryLevel > 0.3f)
		{
			// Smart circling - strafe around enemy
			Vector3 sideDirection = dirToEnemy.Cross(Vector3.Up).Normalized();
			// 60% chance to circle, 40% chance to move closer
			if (GD.Randf() < 0.6f)
			{
				// Alternate between left and right circle
				if ((int)GD.Randi() % 2 == 0)
					sideDirection = -sideDirection;
				moveDirection = sideDirection;
				movementType = "🔄 CIRCLING";
			}
			else
			{
				moveDirection = dirToEnemy * 0.7f;  // Move closer while circling
				movementType = "🔄 CIRCLE→CLOSER";
			}
		}
		// AGGRESSIVE CHARGE: Get closer when winning!
		else if (distance > 2.5f && braveryLevel > 0.75f && healthPercent > 0.6f)
		{
			moveDirection = dirToEnemy * 1.2f;  // Faster aggression
			movementType = "⚡ AGGRESSIVE";
		}
		// HUNT: Normal approach to enemy
		else if (distance > 2.5f)
		{
			moveDirection = dirToEnemy;
			movementType = "🏃 HUNTING";
		}
		// ATTACK STANCE: Stop and prepare to attack
		else
		{
			moveDirection = Vector3.Zero;
			movementType = "⚔️ ATTACKING";
		}

		// Debug movement
		if (!string.IsNullOrEmpty(movementType))
		{
			// Silenced debug output
		}

		// Apply movement with SMOOTH acceleration and deceleration
		if (moveDirection != Vector3.Zero)
		{
			moveDirection = moveDirection.Normalized();
			Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);
			float targetSpeed = (braveryLevel > 0.7f) ? MaxRunSpeed * 1.1f : MaxRunSpeed;  // Faster when brave!
			Vector3 desiredVelocity = moveDirection * targetSpeed;
			
			// Smoother acceleration
			horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, RunAcceleration * delta * 1.2f);

			_velocity.X = horizontalVelocity.X;
			_velocity.Z = horizontalVelocity.Z;
		}
		else
		{
			// Smooth deceleration when stopping
			Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);
			horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, RunAcceleration * 2.0f * delta);  // 2x faster decel for snappier combat
			_velocity.X = horizontalVelocity.X;
			_velocity.Z = horizontalVelocity.Z;
		}
	}

	// ✨ AGGRESSIVE DODGING: Much more responsive to danger!
	private void PredictiveDodging(float distanceToEnemy, float healthPercent, float braveryLevel, float delta)
	{
		// ⚠️ REACTIVE DODGE: When recently hit, dodge MORE!
		if (_lastDamageTime > 0 && IsOnFloor())
		{
			// 90% chance to dodge for 0.3 seconds after being hit!
			if (GD.Randf() < 0.90f)
			{
				_jumpBufferCounter = JumpBufferTime;
				return;
			}
		}

		// ⚠️ CRITICAL: When enemy is very close, ALWAYS dodge!
		if (distanceToEnemy < 2.0f && IsOnFloor())
		{
			// 80% chance to dodge when enemy is about to attack
			if (GD.Randf() < 0.80f)
			{
				_jumpBufferCounter = JumpBufferTime;
				return;
			}
		}

		// High dodge chance system - MUCH more aggressive than before
		float baseDodgeChance = 0.15f;  // 15% base (was 6% - TRIPLED!)
		
		// ALWAYS dodge when threatened
		if (healthPercent < 0.75f) baseDodgeChance = 0.20f;  // 20% when damaged
		if (healthPercent < 0.60f) baseDodgeChance = 0.25f;  // 25% when more damaged
		if (healthPercent < 0.50f) baseDodgeChance = 0.30f;  // 30% when half health
		if (healthPercent < 0.35f) baseDodgeChance = 0.40f;  // 40% when low
		if (healthPercent < 0.20f) baseDodgeChance = 0.50f;  // 50% when critical
		if (healthPercent < 0.10f) baseDodgeChance = 0.60f;  // 60% when almost dead

		// Distance multipliers - dodge MORE when enemy is close!
		float distanceMultiplier = 1.0f;
		if (distanceToEnemy < 3.5f) distanceMultiplier = 2.0f;   // 2x when close
		if (distanceToEnemy < 2.5f) distanceMultiplier = 3.0f;   // 3x when very close
		if (distanceToEnemy < 1.8f) distanceMultiplier = 4.0f;   // 4x when VERY VERY close

		// Bravery affects dodge slightly (brave people dodge LESS, not more - they're confident)
		float braveryModifier = 1.0f - (braveryLevel * 0.1f);  // Reduced impact: only -10% when hero
		braveryModifier = Mathf.Max(braveryModifier, 0.7f);     // Never less than 70% dodge chance

		// Final calculation
		float finalDodgeChance = baseDodgeChance * distanceMultiplier * braveryModifier;
		finalDodgeChance = Mathf.Min(finalDodgeChance, 0.85f);  // Cap at 85% (can't dodge every frame)

		// Guaranteed minimum dodge when very close
		if (distanceToEnemy < 3.0f)
		{
			finalDodgeChance = Mathf.Max(finalDodgeChance, 0.25f);  // At least 25% chance
		}

		// Execute dodge
		if (GD.Randf() < finalDodgeChance && IsOnFloor())
		{
			_jumpBufferCounter = JumpBufferTime;
		}
	}

	// ✨ ENHANCED: Brave attack selection - MORE AGGRESSIVE!
	private AttackMode SelectBraveAttack(float healthPercent, float staminaPercent, float braveryLevel, float enemyHealthPercent, float distance)
	{
		string reason = "";
		AttackMode selected = AttackMode.Light;

		// CRITICAL HEALTH: Survival mode - light attacks only!
		if (healthPercent < 0.08f)
		{
			reason = "CRITICAL HEALTH (survival mode)";
			return AttackMode.Light;
		}

		// VERY LOW STAMINA: Can't do heavy attacks
		if (staminaPercent < 0.12f)
		{
			reason = "LOW STAMINA (regenerating)";
			return AttackMode.Light;
		}

		// ✨ ENEMY ALMOST DEAD: FINISH THEM! Go all out!
		if (enemyHealthPercent < 0.2f && staminaPercent > 0.4f && braveryLevel > 0.5f)
		{
			reason = "ENEMY ALMOST DEAD (finish them!)";
			if (GD.Randf() < 0.8f) { selected = AttackMode.Special; reason += " → SPECIAL (80%)"; return selected; }
			if (GD.Randf() < 0.7f) { selected = AttackMode.Heavy; reason += " → HEAVY (70%)"; return selected; }
		}

		// ✨ ENEMY HALF HEALTH: Push harder!
		if (enemyHealthPercent < 0.5f && staminaPercent > 0.45f)
		{
			reason = "ENEMY WEAK (pushing harder)";
			if (GD.Randf() < 0.65f) { selected = AttackMode.Heavy; reason += " → HEAVY"; return selected; }
			if (staminaPercent > 0.7f && GD.Randf() < 0.4f) { selected = AttackMode.Special; reason += " → SPECIAL"; return selected; }
		}

		// ✨ VERY BRAVE (WINNING!): Go all out with aggression!
		if (braveryLevel > 0.85f)
		{
			reason = "VERY BRAVE (bravery=" + braveryLevel.ToString("F2") + ")";
			if (staminaPercent > 0.65f && GD.Randf() < 0.65f) { selected = AttackMode.Special; reason += " → SPECIAL (spamming!)"; return selected; }
			if (staminaPercent > 0.35f && GD.Randf() < 0.8f) { selected = AttackMode.Heavy; reason += " → HEAVY"; return selected; }
		}
		// BRAVE: Mostly heavy with some specials
		else if (braveryLevel > 0.70f)
		{
			reason = "BRAVE (bravery=" + braveryLevel.ToString("F2") + ")";
			if (staminaPercent > 0.5f && GD.Randf() < 0.7f) { selected = AttackMode.Heavy; reason += " → HEAVY"; return selected; }
			if (staminaPercent > 0.75f && GD.Randf() < 0.45f) { selected = AttackMode.Special; reason += " → SPECIAL"; return selected; }
		}
		// PRETTY BRAVE: Balanced aggression
		else if (braveryLevel > 0.55f)
		{
			reason = "BALANCED (bravery=" + braveryLevel.ToString("F2") + ")";
			if (staminaPercent > 0.45f && GD.Randf() < 0.6f) { selected = AttackMode.Heavy; reason += " → HEAVY"; return selected; }
			if (staminaPercent > 0.8f && GD.Randf() < 0.3f) { selected = AttackMode.Special; reason += " → SPECIAL"; return selected; }
		}
		// NORMAL: Mostly safe with some risks
		else if (braveryLevel > 0.40f)
		{
			reason = "NORMAL (bravery=" + braveryLevel.ToString("F2") + ")";
			if (staminaPercent > 0.5f && GD.Randf() < 0.45f) { selected = AttackMode.Heavy; reason += " → HEAVY"; return selected; }
			if (staminaPercent > 0.8f && GD.Randf() < 0.2f) { selected = AttackMode.Special; reason += " → SPECIAL"; return selected; }
		}
		// CAUTIOUS: Light attacks with rare heavy
		else
		{
			reason = "CAUTIOUS (bravery=" + braveryLevel.ToString("F2") + ")";
			if (staminaPercent > 0.6f && healthPercent > 0.6f && GD.Randf() < 0.25f) { selected = AttackMode.Heavy; reason += " → HEAVY (rare)"; return selected; }
		}

		// Default: Light attack (always safe)
		reason = "DEFAULT (safe light attack)";
		return AttackMode.Light;
	}

	// ✨ NEW: Find nearest enemy
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

		// ✨ IMPROVED: Dodge movement is continuous in dodge direction
		if (_isDodgeRolling)
		{
			maxSpeed = DodgeRollSpeed;
			acceleration = DodgeRollSpeed * 2;  // Faster acceleration for dodge
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

		// ✨ During dodge, commit to dodge direction with smooth momentum
		if (_isDodgeRolling)
		{
			horizontalVelocity = _dodgeRollDirection * DodgeRollSpeed;
		}
		else if (desiredDirection != Vector3.Zero)
		{
			_lastInputDirection = _lastInputDirection.Lerp(desiredDirection, 0.1f);
			Vector3 desiredVelocity = _lastInputDirection * maxSpeed;
			float speed = horizontalVelocity.Length();
			float desiredSpeed = desiredVelocity.Length();

			if (speed < desiredSpeed)
				horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, acceleration * delta);
			else
				horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, (acceleration * 0.5f) * delta);

			if (!_isDodgeRolling)
				RotateTowardDirection(desiredDirection, delta);
		}
		else
		{
			_lastInputDirection = Vector3.Zero;
			horizontalVelocity *= isGrounded ? 0.92f : 0.85f;
		}

		_velocity.X = horizontalVelocity.X;
		_velocity.Z = horizontalVelocity.Z;
	}

	private void HandleJump()
	{
		// ✨ IMPROVED: Allow jumping during dodge!
		if (_jumpBufferCounter > 0 && _coyoteCounter > 0 && !_isJumping)
		{
			_velocity.Y = JumpForce;
			_isJumping = true;
			_jumpBufferCounter = 0.0f;
			_coyoteCounter = 0.0f;
			
			// ✨ NEW: If dodging, end dodge and jump instead
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

		// Check dodge action with state tracking
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

			if (!HasStaminaForAction(DodgeRollStaminaCost))
			{
				_dodgeKeyWasPressed = false;
				return;
			}

			_isDodgeRolling = true;
			_dodgeRollDirection = _lastInputDirection != Vector3.Zero ? _lastInputDirection : -_camera.GlobalTransform.Basis.Z;
			_dodgeRollDirection.Y = 0;
			_dodgeRollDirection = _dodgeRollDirection.Normalized();

			_stamina -= DodgeRollStaminaCost;
			_staminaEmptyTimer = StaminaRegenDelay;
			_invincibilityTimer = DodgeRollDuration;

			// ✨ PLAY DODGE SLIDE ANIMATION
			if (_animationCache.TryGetValue("DodgeSlide", out string dodgeAnimPath))
			{
				PlayAnimation("DodgeSlide");
				
				// Get animation duration for more realistic timing
				Animation dodgeAnim = _animPlayer.GetAnimation(dodgeAnimPath);
				float dodgeDuration = dodgeAnim != null ? (float)dodgeAnim.Length : DodgeRollDuration;
				
				// Wait for animation to finish, then smoothly end dodge
				GetTree().CreateTimer(dodgeDuration).Timeout += () =>
				{
					if (IsInstanceValid(this) && _isDodgeRolling)
					{
						_isDodgeRolling = false;
						_dodgeRollCooldownTimer = DodgeRollCooldown;
						
						// Smooth transition back to idle/walk
						_velocity.X *= 0.7f;  // Reduce momentum smoothly
						_velocity.Z *= 0.7f;
					}
				};
			}
			else
			{
				_isDodgeRolling = false;
			}
		}

		// Reset dodge key state when released
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
		_springArm.GlobalPosition = GlobalPosition + Vector3.Up * _cameraHeightOffset;
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
			("DodgeSlide", DodgeSlideFbxPath, false)  // ✨ NEW: Dodge animation
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

	// ✨ UPDATED: Attack system with attack mode
	private void HandleAttack()
	{
		if (_isAttacking || _isDodgeRolling)
		{
			if (!_animPlayer.IsPlaying())
			{
				_isAttacking = false;
				_hitEnemiesThisAttack.Clear();
			}
			return;
		}

		// Check if attack button (left click) is pressed
		if (!Input.IsActionJustPressed("attack"))
			return;

		// Check if sword is equipped
		if (!_isSwordEquipped)
		{
			return;
		}

		// DEBUG: Show what mode is active
		if (_currentAttackMode == AttackMode.None)
		{
		}
		else
		{
		}

		// Perform attack based on current mode
		switch (_currentAttackMode)
		{
			case AttackMode.Light:
				if (CanAttack()) 
				{
					PerformLightAttack();
				}
				else
				{
				}
				break;
			case AttackMode.Heavy:
				if (CanHeavyAttack()) 
				{
					PerformHeavyAttack();
				}
				else
				{
				}
				break;
			case AttackMode.Special:
				if (CanSpecialAttack()) 
				{
					PerformSpecialAttack();
				}
				else
				{
				}
				break;
			case AttackMode.None:
				// ✨ DEFAULT: If no mode selected, do a normal light attack!
				if (CanAttack()) 
				{
					PerformLightAttack();
				}
				else
				{
				}
				break;
		}
	}

	private bool CanAttack() => !_isSitting && _lightAttackCooldownTimer <= 0 && HasStaminaForAction(5) && IsOnFloor() && _isSwordEquipped;  // ✨ SEPARATE
	private bool CanHeavyAttack() => !_isSitting && _heavyAttackCooldownTimer <= 0 && HasStaminaForAction(HeavyStaminaCost) && IsOnFloor() && _isSwordEquipped;  // ✨ SEPARATE
	private bool CanSpecialAttack() => !_isSitting && _specialAttackCooldownTimer <= 0 && HasStaminaForAction(SpecialStaminaCost) && IsOnFloor() && _isSwordEquipped;

	private void PerformLightAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_lightAttackCooldownTimer = LightAttackCooldown;  // ✨ SEPARATE: Light cooldown only
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
		CreateSlashEffect();  // ✨ Slash visual!
	}

	private void PerformHeavyAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_heavyAttackCooldownTimer = HeavyAttackCooldown;  // ✨ SEPARATE: Heavy cooldown only
		_stamina -= HeavyStaminaCost;
		_staminaEmptyTimer = StaminaRegenDelay;
		_currentAttackMode = AttackMode.None;

		PlayAnimation("SwordSlash");
		CreateSlashEffect();  // ✨ Slash visual!
	}

	private void PerformSpecialAttack()
	{
		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_specialAttackCooldownTimer = SpecialCooldown;
		_stamina -= SpecialStaminaCost;
		_staminaEmptyTimer = StaminaRegenDelay;
		_currentAttackMode = AttackMode.None;

		PlayAnimation("SwordSlash");
		CreateSlashEffect();  // ✨ Slash visual!
	}

	private void CheckAttackHits()
	{
		if (_swordRoot == null) return;

		var space = GetWorld3D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters3D();
		query.Shape = new BoxShape3D { Size = Vector3.One * 2.0f };
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

				// ✨ Screen shake on hit!
				ShakeScreen(0.15f, 0.2f);
				
				// ✨ Enemy flash effect
				enemy.FlashHit();
				
				float enemyHealthBeforeDamage = enemy.CurrentHealth;
				enemy.TakeDamage(damage, knockbackDir, knockback, isCritical);
				
				// ✨ NEW: Check if enemy died and reward XP
				if (enemyHealthBeforeDamage > 0 && enemy.CurrentHealth <= 0)
				{
					int xpReward = 100;  // XP per enemy kill
					LevelingSystem.AddXP(xpReward);
					GD.Print($"💰 +{xpReward} XP! Enemy defeated!");
				}
			}
		}
	}

	private float DetermineAttackDamage(out bool isCritical)
	{
		float baseDamage = LightSlashDamage;

		if (_stamina < MaxStamina - HeavyStaminaCost + 1)
			baseDamage = HeavySlashDamage;

		if (_stamina < MaxStamina - SpecialStaminaCost + 1)
			baseDamage = SpecialDamage;

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
		if (_stamina > MaxStamina - HeavyStaminaCost)
			return LightSlashKnockback;

		if (_stamina > MaxStamina - SpecialStaminaCost)
			return HeavySlashKnockback;

		return SpecialKnockback;
	}

	private void UpdateAnimationState(Vector3 moveDirection, bool isRunning, bool jumped, bool landed)
	{
		if (_isAttacking) return;
		if (_isDodgeRolling) return;  // ✨ NEW: Don't override dodge animation!

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
			_animPlayer.Play(path);
	}

	private void CreatePlayerHealthBar()
	{
		_playerHealth = MaxPlayerHealth;
		_stamina = MaxStamina;

		(_playerHealthBar, _playerHealthLabel) = HealthBarFactory.CreateScreenBar(
			this, MaxPlayerHealth, 50, "Health", new Color(0.2f, 1.0f, 0.2f, 0.8f));

		(_staminaBar, _staminaLabel) = HealthBarFactory.CreateScreenBar(
			this, MaxStamina, 90, "Stamina", new Color(1.0f, 1.0f, 0.2f, 0.8f));

	}

	public void PlayerTakeDamage(float damage)
	{
		if (_isGameOver || _invincibilityTimer > 0.0f) return;

		_playerHealth = Mathf.Max(0, _playerHealth - damage);
		_invincibilityTimer = InvincibilityDuration;
		_lastDamageTime = 0.3f;  // ✨ DODGE MORE for next 0.3 seconds after taking damage!

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

	/// <summary>
	/// ✨ Called when player levels up - restore HP/Stamina to 100%
	/// </summary>
	private void OnPlayerLevelUp()
	{
		_playerHealth = MaxPlayerHealth;  // ✨ RESTORE HP
		_stamina = MaxStamina;            // ✨ RESTORE STAMINA

		GD.Print($"🎉 LEVELED UP! HP and Stamina restored to 100%");

		if (_playerHealthBar != null) _playerHealthBar.Value = _playerHealth;
		if (_staminaBar != null) _staminaBar.Value = _stamina;
		if (_playerHealthLabel != null) _playerHealthLabel.Text = $"Health: {_playerHealth:F0} / {MaxPlayerHealth:F0}";
		if (_staminaLabel != null) _staminaLabel.Text = $"Stamina: {_stamina:F0} / {MaxStamina:F0}";
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
		
		// Safety timeout - delete label after 2 seconds if tween fails
		GetTree().CreateTimer(2.0f).Timeout += () => {
			if (label != null && IsInstanceValid(label))
			{
				label.QueueFree();
			}
		};
	}

	private void ShakeScreen(float duration, float intensity)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		
		// Create shake effect by offsetting camera position
		Vector3 originalPos = camera.GlobalPosition;
		Vector3 shakeOffset = new Vector3(
			(float)GD.Randf() * intensity - intensity / 2,
			(float)GD.Randf() * intensity - intensity / 2,
			0
		);
		
		tween.TweenProperty(camera, "global_position", originalPos + shakeOffset, duration * 0.25f);
		tween.TweenProperty(camera, "global_position", originalPos, duration * 0.75f);
	}

	private void CreateSlashEffect()
	{
		if (_swordRoot == null) return;

		// Create a simple visual slash effect using a panel
		var slashEffect = new Panel();
		slashEffect.ZIndex = -1;  // Behind character
		
		// Create semi-transparent blue/white color for slash
		var slashStyle = new StyleBoxFlat { BgColor = new Color(0.8f, 0.9f, 1.0f, 0.6f) };
		slashEffect.AddThemeStyleboxOverride("panel", slashStyle);
		
		// Position at sword location
		Vector3 swordWorldPos = _swordRoot.GlobalPosition;
		Vector2 screenPos = GetViewport().GetCamera3D().UnprojectPosition(swordWorldPos);
		
		slashEffect.GlobalPosition = screenPos - Vector2.One * 20;
		slashEffect.CustomMinimumSize = Vector2.One * 40;
		
		GetTree().Root.AddChild(slashEffect);
		
		// Animate slash effect
		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.Out);
		
		// Expand and fade out
		tween.Parallel().TweenProperty(slashEffect, "custom_minimum_size", Vector2.One * 100, 0.3f);
		tween.Parallel().TweenProperty(slashEffect, "modulate", new Color(1, 1, 1, 0), 0.3f);
		
		tween.TweenCallback(Callable.From(() => {
			if (slashEffect != null && IsInstanceValid(slashEffect))
				slashEffect.QueueFree();
		}));
	}

	private void UpdateStamina(bool isRunning, float delta)
	{
		// ✨ Auto battle movement drains stamina too
		if (_isAutoBattle && _lastInputDirection != Vector3.Zero && IsOnFloor())
		{
			_stamina = Mathf.Max(0, _stamina - StaminaDrainRateRun * 0.5f * delta);
			_staminaEmptyTimer = 0.3f;  // Shorter regen delay for faster recovery
		}
		else if (isRunning && IsOnFloor())
		{
			_stamina = Mathf.Max(0, _stamina - StaminaDrainRateRun * delta);
			_staminaEmptyTimer = 0.5f;
		}
		else if (_isAttacking)
		{
			_staminaEmptyTimer = 0.2f;  // ✨ SHORTER delay during auto battle - recover faster!
		}
		else
		{
			_staminaEmptyTimer = Mathf.Max(0, _staminaEmptyTimer - delta);
			if (_staminaEmptyTimer <= 0 && !isRunning && !_isAttacking)
				_stamina = Mathf.Min(MaxStamina, _stamina + StaminaRegenRate * delta);
		}

		_stamina = Mathf.Clamp(_stamina, 0, MaxStamina);

		if (_staminaBar != null) _staminaBar.Value = _stamina;
		if (_staminaLabel != null) _staminaLabel.Text = $"Stamina: {_stamina:F0} / {MaxStamina:F0}";
	}

	private bool HasStaminaForAction(float cost) => _stamina >= cost;
	public float GetStamina() => _stamina;
	public bool CanRun() => _stamina > 0.0f;
	public float GetPlayerHealth() => _playerHealth;
	public bool IsPlayerAlive() => _playerHealth > 0.0f;

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
