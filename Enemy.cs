using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody3D
{
	[Export] public float Speed = 2.5f;
	[Export] public float Acceleration = 18.0f;
	[Export] public float Deceleration = 15.0f;
	[Export] public float Friction = 0.90f;
	[Export] public float ChaseRange = 20.0f;
	[Export] public float StopDistance = 1.8f;
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float MaxHealth = 100.0f;
	[Export] public float KnockbackResistance = 0.5f;
	[Export] public float KnockbackDamping = 0.88f;
	[Export] public float HealthBarHeightOffset = 2.5f;
	[Export] public float SpawnInvulnerabilityTime = 0.5f;

	private const string PlayerGroup = "player";
	private const string HitboxPath = "HitBox";
	private const string DeathFbxPath = "res://models/enemy/Flying Back Death.fbx";

	private readonly float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

	private AnimationPlayer _animPlayer;
	private Node3D _player;
	private Area3D _hitBox;
	private ProgressBar _healthBar;
	private CanvasLayer _healthBarCanvas;
	private string _walkAnim = "";
	private string _deathAnim = "";
	private bool _isWalking = false;
	private float _currentHealth;
	private Vector3 _velocity = Vector3.Zero;
	private Vector3 _knockbackVelocity = Vector3.Zero;
	private float _invulnerabilityTimer = 0.0f;
	private bool _isDead = false;
	private bool _isInitialized = false;

	public override void _Ready()
	{
		SetupEnemy();
	}

	private void SetupEnemy()
	{
		if (_isInitialized) return;

		// Find AnimationPlayer
		_animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (_animPlayer == null)
		{
			_animPlayer = FindAnimationPlayer(this);
		}

		if (_animPlayer != null)
		{
			_animPlayer.RootNode = _animPlayer.GetParent().GetPath();
			_walkAnim = ResolveWalkAnimation(_animPlayer);
			
			// Death animation disabled - will implement differently later
			// SetupDeathAnimation();
		}

		// Find HitBox
		_hitBox = GetNodeOrNull<Area3D>(HitboxPath);

		// Create health bar
		CreateHealthBar();

		// Initialize health
		_currentHealth = MaxHealth;
		UpdateHealthBar();

		// Resolve player
		ResolvePlayer();

		_isInitialized = true;
		SetPhysicsProcess(true);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || !_isInitialized) return;

		float dt = (float)delta;
		_invulnerabilityTimer -= dt;

		if (!ResolvePlayer()) return;

		ApplyGravity(ref _velocity, dt);

		Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
		toPlayer.Y = 0.0f;

		float distance = toPlayer.Length();
		bool shouldChase = distance <= ChaseRange && distance > StopDistance;

		// Smooth acceleration-based movement
		Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);

		if (shouldChase && !toPlayer.IsZeroApprox())
		{
			Vector3 direction = toPlayer.Normalized();
			Vector3 desiredVelocity = direction * Speed;

			// Smooth acceleration toward target speed
			horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, Acceleration * dt);

			RotateTowardDirection(direction, dt);
			SetWalking(true);
		}
		else
		{
			// Smooth deceleration when not chasing
			if (IsOnFloor())
			{
				horizontalVelocity *= Friction;
			}
			else
			{
				horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, Deceleration * dt);
			}

			bool stillMoving = horizontalVelocity.Length() > 0.1f;
			SetWalking(stillMoving);
		}

		// Apply knockback with smooth damping
		_knockbackVelocity *= KnockbackDamping;
		if (_knockbackVelocity.Length() < 0.05f)
		{
			_knockbackVelocity = Vector3.Zero;
		}

		horizontalVelocity += _knockbackVelocity;

		_velocity.X = horizontalVelocity.X;
		_velocity.Z = horizontalVelocity.Z;

		Velocity = _velocity;
		MoveAndSlide();
		UpdateHealthBarPosition();
	}

	private bool ResolvePlayer()
	{
		if (IsInstanceValid(_player)) return true;

		_player = GetTree().GetFirstNodeInGroup(PlayerGroup) as Node3D;
		if (_player == null)
		{
			_player = GetTree().Root.FindChild("Player3D", true, false) as Node3D;
		}

		return _player != null;
	}

	private void ApplyGravity(ref Vector3 velocity, float delta)
	{
		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * delta;
		}
	}

	private void RotateTowardDirection(Vector3 direction, float delta)
	{
		if (direction == Vector3.Zero) return;

		float targetYaw = Mathf.Atan2(direction.X, direction.Z);
		float currentYaw = Rotation.Y;

		// Smooth rotation using LerpAngle for better feel
		float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, RotationSpeed * delta);

		Rotation = new Vector3(Rotation.X, newYaw, Rotation.Z);
	}

	private void SetWalking(bool walking)
	{
		if (_isWalking == walking) return;
		_isWalking = walking;

		if (_animPlayer == null || string.IsNullOrEmpty(_walkAnim)) return;

		if (walking)
			_animPlayer.Play(_walkAnim);
		else
			_animPlayer.Stop();
	}

	private string ResolveWalkAnimation(AnimationPlayer animationPlayer)
	{
		foreach (string libraryName in animationPlayer.GetAnimationLibraryList())
		{
			AnimationLibrary library = animationPlayer.GetAnimationLibrary(libraryName);

			if (library.HasAnimation("mixamo_com"))
			{
				Animation animation = library.GetAnimation("mixamo_com");
				animation.LoopMode = Animation.LoopModeEnum.Linear;
				return string.IsNullOrEmpty(libraryName) ? "mixamo_com" : $"{libraryName}/mixamo_com";
			}
		}

		foreach (string libraryName in animationPlayer.GetAnimationLibraryList())
		{
			AnimationLibrary library = animationPlayer.GetAnimationLibrary(libraryName);

			foreach (string animationName in library.GetAnimationList())
			{
				if (animationName == "Take 001") continue;

				Animation animation = library.GetAnimation(animationName);
				animation.LoopMode = Animation.LoopModeEnum.Linear;
				return string.IsNullOrEmpty(libraryName) ? animationName : $"{libraryName}/{animationName}";
			}
		}

		return "";
	}

	private void SetupDeathAnimation()
	{
		if (_animPlayer == null) return;

		// Load death animation from FBX file
		Animation deathAnimation = ExtractAnimationFromFbx(DeathFbxPath, false);
		
		if (deathAnimation == null)
		{
			GD.PrintErr("[Death] Failed to load death animation from Flying Back Death.fbx");
			return;
		}

		// Create or get the animation library
		AnimationLibrary library = null;
		try
		{
			library = _animPlayer.GetAnimationLibrary("EnemyAnims");
		}
		catch
		{
			// Library doesn't exist, will create new one
			library = null;
		}

		if (library == null)
		{
			library = new AnimationLibrary();
			_animPlayer.AddAnimationLibrary("EnemyAnims", library);
		}

		// Add death animation to library
		library.AddAnimation("Death", deathAnimation);
		_deathAnim = "EnemyAnims/Death";
		
		GD.Print("[Death] Death animation loaded: " + _deathAnim);
	}

	private Animation ExtractAnimationFromFbx(string fbxPath, bool loop)
	{
		PackedScene scene = GD.Load<PackedScene>(fbxPath);
		if (scene == null)
		{
			GD.PrintErr($"[ANIM_LOAD] Failed to load FBX: {fbxPath}");
			return null;
		}

		Node instance = scene.Instantiate();
		AnimationPlayer importedAnimPlayer = FindAnimationPlayer(instance);

		if (importedAnimPlayer == null)
		{
			GD.PrintErr($"[ANIM_LOAD] No AnimationPlayer found in {fbxPath}");
			instance.QueueFree();
			return null;
		}

		Animation foundAnimation = null;

		// Try to find mixamo_com animation
		foreach (string libraryName in importedAnimPlayer.GetAnimationLibraryList())
		{
			AnimationLibrary library = importedAnimPlayer.GetAnimationLibrary(libraryName);
			if (library.HasAnimation("mixamo_com"))
			{
				foundAnimation = (Animation)library.GetAnimation("mixamo_com").Duplicate();
				GD.Print($"[ANIM_LOAD] Found mixamo_com in {fbxPath}");
				break;
			}
		}

		// If not found, try first available animation
		if (foundAnimation == null)
		{
			foreach (string libraryName in importedAnimPlayer.GetAnimationLibraryList())
			{
				AnimationLibrary library = importedAnimPlayer.GetAnimationLibrary(libraryName);
				foreach (string animName in library.GetAnimationList())
				{
					if (animName != "Take 001")
					{
						foundAnimation = (Animation)library.GetAnimation(animName).Duplicate();
						GD.Print($"[ANIM_LOAD] Using animation '{animName}' from {fbxPath}");
						break;
					}
				}
				if (foundAnimation != null) break;
			}
		}

		instance.QueueFree();

		if (foundAnimation == null)
		{
			GD.PrintErr($"[ANIM_LOAD] No usable animation found in {fbxPath}");
			return null;
		}

		// Remove Hips track (root motion)
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

	private void CreateHealthBar()
	{
		_healthBarCanvas = new CanvasLayer();
		_healthBarCanvas.Name = "HealthBarCanvas";
		_healthBarCanvas.Layer = 10;
		AddChild(_healthBarCanvas);

		Control control = new Control();
		control.Name = "HealthBarContainer";
		control.AnchorLeft = 0.5f;
		control.AnchorTop = 0.0f;
		control.AnchorRight = 0.5f;
		control.AnchorBottom = 0.0f;
		control.OffsetLeft = -40;
		control.OffsetTop = 0;
		control.OffsetRight = 40;
		control.OffsetBottom = 20;
		_healthBarCanvas.AddChild(control);

		_healthBar = new ProgressBar();
		_healthBar.Name = "HealthBar";
		_healthBar.MinValue = 0.0f;
		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = MaxHealth;
		_healthBar.AnchorLeft = 0.0f;
		_healthBar.AnchorTop = 0.0f;
		_healthBar.AnchorRight = 1.0f;
		_healthBar.AnchorBottom = 1.0f;
		_healthBar.OffsetLeft = 0;
		_healthBar.OffsetTop = 0;
		_healthBar.OffsetRight = 0;
		_healthBar.OffsetBottom = 0;

		StyleBox styleBoxBackground = new StyleBoxFlat();
		((StyleBoxFlat)styleBoxBackground).BgColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
		_healthBar.AddThemeStyleboxOverride("background", styleBoxBackground);

		StyleBox styleBoxFill = new StyleBoxFlat();
		((StyleBoxFlat)styleBoxFill).BgColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);
		_healthBar.AddThemeStyleboxOverride("fill", styleBoxFill);

		control.AddChild(_healthBar);
	}

	private void UpdateHealthBar()
	{
		if (_healthBar != null)
		{
			_healthBar.MaxValue = MaxHealth;
			_healthBar.Value = Mathf.Max(0, _currentHealth);
		}
	}

	private void UpdateHealthBarPosition()
	{
		if (_healthBarCanvas == null) return;

		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Vector3 worldPos = GlobalPosition + Vector3.Up * HealthBarHeightOffset;
		Vector2 screenPos = camera.UnprojectPosition(worldPos);

		Control container = _healthBarCanvas.GetChild(0) as Control;
		if (container != null)
		{
			container.GlobalPosition = screenPos - new Vector2(40, 0);
		}
	}

	public void TakeDamage(float damage, Vector3 knockbackDirection, float knockbackForce)
	{
		if (_isDead || _invulnerabilityTimer > 0.0f) return;

		_currentHealth -= damage;
		UpdateHealthBar();

		_knockbackVelocity = knockbackDirection.Normalized() * knockbackForce * KnockbackResistance;

		if (_currentHealth <= 0.0f)
		{
			_isDead = true;
			Die();
		}
	}

	private void Die()
	{
		SetPhysicsProcess(false);
		SpawnRespawnEnemies();
		QueueFree();
	}

	private void SpawnRespawnEnemies()
	{
		Node parent = GetParent();
		if (parent == null) return;

		string scenePath = GetSceneFilePath();
		if (string.IsNullOrEmpty(scenePath)) return;

		PackedScene enemyScene = GD.Load<PackedScene>(scenePath);
		if (enemyScene == null) return;

		Vector3 spawnOffset1 = GlobalPosition + new Vector3(3, 0, 0);
		Vector3 spawnOffset2 = GlobalPosition + new Vector3(-3, 0, 0);

		// Spawn enemy 1
		try
		{
			Node spawnedNode1 = enemyScene.Instantiate();
			parent.AddChild(spawnedNode1);
			spawnedNode1.Set("global_position", spawnOffset1);

			// Check if _Ready was called
			ProgressBar healthBar = GetNodeOrNull<ProgressBar>($"{spawnedNode1.Name}/HealthBarCanvas/HealthBarContainer/HealthBar");
			if (healthBar == null)
			{
				// _Ready wasn't called, manually call SetupEnemy
				spawnedNode1.CallDeferred("SetupEnemy");
			}

			spawnedNode1.CallDeferred("SetInvulnerabilityTimer", SpawnInvulnerabilityTime);
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Error spawning enemy 1: {e.Message}");
		}

		// Spawn enemy 2
		try
		{
			Node spawnedNode2 = enemyScene.Instantiate();
			parent.AddChild(spawnedNode2);
			spawnedNode2.Set("global_position", spawnOffset2);

			// Check if _Ready was called
			ProgressBar healthBar2 = GetNodeOrNull<ProgressBar>($"{spawnedNode2.Name}/HealthBarCanvas/HealthBarContainer/HealthBar");
			if (healthBar2 == null)
			{
				// _Ready wasn't called, manually call SetupEnemy
				spawnedNode2.CallDeferred("SetupEnemy");
			}

			spawnedNode2.CallDeferred("SetInvulnerabilityTimer", SpawnInvulnerabilityTime);
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Error spawning enemy 2: {e.Message}");
		}
	}

	public void SetInvulnerabilityTimer(float time)
	{
		_invulnerabilityTimer = time;
	}

	public bool IsAlive => _currentHealth > 0.0f;
	public Area3D GetHitBox => _hitBox;
}
