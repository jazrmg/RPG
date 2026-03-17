using Godot;
using System;

public partial class SkillUIManager : Control
{
	private Label _healthLabel;
	private ProgressBar _healthBar;
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

	public override void _Ready()
	{
		_player = GetTree().Root.GetChild(0).FindChild("Player3D", true, false) as Player3D;
		if (_player == null) { return; }

		_statsUI = _player.StatsUI;

		AnchorLeft = AnchorTop = 0.0f;
		AnchorRight = AnchorBottom = 1.0f;

		CreateUI();
	}

	private void CreateUI()
	{
		// Health Bar - Top Left
		CreateHealthBar();

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
		container.OffsetLeft = 10;
		container.OffsetTop = 10;
		container.OffsetRight = -10;
		container.OffsetBottom = -10;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.02f, 0.02f, 0.02f, 0.9f) };
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 4);
		container.AddChild(vbox);

		var titleLabel = new Label { Text = "❤ HEALTH" };
		titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.3f, 0.3f, 1));
		titleLabel.AddThemeFontSizeOverride("font_size", 14);
		vbox.AddChild(titleLabel);

		_healthLabel = new Label { Text = "100 / 100" };
		_healthLabel.AddThemeColorOverride("font_color", Colors.White);
		_healthLabel.AddThemeFontSizeOverride("font_size", 12);
		vbox.AddChild(_healthLabel);

		_healthBar = new ProgressBar();
		_healthBar.MinValue = 0;
		_healthBar.MaxValue = _player.MaxPlayerHealth;
		_healthBar.Value = _player._playerHealth;
		_healthBar.CustomMinimumSize = new Vector2(200, 20);

		var barBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f) };
		_healthBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = new Color(1, 0.3f, 0.3f, 1) };
		_healthBar.AddThemeStyleboxOverride("fill", barFill);

		vbox.AddChild(_healthBar);
		AddChild(container);
	}

	private void CreateLevelDisplay()
	{
		// ✨ Level panel - Top center
		var container = new PanelContainer();
		container.AnchorLeft = 0.35f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 0.65f;
		container.AnchorBottom = 0.15f;
		container.OffsetLeft = 10;
		container.OffsetTop = 10;
		container.OffsetRight = -10;
		container.OffsetBottom = -10;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.02f, 0.02f, 0.02f, 0.9f) };
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 4);
		container.AddChild(vbox);

		_levelLabel = new Label { Text = "⭐ LEVEL 1" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", 20);
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_levelLabel);

		_xpLabel = new Label { Text = "XP: 0 / 100" };
		_xpLabel.AddThemeColorOverride("font_color", Colors.White);
		_xpLabel.AddThemeFontSizeOverride("font_size", 12);
		_xpLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_xpLabel);

		_xpBar = new ProgressBar();
		_xpBar.MinValue = 0;
		_xpBar.MaxValue = 100;
		_xpBar.Value = 0;
		_xpBar.CustomMinimumSize = new Vector2(200, 16);

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
		vbox.OffsetLeft = -80;
		vbox.OffsetTop = 0;
		vbox.OffsetRight = -5;
		vbox.OffsetBottom = -10;
		vbox.AddThemeConstantOverride("separation", 8);

		// ✨ AUTO BATTLE BUTTON
		var autoBattleBtn = new Button { Text = "⚔️ AUTO" };
		autoBattleBtn.CustomMinimumSize = new Vector2(70, 40);
		autoBattleBtn.AddThemeFontSizeOverride("font_size", 16);
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
			btnContainer.CustomMinimumSize = new Vector2(70, 70);

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
			btn.AddThemeFontSizeOverride("font_size", 40);

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
			cooldownLabel.AddThemeFontSizeOverride("font_size", 24);
			cooldownLabel.AnchorLeft = cooldownLabel.AnchorTop = 0.5f;
			cooldownLabel.AnchorRight = cooldownLabel.AnchorBottom = 0.5f;
			cooldownLabel.OffsetLeft = cooldownLabel.OffsetTop = -12;
			overlay.AddChild(cooldownLabel);

			btnContainer.AddChild(overlay);
			vbox.AddChild(btnContainer);

			buttons[i] = btn;
			overlays[i] = overlay;
			cooldownLabels[i] = cooldownLabel;
		}

		// Update references
		var lightBtn = buttons[0];
		var heavyBtn = buttons[1];
		var specialBtn = buttons[2];
		var lightOverlay = overlays[0];
		var heavyOverlay = overlays[1];
		var specialOverlay = overlays[2];
		var lightCooldown = cooldownLabels[0];
		var heavyCooldown = cooldownLabels[1];
		var specialCooldown = cooldownLabels[2];

		// ✨ NEW: STATS BUTTON
		var statsBtn = new Button { Text = "📊 STATS" };
		statsBtn.CustomMinimumSize = new Vector2(70, 40);
		statsBtn.AddThemeFontSizeOverride("font_size", 14);
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
		// Simplified - only showing skill cooldowns, no stamina
	}

	private void UpdateSelection()
	{
		// Selection logic removed - simplified
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
