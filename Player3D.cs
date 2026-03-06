using Godot;

public partial class Player3D : CharacterBody3D
{
	[Export] public float Speed = 2.5f;
	[Export] public float RunSpeed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 0.3f;

	private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	private Camera3D _camera;
	private AnimationPlayer _animPlayer;

	private string _idleAnim = "";
	private string _walkAnim = "";
	private string _runAnim = "";
	private string _jumpAnim = "";
	private string _sitAnim = "";
	private string _currentState = "";

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		_animPlayer = GetNode<AnimationPlayer>("CharacterModel/AnimationPlayer");

		Input.MouseMode = Input.MouseModeEnum.Captured;

		// Force root node to CharacterModel.
		_animPlayer.RootNode = _animPlayer.GetParent().GetPath();

		// Find the idle animation name by checking what's available.
		foreach (var libName in _animPlayer.GetAnimationLibraryList())
		{
			var lib = _animPlayer.GetAnimationLibrary(libName);
			foreach (var animName in lib.GetAnimationList())
			{
				string fullName = libName == "" ? animName : $"{libName}/{animName}";
				GD.Print($"Available: '{fullName}'");

				// Skip "Take 001" — that's the T-pose.
				if (animName != "Take 001")
				{
					_idleAnim = fullName;
					// Make idle loop.
					var idleAnim = lib.GetAnimation(animName);
					idleAnim.LoopMode = Animation.LoopModeEnum.Linear;
				}
			}
		}

		// Create custom library for walk, run, jump, and sit.
		var customLibrary = new AnimationLibrary();
		_animPlayer.AddAnimationLibrary("Custom", customLibrary);

		var walkAnim = ExtractAnimation("res://models/Walking.fbx");
		if (walkAnim != null)
		{
			customLibrary.AddAnimation("Walking", walkAnim);
			_walkAnim = "Custom/Walking";
			GD.Print("Walking animation loaded!");
		}

		var runAnim = ExtractAnimation("res://models/Running.fbx");
		if (runAnim != null)
		{
			customLibrary.AddAnimation("Running", runAnim);
			_runAnim = "Custom/Running";
			GD.Print("Running animation loaded!");
		}

		var jumpAnim = ExtractAnimation("res://models/Jump.fbx");
		if (jumpAnim != null)
		{
			// Don't loop jump — it should play once.
			jumpAnim.LoopMode = Animation.LoopModeEnum.None;
			customLibrary.AddAnimation("Jump", jumpAnim);
			_jumpAnim = "Custom/Jump";
			GD.Print("Jump animation loaded!");
		}

		var sitAnim = ExtractAnimation("res://models/Sitting Idle.fbx");
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
		var otherAnimPlayer = instance.GetNode<AnimationPlayer>("AnimationPlayer");
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

		// Remove root motion — strip the Hips POSITION track so the
		// character doesn't drift away from the Player3D node.
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

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));

			_camera.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));

			Vector3 cameraRotation = _camera.Rotation;
			cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-80), Mathf.DegToRad(80));
			_camera.Rotation = cameraRotation;
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
				// Stand back up.
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
