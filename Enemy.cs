using Godot;

public partial class Enemy : CharacterBody3D
{
	[Export] public float Speed = 1.5f;
	[Export] public float ChaseRange = 20.0f;
	[Export] public float StopDistance = 1.5f;
	[Export] public float RotationSpeed = 8.0f;
	[Export] public float GroundDeceleration = 12.0f;
	
	[Export] public float MaxHealth = 100.0f;
	[Export] public float KnockbackResistance = 0.5f;

	private const string PlayerGroup = "player";
	private const string MonsterModelPath = "MonsterModel";
	private const string HitboxPath = "HitBox";

	private readonly float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

	private AnimationPlayer _animPlayer;
	private Node3D _player;
	private Area3D _hitBox;

	private string _walkAnim = "";
	private bool _isWalking = false;
	
	private float _currentHealth;
	private Vector3 _knockbackVelocity = Vector3.Zero;
	private float _knockbackDamping = 0.9f;

	public override void _Ready()
	{
		Node monsterModel = GetNodeOrNull<Node>(MonsterModelPath);
		if (monsterModel == null)
		{
			GD.PrintErr("Enemy: MonsterModel node not found.");
			SetPhysicsProcess(false);
			return;
		}

		_animPlayer = FindAnimationPlayer(monsterModel);
		if (_animPlayer == null)
		{
			GD.PrintErr("Enemy: AnimationPlayer not found.");
		}
		else
		{
			_animPlayer.RootNode = _animPlayer.GetParent().GetPath();
			_walkAnim = ResolveWalkAnimation(_animPlayer);
		}

		_hitBox = GetNodeOrNull<Area3D>(HitboxPath);
		if (_hitBox == null)
		{
			GD.PrintErr("Enemy: HitBox node not found at 'HitBox'. Add an Area3D child node with this name.");
		}

		_currentHealth = MaxHealth;
		ResolvePlayer();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (!ResolvePlayer())
			return;

		Vector3 velocity = Velocity;
		ApplyGravity(ref velocity, dt);

		Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
		toPlayer.Y = 0.0f;

		float distance = toPlayer.Length();
		bool shouldChase = distance <= ChaseRange && distance > StopDistance;

		if (shouldChase && !toPlayer.IsZeroApprox())
		{
			Vector3 direction = toPlayer.Normalized();

			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;

			RotateTowardDirection(direction, dt);
			SetWalking(true);
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0.0f, GroundDeceleration * dt);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0.0f, GroundDeceleration * dt);

			bool stillMoving =
				Mathf.Abs(velocity.X) > 0.01f ||
				Mathf.Abs(velocity.Z) > 0.01f;

			SetWalking(stillMoving);
		}

		// Apply knockback
		velocity.X += _knockbackVelocity.X;
		velocity.Z += _knockbackVelocity.Z;
		_knockbackVelocity *= _knockbackDamping;

		if (_knockbackVelocity.Length() < 0.1f)
		{
			_knockbackVelocity = Vector3.Zero;
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private bool ResolvePlayer()
	{
		if (IsInstanceValid(_player))
			return true;

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
		if (direction == Vector3.Zero)
			return;

		float targetYaw = Mathf.Atan2(direction.X, direction.Z);
		float t = Mathf.Clamp(RotationSpeed * delta, 0.0f, 1.0f);

		Rotation = new Vector3(
			Rotation.X,
			Mathf.LerpAngle(Rotation.Y, targetYaw, t),
			Rotation.Z
		);
	}

	private void SetWalking(bool walking)
	{
		if (_isWalking == walking)
			return;

		_isWalking = walking;

		if (_animPlayer == null || string.IsNullOrEmpty(_walkAnim))
			return;

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
				if (animationName == "Take 001")
					continue;

				Animation animation = library.GetAnimation(animationName);
				animation.LoopMode = Animation.LoopModeEnum.Linear;
				return string.IsNullOrEmpty(libraryName) ? animationName : $"{libraryName}/{animationName}";
			}
		}

		GD.PrintErr("Enemy: No usable walk animation found.");
		return "";
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

	public void TakeDamage(float damage, Vector3 knockbackDirection, float knockbackForce)
	{
		_currentHealth -= damage;
		
		// Apply knockback
		_knockbackVelocity = knockbackDirection.Normalized() * knockbackForce * KnockbackResistance;

		GD.Print($"Enemy hit! Health: {_currentHealth}/{MaxHealth}");

		if (_currentHealth <= 0.0f)
		{
			Die();
		}
	}

	private void Die()
	{
		GD.Print("Enemy died!");
		QueueFree();
	}

	public bool IsAlive => _currentHealth > 0.0f;

	public Area3D GetHitBox => _hitBox;
}
