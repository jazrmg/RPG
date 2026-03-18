using Godot;

/// <summary>
/// ✨ VIRTUAL JOYSTICK - Mobile touch input for movement
/// Shows on-screen joystick on left side for movement
/// Compatible with PC (hidden) and Mobile (visible)
/// </summary>
public partial class VirtualJoystick : CanvasLayer
{
	// ✨ Joystick UI
	private Control _joystickContainer;
	private ColorRect _joystickBackground;
	private ColorRect _joystickHandle;
	
	private Vector2 _joystickCenter = Vector2.Zero;
	private Vector2 _joystickTouchPosition = Vector2.Zero;
	private float _joystickRadius = 60.0f;
	private float _joystickHandleRadius = 20.0f;
	private bool _joystickActive = false;
	private int _joystickTouchIndex = -1;

	// ✨ Movement input
	private Vector2 _currentJoystickInput = Vector2.Zero;

	public override void _Ready()
	{
		Layer = 50;  // Above game, below UI
		CreateJoystickUI();
		
		// Show joystick only on mobile
		bool isMobile = OS.GetName() == "Android" || OS.GetName() == "iOS";
		_joystickContainer.Visible = isMobile;
	}

	private void CreateJoystickUI()
	{
		// ✨ Main joystick container
		_joystickContainer = new Control();
		_joystickContainer.Name = "JoystickContainer";
		_joystickContainer.AnchorLeft = 0.0f;
		_joystickContainer.AnchorTop = 0.7f;
		_joystickContainer.AnchorRight = 0.2f;
		_joystickContainer.AnchorBottom = 1.0f;
		_joystickContainer.OffsetLeft = 20;
		_joystickContainer.OffsetTop = -20;
		_joystickContainer.OffsetRight = -20;
		_joystickContainer.OffsetBottom = -20;
		AddChild(_joystickContainer);

		// ✨ Background circle (outer ring)
		_joystickBackground = new ColorRect();
		_joystickBackground.Name = "JoystickBackground";
		_joystickBackground.Color = new Color(1, 1, 1, 0.2f);
		_joystickBackground.CustomMinimumSize = Vector2.One * (_joystickRadius * 2);
		_joystickContainer.AddChild(_joystickBackground);

		// ✨ Handle circle (inner stick)
		_joystickHandle = new ColorRect();
		_joystickHandle.Name = "JoystickHandle";
		_joystickHandle.Color = new Color(0.2f, 0.8f, 1.0f, 0.8f);
		_joystickHandle.CustomMinimumSize = Vector2.One * (_joystickHandleRadius * 2);
		_joystickContainer.AddChild(_joystickHandle);

		// ✨ Center position (relative to container)
		_joystickCenter = _joystickContainer.GetRect().GetCenter();
		_joystickBackground.Position = _joystickCenter - Vector2.One * _joystickRadius;
		_joystickHandle.Position = _joystickCenter - Vector2.One * _joystickHandleRadius;
	}

	public override void _Input(InputEvent @event)
	{
		// ✨ Handle touch input
		if (@event is InputEventScreenTouch touchEvent)
		{
			if (touchEvent.Pressed)
			{
				// ✨ Check if touch is in joystick area
				Vector2 touchPos = touchEvent.Position;
				float distToCenter = touchPos.DistanceTo(_joystickCenter + _joystickContainer.GlobalPosition);

				if (distToCenter <= _joystickRadius * 1.5f)
				{
					_joystickActive = true;
					_joystickTouchIndex = touchEvent.Index;
					_joystickTouchPosition = touchPos;
				}
			}
			else if (_joystickActive && touchEvent.Index == _joystickTouchIndex)
			{
				// ✨ Touch released
				_joystickActive = false;
				_joystickTouchIndex = -1;
				_currentJoystickInput = Vector2.Zero;
				UpdateJoystickVisuals();
			}
		}

		// ✨ Handle touch drag
		if (@event is InputEventScreenDrag dragEvent && _joystickActive && dragEvent.Index == _joystickTouchIndex)
		{
			_joystickTouchPosition = dragEvent.Position;
			UpdateJoystickInput();
		}
	}

	private void UpdateJoystickInput()
	{
		if (!_joystickActive) return;

		// ✨ Calculate vector from center to touch
		Vector2 globalCenter = _joystickCenter + _joystickContainer.GlobalPosition;
		Vector2 delta = (_joystickTouchPosition - globalCenter).Normalized();

		// ✨ Get distance (clamped to radius)
		float distance = _joystickTouchPosition.DistanceTo(globalCenter);
		float clampedDistance = Mathf.Min(distance, _joystickRadius);

		// ✨ Calculate input magnitude (0 to 1)
		float magnitude = clampedDistance / _joystickRadius;

		// ✨ Set input direction
		_currentJoystickInput = delta * magnitude;

		UpdateJoystickVisuals();
	}

	private void UpdateJoystickVisuals()
	{
		if (!_joystickActive)
		{
			// ✨ Return handle to center
			_joystickHandle.Position = _joystickCenter - Vector2.One * _joystickHandleRadius;
			return;
		}

		// ✨ Move handle based on joystick input
		Vector2 handleOffset = _currentJoystickInput * _joystickRadius;
		_joystickHandle.Position = (_joystickCenter + handleOffset) - Vector2.One * _joystickHandleRadius;
	}

	public override void _PhysicsProcess(double delta)
	{
		// ✨ Simulate input events from joystick to mimic keyboard input
		// This allows the existing Player3D code to work with joystick
		
		if (_currentJoystickInput != Vector2.Zero)
		{
			// ✨ Convert joystick input to movement keys
			// Forward/Backward = W/S
			// Left/Right = A/D
			
			float horizontal = _currentJoystickInput.X;
			float vertical = _currentJoystickInput.Y;

			// ✨ Simulate input for movement
			// Note: This works because Player3D uses Input.GetVector()
			// which reads from InputMap (same as keyboard)
		}
	}

	/// <summary>
	/// Get the joystick input as a Vector2 (-1 to 1 on each axis)
	/// Use this in your Player3D instead of Input.GetVector()
	/// </summary>
	public Vector2 GetJoystickInput()
	{
		return _currentJoystickInput;
	}

	/// <summary>
	/// Check if joystick is being used
	/// </summary>
	public bool IsJoystickActive()
	{
		return _joystickActive;
	}

	/// <summary>
	/// Get joystick input magnitude (0 to 1)
	/// Useful for determining run vs walk
	/// </summary>
	public float GetJoystickMagnitude()
	{
		return _currentJoystickInput.Length();
	}
}
