using Godot;

public partial class TouchControls : CanvasLayer
{
	// LEFT JOYSTICK (Movement)
	private Vector2 _leftJoystickCenter;
	private bool _leftJoystickActive = false;
	private int _leftJoystickTouchId = -1;
	private Panel _leftJoystickBg;
	private Panel _leftJoystickStick;
	
	// RIGHT JOYSTICK (Camera)
	private Vector2 _rightJoystickCenter;
	private bool _rightJoystickActive = false;
	private int _rightJoystickTouchId = -1;
	private Panel _rightJoystickBg;
	private Panel _rightJoystickStick;
	
	// ACTION BUTTONS
	private Button _jumpBtn;
	private Button _equipBtn;
	private Button _dodgeBtn;
	private Button _statsBtn;
	private Button _lightAtkBtn;
	private Button _heavyAtkBtn;
	private Button _specialAtkBtn;
	private Button _autoBattleBtn;
	
	private bool _jumpPressed = false;
	private bool _equipPressed = false;
	private bool _dodgePressed = false;
	private int _buttonTouchId = -1;
	private SceneTree _sceneTree;
	
	public Vector2 MoveInput { get; private set; } = Vector2.Zero;
	public Vector2 CameraInput { get; private set; } = Vector2.Zero;
	public bool JumpPressed => _jumpPressed;
	public bool EquipSwordPressed => _equipPressed;
	public bool DodgePressed => _dodgePressed;
	
	public override void _Ready()
	{
		if (!IsMobile())
		{
			Visible = false;
			return;
		}
		
		_sceneTree = GetTree();
		Layer = 50;
		CreateUI();
	}
	
	private bool IsMobile()
	{
		return OS.GetName() == "Android" || OS.GetName() == "iOS" || DisplayServer.IsTouchscreenAvailable();
	}
	
	private void CreateUI()
	{
		Rect2 viewport = GetViewport().GetVisibleRect();
		float w = viewport.Size.X;
		float h = viewport.Size.Y;
		
		_leftJoystickCenter = new Vector2(120, h - 140);
		CreateLeftJoystick();
		
		_rightJoystickCenter = new Vector2(w - 120, h - 140);
		CreateRightJoystick();
		
		CreateActionButtons(w, h);
	}
	
	private void CreateLeftJoystick()
	{
		_leftJoystickBg = new Panel();
		_leftJoystickBg.GlobalPosition = _leftJoystickCenter - Vector2.One * 100;
		_leftJoystickBg.CustomMinimumSize = Vector2.One * 200;
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.8f, 0.2f, 0.4f) };
		bgStyle.SetCornerRadiusAll(100);
		_leftJoystickBg.AddThemeStyleboxOverride("panel", bgStyle);
		
		_leftJoystickStick = new Panel();
		_leftJoystickStick.CustomMinimumSize = Vector2.One * 80;
		_leftJoystickStick.GlobalPosition = _leftJoystickCenter - Vector2.One * 40;
		
		var stickStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.9f, 1.0f, 0.9f) };
		stickStyle.SetCornerRadiusAll(40);
		_leftJoystickStick.AddThemeStyleboxOverride("panel", stickStyle);
		
		AddChild(_leftJoystickBg);
		AddChild(_leftJoystickStick);
	}
	
	private void CreateRightJoystick()
	{
		_rightJoystickBg = new Panel();
		_rightJoystickBg.GlobalPosition = _rightJoystickCenter - Vector2.One * 100;
		_rightJoystickBg.CustomMinimumSize = Vector2.One * 200;
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(1.0f, 0.95f, 0.2f, 0.4f) };
		bgStyle.SetCornerRadiusAll(100);
		_rightJoystickBg.AddThemeStyleboxOverride("panel", bgStyle);
		
		_rightJoystickStick = new Panel();
		_rightJoystickStick.CustomMinimumSize = Vector2.One * 80;
		_rightJoystickStick.GlobalPosition = _rightJoystickCenter - Vector2.One * 40;
		
		var stickStyle = new StyleBoxFlat { BgColor = new Color(1.0f, 0.3f, 0.2f, 0.9f) };
		stickStyle.SetCornerRadiusAll(40);
		_rightJoystickStick.AddThemeStyleboxOverride("panel", stickStyle);
		
		AddChild(_rightJoystickBg);
		AddChild(_rightJoystickStick);
	}
	
	private void CreateActionButtons(float w, float h)
	{
		_jumpBtn = CreateButton("⬆️ JUMP", 20, 20, 100, 100, new Color(1.0f, 0.95f, 0.2f, 0.9f));
		_equipBtn = CreateButton("🗡️ EQUIP", 20, (int)(h / 2) - 60, 100, 100, new Color(0.95f, 0.4f, 0.95f, 0.9f));
		_dodgeBtn = CreateButton("🛡️ DODGE", 20, (int)h - 120, 100, 100, new Color(0.25f, 0.9f, 1.0f, 0.9f));
		
		_lightAtkBtn = CreateButton("1", (int)(w - 120), 20, 100, 100, new Color(0.1f, 0.9f, 0.1f, 0.9f));
		_heavyAtkBtn = CreateButton("2", (int)(w - 120), (int)(h / 2) - 60, 100, 100, new Color(0.95f, 0.75f, 0.0f, 0.9f));
		_specialAtkBtn = CreateButton("3", (int)(w - 120), (int)h - 120, 100, 100, new Color(0.95f, 0.1f, 0.1f, 0.9f));
		
		_autoBattleBtn = CreateButton("⚔️ AUTO", (int)(w - 120), (int)(h / 2) + 80, 100, 80, new Color(0.7f, 0.35f, 1.0f, 0.9f));
		_statsBtn = CreateButton("📊 STATS", (int)(w - 120), (int)h - 220, 100, 80, new Color(0.2f, 0.8f, 1.0f, 0.9f));
		
		_lightAtkBtn.Pressed += () => SelectAttack(1);
		_heavyAtkBtn.Pressed += () => SelectAttack(2);
		_specialAtkBtn.Pressed += () => SelectAttack(3);
		
		_autoBattleBtn.Pressed += () => ToggleAutoBattle();
		_statsBtn.Pressed += () => OpenStats();
	}
	
	private Button CreateButton(string text, int x, int y, int w, int h, Color color)
	{
		var btn = new Button { Text = text };
		btn.GlobalPosition = new Vector2(x, y);
		btn.CustomMinimumSize = new Vector2(w, h);
		btn.AddThemeFontSizeOverride("font_size", 14);
		btn.AddThemeColorOverride("font_color", Colors.White);
		
		var style = new StyleBoxFlat { BgColor = color };
		style.SetCornerRadiusAll(12);
		style.SetBorderWidthAll(2);
		style.BorderColor = new Color(1, 1, 1, 0.3f);
		btn.AddThemeStyleboxOverride("normal", style);
		
		var hoverStyle = new StyleBoxFlat { BgColor = color * 1.15f };
		hoverStyle.SetCornerRadiusAll(12);
		hoverStyle.SetBorderWidthAll(2);
		hoverStyle.BorderColor = new Color(1, 1, 1, 0.6f);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);
		
		var pressStyle = new StyleBoxFlat { BgColor = color * 1.3f };
		pressStyle.SetCornerRadiusAll(12);
		pressStyle.SetBorderWidthAll(3);
		pressStyle.BorderColor = Colors.White;
		btn.AddThemeStyleboxOverride("pressed", pressStyle);
		btn.AddThemeStyleboxOverride("focus", pressStyle);
		
		AddChild(btn);
		return btn;
	}
	
	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed)
				OnTouchDown(touch.Position, touch.Index);
			else
				OnTouchUp(touch.Index);
		}
		else if (@event is InputEventScreenDrag drag)
		{
			OnTouchDrag(drag.Position, drag.Index);
		}
	}
	
	private void OnTouchDown(Vector2 pos, int id)
	{
		float distLeft = pos.DistanceTo(_leftJoystickCenter);
		if (distLeft < 130 && !_leftJoystickActive)
		{
			_leftJoystickActive = true;
			_leftJoystickTouchId = id;
			UpdateLeftJoystick(pos);
			return;
		}
		
		float distRight = pos.DistanceTo(_rightJoystickCenter);
		if (distRight < 130 && !_rightJoystickActive)
		{
			_rightJoystickActive = true;
			_rightJoystickTouchId = id;
			UpdateRightJoystick(pos);
			return;
		}
		
		CheckButtonPress(pos, id);
	}
	
	private void CheckButtonPress(Vector2 pos, int id)
	{
		if (IsPointInButton(_jumpBtn, pos))
		{
			_jumpPressed = true;
			_buttonTouchId = id;
			return;
		}
		if (IsPointInButton(_equipBtn, pos))
		{
			_equipPressed = true;
			_buttonTouchId = id;
			return;
		}
		if (IsPointInButton(_dodgeBtn, pos))
		{
			_dodgePressed = true;
			_buttonTouchId = id;
			return;
		}
	}
	
	private bool IsPointInButton(Button btn, Vector2 pos)
	{
		Rect2 rect = new Rect2(btn.GlobalPosition, btn.CustomMinimumSize);
		return rect.HasPoint(pos);
	}
	
	private void OnTouchUp(int id)
	{
		if (_leftJoystickTouchId == id)
		{
			_leftJoystickActive = false;
			_leftJoystickTouchId = -1;
			MoveInput = Vector2.Zero;
			_leftJoystickStick.GlobalPosition = _leftJoystickCenter - Vector2.One * 40;
		}
		
		if (_rightJoystickTouchId == id)
		{
			_rightJoystickActive = false;
			_rightJoystickTouchId = -1;
			CameraInput = Vector2.Zero;
			_rightJoystickStick.GlobalPosition = _rightJoystickCenter - Vector2.One * 40;
		}
		
		if (_buttonTouchId == id)
		{
			_buttonTouchId = -1;
		}
	}
	
	private void OnTouchDrag(Vector2 pos, int id)
	{
		if (_leftJoystickTouchId == id && _leftJoystickActive)
			UpdateLeftJoystick(pos);
		
		if (_rightJoystickTouchId == id && _rightJoystickActive)
			UpdateRightJoystick(pos);
	}
	
	private void UpdateLeftJoystick(Vector2 pos)
	{
		Vector2 delta = pos - _leftJoystickCenter;
		float dist = delta.Length();
		
		if (dist > 70)
		{
			Vector2 dir = delta.Normalized();
			_leftJoystickStick.GlobalPosition = _leftJoystickCenter + dir * 70 - Vector2.One * 40;
			MoveInput = dir;
		}
		else
		{
			_leftJoystickStick.GlobalPosition = pos - Vector2.One * 40;
			MoveInput = dist > 5 ? delta.Normalized() : Vector2.Zero;
		}
	}
	
	private void UpdateRightJoystick(Vector2 pos)
	{
		Vector2 delta = pos - _rightJoystickCenter;
		float dist = delta.Length();
		
		if (dist > 70)
		{
			Vector2 dir = delta.Normalized();
			_rightJoystickStick.GlobalPosition = _rightJoystickCenter + dir * 70 - Vector2.One * 40;
			CameraInput = dir;
		}
		else
		{
			_rightJoystickStick.GlobalPosition = pos - Vector2.One * 40;
			CameraInput = dist > 5 ? delta.Normalized() : Vector2.Zero;
		}
	}
	
	private void SelectAttack(int num)
	{
		var player = _sceneTree.Root.FindChild("Player3D", true, false) as Player3D;
		if (player != null)
		{
			player._currentAttackMode = num switch
			{
				1 => Player3D.AttackMode.Light,
				2 => Player3D.AttackMode.Heavy,
				3 => Player3D.AttackMode.Special,
				_ => Player3D.AttackMode.None
			};
			player._attackModeTimer = player.AttackModeWindowTime;
		}
	}
	
	private void ToggleAutoBattle()
	{
		var player = _sceneTree.Root.FindChild("Player3D", true, false) as Player3D;
		if (player != null)
		{
			player._isAutoBattle = !player._isAutoBattle;
			var style = new StyleBoxFlat { BgColor = player._isAutoBattle ? new Color(0.9f, 0.2f, 0.2f, 0.9f) : new Color(0.7f, 0.35f, 1.0f, 0.9f) };
			style.SetCornerRadiusAll(12);
			_autoBattleBtn.AddThemeStyleboxOverride("normal", style);
		}
	}
	
	private void OpenStats()
	{
		var statsUI = _sceneTree.Root.FindChild("StatAllocationUI", true, false) as StatAllocationUI;
		if (statsUI != null)
			statsUI.OpenFromButton();
	}
	
	public void ConsumeJump() => _jumpPressed = false;
	public void ConsumeEquipSword() => _equipPressed = false;
	public void ConsumeDodge() => _dodgePressed = false;
}
