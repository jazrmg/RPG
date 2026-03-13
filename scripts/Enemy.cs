using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody3D
{
	[Export] public float Speed = 2.5f;
	[Export] public float Acceleration = 8.0f;
	[Export] public float Deceleration = 6.0f;
	[Export] public float ChaseRange = 20.0f;
	[Export] public float StopDistance = 1.5f;
	[Export] public float RotationSpeed = 8.0f;
	[Export] public float MaxHealth = 100.0f;
	[Export] public float KnockbackResistance = 0.3f;
	[Export] public float KnockbackDecaySpeed = 6.0f;
	[Export] public float HealthBarHeightOffset = 2.5f;
	[Export] public float SpawnInvulnerabilityTime = 0.5f;
	[Export] public float DamageToPlayer = 15.0f;
	[Export] public float AttackCooldown = 0.8f;
	[Export] public float AnimBlendTime = 0.15f;
	[Export] public float DeathAnimDuration = 2.0f;

	// PUBLIC ACCESS: Current health for auto battle AI
	public float CurrentHealth => _currentHealth;

	private const string PlayerGroup = "player";
	private const string HitboxPath = "HitBox";
	private const string DeathFbxPath = "res://models/enemy/Flying Back Death.fbx";
	private const string AttackFbxPath = "res://models/enemy/Zombie_Attack.fbx";

	private readonly float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

	private AnimationPlayer _animPlayer;
	private Node3D _player;
	private Area3D _hitBox;
	private ProgressBar _healthBar;
	private CanvasLayer _healthBarCanvas;
	private string _walkAnim = "";
	private string _deathAnim = "";
	private string _attackAnim = "";

	private bool _isWalking = false;
	private bool _isAttacking = false;
	private float _currentHealth;
	private Vector3 _velocity = Vector3.Zero;
	private Vector3 _knockbackVelocity = Vector3.Zero;
	private float _invulnerabilityTimer = 0.0f;
	private bool _isDead = false;
	private bool _isDying = false;  // Separate from _isDead for death animation phase
	private bool _isInitialized = false;
	private float _attackCooldownTimer = 0.0f;
	private Vector2 _smoothHealthBarPos = Vector2.Zero;
	private bool _healthBarPosInitialized = false;
	private string _currentAnimName = "";

	/// <summary>
	/// Frame-rate independent exponential decay factor.
	/// </summary>
	private static float ExpDecay(float speed, float delta)
	{
		return 1.0f - Mathf.Exp(-speed * delta);
	}

	public override void _Ready()
	{
		AddToGroup("enemy");
		SetupEnemy();
	}

	private void SetupEnemy()
	{
		if (_isInitialized) return;

		_animPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (_animPlayer == null)
		{
			_animPlayer = FindAnimationPlayer(this);
		}

		if (_animPlayer != null)
		{
			_animPlayer.RootNode = _animPlayer.GetParent().GetPath();
			_walkAnim = ResolveWalkAnimation(_animPlayer);
			_attackAnim = LoadAttackAnimation();
			SetupDeathAnimation();  // Load death animation at setup time
		}

		_hitBox = GetNodeOrNull<Area3D>(HitboxPath);
		CreateHealthBar();

		_currentHealth = MaxHealth;
		UpdateHealthBar();

		ResolvePlayer();

		_isInitialized = true;
		SetPhysicsProcess(true);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isInitialized) return;

		float dt = (float)delta;

		// During death animation: just apply gravity and knockback slide, no AI
		if (_isDying)
		{
			if (!IsOnFloor())
				_velocity.Y -= _gravity * dt;
			else
				_velocity.Y = Mathf.Lerp(_velocity.Y, 0.0f, ExpDecay(20.0f, dt));

			// Decay any remaining knockback during death
			_knockbackVelocity = _knockbackVelocity.Lerp(Vector3.Zero, ExpDecay(KnockbackDecaySpeed * 0.5f, dt));
			_velocity.X = _knockbackVelocity.X;
			_velocity.Z = _knockbackVelocity.Z;

			Velocity = _velocity;
			MoveAndSlide();
			UpdateHealthBarPosition(dt);
			return;
		}

		if (_isDead) return;

		_invulnerabilityTimer -= dt;

		if (!ResolvePlayer()) return;

		ApplyGravity(ref _velocity, dt);

		Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
		toPlayer.Y = 0.0f;

		float distance = toPlayer.Length();
		bool shouldChase = distance <= ChaseRange && distance > StopDistance;

		Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);

		if (_isAttacking)
		{
			// Smooth deceleration while attacking (frame-rate independent)
			horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, ExpDecay(Deceleration * 2.0f, dt));
			SetWalking(false);
		}
		else if (shouldChase && !toPlayer.IsZeroApprox())
		{
			Vector3 direction = toPlayer.Normalized();
			Vector3 desiredVelocity = direction * Speed;

			// Frame-rate independent acceleration
			horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, ExpDecay(Acceleration, dt));

			RotateTowardDirection(direction, dt);
			SetWalking(true);
		}
		else
		{
			// Frame-rate independent friction / deceleration
			float frictionSpeed = IsOnFloor() ? Deceleration : Deceleration * 0.7f;
			horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, ExpDecay(frictionSpeed, dt));

			bool stillMoving = horizontalVelocity.LengthSquared() > 0.01f;
			SetWalking(stillMoving);
		}

		// Frame-rate independent knockback decay
		_knockbackVelocity = _knockbackVelocity.Lerp(Vector3.Zero, ExpDecay(KnockbackDecaySpeed, dt));
		if (_knockbackVelocity.LengthSquared() < 0.001f)
		{
			_knockbackVelocity = Vector3.Zero;
		}

		horizontalVelocity += _knockbackVelocity;

		_velocity.X = horizontalVelocity.X;
		_velocity.Z = horizontalVelocity.Z;

		_attackCooldownTimer -= dt;

		float attackRange = StopDistance + 0.4f;
		if (_player != null && distance <= attackRange && _attackCooldownTimer <= 0.0f)
		{
			DamagePlayerIfClose();
		}

		Velocity = _velocity;
		MoveAndSlide();
		UpdateHealthBarPosition(dt);
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
		else
		{
			// Smooth ground settle
			velocity.Y = Mathf.Lerp(velocity.Y, 0.0f, ExpDecay(20.0f, delta));
		}
	}

	private void RotateTowardDirection(Vector3 direction, float delta)
	{
		if (direction == Vector3.Zero) return;

		float targetYaw = Mathf.Atan2(direction.X, direction.Z);
		float currentYaw = Rotation.Y;

		// Frame-rate independent rotation
		float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, ExpDecay(RotationSpeed, delta));

		Rotation = new Vector3(Rotation.X, newYaw, Rotation.Z);
	}

	private void SetWalking(bool walking)
	{
		if (_isAttacking || _isDying) return;

		if (_isWalking == walking) return;
		_isWalking = walking;

		if (_animPlayer == null || string.IsNullOrEmpty(_walkAnim)) return;

		if (walking)
		{
			PlayAnimSmooth(_walkAnim, "walk");
			_animPlayer.SpeedScale = 1.1f;
		}
		else
		{
			// Freeze at current animation frame instead of snapping to bind pose
			if (_animPlayer.IsPlaying())
			{
				_animPlayer.SpeedScale = 0.0f;
			}
			_currentAnimName = "";
		}
	}

	/// <summary>
	/// Cross-fade to animation path with blend time. Prevents popping.
	/// </summary>
	private void PlayAnimSmooth(string animPath, string trackingName, float customBlend = -1.0f)
	{
		if (_currentAnimName == trackingName) return;
		_currentAnimName = trackingName;

		float blend = customBlend >= 0 ? customBlend : AnimBlendTime;

		if (_animPlayer.CurrentAnimation != "" && blend > 0)
		{
			_animPlayer.Play(animPath, blend);
		}
		else
		{
			_animPlayer.Play(animPath);
		}
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

	private string LoadAttackAnimation()
	{
		if (_animPlayer == null) return "";

		try
		{
			Animation attackAnimation = AnimationHelper.ExtractAnimationFromFbx(AttackFbxPath, false);

			if (attackAnimation == null)
			{
				return "";
			}

			AnimationLibrary library = GetOrCreateEnemyAnimLibrary();
			library.AddAnimation("Attack", attackAnimation);
			return "EnemyAnims/Attack";
		}
		catch
		{
			return "";
		}
	}

	/// <summary>
	/// Load death animation from FBX so it's ready when the enemy dies.
	/// </summary>
	private void SetupDeathAnimation()
	{
		if (_animPlayer == null) return;

		try
		{
			Animation deathAnimation = AnimationHelper.ExtractAnimationFromFbx(DeathFbxPath, false);

			if (deathAnimation == null)
			{
				return;
			}

			AnimationLibrary library = GetOrCreateEnemyAnimLibrary();

			if (!library.HasAnimation("Death"))
			{
				library.AddAnimation("Death", deathAnimation);
			}

			_deathAnim = "EnemyAnims/Death";
		}
		catch
		{
		}
	}

	/// <summary>
	/// Helper: get existing or create new EnemyAnims animation library.
	/// </summary>
	private AnimationLibrary GetOrCreateEnemyAnimLibrary()
	{
		AnimationLibrary library = null;

		try
		{
			if (_animPlayer.HasAnimationLibrary("EnemyAnims"))
			{
				library = _animPlayer.GetAnimationLibrary("EnemyAnims");
			}
		}
		catch
		{
			library = null;
		}

		if (library == null)
		{
			library = new AnimationLibrary();
			_animPlayer.AddAnimationLibrary("EnemyAnims", library);
		}

		return library;
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

	private void UpdateHealthBarPosition(float delta)
	{
		if (_healthBarCanvas == null) return;

		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Vector3 worldPos = GlobalPosition + Vector3.Up * HealthBarHeightOffset;
		Vector2 targetScreenPos = camera.UnprojectPosition(worldPos) - new Vector2(40, 0);

		// Smooth health bar position to prevent jitter
		if (!_healthBarPosInitialized)
		{
			_smoothHealthBarPos = targetScreenPos;
			_healthBarPosInitialized = true;
		}
		else
		{
			_smoothHealthBarPos = _smoothHealthBarPos.Lerp(targetScreenPos, ExpDecay(20.0f, delta));
		}

		Control container = _healthBarCanvas.GetChild(0) as Control;
		if (container != null)
		{
			container.GlobalPosition = _smoothHealthBarPos;
		}
	}

	private void ShowDamageNumber(float damage, bool isCritical = false)
	{
		Camera3D camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		Label damageLabel = new Label();

		if (isCritical)
		{
			damageLabel.Text = $"{damage:F0}!";
			damageLabel.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0, 1));
		}
		else
		{
			damageLabel.Text = damage.ToString("F0");
			damageLabel.AddThemeColorOverride("font_color", Colors.Red);
		}

		int fontSize = isCritical ? 40 : 32;
		damageLabel.AddThemeFontSizeOverride("font_size", fontSize);
		damageLabel.ZIndex = 100;

		Vector3 worldPos = GlobalPosition + Vector3.Up * 2.5f;
		Vector2 screenPos = camera.UnprojectPosition(worldPos);
		damageLabel.GlobalPosition = screenPos;

		GetTree().Root.AddChild(damageLabel);

		// Smooth ease-out float + fade
		Tween tween = CreateTween();
		Vector2 startPos = damageLabel.GlobalPosition;
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.SetEase(Tween.EaseType.Out);

		float moveDistance = isCritical ? 80.0f : 60.0f;
		tween.Parallel().TweenProperty(damageLabel, "global_position", startPos - Vector2.Up * moveDistance, 1.0f);
		tween.Parallel().TweenProperty(damageLabel, "modulate", new Color(1, 1, 1, 0), 1.0f);
		tween.TweenCallback(Callable.From(() => {
			if (damageLabel != null && IsInstanceValid(damageLabel))
				damageLabel.QueueFree();
		}));

		GetTree().CreateTimer(2.0f).Timeout += () => {
			if (damageLabel != null && IsInstanceValid(damageLabel))
			{
				damageLabel.QueueFree();
			}
		};
	}

	public void TakeDamage(float damage, Vector3 knockbackDirection, float knockbackForce, bool isCritical = false)
	{
		if (_isDead || _isDying || _invulnerabilityTimer > 0.0f) return;

		_currentHealth -= damage;
		UpdateHealthBar();

		_knockbackVelocity = knockbackDirection.Normalized() * knockbackForce * KnockbackResistance;

		ShowDamageNumber(damage, isCritical);

		if (_currentHealth <= 0.0f)
		{
			_isDead = true;
			Die();
		}
	}

	/// <summary>
	/// Play death animation, then spawn replacements and remove self.
	/// During the dying phase, the enemy slides from knockback but does no AI.
	/// </summary>
	private void Die()
	{
		_isDying = true;
		_isAttacking = false;
		_isWalking = false;

		// Remove from enemy group immediately so auto-battle doesn't target a dying enemy
		RemoveFromGroup("enemy");

		// Hide health bar
		if (_healthBarCanvas != null)
			_healthBarCanvas.Visible = false;

		// Play death animation if available
		if (_animPlayer != null && !string.IsNullOrEmpty(_deathAnim))
		{
			try
			{
				_animPlayer.Stop();
				_animPlayer.SpeedScale = 1.0f;
				PlayAnimSmooth(_deathAnim, "death", 0.1f);

				// Get actual animation duration, fall back to export value
				Animation deathAnimation = _animPlayer.GetAnimation(_deathAnim);
				float duration = deathAnimation != null ? (float)deathAnimation.Length : DeathAnimDuration;

				// After animation finishes, spawn replacements and remove
				GetTree().CreateTimer(duration + 0.1f).Timeout += () =>
				{
					if (IsInstanceValid(this))
					{
						SpawnRespawnEnemies();
						QueueFree();
					}
				};
			}
			catch
			{
				// If animation fails, die immediately
				SpawnRespawnEnemies();
				QueueFree();
			}
		}
		else
		{
			// No death animation available — die immediately
			SpawnRespawnEnemies();
			QueueFree();
		}
	}

	public void FlashHit()
	{
		// Find first MeshInstance3D in the enemy
		MeshInstance3D meshInstance = FindFirstMeshInstance(this);
		if (meshInstance == null) return;

		// Store original material
		Material originalMaterial = meshInstance.GetActiveMaterial(0);
		if (originalMaterial == null) return;

		// Create white material for flash
		var flashMaterial = new StandardMaterial3D();
		flashMaterial.AlbedoColor = Colors.White;

		// Apply white material
		meshInstance.SetSurfaceOverrideMaterial(0, flashMaterial);

		// Return to normal after short duration
		GetTree().CreateTimer(0.1f).Timeout += () => {
			if (meshInstance != null && IsInstanceValid(meshInstance) && originalMaterial != null)
			{
				meshInstance.SetSurfaceOverrideMaterial(0, originalMaterial);
			}
		};
	}

	private MeshInstance3D FindFirstMeshInstance(Node node)
	{
		if (node is MeshInstance3D mesh)
			return mesh;

		foreach (Node child in node.GetChildren())
		{
			MeshInstance3D found = FindFirstMeshInstance(child);
			if (found != null)
				return found;
		}

		return null;
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

		try
		{
			Node spawnedNode1 = enemyScene.Instantiate();
			parent.AddChild(spawnedNode1);
			spawnedNode1.Set("global_position", spawnOffset1);
			spawnedNode1.CallDeferred("SetInvulnerabilityTimer", SpawnInvulnerabilityTime);
		}
		catch
		{
		}

		try
		{
			Node spawnedNode2 = enemyScene.Instantiate();
			parent.AddChild(spawnedNode2);
			spawnedNode2.Set("global_position", spawnOffset2);
			spawnedNode2.CallDeferred("SetInvulnerabilityTimer", SpawnInvulnerabilityTime);
		}
		catch
		{
		}
	}

	public void SetInvulnerabilityTimer(float time)
	{
		_invulnerabilityTimer = time;
	}

	private void DamagePlayerIfClose()
	{
		if (_player is not Player3D player)
			return;

		if (!IsInstanceValid(player))
			return;

		if (_isAttacking || _isDying)
			return;

		_isAttacking = true;

		Vector3 attackStartPosition = _player.GlobalPosition;
		float initialDistance = (_player.GlobalPosition - GlobalPosition).Length();

		// Stop walk animation smoothly before attacking
		if (_animPlayer != null)
		{
			_animPlayer.SpeedScale = 1.0f;
		}

		if (_animPlayer != null && !string.IsNullOrEmpty(_attackAnim))
		{
			try
			{
				// Cross-fade into attack animation
				PlayAnimSmooth(_attackAnim, "attack", 0.1f);
				_animPlayer.SpeedScale = 1.5f;

				Animation attackAnimation = _animPlayer.GetAnimation(_attackAnim);
				if (attackAnimation != null)
				{
					float animDuration = (float)attackAnimation.Length / 1.5f;

					float damageDelay = animDuration * 0.3f;
					GetTree().CreateTimer(damageDelay).Timeout += () =>
					{
						if (!IsInstanceValid(this) || !IsInstanceValid(player) || !IsInstanceValid(_player))
							return;

						if (_isDying)
							return;

						Vector3 currentPosition = _player.GlobalPosition;
						float currentDistance = (currentPosition - GlobalPosition).Length();

						if (currentDistance <= initialDistance + 0.5f && currentDistance <= StopDistance + 0.5f)
						{
							player.PlayerTakeDamage(DamageToPlayer);
						}
					};

					GetTree().CreateTimer(animDuration + 0.1f).Timeout += () =>
					{
						if (IsInstanceValid(this))
						{
							_animPlayer.SpeedScale = 1.0f;
							_isAttacking = false;
							_isWalking = false;
							_currentAnimName = "";  // Allow next animation to cross-fade in
						}
					};

					_attackCooldownTimer = animDuration + 0.2f;
				}
			}
			catch
			{
				_animPlayer.SpeedScale = 1.0f;
				_isAttacking = false;
				_isWalking = false;
				_currentAnimName = "";
			}
		}
		else
		{
			_isAttacking = false;
		}
	}

	public bool IsAlive => _currentHealth > 0.0f;
	public Area3D GetHitBox => _hitBox;
}
