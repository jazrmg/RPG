using Godot;
using System;

public partial class SkillUIManager : Control
{
	private Label _healthLabel, _staminaLabel;
	private ProgressBar _healthBar, _staminaBar;
	private Button _lightBtn, _heavyBtn, _specialBtn;
	private Control _lightOverlay, _heavyOverlay, _specialOverlay;
	private Label _lightCooldown, _heavyCooldown, _specialCooldown;
	private Player3D _player;

	// ✨ NEW: Level display
	private Label _levelLabel;
	private ProgressBar _xpBar;
	private Label _xpLabel;

	// ✨ NEW: Stats UI reference
	private StatAllocationUI _statsUI;

	private readonly Color[] _skillColors = new[] {
		new Color(0.1f, 0.9f, 0.1f),      // Green
		new Color(0.95f, 0.75f, 0.0f),    // Yellow
		new Color(0.95f, 0.1f, 0.1f)      // Red
	};

	// ✨ RESPONSIVE: Screen size adaptive values
	private Vector2 _screenSize;
	private float _uiScale = 1.0f;
	private float _baseFont = 14.0f;

	public override void _Ready()
	{
		_player = GetTree().Root.GetChild(0).FindChild("Player3D", true, false) as Player3D;
		if (_player == null) { return; }

		// ✨ FIXED: Get StatsUI from Player3D directly instead of searching
		_statsUI = _player.StatsUI;

		AnchorLeft = AnchorTop = 0.0f;
		AnchorRight = AnchorBottom = 1.0f;

		// ✨ RESPONSIVE: Get initial screen size
		_screenSize = GetViewport().GetVisibleRect().Size;
		CalculateUIScale();

		CreateUI();
	}

	// ✨ RESPONSIVE: Calculate UI scale based on screen size
	private void CalculateUIScale()
	{
		// Reference: 1920x1080 (full HD)
		float referenceWidth = 1920f;

		// Get current viewport size
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

		// Calculate scale based on width (more reliable than height)
		_uiScale = viewportSize.X / referenceWidth;

		// Clamp between 0.5x and 2.0x to avoid extremes
		_uiScale = Mathf.Clamp(_uiScale, 0.5f, 2.0f);

		_baseFont = 14.0f * _uiScale;
	}

	private void CreateUI()
	{
		// Health Bar - Top Left
		CreateHealthBar();
		
		// Stamina Bar - Top Right
		CreateStaminaBar();

		// ✨ NEW: Level Display - Top Center
		CreateLevelDisplay();
		
		// Skill Buttons - Right Side
		CreateSkillButtons();
	}

	private void CreateHealthBar()
	{
		var container = new PanelContainer();
		container.AnchorLeft = 0.0f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 0.25f;
		container.AnchorBottom = 0.15f;

		// ✨ RESPONSIVE: Scale margins based on screen size
		float marginH = 10 * _uiScale;
		float marginV = 10 * _uiScale;
		container.OffsetLeft = marginH;
		container.OffsetTop = marginV;
		container.OffsetRight = -marginH;
		container.OffsetBottom = -marginV;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.02f, 0.02f, 0.02f, 0.9f) };
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", (int)(4 * _uiScale));
		container.AddChild(vbox);

		var titleLabel = new Label { Text = "❤ HEALTH" };
		titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.3f, 0.3f, 1));
		titleLabel.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 1.0f));
		vbox.AddChild(titleLabel);

		_healthLabel = new Label { Text = "100 / 100" };
		_healthLabel.AddThemeColorOverride("font_color", Colors.White);
		_healthLabel.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 0.85f));
		vbox.AddChild(_healthLabel);

		_healthBar = new ProgressBar();
		_healthBar.MinValue = 0;
		_healthBar.MaxValue = _player.MaxPlayerHealth;
		_healthBar.Value = _player._playerHealth;
		_healthBar.CustomMinimumSize = new Vector2(200 * _uiScale, 20 * _uiScale);

		var barBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f) };
		_healthBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = new Color(1, 0.3f, 0.3f, 1) };
		_healthBar.AddThemeStyleboxOverride("fill", barFill);

		vbox.AddChild(_healthBar);
		AddChild(container);
	}

	private void CreateStaminaBar()
	{
		var container = new PanelContainer();
		container.AnchorLeft = 0.75f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 1.0f;
		container.AnchorBottom = 0.15f;

		// ✨ RESPONSIVE: Scale margins
		float marginH = 10 * _uiScale;
		float marginV = 10 * _uiScale;
		container.OffsetLeft = marginH;
		container.OffsetTop = marginV;
		container.OffsetRight = -marginH;
		container.OffsetBottom = -marginV;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.02f, 0.02f, 0.02f, 0.9f) };
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", (int)(4 * _uiScale));
		container.AddChild(vbox);

		var titleLabel = new Label { Text = "⚡ STAMINA" };
		titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.9f, 0.2f, 1));
		titleLabel.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 1.0f));
		vbox.AddChild(titleLabel);

		_staminaLabel = new Label { Text = "100 / 100" };
		_staminaLabel.AddThemeColorOverride("font_color", Colors.White);
		_staminaLabel.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 0.85f));
		vbox.AddChild(_staminaLabel);

		_staminaBar = new ProgressBar();
		_staminaBar.MinValue = 0;
		_staminaBar.MaxValue = _player.MaxStamina;
		_staminaBar.Value = _player._stamina;
		_staminaBar.CustomMinimumSize = new Vector2(200 * _uiScale, 20 * _uiScale);

		var barBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f) };
		_staminaBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = new Color(1, 0.9f, 0.2f, 1) };
		_staminaBar.AddThemeStyleboxOverride("fill", barFill);

		vbox.AddChild(_staminaBar);
		AddChild(container);
	}

	private void CreateLevelDisplay()
	{
		// ✨ RESPONSIVE: Level panel - Top center
		var container = new PanelContainer();
		container.AnchorLeft = 0.35f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 0.65f;
		container.AnchorBottom = 0.15f;

		// ✨ RESPONSIVE: Scale margins
		float marginH = 10 * _uiScale;
		float marginV = 10 * _uiScale;
		container.OffsetLeft = marginH;
		container.OffsetTop = marginV;
		container.OffsetRight = -marginH;
		container.OffsetBottom = -marginV;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.02f, 0.02f, 0.02f, 0.9f) };
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", (int)(4 * _uiScale));
		container.AddChild(vbox);

		_levelLabel = new Label { Text = "⭐ LEVEL 1" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 1.4f));
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_levelLabel);

		_xpLabel = new Label { Text = "XP: 0 / 100" };
		_xpLabel.AddThemeColorOverride("font_color", Colors.White);
		_xpLabel.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 0.85f));
		_xpLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_xpLabel);

		_xpBar = new ProgressBar();
		_xpBar.MinValue = 0;
		_xpBar.MaxValue = 100;
		_xpBar.Value = 0;
		_xpBar.CustomMinimumSize = new Vector2(200 * _uiScale, 16 * _uiScale);

		var barBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f) };
		_xpBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = new Color(0.3f, 0.8f, 1f, 1) };
		_xpBar.AddThemeStyleboxOverride("fill", barFill);

		vbox.AddChild(_xpBar);
		AddChild(container);
	}

	private void CreateSkillButtons()
	{
		var vbox = new VBoxContainer();
		vbox.AnchorLeft = 0.93f;
		vbox.AnchorTop = 0.2f;
		vbox.AnchorRight = 1.0f;
		vbox.AnchorBottom = 1.0f;

		// ✨ RESPONSIVE: Scale button spacing
		float offsetLeft = -80 * _uiScale;
		float offsetRight = -5 * _uiScale;
		float offsetBottom = -10 * _uiScale;
		float separation = 8 * _uiScale;

		vbox.OffsetLeft = offsetLeft;
		vbox.OffsetTop = 0;
		vbox.OffsetRight = offsetRight;
		vbox.OffsetBottom = offsetBottom;
		vbox.AddThemeConstantOverride("separation", (int)separation);

		// ✨ AUTO BATTLE BUTTON
		var autoBattleBtn = new Button { Text = "⚔️ AUTO" };
		autoBattleBtn.CustomMinimumSize = new Vector2(70 * _uiScale, 40 * _uiScale);
		autoBattleBtn.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 1.15f));
		autoBattleBtn.AddThemeColorOverride("font_color", Colors.White);
		var autoBattleStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.3f, 0.8f, 1) };
		autoBattleBtn.AddThemeStyleboxOverride("normal", autoBattleStyle);
		autoBattleBtn.Pressed += () => {
			if (_player != null)
			{
				_player._isAutoBattle = !_player._isAutoBattle;
				var activeStyle = new StyleBoxFlat { BgColor = new Color(0.9f, 0.2f, 0.2f, 1) };
				var inactiveStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.3f, 0.8f, 1) };
				autoBattleBtn.AddThemeStyleboxOverride("normal", 
					_player._isAutoBattle ? activeStyle : inactiveStyle);
			}
		};
		vbox.AddChild(autoBattleBtn);

		var skills = new[] {
			("1", _skillColors[0], (Action)(() => SelectSkill(1))),
			("2", _skillColors[1], (Action)(() => SelectSkill(2))),
			("3", _skillColors[2], (Action)(() => SelectSkill(3)))
		};

		Button[] buttons = new Button[3];
		Control[] overlays = new Control[3];
		Label[] cooldownLabels = new Label[3];

		for (int i = 0; i < 3; i++)
		{
			var btnContainer = new PanelContainer();
			btnContainer.CustomMinimumSize = new Vector2(70 * _uiScale, 70 * _uiScale);

			var btn = new Button { Text = skills[i].Item1 };
			btn.Pressed += skills[i].Item3;
			btn.AnchorLeft = btn.AnchorRight = btn.AnchorTop = btn.AnchorBottom = 0.5f;

			var normalStyle = new StyleBoxFlat { BgColor = skills[i].Item2 };
			btn.AddThemeStyleboxOverride("normal", normalStyle);

			var hoverStyle = new StyleBoxFlat { BgColor = skills[i].Item2 * 1.25f };
			btn.AddThemeStyleboxOverride("hover", hoverStyle);

			var pressStyle = new StyleBoxFlat { BgColor = skills[i].Item2 * 1.4f };
			btn.AddThemeStyleboxOverride("focus", pressStyle);
			btn.AddThemeStyleboxOverride("pressed", pressStyle);

			btn.AddThemeColorOverride("font_color", Colors.White);
			btn.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 2.8f));

			btnContainer.AddChild(btn);

			var overlay = new Control { Visible = false };
			overlay.AnchorLeft = overlay.AnchorTop = 0.0f;
			overlay.AnchorRight = overlay.AnchorBottom = 1.0f;

			var overlayPanel = new Panel();
			overlayPanel.AnchorLeft = overlayPanel.AnchorTop = 0.0f;
			overlayPanel.AnchorRight = overlayPanel.AnchorBottom = 1.0f;
			var overlayStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.05f, 0.05f, 0.9f) };
			overlayPanel.AddThemeStyleboxOverride("panel", overlayStyle);
			overlay.AddChild(overlayPanel);

			var cooldownLabel = new Label { Text = "●" };
			cooldownLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
			cooldownLabel.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 1.7f));
			cooldownLabel.AnchorLeft = cooldownLabel.AnchorTop = 0.5f;
			cooldownLabel.AnchorRight = cooldownLabel.AnchorBottom = 0.5f;
			float labelOffset = 12 * _uiScale;
			cooldownLabel.OffsetLeft = -labelOffset;
			cooldownLabel.OffsetTop = -labelOffset;
			overlay.AddChild(cooldownLabel);

			btnContainer.AddChild(overlay);
			vbox.AddChild(btnContainer);

			buttons[i] = btn;
			overlays[i] = overlay;
			cooldownLabels[i] = cooldownLabel;
		}

		_lightBtn = buttons[0];
		_heavyBtn = buttons[1];
		_specialBtn = buttons[2];
		_lightOverlay = overlays[0];
		_heavyOverlay = overlays[1];
		_specialOverlay = overlays[2];
		_lightCooldown = cooldownLabels[0];
		_heavyCooldown = cooldownLabels[1];
		_specialCooldown = cooldownLabels[2];

		// ✨ NEW: STATS BUTTON
		var statsBtn = new Button { Text = "📊 STATS" };
		statsBtn.CustomMinimumSize = new Vector2(70 * _uiScale, 40 * _uiScale);
		statsBtn.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 1.0f));
		statsBtn.AddThemeColorOverride("font_color", Colors.White);
		var statsStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.6f, 0.8f, 1) };
		statsBtn.AddThemeStyleboxOverride("normal", statsStyle);
		
		var statsHoverStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.8f, 1.0f, 1) };
		statsBtn.AddThemeStyleboxOverride("hover", statsHoverStyle);
		
		var statsPressStyle = new StyleBoxFlat { BgColor = new Color(0.4f, 0.9f, 1.0f, 1) };
		statsBtn.AddThemeStyleboxOverride("focus", statsPressStyle);
		statsBtn.AddThemeStyleboxOverride("pressed", statsPressStyle);
		
		statsBtn.Pressed += () => {
			if (_statsUI != null)
			{
				_statsUI.ToggleStatsPanel();
			}
		};
		vbox.AddChild(statsBtn);

		AddChild(vbox);
	}

	public override void _Process(double delta)
	{
		if (_player == null) return;

		// ✨ RESPONSIVE: Check for screen resize
		Vector2 newScreenSize = GetViewport().GetVisibleRect().Size;
		if (newScreenSize != _screenSize)
		{
			_screenSize = newScreenSize;
			CalculateUIScale();
		}

		UpdateBars();
		UpdateCooldowns();
		UpdateSelection();
		UpdateLevelDisplay();
		HandleInput();
	}

	private void UpdateBars()
	{
		_healthLabel.Text = $"{_player._playerHealth:F0} / {_player.MaxPlayerHealth:F0}";
		_healthBar.Value = _player._playerHealth;
		_staminaLabel.Text = $"{_player._stamina:F0} / {_player.MaxStamina:F0}";
		_staminaBar.Value = _player._stamina;
	}

	private void UpdateLevelDisplay()
	{
		if (_player?.LevelingSystem == null) return;

		_levelLabel.Text = $"⭐ LEVEL {_player.LevelingSystem.CurrentLevel}";
		_xpLabel.Text = $"XP: {_player.LevelingSystem.CurrentXP:F0} / {_player.LevelingSystem.XPRequiredForNextLevel:F0}";
		
		float xpPercent = _player.LevelingSystem.CurrentXP / _player.LevelingSystem.XPRequiredForNextLevel;
		_xpBar.Value = Mathf.Clamp(xpPercent * 100, 0, 100);
	}

	private void UpdateCooldowns()
	{
		UpdateCooldown(_lightOverlay, _lightCooldown, _player._lightAttackCooldownTimer);
		UpdateCooldown(_heavyOverlay, _heavyCooldown, _player._heavyAttackCooldownTimer);
		UpdateCooldown(_specialOverlay, _specialCooldown, _player._specialAttackCooldownTimer);
	}

	private void UpdateCooldown(Control overlay, Label label, float timer)
	{
		overlay.Visible = timer > 0;
		if (timer > 0)
		{
			label.Text = $"{timer:F1}s";
			label.AddThemeFontSizeOverride("font_size", (int)(_baseFont * 1.6f));
			label.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		}
	}

	private void UpdateSelection()
	{
		ResetButtonColor(_lightBtn, _skillColors[0]);
		ResetButtonColor(_heavyBtn, _skillColors[1]);
		ResetButtonColor(_specialBtn, _skillColors[2]);

		if (_player._attackModeTimer > 0)
		{
			switch (_player._currentAttackMode)
			{
				case Player3D.AttackMode.Light:
					ApplySelectedStyle(_lightBtn, _skillColors[0]);
					break;
				case Player3D.AttackMode.Heavy:
					ApplySelectedStyle(_heavyBtn, _skillColors[1]);
					break;
				case Player3D.AttackMode.Special:
					ApplySelectedStyle(_specialBtn, _skillColors[2]);
					break;
			}
		}
	}

	private void ResetButtonColor(Button btn, Color color)
	{
		var normalStyle = new StyleBoxFlat { BgColor = color };
		btn.AddThemeStyleboxOverride("normal", normalStyle);
	}

	private void ApplySelectedStyle(Button btn, Color color)
	{
		var selectedStyle = new StyleBoxFlat 
		{ 
			BgColor = color * 1.5f
		};
		
		selectedStyle.SetBorderWidthAll(4);
		selectedStyle.BorderColor = new Color(1, 1, 0, 1);
		
		btn.AddThemeStyleboxOverride("normal", selectedStyle);
	}

	private void HandleInput()
	{
		if (Input.IsKeyPressed(Key.Key1)) SelectSkill(1);
		if (Input.IsKeyPressed(Key.Key2)) SelectSkill(2);
		if (Input.IsKeyPressed(Key.Key3)) SelectSkill(3);
	}

	private void SelectSkill(int skillNum)
	{
		_player._currentAttackMode = skillNum switch
		{
			1 => Player3D.AttackMode.Light,
			2 => Player3D.AttackMode.Heavy,
			3 => Player3D.AttackMode.Special,
			_ => Player3D.AttackMode.None
		};
		_player._attackModeTimer = _player.AttackModeWindowTime;
	}
}
