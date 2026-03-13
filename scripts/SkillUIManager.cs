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

	// Level display
	private Label _levelLabel;
	private ProgressBar _xpBar;
	private Label _xpLabel;

	// Stats UI reference
	private StatAllocationUI _statsUI;

	// Auto battle button (need reference for live color updates)
	private Button _autoBattleBtn;

	// Skill button labels for key hints
	private Label _lightKeyLabel, _heavyKeyLabel, _specialKeyLabel;

	private readonly Color _healthColor = new Color(0.85f, 0.2f, 0.25f, 1);
	private readonly Color _healthBarBg = new Color(0.25f, 0.05f, 0.05f, 1);
	private readonly Color _staminaColor = new Color(0.95f, 0.8f, 0.15f, 1);
	private readonly Color _staminaBarBg = new Color(0.25f, 0.2f, 0.02f, 1);
	private readonly Color _xpColor = new Color(0.3f, 0.75f, 1f, 1);

	private readonly Color[] _skillColors = new[] {
		new Color(0.15f, 0.8f, 0.15f),     // Green  - Light
		new Color(0.9f, 0.7f, 0.05f),      // Gold   - Heavy
		new Color(0.9f, 0.15f, 0.15f)      // Red    - Special
	};
	private readonly string[] _skillIcons = { "⚔", "🔥", "💀" };
	private readonly string[] _skillLabels = { "Light", "Heavy", "Special" };

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
		CreateHealthBar();
		CreateStaminaBar();
		CreateLevelDisplay();
		CreateSkillButtons();
	}

	// ═══════════════════════════════════════════════════════
	//  HEALTH BAR — Top Left
	// ═══════════════════════════════════════════════════════
	private void CreateHealthBar()
	{
		var container = new PanelContainer();
		container.AnchorLeft = 0.0f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 0.24f;
		container.AnchorBottom = 0.0f;
		container.OffsetLeft = 12;
		container.OffsetTop = 12;
		container.OffsetRight = 0;
		container.OffsetBottom = 80;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.04f, 0.04f, 0.07f, 0.92f) };
		bgStyle.SetCornerRadiusAll(10);
		bgStyle.SetBorderWidthAll(2);
		bgStyle.BorderColor = new Color(0.6f, 0.15f, 0.15f, 0.6f);
		bgStyle.ContentMarginLeft = 14;
		bgStyle.ContentMarginRight = 14;
		bgStyle.ContentMarginTop = 8;
		bgStyle.ContentMarginBottom = 8;
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 4);
		container.AddChild(vbox);

		// Title row with icon
		var titleRow = new HBoxContainer();
		titleRow.AddThemeConstantOverride("separation", 6);
		vbox.AddChild(titleRow);

		var iconLabel = new Label { Text = "❤" };
		iconLabel.AddThemeFontSizeOverride("font_size", 16);
		titleRow.AddChild(iconLabel);

		var titleLabel = new Label { Text = "HEALTH" };
		titleLabel.AddThemeColorOverride("font_color", _healthColor);
		titleLabel.AddThemeFontSizeOverride("font_size", 14);
		titleRow.AddChild(titleLabel);

		// Value label (right-aligned in its own row for clarity)
		_healthLabel = new Label { Text = "100 / 100" };
		_healthLabel.AddThemeColorOverride("font_color", Colors.White);
		_healthLabel.AddThemeFontSizeOverride("font_size", 13);
		vbox.AddChild(_healthLabel);

		// Progress bar
		_healthBar = new ProgressBar();
		_healthBar.MinValue = 0;
		_healthBar.MaxValue = _player.MaxPlayerHealth;
		_healthBar.Value = _player._playerHealth;
		_healthBar.CustomMinimumSize = new Vector2(0, 14);
		_healthBar.ShowPercentage = false;

		var barBg = new StyleBoxFlat { BgColor = _healthBarBg };
		barBg.SetCornerRadiusAll(5);
		_healthBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = _healthColor };
		barFill.SetCornerRadiusAll(5);
		_healthBar.AddThemeStyleboxOverride("fill", barFill);

		vbox.AddChild(_healthBar);
		AddChild(container);
	}

	// ═══════════════════════════════════════════════════════
	//  STAMINA BAR — Top Right
	// ═══════════════════════════════════════════════════════
	private void CreateStaminaBar()
	{
		var container = new PanelContainer();
		container.AnchorLeft = 0.76f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 1.0f;
		container.AnchorBottom = 0.0f;
		container.OffsetLeft = 0;
		container.OffsetTop = 12;
		container.OffsetRight = -12;
		container.OffsetBottom = 80;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.04f, 0.04f, 0.07f, 0.92f) };
		bgStyle.SetCornerRadiusAll(10);
		bgStyle.SetBorderWidthAll(2);
		bgStyle.BorderColor = new Color(0.5f, 0.45f, 0.05f, 0.6f);
		bgStyle.ContentMarginLeft = 14;
		bgStyle.ContentMarginRight = 14;
		bgStyle.ContentMarginTop = 8;
		bgStyle.ContentMarginBottom = 8;
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 4);
		container.AddChild(vbox);

		var titleRow = new HBoxContainer();
		titleRow.AddThemeConstantOverride("separation", 6);
		vbox.AddChild(titleRow);

		var iconLabel = new Label { Text = "⚡" };
		iconLabel.AddThemeFontSizeOverride("font_size", 16);
		titleRow.AddChild(iconLabel);

		var titleLabel = new Label { Text = "STAMINA" };
		titleLabel.AddThemeColorOverride("font_color", _staminaColor);
		titleLabel.AddThemeFontSizeOverride("font_size", 14);
		titleRow.AddChild(titleLabel);

		_staminaLabel = new Label { Text = "100 / 100" };
		_staminaLabel.AddThemeColorOverride("font_color", Colors.White);
		_staminaLabel.AddThemeFontSizeOverride("font_size", 13);
		vbox.AddChild(_staminaLabel);

		_staminaBar = new ProgressBar();
		_staminaBar.MinValue = 0;
		_staminaBar.MaxValue = _player.MaxStamina;
		_staminaBar.Value = _player._stamina;
		_staminaBar.CustomMinimumSize = new Vector2(0, 14);
		_staminaBar.ShowPercentage = false;

		var barBg = new StyleBoxFlat { BgColor = _staminaBarBg };
		barBg.SetCornerRadiusAll(5);
		_staminaBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = _staminaColor };
		barFill.SetCornerRadiusAll(5);
		_staminaBar.AddThemeStyleboxOverride("fill", barFill);

		vbox.AddChild(_staminaBar);
		AddChild(container);
	}

	// ═══════════════════════════════════════════════════════
	//  LEVEL / XP DISPLAY — Top Center
	// ═══════════════════════════════════════════════════════
	private void CreateLevelDisplay()
	{
		var container = new PanelContainer();
		container.AnchorLeft = 0.35f;
		container.AnchorTop = 0.0f;
		container.AnchorRight = 0.65f;
		container.AnchorBottom = 0.0f;
		container.OffsetTop = 12;
		container.OffsetBottom = 72;

		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.04f, 0.04f, 0.07f, 0.92f) };
		bgStyle.SetCornerRadiusAll(10);
		bgStyle.SetBorderWidthAll(2);
		bgStyle.BorderColor = new Color(0.2f, 0.55f, 0.8f, 0.5f);
		bgStyle.ContentMarginLeft = 16;
		bgStyle.ContentMarginRight = 16;
		bgStyle.ContentMarginTop = 6;
		bgStyle.ContentMarginBottom = 6;
		container.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 3);
		container.AddChild(vbox);

		_levelLabel = new Label { Text = "⭐ LEVEL 1" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 0.92f, 0.35f, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", 20);
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_levelLabel);

		_xpLabel = new Label { Text = "XP: 0 / 100" };
		_xpLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.85f, 1f, 1));
		_xpLabel.AddThemeFontSizeOverride("font_size", 12);
		_xpLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_xpLabel);

		_xpBar = new ProgressBar();
		_xpBar.MinValue = 0;
		_xpBar.MaxValue = 100;
		_xpBar.Value = 0;
		_xpBar.CustomMinimumSize = new Vector2(0, 10);
		_xpBar.ShowPercentage = false;

		var barBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.15f, 1) };
		barBg.SetCornerRadiusAll(4);
		_xpBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = _xpColor };
		barFill.SetCornerRadiusAll(4);
		_xpBar.AddThemeStyleboxOverride("fill", barFill);

		vbox.AddChild(_xpBar);
		AddChild(container);
	}

	// ═══════════════════════════════════════════════════════
	//  SKILL BUTTONS — Right Side (vertical strip)
	// ═══════════════════════════════════════════════════════
	private void CreateSkillButtons()
	{
		var vbox = new VBoxContainer();
		vbox.AnchorLeft = 1.0f;
		vbox.AnchorTop = 0.15f;
		vbox.AnchorRight = 1.0f;
		vbox.AnchorBottom = 1.0f;
		vbox.OffsetLeft = -95;
		vbox.OffsetTop = 0;
		vbox.OffsetRight = -8;
		vbox.OffsetBottom = -10;
		vbox.AddThemeConstantOverride("separation", 10);

		// ── AUTO BATTLE BUTTON ──
		_autoBattleBtn = CreateActionButton("⚔ AUTO", new Color(0.55f, 0.25f, 0.75f, 1), 42);
		_autoBattleBtn.Pressed += () => {
			if (_player != null)
			{
				_player._isAutoBattle = !_player._isAutoBattle;
				UpdateAutoBattleButton();
			}
		};
		vbox.AddChild(_autoBattleBtn);

		// ── SKILL 1, 2, 3 ──
		Button[] buttons = new Button[3];
		Control[] overlays = new Control[3];
		Label[] cooldownLabels = new Label[3];

		for (int i = 0; i < 3; i++)
		{
			int capturedIndex = i;
			var skillCard = CreateSkillCard(i, out buttons[i], out overlays[i], out cooldownLabels[i]);
			buttons[i].Pressed += () => SelectSkill(capturedIndex + 1);
			vbox.AddChild(skillCard);
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

		// ── STATS BUTTON ──
		var statsBtn = CreateActionButton("📊 STATS", new Color(0.2f, 0.55f, 0.75f, 1), 42);
		statsBtn.Pressed += () => { _statsUI?.ToggleStatsPanel(); };
		vbox.AddChild(statsBtn);

		// ── KEY HINT ──
		var hintLabel = new Label { Text = "Z = Stats" };
		hintLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f, 1));
		hintLabel.AddThemeFontSizeOverride("font_size", 11);
		hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(hintLabel);

		AddChild(vbox);
	}

	/// <summary>
	/// Builds a single skill card: icon + key label + button + cooldown overlay
	/// </summary>
	private PanelContainer CreateSkillCard(int index, out Button btn, out Control overlay, out Label cooldownLabel)
	{
		Color color = _skillColors[index];
		string icon = _skillIcons[index];
		string label = _skillLabels[index];
		string keyNum = (index + 1).ToString();

		// Card background
		var card = new PanelContainer();
		card.CustomMinimumSize = new Vector2(84, 84);

		var cardStyle = new StyleBoxFlat { BgColor = new Color(0.06f, 0.06f, 0.1f, 0.9f) };
		cardStyle.SetCornerRadiusAll(10);
		cardStyle.SetBorderWidthAll(2);
		cardStyle.BorderColor = color * 0.5f;
		card.AddThemeStyleboxOverride("panel", cardStyle);

		// Vertical layout inside card
		var innerVBox = new VBoxContainer();
		innerVBox.AddThemeConstantOverride("separation", 2);
		card.AddChild(innerVBox);

		// Key number hint at top
		var keyLabel = new Label { Text = keyNum };
		keyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f, 1));
		keyLabel.AddThemeFontSizeOverride("font_size", 11);
		keyLabel.HorizontalAlignment = HorizontalAlignment.Center;
		innerVBox.AddChild(keyLabel);

		// Main button (icon)
		btn = new Button { Text = icon };
		btn.CustomMinimumSize = new Vector2(0, 44);
		btn.AddThemeFontSizeOverride("font_size", 28);
		btn.AddThemeColorOverride("font_color", Colors.White);

		var normalStyle = new StyleBoxFlat { BgColor = color * 0.6f };
		normalStyle.SetCornerRadiusAll(8);
		btn.AddThemeStyleboxOverride("normal", normalStyle);

		var hoverStyle = new StyleBoxFlat { BgColor = color * 0.85f };
		hoverStyle.SetCornerRadiusAll(8);
		hoverStyle.SetBorderWidthAll(2);
		hoverStyle.BorderColor = Colors.White;
		btn.AddThemeStyleboxOverride("hover", hoverStyle);

		var pressStyle = new StyleBoxFlat { BgColor = color };
		pressStyle.SetCornerRadiusAll(8);
		btn.AddThemeStyleboxOverride("pressed", pressStyle);
		btn.AddThemeStyleboxOverride("focus", pressStyle);

		innerVBox.AddChild(btn);

		// Skill name below button
		var nameLabel = new Label { Text = label };
		nameLabel.AddThemeColorOverride("font_color", color);
		nameLabel.AddThemeFontSizeOverride("font_size", 11);
		nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		innerVBox.AddChild(nameLabel);

		// ── COOLDOWN OVERLAY (covers entire card) ──
		overlay = new Control { Visible = false };
		overlay.AnchorLeft = overlay.AnchorTop = 0.0f;
		overlay.AnchorRight = overlay.AnchorBottom = 1.0f;

		var overlayPanel = new Panel();
		overlayPanel.AnchorLeft = overlayPanel.AnchorTop = 0.0f;
		overlayPanel.AnchorRight = overlayPanel.AnchorBottom = 1.0f;
		var overlayStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.03f, 0.03f, 0.85f) };
		overlayStyle.SetCornerRadiusAll(10);
		overlayPanel.AddThemeStyleboxOverride("panel", overlayStyle);
		overlay.AddChild(overlayPanel);

		cooldownLabel = new Label { Text = "0.0s" };
		cooldownLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0.5f, 1));
		cooldownLabel.AddThemeFontSizeOverride("font_size", 18);
		cooldownLabel.HorizontalAlignment = HorizontalAlignment.Center;
		cooldownLabel.VerticalAlignment = VerticalAlignment.Center;
		cooldownLabel.AnchorLeft = 0;
		cooldownLabel.AnchorTop = 0.25f;
		cooldownLabel.AnchorRight = 1;
		cooldownLabel.AnchorBottom = 0.75f;
		overlay.AddChild(cooldownLabel);

		card.AddChild(overlay);

		return card;
	}

	/// <summary>
	/// Creates a small action button (AUTO, STATS, etc.)
	/// </summary>
	private Button CreateActionButton(string text, Color color, int height)
	{
		var btn = new Button { Text = text };
		btn.CustomMinimumSize = new Vector2(84, height);
		btn.AddThemeFontSizeOverride("font_size", 14);
		btn.AddThemeColorOverride("font_color", Colors.White);

		var normalStyle = new StyleBoxFlat { BgColor = color * 0.7f };
		normalStyle.SetCornerRadiusAll(8);
		btn.AddThemeStyleboxOverride("normal", normalStyle);

		var hoverStyle = new StyleBoxFlat { BgColor = color };
		hoverStyle.SetCornerRadiusAll(8);
		hoverStyle.SetBorderWidthAll(2);
		hoverStyle.BorderColor = Colors.White;
		btn.AddThemeStyleboxOverride("hover", hoverStyle);

		var pressStyle = new StyleBoxFlat { BgColor = color * 1.2f };
		pressStyle.SetCornerRadiusAll(8);
		btn.AddThemeStyleboxOverride("pressed", pressStyle);
		btn.AddThemeStyleboxOverride("focus", pressStyle);

		return btn;
	}

	private void UpdateAutoBattleButton()
	{
		if (_autoBattleBtn == null || _player == null) return;

		Color activeColor = new Color(0.85f, 0.15f, 0.15f, 1);
		Color inactiveColor = new Color(0.55f, 0.25f, 0.75f, 1);
		Color color = _player._isAutoBattle ? activeColor : inactiveColor;

		var style = new StyleBoxFlat { BgColor = color * 0.7f };
		style.SetCornerRadiusAll(8);
		if (_player._isAutoBattle)
		{
			style.SetBorderWidthAll(2);
			style.BorderColor = new Color(1, 0.4f, 0.4f, 1);
		}
		_autoBattleBtn.AddThemeStyleboxOverride("normal", style);
		_autoBattleBtn.Text = _player._isAutoBattle ? "⚔ STOP" : "⚔ AUTO";
	}

	// ═══════════════════════════════════════════════════════
	//  PROCESS — Updates every frame
	// ═══════════════════════════════════════════════════════
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
		// Health
		_healthLabel.Text = $"{_player._playerHealth:F0} / {_player.MaxPlayerHealth:F0}";
		_healthBar.Value = _player._playerHealth;

		// Flash health bar red when low
		if (_player._playerHealth < _player.MaxPlayerHealth * 0.25f)
		{
			float pulse = (Mathf.Sin((float)Time.GetTicksMsec() / 200f) + 1f) * 0.5f;
			Color flashColor = _healthColor.Lerp(new Color(1, 0, 0, 1), pulse * 0.5f);
			var flashFill = new StyleBoxFlat { BgColor = flashColor };
			flashFill.SetCornerRadiusAll(5);
			_healthBar.AddThemeStyleboxOverride("fill", flashFill);
		}
		else
		{
			var normalFill = new StyleBoxFlat { BgColor = _healthColor };
			normalFill.SetCornerRadiusAll(5);
			_healthBar.AddThemeStyleboxOverride("fill", normalFill);
		}

		// Stamina
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
		}
	}

	private void UpdateSelection()
	{
		ResetButtonStyle(_lightBtn, _skillColors[0]);
		ResetButtonStyle(_heavyBtn, _skillColors[1]);
		ResetButtonStyle(_specialBtn, _skillColors[2]);

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

	private void ResetButtonStyle(Button btn, Color color)
	{
		var normalStyle = new StyleBoxFlat { BgColor = color * 0.6f };
		normalStyle.SetCornerRadiusAll(8);
		btn.AddThemeStyleboxOverride("normal", normalStyle);
	}

	private void ApplySelectedStyle(Button btn, Color color)
	{
		var selectedStyle = new StyleBoxFlat { BgColor = color };
		selectedStyle.SetCornerRadiusAll(8);
		selectedStyle.SetBorderWidthAll(3);
		selectedStyle.BorderColor = new Color(1, 1, 0.6f, 1);
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
