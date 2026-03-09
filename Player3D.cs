using Godot;

public partial class Player3D : CharacterBody3D
{
	[Export] public float Speed = 2.5f;
	[Export] public float RunSpeed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 0.3f;

	private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	private Camera3D _camera;
	private SpringArm3D _springArm;
	private AnimationPlayer _animPlayer;

	private string _idleAnim = "";
	private string _walkAnim = "";
	private string _runAnim = "";
	private string _jumpAnim = "";
	private string _sitAnim = "";
	private string _currentState = "";

	public override void _Ready()
	{
		_springArm = GetNode<SpringArm3D>("SpringArm3D");
		_camera = GetNode<Camera3D>("SpringArm3D/Camera3D");
		_animPlayer = GetNode<AnimationPlayer>("CharacterModel/AnimationPlayer");

		Input.MouseMode = Input.MouseModeEnum.Captured;

		// Force root node to CharacterModel.
		_animPlayer.RootNode = _animPlayer.GetParent().GetPath();

		// Print what's available in the model.
		foreach (var libName in _animPlayer.GetAnimationLibraryList())
		{
			var lib = _animPlayer.GetAnimationLibrary(libName);
			foreach (var animName in lib.GetAnimationList())
			{
				string fullName = libName == "" ? animName : $"{libName}/{animName}";
				GD.Print($"Available: '{fullName}'");
			}
		}

		// Create custom library for all animations.
		var customLibrary = new AnimationLibrary();
		_animPlayer.AddAnimationLibrary("Custom", customLibrary);

		// Load idle animation from Idle.fbx.
		var idleAnim = ExtractAnimation("res://models/player/Idle.fbx");
		if (idleAnim != null)
		{
			idleAnim.LoopMode = Animation.LoopModeEnum.Linear;
			customLibrary.AddAnimation("Idle", idleAnim);
			_idleAnim = "Custom/Idle";
			GD.Print("Idle animation loaded!");
		}

		// Load walking animation.
		var walkAnim = ExtractAnimation("res://models/player/Walking.fbx");
		if (walkAnim != null)
		{
			customLibrary.AddAnimation("Walking", walkAnim);
			_walkAnim = "Custom/Walking";
			GD.Print("Walking animation loaded!");
		}

		// Load running animation.
		var runAnim = ExtractAnimation("res://models/player/Running.fbx");
		if (runAnim != null)
		{
			customLibrary.AddAnimation("Running", runAnim);
			_runAnim = "Custom/Running";
			GD.Print("Running animation loaded!");
		}

		// Load jump animation.
		var jumpAnim = ExtractAnimation("res://models/player/Jump.fbx");
		if (jumpAnim != null)
		{
			jumpAnim.LoopMode = Animation.LoopModeEnum.None;
			customLibrary.AddAnimation("Jump", jumpAnim);
			_jumpAnim = "Custom/Jump";
			GD.Print("Jump animation loaded!");
		}

		// Load sitting animation.
		var sitAnim = ExtractAnimation("res://models/player/Sitting Idle.fbx");
		if (sitAnim != null)
		{
			sitAnim.LoopMode = Animation.LoopModeEnum.Linear;
			customLibrary.AddAnimation("Sitting", sitAnim);
			_sitAnim = "Custom/Sitting";
			GD.Print("Sitting animation loaded!");
		}

		GD.Print($"Idle: '{_idleAnim}' Walk: '{_walkAnim}' Run: '{_runAnim}' Jump: '{_jumpAnim}' Sit: '{_sitAnim}'");

		// Start idle.
		if (_idleAnim != "")
		{
			_animPlayer.Play(_idleAnim);
			_currentState = "idle";
		}
	}

	private Animation ExtractAnimation(string fbxPath)
	{
		var scene = GD.Load<PackedScene>(fbxPath);
		if (scene == null)
		{
			GD.PrintErr($"Could not load: {fbxPath}");
			return null;
		}

		var instance = scene.Instantiate();

		// Search for AnimationPlayer anywhere in the scene.
		var otherAnimPlayer = FindAnimationPlayer(instance);
		if (otherAnimPlayer == null)
		{
			GD.PrintErr($"No AnimationPlayer in: {fbxPath}");
			instance.QueueFree();
			return null;
		}

		Animation foundAnim = null;
		foreach (var libName in otherAnimPlayer.GetAnimationLibraryList())
		{
			var lib = otherAnimPlayer.GetAnimationLibrary(libName);
			if (lib.HasAnimation("mixamo_com"))
			{
				foundAnim = (Animation)lib.GetAnimation("mixamo_com").Duplicate();
				break;
			}
		}

		instance.QueueFree();

		// Remove root motion — strip the Hips POSITION track.
		if (foundAnim != null)
		{
			for (int i = foundAnim.GetTrackCount() - 1; i >= 0; i--)
			{
				string path = foundAnim.TrackGetPath(i);

				if (path.Contains("Hips") && foundAnim.TrackGetType(i) == Animation.TrackType.Position3D)
				{
					GD.Print($"Removing root motion track: {path}");
					foundAnim.RemoveTrack(i);
				}
			}

			// Make the animation loop by default (jump overrides this later).
			foundAnim.LoopMode = Animation.LoopModeEnum.Linear;
		}

		return foundAnim;
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

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			// Horizontal mouse rotates the player.
			RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));

			// Vertical mouse rotates the spring arm up/down.
			_springArm.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));

			Vector3 armRotation = _springArm.Rotation;
			armRotation.X = Mathf.Clamp(armRotation.X, Mathf.DegToRad(-60), Mathf.DegToRad(30));
			_springArm.Rotation = armRotation;
		}

		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * (float)delta;
		}

		// Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor() && _currentState != "sit")
		{
			velocity.Y = JumpVelocity;
			if (_jumpAnim != "")
			{
				_animPlayer.Play(_jumpAnim);
				_currentState = "jump";
			}
		}

		// Sit toggle.
		if (Input.IsActionJustPressed("sit") && IsOnFloor())
		{
			if (_currentState == "sit")
			{
				_animPlayer.Play(_idleAnim);
				_currentState = "idle";
			}
			else if (_sitAnim != "")
			{
				_animPlayer.Play(_sitAnim);
				_currentState = "sit";
			}
		}

		// Block movement while sitting.
		Vector2 inputDir = Vector2.Zero;
		if (_currentState != "sit")
		{
			inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		}

		Vector3 forward = -GlobalTransform.Basis.Z;
		Vector3 right = GlobalTransform.Basis.X;

		forward.Y = 0;
		right.Y = 0;
		forward = forward.Normalized();
		right = right.Normalized();

		Vector3 direction = (forward * inputDir.Y * -1) + (right * inputDir.X);

		bool isRunning = Input.IsKeyPressed(Key.Shift);
		float currentSpeed = isRunning ? RunSpeed : Speed;

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
		}

		// Only change animations when on the floor AND not jumping or sitting.
		if (IsOnFloor() && _currentState != "jump" && _currentState != "sit")
		{
			if (direction != Vector3.Zero)
			{
				if (isRunning && _currentState != "run" && _runAnim != "")
				{
					_animPlayer.Play(_runAnim);
					_currentState = "run";
				}
				else if (!isRunning && _currentState != "walk" && _walkAnim != "")
				{
					_animPlayer.Play(_walkAnim);
					_currentState = "walk";
				}
			}
			else
			{
				if (_currentState != "idle" && _idleAnim != "")
				{
					_animPlayer.Play(_idleAnim);
					_currentState = "idle";
				}
			}
		}

		// When landing after a jump, reset state so animations can switch again.
		if (IsOnFloor() && _currentState == "jump" && velocity.Y <= 0)
		{
			_currentState = "landed";
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
