using Godot;
using System;

/// <summary>
/// ✨ STAT ALLOCATION UI - Full RPG stat panel
/// Shows stat cards with icons, current values, bonuses, and allocation buttons
/// Opens with Z key or STATS button in the HUD
/// </summary>
public partial class StatAllocationUI : CanvasLayer
{
	private LevelingSystem _levelSystem;
	private PanelContainer _panel;
	private Label _titleLabel;
	private Label _pointsLabel;
	private VBoxContainer _statListContainer;

	// Key press tracking
	private bool _lastZPressed = false;

	// Stat definitions
	private readonly string[] _statNames = { "Health", "Damage", "AttackSpeed", "Stamina", "Dodge", "Crit" };
	private readonly string[] _statIcons = { "❤", "⚔", "⚡", "💨", "🛡", "💥" };
	private readonly string[] _statDescriptions = {
		"+5 Max HP per point",
		"+2 Base Damage per point",
		"+5% Attack Speed per point",
		"+5 Max Stamina per point",
		"+2% Dodge Chance per point",
		"+1% Crit Chance per point"
	};
	private readonly Color[] _statColors = new Color[]
	{
		new Color(0.3f, 0.95f, 0.3f, 1),   // Green - Health
		new Color(1f, 0.35f, 0.35f, 1),     // Red - Damage
		new Color(1f, 0.95f, 0.25f, 1),     // Yellow - Speed
		new Color(0.3f, 0.75f, 1f, 1),      // Blue - Stamina
		new Color(0.75f, 0.35f, 1f, 1),     // Purple - Dodge
		new Color(1f, 0.6f, 0.2f, 1)        // Orange - Crit
	};

	// Per-stat UI elements for live updates
	private Label[] _statValueLabels;
	private ProgressBar[] _statBars;
	private Button[] _statButtons;

	public override void _Ready()
	{
		Layer = 100;
	}

	// Called via CallDeferred from Player3D
	public void InitializeDirectly(LevelingSystem levelingSystem)
	{
		if (levelingSystem == null) return;
		Initialize(levelingSystem);
	}

	public void Initialize(LevelingSystem levelingSystem)
	{
		_levelSystem = levelingSystem;
		if (_levelSystem == null) return;

		CreateUI();
		Visible = false;

		_levelSystem.LevelUp += ShowLevelUpPanel;
	}

	private void CreateUI()
	{
		_statValueLabels = new Label[_statNames.Length];
		_statBars = new ProgressBar[_statNames.Length];
		_statButtons = new Button[_statNames.Length];

		// ── DARK FULLSCREEN BACKDROP (semi-transparent) ──
		var backdrop = new ColorRect();
		backdrop.AnchorLeft = 0;
		backdrop.AnchorTop = 0;
		backdrop.AnchorRight = 1;
		backdrop.AnchorBottom = 1;
		backdrop.Color = new Color(0, 0, 0, 0.5f);
		AddChild(backdrop);

		// ── MAIN PANEL (centered, taller for stat cards) ──
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0.15f;
		_panel.AnchorTop = 0.10f;
		_panel.AnchorRight = 0.85f;
		_panel.AnchorBottom = 0.90f;

		var bgStyle = new StyleBoxFlat();
		bgStyle.BgColor = new Color(0.06f, 0.06f, 0.12f, 0.97f);
		bgStyle.SetBorderWidthAll(3);
		bgStyle.BorderColor = new Color(0.85f, 0.7f, 0.2f, 1);
		bgStyle.SetCornerRadiusAll(12);
		bgStyle.ContentMarginLeft = 24;
		bgStyle.ContentMarginRight = 24;
		bgStyle.ContentMarginTop = 16;
		bgStyle.ContentMarginBottom = 16;
		_panel.AddThemeStyleboxOverride("panel", bgStyle);

		// ── ROOT VBOX ──
		var rootVBox = new VBoxContainer();
		rootVBox.AddThemeConstantOverride("separation", 10);
		_panel.AddChild(rootVBox);

		// ── HEADER ROW: Title + Close Button ──
		var headerRow = new HBoxContainer();
		headerRow.AddThemeConstantOverride("separation", 8);
		rootVBox.AddChild(headerRow);

		_titleLabel = new Label { Text = "⭐ CHARACTER STATS" };
		_titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.9f, 0.3f, 1));
		_titleLabel.AddThemeFontSizeOverride("font_size", 28);
		_titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		headerRow.AddChild(_titleLabel);

		var closeBtn = new Button { Text = "✕" };
		closeBtn.CustomMinimumSize = new Vector2(36, 36);
		closeBtn.AddThemeFontSizeOverride("font_size", 20);
		closeBtn.AddThemeColorOverride("font_color", Colors.White);
		var closeBtnStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.15f, 0.15f, 1) };
		closeBtnStyle.SetCornerRadiusAll(6);
		closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
		var closeBtnHover = new StyleBoxFlat { BgColor = new Color(0.85f, 0.2f, 0.2f, 1) };
		closeBtnHover.SetCornerRadiusAll(6);
		closeBtn.AddThemeStyleboxOverride("hover", closeBtnHover);
		closeBtn.Pressed += () => { Visible = false; };
		headerRow.AddChild(closeBtn);

		// ── AVAILABLE POINTS LABEL ──
		_pointsLabel = new Label { Text = "Available Points: 0" };
		_pointsLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 1f, 1));
		_pointsLabel.AddThemeFontSizeOverride("font_size", 18);
		_pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
		rootVBox.AddChild(_pointsLabel);

		// ── THIN SEPARATOR LINE ──
		var separator = new HSeparator();
		separator.AddThemeConstantOverride("separation", 8);
		rootVBox.AddChild(separator);

		// ── SCROLLABLE STAT LIST ──
		var scrollContainer = new ScrollContainer();
		scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		rootVBox.AddChild(scrollContainer);

		_statListContainer = new VBoxContainer();
		_statListContainer.AddThemeConstantOverride("separation", 8);
		_statListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		scrollContainer.AddChild(_statListContainer);

		// ── BUILD EACH STAT CARD ──
		for (int i = 0; i < _statNames.Length; i++)
		{
			CreateStatCard(i);
		}

		// ── FOOTER HINT ──
		var footerLabel = new Label { Text = "Press Z to close  •  Click + to allocate points" };
		footerLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f, 1));
		footerLabel.AddThemeFontSizeOverride("font_size", 13);
		footerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		rootVBox.AddChild(footerLabel);

		AddChild(_panel);
	}

	/// <summary>
	/// Creates one stat card row: [Icon + Name + Desc] [Bar] [Value] [+Button]
	/// </summary>
	private void CreateStatCard(int index)
	{
		string statName = _statNames[index];
		string icon = _statIcons[index];
		string desc = _statDescriptions[index];
		Color color = _statColors[index];

		// ── CARD CONTAINER (dark row with colored left accent) ──
		var card = new PanelContainer();
		card.CustomMinimumSize = new Vector2(0, 70);

		var cardStyle = new StyleBoxFlat();
		cardStyle.BgColor = new Color(0.08f, 0.08f, 0.15f, 1);
		cardStyle.SetCornerRadiusAll(8);
		cardStyle.BorderWidthLeft = 5;
		cardStyle.BorderColor = color;
		cardStyle.ContentMarginLeft = 16;
		cardStyle.ContentMarginRight = 12;
		cardStyle.ContentMarginTop = 8;
		cardStyle.ContentMarginBottom = 8;
		card.AddThemeStyleboxOverride("panel", cardStyle);

		// ── MAIN HBOX inside card ──
		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 12);
		card.AddChild(hbox);

		// ── LEFT SECTION: Icon + Name + Description ──
		var leftVBox = new VBoxContainer();
		leftVBox.AddThemeConstantOverride("separation", 2);
		leftVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		leftVBox.CustomMinimumSize = new Vector2(160, 0);
		hbox.AddChild(leftVBox);

		var nameRow = new HBoxContainer();
		nameRow.AddThemeConstantOverride("separation", 6);
		leftVBox.AddChild(nameRow);

		var iconLabel = new Label { Text = icon };
		iconLabel.AddThemeFontSizeOverride("font_size", 22);
		nameRow.AddChild(iconLabel);

		var nameLabel = new Label { Text = statName.ToUpper() };
		nameLabel.AddThemeColorOverride("font_color", color);
		nameLabel.AddThemeFontSizeOverride("font_size", 18);
		nameRow.AddChild(nameLabel);

		var descLabel = new Label { Text = desc };
		descLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.55f, 0.65f, 1));
		descLabel.AddThemeFontSizeOverride("font_size", 12);
		leftVBox.AddChild(descLabel);

		// ── MIDDLE SECTION: Stat bar showing allocated points (max 20 visual) ──
		var barVBox = new VBoxContainer();
		barVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		barVBox.AddThemeConstantOverride("separation", 2);
		hbox.AddChild(barVBox);

		// Small spacer to vertically center the bar
		var barSpacer = new Control();
		barSpacer.CustomMinimumSize = new Vector2(0, 10);
		barVBox.AddChild(barSpacer);

		var statBar = new ProgressBar();
		statBar.MinValue = 0;
		statBar.MaxValue = 20;
		statBar.Value = 0;
		statBar.CustomMinimumSize = new Vector2(140, 16);
		statBar.ShowPercentage = false;

		var barBg = new StyleBoxFlat { BgColor = new Color(0.12f, 0.12f, 0.18f, 1) };
		barBg.SetCornerRadiusAll(4);
		statBar.AddThemeStyleboxOverride("background", barBg);

		var barFill = new StyleBoxFlat { BgColor = color * 0.85f };
		barFill.SetCornerRadiusAll(4);
		statBar.AddThemeStyleboxOverride("fill", barFill);

		barVBox.AddChild(statBar);
		_statBars[index] = statBar;

		// ── RIGHT SECTION: Value label + Allocate button ──
		var rightHBox = new HBoxContainer();
		rightHBox.AddThemeConstantOverride("separation", 8);
		hbox.AddChild(rightHBox);

		var valueLabel = new Label { Text = "0" };
		valueLabel.AddThemeColorOverride("font_color", Colors.White);
		valueLabel.AddThemeFontSizeOverride("font_size", 22);
		valueLabel.CustomMinimumSize = new Vector2(36, 0);
		valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
		rightHBox.AddChild(valueLabel);
		_statValueLabels[index] = valueLabel;

		var allocBtn = new Button { Text = "+" };
		allocBtn.CustomMinimumSize = new Vector2(44, 44);
		allocBtn.AddThemeFontSizeOverride("font_size", 24);
		allocBtn.AddThemeColorOverride("font_color", Colors.White);

		var btnNormal = new StyleBoxFlat { BgColor = color * 0.7f };
		btnNormal.SetCornerRadiusAll(8);
		allocBtn.AddThemeStyleboxOverride("normal", btnNormal);

		var btnHover = new StyleBoxFlat { BgColor = color };
		btnHover.SetCornerRadiusAll(8);
		btnHover.SetBorderWidthAll(2);
		btnHover.BorderColor = Colors.White;
		allocBtn.AddThemeStyleboxOverride("hover", btnHover);

		var btnPress = new StyleBoxFlat { BgColor = color * 1.3f };
		btnPress.SetCornerRadiusAll(8);
		allocBtn.AddThemeStyleboxOverride("pressed", btnPress);
		allocBtn.AddThemeStyleboxOverride("focus", btnPress);

		int capturedIndex = index;
		string capturedName = statName;
		allocBtn.Pressed += () => AllocateStatPoint(capturedName, capturedIndex);
		rightHBox.AddChild(allocBtn);
		_statButtons[index] = allocBtn;

		_statListContainer.AddChild(card);
	}

	private void ShowLevelUpPanel()
	{
		// Player can open stats from the STATS button or Z key
	}

	private void AllocateStatPoint(string statName, int index)
	{
		if (_levelSystem == null) return;
		if (_levelSystem.AvailableStatPoints <= 0) return;

		_levelSystem.AllocateStatPoint(statName);

		// Refresh the display immediately
		RefreshStatDisplay();

		// If no more points, close panel
		if (_levelSystem.AvailableStatPoints <= 0)
		{
			Visible = false;
		}
	}

	/// <summary>
	/// Refreshes all stat values, bars, points label, and button states
	/// </summary>
	private void RefreshStatDisplay()
	{
		if (_levelSystem == null) return;

		_titleLabel.Text = $"⭐ CHARACTER STATS — LEVEL {_levelSystem.CurrentLevel}";

		int pts = _levelSystem.AvailableStatPoints;
		_pointsLabel.Text = pts > 0
			? $"✦ Available Points: {pts} ✦"
			: "No points available";
		_pointsLabel.AddThemeColorOverride("font_color",
			pts > 0 ? new Color(1f, 0.9f, 0.3f, 1) : new Color(0.5f, 0.5f, 0.6f, 1));

		// Update each stat card
		int[] statValues = {
			_levelSystem.HealthPoints,
			_levelSystem.DamagePoints,
			_levelSystem.AttackSpeedPoints,
			_levelSystem.StaminaPoints,
			_levelSystem.DodgeChancePoints,
			_levelSystem.CritChancePoints
		};

		for (int i = 0; i < _statNames.Length; i++)
		{
			_statValueLabels[i].Text = statValues[i].ToString();
			_statBars[i].Value = Mathf.Min(statValues[i], 20);

			// Dim the + button if no points available
			bool canAllocate = pts > 0;
			_statButtons[i].Disabled = !canAllocate;
			_statButtons[i].Modulate = canAllocate
				? new Color(1, 1, 1, 1)
				: new Color(0.4f, 0.4f, 0.4f, 1);
		}
	}

	/// <summary>
	/// Public toggle called from SkillUIManager STATS button or Z key
	/// </summary>
	public void ToggleStatsPanel()
	{
		if (_levelSystem == null) return;

		if (Visible)
		{
			Visible = false;
		}
		else
		{
			RefreshStatDisplay();
			Visible = true;
		}
	}

	public override void _Process(double delta)
	{
		// Z key toggles the panel
		if (Input.IsKeyPressed(Key.Z))
		{
			if (!_lastZPressed)
			{
				ToggleStatsPanel();
				_lastZPressed = true;
			}
		}
		else
		{
			_lastZPressed = false;
		}

		// Live-update while panel is open (e.g., if XP ticks in background)
		if (Visible && _levelSystem != null)
		{
			RefreshStatDisplay();
		}
	}
}
