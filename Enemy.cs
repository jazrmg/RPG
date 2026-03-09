using Godot;

public partial class Enemy : CharacterBody3D
{
	[Export] public float Speed = 1.0f;
	[Export] public float ChaseRange = 20.0f;

	private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	private AnimationPlayer _animPlayer;
	private Node3D _player;

	private string _walkAnim = "";
	private bool _isWalking = false;

	public override void _Ready()
	{
		_animPlayer = FindAnimationPlayer(GetNode("MonsterModel"));

		if (_animPlayer != null)
		{
			_animPlayer.RootNode = _animPlayer.GetParent().GetPath();

			foreach (var libName in _animPlayer.GetAnimationLibraryList())
			{
				var lib = _animPlayer.GetAnimationLibrary(libName);
				foreach (var animName in lib.GetAnimationList())
				{
					if (animName != "Take 001")
					{
						_walkAnim = libName == "" ? animName : $"{libName}/{animName}";
						var anim = lib.GetAnimation(animName);
						anim.LoopMode = Animation.LoopModeEnum.Linear;
						GD.Print($"Monster animation found: '{_walkAnim}'");
					}
				}
			}
		}
	}

	private AnimationPlayer FindAnimationPlayer(Node node)
	{
		if (node is AnimationPlayer ap)
			return ap;

		foreach (var child in node.GetChildren())
		{
			var found = FindAnimationPlayer(child);
			if (found != null)
				return found;
		}

		return null;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null)
		{
			_player = GetTree().Root.FindChild("Player3D", true, false) as Node3D;
			if (_player == null) return;
		}

		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * (float)delta;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		if (distance < ChaseRange && distance > 1.5f)
		{
			Vector3 direction = (_player.GlobalPosition - GlobalPosition);
			direction.Y = 0;
			direction = direction.Normalized();

			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;

			// Face the player.
			if (direction != Vector3.Zero)
			{
				LookAt(GlobalPosition + direction, Vector3.Up);
			}

			if (!_isWalking && _walkAnim != "")
			{
				_animPlayer.Play(_walkAnim);
				_isWalking = true;
			}
		}
		else
		{
			velocity.X = 0;
			velocity.Z = 0;

			if (_isWalking)
			{
				_animPlayer.Stop();
				_isWalking = false;
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
