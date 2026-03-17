using Godot;
using System.Collections.Generic;

public partial class Enemy : CharacterBody3D
{
	public enum EnemyType
	{
		Mutant,
		Warrok
	}

	[Export] public EnemyType EnemyTypeToSpawn = EnemyType.Mutant;

	private Dictionary<EnemyType, EnemyStats> _typeStats = new();

	public struct EnemyStats
	{
		public float Health;
		public float Damage;
		public float Speed;
		public float AttackSpeed;
		public float Scale;
		public Color ColorTint;
		public string TypeName;
	}

	[Export] public float Speed = 2.5f;
	[Export] public float Acceleration = 25.0f;
	[Export] public float Deceleration = 15.0f;
	[Export] public float Friction = 0.90f;
	[Export] public float ChaseRange = 20.0f;
	[Export] public float StopDistance = 1.5f;
	[Export] public float RotationSpeed = 12.0f;
	[Export] public float MaxHealth = 100.0f;
	[Export] public float KnockbackResistance = 0.3f;
	[Export] public float KnockbackDamping = 0.80f;
	[Export] public float HealthBarHeightOffset = 2.5f;
	[Export] public float SpawnInvulnerabilityTime = 0.5f;
	[Export] public float DamageToPlayer = 15.0f;
	[Export] public float AttackCooldown = 0.8f;

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
	private bool _isInitialized = false;
	private float _attackCooldownTimer = 0.0f;

	private EnemyStats _currentStats;

	private float _currentAnimSpeedScale = 1.0f;
	private float _targetAnimSpeedScale = 1.0f;
	private const float ANIMATION_SPEED_LERP = 10.0f;

	private float _targetRotationY = 0.0f;

	public override void _Ready()
	{
		AddToGroup("enemy");
		InitializeTypeStats();
		SetupEnemy();
	}

	private void InitializeTypeStats()
	{
		_typeStats[EnemyType.Mutant] = new EnemyStats
		{
			Health = 120f,
			Damage = 22f,
			Speed = 2.5f,
			AttackSpeed = 1.0f,
			Scale = 1.0f,
			ColorTint = new Color(0.9f, 0.6f, 0.2f, 1),
			TypeName = "Mutant"
		};

		_typeStats[EnemyType.Warrok] = new EnemyStats
		{
			Health = 70f,
			Damage = 25f,
			Speed = 4.0f,
			AttackSpeed = 1.3f,
			Scale = 0.9f,
			ColorTint = new Color(0.2f, 0.8f, 1.0f, 1),
			TypeName = "Warrok"
		};
	}

	private void SetupEnemy()
	{
		if (_isInitialized) return;

		_currentStats = _typeStats[EnemyTypeToSpawn];

		MaxHealth = _currentStats.Health;
		DamageToPlayer = _currentStats.Damage;
		Speed = _currentStats.Speed;
		AttackCooldown = _currentStats.AttackSpeed;

		Scale = Vector3.One * _currentStats.Scale;

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
		}

		_hitBox = GetNodeOrNull<Area3D>(HitboxPath);
		CreateHealthBar();

		_currentHealth = MaxHealth;
		UpdateHealthBar();

		ResolvePlayer();

		_targetRotationY = Rotation.Y;
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
		bool shouldChase = (distance <= ChaseRange) && (distance > (StopDistance + 0.3f));

		Vector3 horizontalVelocity = new Vector3(_velocity.X, 0, _velocity.Z);

		if (_isAttacking)
		{
			horizontalVelocity = Vector3.Zero;
			SetWalking(false);
		}
		else if (shouldChase && !toPlayer.IsZeroApprox())
		{
			Vector3 direction = toPlayer.Normalized();
			Vector3 desiredVelocity = direction * Speed;

			float accelFactor = EaseOutQuad(Mathf.Min(1.0f, Acceleration * 0.85f * dt));
			horizontalVelocity = horizontalVelocity.Lerp(desiredVelocity, accelFactor);

			RotateTowardDirectionSmoothed(direction, dt);
			SetWalking(true);
		}
		else
		{
			if (IsOnFloor())
			{
				horizontalVelocity *= 0.92f;
			}
			else
			{
				float airDecelFactor = EaseOutQuad(Mathf.Min(1.0f, Deceleration * 1.2f * dt));
				horizontalVelocity = horizontalVelocity.Lerp(Vector3.Zero, airDecelFactor);
			}

			bool stillMoving = horizontalVelocity.Length() > 0.1f;
			SetWalking(stillMoving);
		}

		_knockbackVelocity *= KnockbackDamping;
		_knockbackVelocity = _knockbackVelocity.Lerp(Vector3.Zero, 0.15f);
		if (_knockbackVelocity.Length() < 0.005f)
		{
			_knockbackVelocity = Vector3.Zero;
		}

		horizontalVelocity += _knockbackVelocity;

		_velocity.X = horizontalVelocity.X;
		_velocity.Z = horizontalVelocity.Z;

		_attackCooldownTimer -= dt;

		float attackRange = StopDistance + 0.4f;
		if (!_isAttacking && _player != null && distance <= attackRange && _attackCooldownTimer <= 0.0f)
		{
			DamagePlayerIfClose();
		}

		Velocity = _velocity;
		MoveAndSlide();
		UpdateHealthBarPosition();
	}

	public override void _Process(double delta)
	{
		if (_animPlayer != null)
		{
			float dt = (float)delta;
			_currentAnimSpeedScale = Mathf.Lerp(_currentAnimSpeedScale, _targetAnimSpeedScale, ANIMATION_SPEED_LERP * dt);
			
			if (!_isAttacking && _animPlayer.IsPlaying())
			{
				_animPlayer.SpeedScale = Mathf.Max(0.01f, _currentAnimSpeedScale);
			}
			else if (_isAttacking)
			{
				_animPlayer.SpeedScale = _currentAnimSpeedScale;
			}
		}

		if (Rotation.Y != _targetRotationY)
		{
			float dt = (float)delta;
			float newYaw = Mathf.LerpAngle(Rotation.Y, _targetRotationY, RotationSpeed * dt);
			Rotation = new Vector3(Rotation.X, newYaw, Rotation.Z);
		}
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

	private void RotateTowardDirectionSmoothed(Vector3 direction, float delta)
	{
		if (direction == Vector3.Zero) return;
		_targetRotationY = Mathf.Atan2(direction.X, direction.Z);
	}

	private void SetWalking(bool walking)
	{
		if (_isAttacking) return;

		if (_isWalking == walking) return;
		_isWalking = walking;

		if (_animPlayer == null || string.IsNullOrEmpty(_walkAnim)) return;

		if (walking)
		{
			if (_animPlayer.CurrentAnimation != _walkAnim || !_animPlayer.IsPlaying())
			{
				_animPlayer.Play(_walkAnim);
			}
			_targetAnimSpeedScale = 1.1f;
		}
		else
		{
			_targetAnimSpeedScale = 0.0f;
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
				RemovePositionTracks(animation);
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
				RemovePositionTracks(animation);
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

			library.AddAnimation("Attack", attackAnimation);
			string attackAnimPath = "EnemyAnims/Attack";
			
			return attackAnimPath;
		}
		catch
		{
			return "";
		}
	}

	private void SetupDeathAnimation()
	{
		if (_animPlayer == null) return;

		Animation deathAnimation = AnimationHelper.ExtractAnimationFromFbx(DeathFbxPath, false);
		
		if (deathAnimation == null)
		{
			return;
		}

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

		library.AddAnimation("Death", deathAnimation);
		_deathAnim = "EnemyAnims/Death";
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

		Color barColor = _currentStats.ColorTint;
		StyleBox styleBoxFill = new StyleBoxFlat();
		((StyleBoxFlat)styleBoxFill).BgColor = barColor;
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
		
		Tween tween = CreateTween();
		Vector2 startPos = damageLabel.GlobalPosition;
		tween.SetTrans(Tween.TransitionType.Linear);
		
		float moveDistance = isCritical ? 80.0f : 60.0f;
		tween.Parallel().TweenProperty(damageLabel, "global_position", startPos - Vector2.Up * moveDistance, 1.2f);
		tween.Parallel().TweenProperty(damageLabel, "modulate", new Color(1, 1, 1, 0), 1.2f);
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
		if (_isDead || _invulnerabilityTimer > 0.0f) return;

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

	private void Die()
	{
		SetPhysicsProcess(false);
		SpawnRespawnEnemies();
		QueueFree();
	}

	public void FlashHit()
	{
		MeshInstance3D meshInstance = FindFirstMeshInstance(this);
		if (meshInstance == null) return;

		Material originalMaterial = meshInstance.GetActiveMaterial(0);
		if (originalMaterial == null) return;

		var flashMaterial = new StandardMaterial3D();
		flashMaterial.AlbedoColor = Colors.White;

		meshInstance.SetSurfaceOverrideMaterial(0, flashMaterial);

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

		if (_isAttacking)
			return;

		_isAttacking = true;

		Vector3 attackStartPosition = _player.GlobalPosition;
		float initialDistance = (_player.GlobalPosition - GlobalPosition).Length();

		if (_animPlayer != null)
		{
			_animPlayer.Stop();
		}

		if (_animPlayer != null && !string.IsNullOrEmpty(_attackAnim))
		{
			try
			{
				_targetAnimSpeedScale = 1.6f;
				_animPlayer.Play(_attackAnim);

				Animation attackAnimation = _animPlayer.GetAnimation(_attackAnim);
				if (attackAnimation != null)
				{
					float animDuration = (float)attackAnimation.Length / 1.6f;
					
					float damageDelay = animDuration * 0.3f;
					GetTree().CreateTimer(damageDelay).Timeout += () =>
					{
						if (!IsInstanceValid(this) || !IsInstanceValid(player) || !IsInstanceValid(_player))
							return;
						
						Vector3 currentPosition = _player.GlobalPosition;
						float currentDistance = (currentPosition - GlobalPosition).Length();
						
						if (currentDistance <= initialDistance + 1.5f)
						{
							player.PlayerTakeDamage(DamageToPlayer);
						}
					};

					float cooldownDuration = animDuration + 0.5f;
					_attackCooldownTimer = cooldownDuration;

					GetTree().CreateTimer(animDuration + 0.15f).Timeout += () =>
					{
						if (IsInstanceValid(this))
						{
							_targetAnimSpeedScale = 1.0f;
							_isAttacking = false;
							_isWalking = false;
						}
					};
				}
			}
			catch
			{
				_targetAnimSpeedScale = 1.0f;
				_isAttacking = false;
				_isWalking = false;
			}
		}
		else
		{
			_isAttacking = false;
		}
	}

	private void RemovePositionTracks(Animation animation)
	{
		for (int i = animation.GetTrackCount() - 1; i >= 0; i--)
		{
			if (animation.TrackGetType(i) == Animation.TrackType.Position3D)
				animation.RemoveTrack(i);
		}
	}

	private float EaseOutQuad(float t)
	{
		return 1.0f - (1.0f - t) * (1.0f - t);
	}

	private float EaseInOutQuad(float t)
	{
		return t < 0.5f ? 2.0f * t * t : -1.0f + (4.0f - 2.0f * t) * t;
	}

	public bool IsAlive => _currentHealth > 0.0f;
	public Area3D GetHitBox => _hitBox;
}
