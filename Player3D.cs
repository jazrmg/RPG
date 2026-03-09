using Godot;
using System.Collections.Generic;

public partial class Player3D : CharacterBody3D
{
	[Export] public float MaxGroundSpeed = 5.0f;
	[Export] public float GroundAcceleration = 20.0f;
	[Export] public float GroundDeceleration = 25.0f;
	[Export] public float GroundFriction = 0.92f;
	
	[Export] public float MaxRunSpeed = 8.5f;
	[Export] public float RunAcceleration = 30.0f;
	[Export] public float RunDeceleration = 35.0f;
	
	[Export] public float AirAcceleration = 12.0f;
	[Export] public float AirDeceleration = 8.0f;
	[Export] public float AirControl = 0.5f;
	
	[Export] public float JumpForce = 4.0f;
	[Export] public float JumpCutMultiplier = 0.5f;
	[Export] public float ApexGravityMultiplier = 0.5f;
	[Export] public float FallGravityMultiplier = 1.2f;
	[Export] public float TerminalVelocity = 20.0f;
	
	[Export] public float MouseSensitivity = 0.3f;
	[Export] public float RotationSpeed = 12.0f;
	[Export] public float CameraMinPitchDegrees = -60.0f;
	[Export] public float CameraMaxPitchDegrees = 30.0f;
	[Export] public float AttackCooldown = 0.6f;
	
	[Export] public float CoyoteTime = 0.12f;
	[Export] public float JumpBufferTime = 0.12f;
	
	[Export] public float SlashDamage = 25.0f;
	[Export] public float SlashKnockback = 10.0f;
	
	[Export] public float MaxPlayerHealth = 100.0f;

	// Stamina system
	[Export] public float MaxStamina = 100.0f;
	[Export] public float StaminaDrainRateRun = 30.0f;  // Per second while running
	[Export] public float StaminaDrainRateAttack = 25.0f;  // Per attack
	[Export] public float StaminaRegenRate = 15.0f;  // Per second while walking/idle
	[Export] public float StaminaRegenDelay = 0.5f;  // Delay before regen starts (realistic fatigue)
	
	// Health system
	private float _playerHealth = 100.0f;
	private ProgressBar _playerHealthBar;
	private Label _playerHealthLabel;
	
	private float _stamina = 100.0f;
	private ProgressBar _staminaBar;
	private Label _staminaLabel;
	private float _staminaEmptyTimer = 0.0f;  // Cooldown before stamina can regen

	private const string PlayerGroup = "player";

	private const string AnimationPlayerPath = "CharacterModel/AnimationPlayer";
	private const string SpringArmPath = "SpringArm3D";
	private const string CameraPath = "SpringArm3D/Camera3D";

	private const string SwordRootPath = "CharacterModel/Skeleton3D/RightHandAttachment/antique_estoc_1k";
	private const string SwordMeshPath = "CharacterModel/Skeleton3D/RightHandAttachment/antique_estoc_1k/antique_estoc";

	private const string IdleFbxPath = "res://models/player/Idle.fbx";
	private const string WalkFbxPath = "res://models/player/Walking.fbx";
	private const string RunFbxPath = "res://models/player/Running.fbx";
	private const string JumpFbxPath = "res://models/player/Jump.fbx";
	private const string SitFbxPath = "res://models/player/Sitting Idle.fbx";
	private const string SwordIdleFbxPath = "res://models/player/Great_Sword_Idle.fbx";
	private const string SwordSlashFbxPath = "res://models/player/Great_Sword_Slash.fbx";

	private enum AnimState
	{
		None,
		Idle,
		Walk,
		Run,
		Jump,
		Sit,
		SwordIdle,
		SwordSlash
	}

	private readonly float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

	private Camera3D _camera;
	private SpringArm3D _springArm;
	private AnimationPlayer _animPlayer;

	private Node3D _swordRoot;
	private MeshInstance3D _swordMesh;

	private string _idleAnim = "";
	private string _walkAnim = "";
	private string _runAnim = "";
	private string _jumpAnim = "";
	private string _sitAnim = "";
	private string _swordIdleAnim = "";
	private string _swordSlashAnim = "";

	private AnimState _currentAnimState = AnimState.None;

	private bool _isSwordEquipped = false;
	private bool _isSitting = false;
	private bool _isAttacking = false;
	private float _attackCooldownTimer = 0.0f;
	
	// Game state
	private bool _isGameOver = false;
	private Control _gameOverUI = null;

	// Physics state
	private Vector3 _velocity = Vector3.Zero;
	private Vector3 _lastInputDirection = Vector3.Zero;
	private float _coyoteCounter = 0.0f;
	private float _jumpBufferCounter = 0.0f;
	private bool _isJumping = false;
	private bool _wasInAir = false;  // For landing detection

	// Attack tracking
	private HashSet<Enemy> _hitEnemiesThisAttack = new HashSet<Enemy>();

	// Camera rig
	private float _cameraYaw;
	private float _cameraPitch;
	private float _cameraHeightOffset = 1.5f;

	public override void _Ready()
	{
		AddToGroup(PlayerGroup);

		_springArm = GetNodeOrNull<SpringArm3D>(SpringArmPath);
		_camera = GetNodeOrNull<Camera3D>(CameraPath);
		_animPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

		if (_springArm == null || _camera == null || _animPlayer == null)
		{
			SetPhysicsProcess(false);
			SetProcessInput(false);
			return;
		}

		Input.MouseMode = Input.MouseModeEnum.Captured;

		_animPlayer.RootNode = _animPlayer.GetParent().GetPath();

		_cameraHeightOffset = _springArm.Position.Y;
		_cameraYaw = Rotation.Y;
		_cameraPitch = _springArm.Rotation.X;

		_springArm.TopLevel = true;
		UpdateCameraRig();

		InitializeAnimations();
		ResolveSwordNodes();
		SetSwordVisible(false);
		CreatePlayerHealthBar();

		PlayAnimationState(AnimState.Idle, _idleAnim);
	}

	public override void _Input(InputEvent @event)
	{
		// Handle restart on Enter key when game is over
		if (_isGameOver && @event.IsActionPressed("ui_accept"))
		{
			RestartGame();
			GetTree().Root.SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_cameraYaw += Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity);
			_cameraPitch += Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity);

			float minPitch = Mathf.DegToRad(CameraMinPitchDegrees);
			float maxPitch = Mathf.DegToRad(CameraMaxPitchDegrees);
			_cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);

			UpdateCameraRig();
		}

		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.Pressed &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		HandleSwordToggle();
		HandleSitToggle();
		HandleAttack();

		// Jump buffer and coyote time
		_jumpBufferCounter -= dt;
		_coyoteCounter -= dt;

		Vector2 inputDir = (_isSitting || _isAttacking)
			? Vector2.Zero
			: Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		bool isRunning = inputDir != Vector2.Zero && Input.IsKeyPressed(Key.Shift);
		
		// Can't run without stamina - FORCE STOP if stamina is 0
		if (isRunning && !CanRun())
			isRunning = false;
		
		// If stamina just ran out (was running, now empty), force walk
		if (_stamina <= 0.0f && isRunning)
			isRunning = false;
		
		Vector3 moveDirection = GetCameraRelativeDirection(inputDir);

		// Handle jump input
		if (Input.IsActionJustPressed("ui_accept") && !_isSitting && !_isAttacking)
		{
			_jumpBufferCounter = JumpBufferTime;
		}

		// Apply physics
		bool isGrounded = IsOnFloor();
		
		// Detect landing (just touched ground after being in air)
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

		// Check attack hits during attack animation
		if (_isAttacking)
		{
			CheckAttackHits();
		}

		// Clamp terminal velocity
		if (_velocity.Y < -TerminalVelocity)
		{
			_velocity.Y = -TerminalVelocity;
		}

		Velocity = _velocity;
		MoveAndSlide();

		// Update stamina
		UpdateStamina(isRunning, dt);

		UpdateCameraRig();
		UpdateAnimationState(moveDirection, isRunning, _isJumping, justLanded);

		_attackCooldownTimer -= dt;
		_isJumping = false;
	}

	private void ApplyGravity(ref Vector3 velocity, float delta)
	{
		if (!IsOnFloor())
		{
			// Apex gravity - lighter gravity at jump peak
			float gravityMultiplier = velocity.Y > 0 ? ApexGravityMultiplier : FallGravityMultiplier;
			velocity.Y -= _gravity * gravityMultiplier * delta;
		}
	}

	private void HandleMovement(Vector3 desiredDirection, bool isRunning, bool isGrounded, float delta)
	{
		float maxSpeed = isRunning ? MaxRunSpeed : MaxGroundSpeed;
		float acceleration = isRunning ? RunAcceleration : GroundAcceleration;
		float deceleration = isRunning ? RunDeceleration : GroundDeceleration;

		// Apply air control reduction
		if (!isGrounded)
		{
			acceleration *= AirControl;
			deceleration *= AirControl;
			maxSpeed *= AirControl;
		}

		Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);

		// Handle movement input
		if (desiredDirection != Vector3.Zero)
		{
			// Smooth input transitions
			_lastInputDirection = _lastInputDirection.Lerp(desiredDirection, 0.1f);
			
			Vector3 desiredVelocity = _lastInputDirection * maxSpeed;

			// Smooth acceleration - use MoveToward for more natural feel
			float currentSpeed = horizontalVelocity.Length();
			float desiredSpeed = desiredVelocity.Length();
			
			if (currentSpeed < desiredSpeed)
			{
				// Accelerate
				horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, acceleration * delta);
			}
			else
			{
				// Match desired velocity smoothly
				horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, deceleration * delta * 0.5f);
			}
		}
		else
		{
			_lastInputDirection = Vector3.Zero;
			
			// Decelerate
			if (isGrounded)
			{
				// Apply friction on ground - more realistic deceleration
				horizontalVelocity *= GroundFriction;
			}
			else
			{
				// Air deceleration
				horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, deceleration * delta);
			}
		}

		_velocity.X = horizontalVelocity.X;
		_velocity.Z = horizontalVelocity.Z;

		// Rotate character toward movement direction
		if (desiredDirection != Vector3.Zero)
		{
			RotateTowardDirection(desiredDirection, delta);
		}
	}

	private void HandleJump()
	{
		// Check if we should jump
		if (_jumpBufferCounter > 0 && _coyoteCounter > 0 && !_isJumping)
		{
			_velocity.Y = JumpForce;
			_isJumping = true;
			_jumpBufferCounter = 0.0f;
			_coyoteCounter = 0.0f;
		}

		// Jump cut - reduce upward velocity if player releases jump early
		if (_isJumping && !Input.IsActionPressed("ui_accept") && _velocity.Y > 0)
		{
			_velocity.Y *= JumpCutMultiplier;
			_isJumping = false;
		}
	}

	private Vector3 GetCameraRelativeDirection(Vector2 inputDir)
	{
		if (inputDir == Vector2.Zero)
			return Vector3.Zero;

		Vector3 camForward = -_camera.GlobalTransform.Basis.Z;
		Vector3 camRight = _camera.GlobalTransform.Basis.X;

		camForward.Y = 0;
		camRight.Y = 0;

		camForward = camForward.Normalized();
		camRight = camRight.Normalized();

		Vector3 direction = (camForward * -inputDir.Y) + (camRight * inputDir.X);
		return direction.Normalized();
	}

	private void RotateTowardDirection(Vector3 direction, float delta)
	{
		if (direction == Vector3.Zero)
			return;

		float targetYaw = Mathf.Atan2(direction.X, direction.Z);
		float currentYaw = Rotation.Y;

		// Smooth rotation with LerpAngle
		float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, RotationSpeed * delta);

		Rotation = new Vector3(
			Rotation.X,
			newYaw,
			Rotation.Z
		);
	}

	private void UpdateCameraRig()
	{
		_springArm.GlobalPosition = GlobalPosition + Vector3.Up * _cameraHeightOffset;
		_springArm.GlobalRotation = new Vector3(_cameraPitch, _cameraYaw, 0.0f);
	}

	private void InitializeAnimations()
	{
		var customLibrary = new AnimationLibrary();
		_animPlayer.AddAnimationLibrary("Custom", customLibrary);

		_idleAnim = LoadAnimationToLibrary(customLibrary, IdleFbxPath, "Idle", true);
		_walkAnim = LoadAnimationToLibrary(customLibrary, WalkFbxPath, "Walking", true);
		_runAnim = LoadAnimationToLibrary(customLibrary, RunFbxPath, "Running", true);
		_jumpAnim = LoadAnimationToLibrary(customLibrary, JumpFbxPath, "Jump", false);
		_sitAnim = LoadAnimationToLibrary(customLibrary, SitFbxPath, "Sitting", true);
		_swordIdleAnim = LoadAnimationToLibrary(customLibrary, SwordIdleFbxPath, "SwordIdle", true);
		_swordSlashAnim = LoadAnimationToLibrary(customLibrary, SwordSlashFbxPath, "SwordSlash", false);
	}

	private string LoadAnimationToLibrary(AnimationLibrary library, string fbxPath, string animationName, bool loop)
	{
		Animation animation = ExtractAnimation(fbxPath, loop);
		if (animation == null)
		{
			return "";
		}

		library.AddAnimation(animationName, animation);
		return $"Custom/{animationName}";
	}

	private Animation ExtractAnimation(string fbxPath, bool loop)
	{
		PackedScene scene = GD.Load<PackedScene>(fbxPath);
		if (scene == null)
		{
			return null;
		}

		Node instance = scene.Instantiate();
		AnimationPlayer importedAnimPlayer = FindAnimationPlayer(instance);

		if (importedAnimPlayer == null)
		{
			instance.QueueFree();
			return null;
		}

		Animation foundAnimation = null;

		foreach (string libraryName in importedAnimPlayer.GetAnimationLibraryList())
		{
			AnimationLibrary library = importedAnimPlayer.GetAnimationLibrary(libraryName);
			if (library.HasAnimation("mixamo_com"))
			{
				foundAnimation = (Animation)library.GetAnimation("mixamo_com").Duplicate();
				break;
			}
		}

		instance.QueueFree();

		if (foundAnimation == null)
		{
			return null;
		}

		for (int i = foundAnimation.GetTrackCount() - 1; i >= 0; i--)
		{
			string trackPath = foundAnimation.TrackGetPath(i).ToString();
			if (trackPath.Contains("Hips") && foundAnimation.TrackGetType(i) == Animation.TrackType.Position3D)
			{
				foundAnimation.RemoveTrack(i);
			}
		}

		foundAnimation.LoopMode = loop
			? Animation.LoopModeEnum.Linear
			: Animation.LoopModeEnum.None;

		return foundAnimation;
	}

	private AnimationPlayer FindAnimationPlayer(Node node)
	{
		if (node is AnimationPlayer animationPlayer)
			return animationPlayer;

		foreach (Node child in node.GetChildren())
		{
			AnimationPlayer found = FindAnimationPlayer(child);
			if (found != null)
				return found;
		}

		return null;
	}

	private void ResolveSwordNodes()
	{
		_swordRoot = GetNodeOrNull<Node3D>(SwordRootPath);
		_swordMesh = GetNodeOrNull<MeshInstance3D>(SwordMeshPath);

		if (_swordRoot == null)
			GD.PrintErr($"Player3D: Sword root not found at '{SwordRootPath}'.");

		if (_swordMesh == null)
			GD.PrintErr($"Player3D: Sword mesh not found at '{SwordMeshPath}'.");
	}

	private void SetSwordVisible(bool visible)
	{
		if (_swordRoot != null)
			_swordRoot.Visible = visible;

		if (_swordMesh != null)
			_swordMesh.Visible = visible;
	}

	private void HandleSwordToggle()
	{
		if (!Input.IsActionJustPressed("equip_sword"))
			return;

		if (!IsOnFloor() || _isSitting || _isAttacking)
			return;

		_isSwordEquipped = !_isSwordEquipped;
		SetSwordVisible(_isSwordEquipped);
	}

	private void HandleSitToggle()
	{
		if (!Input.IsActionJustPressed("sit"))
			return;

		if (!IsOnFloor() || _isAttacking)
			return;

		_isSitting = !_isSitting;
	}

	private void HandleAttack()
	{
		if (_isAttacking)
		{
			if (!_animPlayer.IsPlaying())
			{
				_isAttacking = false;
				_hitEnemiesThisAttack.Clear();
			}
			return;
		}

		if (!Input.IsActionJustPressed("attack"))
			return;

		if (!IsOnFloor())
			return;

		if (!_isSwordEquipped)
			return;

		if (_isSitting)
			return;

		if (_attackCooldownTimer > 0.0f)
			return;

		// Check if player has stamina for attack
		if (!HasStaminaForAction(StaminaDrainRateAttack))
			return;

		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_attackCooldownTimer = AttackCooldown;
		DrainStaminaForAttack();  // Drain stamina on attack
		PlayAnimationState(AnimState.SwordSlash, _swordSlashAnim);
	}

	private void CheckAttackHits()
	{
		if (_swordRoot == null)
		{
			return;
		}

		var space = GetWorld3D().DirectSpaceState;
		var query = new PhysicsShapeQueryParameters3D();
		query.Shape = new BoxShape3D { Size = Vector3.One * 2.0f };
		query.Transform = _swordRoot.GlobalTransform;

		var results = space.IntersectShape(query);

		foreach (var result in results)
		{
			var collider = (Node)result["collider"];
			
			if (collider is Enemy enemy)
			{
				if (!_hitEnemiesThisAttack.Contains(enemy))
				{
					_hitEnemiesThisAttack.Add(enemy);
					
					Vector3 knockbackDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
					knockbackDir.Y = 0;
					knockbackDir = knockbackDir.Normalized();
					
					enemy.TakeDamage(SlashDamage, knockbackDir, SlashKnockback);
				}
			}
		}
	}

	private void UpdateAnimationState(Vector3 moveDirection, bool isRunning, bool jumpedThisFrame, bool justLanded)
	{
		if (_isAttacking)
			return;

		if (_isSitting)
		{
			PlayAnimationState(AnimState.Sit, _sitAnim);
			return;
		}

		// Immediate idle on landing (fixes sliding)
		if (justLanded && !jumpedThisFrame)
		{
			if (_isSwordEquipped)
				PlayAnimationState(AnimState.SwordIdle, _swordIdleAnim);
			else
				PlayAnimationState(AnimState.Idle, _idleAnim);
			return;
		}

		if (jumpedThisFrame || !IsOnFloor())
		{
			PlayAnimationState(AnimState.Jump, _jumpAnim);
			return;
		}

		if (moveDirection != Vector3.Zero)
		{
			if (isRunning)
				PlayAnimationState(AnimState.Run, _runAnim);
			else
				PlayAnimationState(AnimState.Walk, _walkAnim);

			return;
		}

		if (_isSwordEquipped)
		{
			PlayAnimationState(AnimState.SwordIdle, _swordIdleAnim);
		}
		else
		{
			PlayAnimationState(AnimState.Idle, _idleAnim);
		}
	}

	private void PlayAnimationState(AnimState state, string animationName)
	{
		if (string.IsNullOrEmpty(animationName))
			return;

		if (_currentAnimState == state)
			return;

		_animPlayer.Play(animationName);
		_currentAnimState = state;
	}

	private void CreatePlayerHealthBar()
	{
		_playerHealth = MaxPlayerHealth;
		_stamina = MaxStamina;

		// Create canvas layer for UI
		CanvasLayer canvasLayer = new CanvasLayer();
		canvasLayer.Layer = 100;
		AddChild(canvasLayer);

		// Create container
		Control container = new Control();
		container.MouseFilter = Control.MouseFilterEnum.Ignore;
		container.AnchorLeft = 0.0f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 1.0f;
		container.AnchorBottom = 1.0f;
		canvasLayer.AddChild(container);

		// Create health bar
		_playerHealthBar = new ProgressBar();
		_playerHealthBar.MinValue = 0.0f;
		_playerHealthBar.MaxValue = MaxPlayerHealth;
		_playerHealthBar.Value = _playerHealth;
		_playerHealthBar.AnchorLeft = 0.0f;
		_playerHealthBar.AnchorTop = 0.0f;
		_playerHealthBar.AnchorRight = 0.3f;
		_playerHealthBar.AnchorBottom = 0.0f;
		_playerHealthBar.OffsetLeft = 10.0f;
		_playerHealthBar.OffsetTop = 10.0f;
		_playerHealthBar.OffsetRight = -10.0f;
		_playerHealthBar.OffsetBottom = 30.0f;
		_playerHealthBar.MouseFilter = Control.MouseFilterEnum.Ignore;
		_playerHealthBar.SelfModulate = new Color(0.2f, 1.0f, 0.2f, 0.8f);  // Green
		container.AddChild(_playerHealthBar);

		// Create health label
		_playerHealthLabel = new Label();
		_playerHealthLabel.Text = $"Health: {_playerHealth:F0} / {MaxPlayerHealth:F0}";
		_playerHealthLabel.AnchorLeft = 0.0f;
		_playerHealthLabel.AnchorTop = 0.0f;
		_playerHealthLabel.AnchorRight = 0.3f;
		_playerHealthLabel.AnchorBottom = 0.0f;
		_playerHealthLabel.OffsetLeft = 10.0f;
		_playerHealthLabel.OffsetTop = 35.0f;
		_playerHealthLabel.OffsetRight = -10.0f;
		_playerHealthLabel.OffsetBottom = 55.0f;
		_playerHealthLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_playerHealthLabel.AddThemeColorOverride("font_color", Colors.White);
		container.AddChild(_playerHealthLabel);

		// Create stamina bar
		_staminaBar = new ProgressBar();
		_staminaBar.MinValue = 0.0f;
		_staminaBar.MaxValue = MaxStamina;
		_staminaBar.Value = _stamina;
		_staminaBar.AnchorLeft = 0.0f;
		_staminaBar.AnchorTop = 0.0f;
		_staminaBar.AnchorRight = 0.3f;
		_staminaBar.AnchorBottom = 0.0f;
		_staminaBar.OffsetLeft = 10.0f;
		_staminaBar.OffsetTop = 60.0f;
		_staminaBar.OffsetRight = -10.0f;
		_staminaBar.OffsetBottom = 80.0f;
		_staminaBar.MouseFilter = Control.MouseFilterEnum.Ignore;
		_staminaBar.SelfModulate = new Color(1.0f, 1.0f, 0.2f, 0.8f);  // Yellow
		container.AddChild(_staminaBar);

		// Create stamina label
		_staminaLabel = new Label();
		_staminaLabel.Text = $"Stamina: {_stamina:F0} / {MaxStamina:F0}";
		_staminaLabel.AnchorLeft = 0.0f;
		_staminaLabel.AnchorTop = 0.0f;
		_staminaLabel.AnchorRight = 0.3f;
		_staminaLabel.AnchorBottom = 0.0f;
		_staminaLabel.OffsetLeft = 10.0f;
		_staminaLabel.OffsetTop = 85.0f;
		_staminaLabel.OffsetRight = -10.0f;
		_staminaLabel.OffsetBottom = 105.0f;
		_staminaLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_staminaLabel.AddThemeColorOverride("font_color", Colors.White);
		container.AddChild(_staminaLabel);
	}

	public void PlayerTakeDamage(float damage)
	{
		if (_isGameOver) return;  // Can't take damage if already dead

		_playerHealth -= damage;
		if (_playerHealth < 0.0f)
			_playerHealth = 0.0f;

		if (_playerHealthBar != null)
			_playerHealthBar.Value = _playerHealth;

		if (_playerHealthLabel != null)
			_playerHealthLabel.Text = $"Health: {_playerHealth:F0} / {MaxPlayerHealth:F0}";

		// Apply knockback away from enemy to prevent getting stuck
		Node3D enemy = GetTree().GetFirstNodeInGroup("enemy") as Node3D;
		if (enemy != null)
		{
			Vector3 knockbackDir = (GlobalPosition - enemy.GlobalPosition).Normalized();
			knockbackDir.Y = 0;  // Keep on ground plane
			knockbackDir = knockbackDir.Normalized();
			
			_velocity.X += knockbackDir.X * 5.0f;  // Knockback force
			_velocity.Z += knockbackDir.Z * 5.0f;
		}

		// Check for game over
		if (_playerHealth <= 0.0f)
		{
			GameOver();
		}
	}

	private void UpdateStamina(bool isRunning, float delta)
	{
		// Drain stamina while running
		if (isRunning && IsOnFloor())
		{
			_stamina -= StaminaDrainRateRun * delta;
			if (_stamina < 0.0f)
				_stamina = 0.0f;
			
			// Reset regen delay timer when actively running
			_staminaEmptyTimer = StaminaRegenDelay;
		}
		// Drain stamina while attacking
		else if (_isAttacking)
		{
			// Attack drains are handled in DrainStaminaForAttack()
			// Just prevent regen during attack
			_staminaEmptyTimer = StaminaRegenDelay;
		}
		// Regenerate stamina only when WALKING/IDLE (not running, not attacking)
		else
		{
			// Decrease the empty timer (fatigue delay before regen)
			if (_staminaEmptyTimer > 0.0f)
			{
				_staminaEmptyTimer -= delta;
			}
			
			// Only regen if delay has passed AND not running AND not attacking
			if (_staminaEmptyTimer <= 0.0f && !isRunning && !_isAttacking)
			{
				_stamina += StaminaRegenRate * delta;
				if (_stamina > MaxStamina)
					_stamina = MaxStamina;
			}
		}

		// Clamp stamina
		if (_stamina < 0.0f)
			_stamina = 0.0f;
		if (_stamina > MaxStamina)
			_stamina = MaxStamina;

		// Update UI
		if (_staminaBar != null)
			_staminaBar.Value = _stamina;

		if (_staminaLabel != null)
			_staminaLabel.Text = $"Stamina: {_stamina:F0} / {MaxStamina:F0}";
	}

	private bool HasStaminaForAction(float cost = 0.0f)
	{
		return _stamina >= cost;
	}

	private void DrainStaminaForAttack()
	{
		_stamina -= StaminaDrainRateAttack;
		if (_stamina < 0.0f)
			_stamina = 0.0f;

		// Reset regen delay after attack so stamina can't regen immediately
		_staminaEmptyTimer = StaminaRegenDelay;

		if (_staminaBar != null)
			_staminaBar.Value = _stamina;

		if (_staminaLabel != null)
			_staminaLabel.Text = $"Stamina: {_stamina:F0} / {MaxStamina:F0}";
	}

	public float GetStamina() => _stamina;
	public bool CanRun() => _stamina > 0.0f;
	
	public float GetPlayerHealth() => _playerHealth;
	public bool IsPlayerAlive() => _playerHealth > 0.0f;

	private void GameOver()
	{
		_isGameOver = true;
		SetPhysicsProcess(false);
		// Don't disable all input - we need to detect Enter key for restart!
		// SetProcessInput(false);
		
		
		ShowGameOverUI();
	}

	private void ShowGameOverUI()
	{
		// Create canvas layer for game over UI (add to scene, not root)
		CanvasLayer canvasLayer = new CanvasLayer();
		canvasLayer.Layer = 200;
		AddChild(canvasLayer);  // Add to Player node instead of tree root

		// Create dark background
		ColorRect background = new ColorRect();
		background.Color = new Color(0, 0, 0, 0.7f);
		background.AnchorLeft = 0.0f;
		background.AnchorTop = 0.0f;
		background.AnchorRight = 1.0f;
		background.AnchorBottom = 1.0f;
		canvasLayer.AddChild(background);

		// Create container for text and button
		VBoxContainer container = new VBoxContainer();
		container.AnchorLeft = 0.25f;
		container.AnchorTop = 0.3f;
		container.AnchorRight = 0.75f;
		container.AnchorBottom = 0.7f;
		container.Alignment = BoxContainer.AlignmentMode.Center;
		canvasLayer.AddChild(container);

		// Game Over title
		Label titleLabel = new Label();
		titleLabel.Text = "GAME OVER";
		titleLabel.AddThemeColorOverride("font_color", Colors.Red);
		titleLabel.AddThemeFontSizeOverride("font_size", 80);
		container.AddChild(titleLabel);

		// Health status
		Label statusLabel = new Label();
		statusLabel.Text = "You died!";
		statusLabel.AddThemeColorOverride("font_color", Colors.White);
		statusLabel.AddThemeFontSizeOverride("font_size", 40);
		container.AddChild(statusLabel);

		// Restart button
		Button restartButton = new Button();
		restartButton.Text = "Restart Game";
		restartButton.CustomMinimumSize = new Vector2(300, 80);
		restartButton.AddThemeFontSizeOverride("font_size", 32);
		restartButton.Pressed += () => RestartGame();
		container.AddChild(restartButton);

		// Hint text
		Label hintLabel = new Label();
		hintLabel.Text = "or Press ENTER";
		hintLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		hintLabel.AddThemeFontSizeOverride("font_size", 24);
		container.AddChild(hintLabel);

		_gameOverUI = container;
	}

	private void RestartGame()
	{
		// Clean up the UI before reloading
		if (_gameOverUI != null && IsInstanceValid(_gameOverUI))
		{
			_gameOverUI.QueueFree();
		}

		// Reload the scene
		GetTree().ReloadCurrentScene();
	}
}
