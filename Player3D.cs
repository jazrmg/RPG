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

	// Physics state
	private Vector3 _velocity = Vector3.Zero;
	private Vector3 _lastInputDirection = Vector3.Zero;
	private float _coyoteCounter = 0.0f;
	private float _jumpBufferCounter = 0.0f;
	private bool _isJumping = false;

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

		PlayAnimationState(AnimState.Idle, _idleAnim);
	}

	public override void _Input(InputEvent @event)
	{
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
		Vector3 moveDirection = GetCameraRelativeDirection(inputDir);

		// Handle jump input
		if (Input.IsActionJustPressed("ui_accept") && !_isSitting && !_isAttacking)
		{
			_jumpBufferCounter = JumpBufferTime;
		}

		// Apply physics
		bool isGrounded = IsOnFloor();
		
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

		UpdateCameraRig();
		UpdateAnimationState(moveDirection, isRunning, _isJumping);

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

		_isAttacking = true;
		_hitEnemiesThisAttack.Clear();
		_attackCooldownTimer = AttackCooldown;
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

	private void UpdateAnimationState(Vector3 moveDirection, bool isRunning, bool jumpedThisFrame)
	{
		if (_isAttacking)
			return;

		if (_isSitting)
		{
			PlayAnimationState(AnimState.Sit, _sitAnim);
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
}
