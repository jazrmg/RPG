using Godot;
using System;

/// <summary>
/// ✨ REAL-TIME STAT ALLOCATION UI - Updates every frame!
/// Shows actual bonus values and attack speed multiplier in real-time
/// </summary>
public partial class StatAllocationUI : CanvasLayer
{
	private LevelingSystem _levelSystem;
	private PanelContainer _panel;
	private Label _levelLabel;
	private Label _availablePointsLabel;
	private Label _spentPointsLabel;
	private VBoxContainer _statsContainer;
	private Button[] _allocateButtons = new Button[6];
	private Label[] _pointLabels = new Label[6];
	private Label[] _bonusLabels = new Label[6];

	private bool _lastZPressed = false;

	private readonly string[] _statNames = { "Health", "Damage", "AttackSpeed", "Stamina", "Dodge", "Crit" };
	private readonly string[] _statIcons = { "❤️", "⚔️", "⚡", "🔷", "🛡️", "💥" };
	
	private readonly Color[] _statColors = new Color[]
	{
		new Color(0.2f, 1f, 0.2f, 1),      // Green - Health
		new Color(1f, 0.3f, 0.3f, 1),      // Red - Damage
		new Color(1f, 1f, 0.2f, 1),        // Yellow - Speed
		new Color(0.2f, 0.8f, 1f, 1),      // Blue - Stamina
		new Color(0.8f, 0.2f, 1f, 1),      // Purple - Dodge
		new Color(1f, 0.6f, 0.2f, 1)       // Orange - Crit
	};

	public override void _Ready()
	{
		Layer = 100;
	}

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
		// ✨ MASSIVE panel
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0.1f;
		_panel.AnchorTop = 0.1f;
		_panel.AnchorRight = 0.9f;
		_panel.AnchorBottom = 0.9f;
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.15f, 0.98f) };
		bgStyle.SetBorderWidthAll(6);
		bgStyle.BorderColor = new Color(1, 1, 0, 1);
		_panel.AddThemeStyleboxOverride("panel", bgStyle);

		var mainVBox = new VBoxContainer();
		mainVBox.AddThemeConstantOverride("separation", 15);
		_panel.AddChild(mainVBox);

		// ✨ TITLE
		_levelLabel = new Label { Text = "⭐ STAT ALLOCATION - LEVEL 1" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", 48);
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(_levelLabel);

		// ✨ INFO ROW
		var infoBox = new HBoxContainer();
		infoBox.AddThemeConstantOverride("separation", 30);
		infoBox.CustomMinimumSize = new Vector2(0, 60);
		mainVBox.AddChild(infoBox);

		_availablePointsLabel = new Label { Text = "🟢 AVAILABLE: 0" };
		_availablePointsLabel.AddThemeColorOverride("font_color", new Color(0, 1, 0, 1));
		_availablePointsLabel.AddThemeFontSizeOverride("font_size", 32);
		infoBox.AddChild(_availablePointsLabel);

		_spentPointsLabel = new Label { Text = "📊 SPENT: 0" };
		_spentPointsLabel.AddThemeColorOverride("font_color", new Color(1, 0.7f, 0, 1));
		_spentPointsLabel.AddThemeFontSizeOverride("font_size", 32);
		infoBox.AddChild(_spentPointsLabel);

		// ✨ SEPARATOR
		var separator = new HSeparator();
		mainVBox.AddChild(separator);

		// ✨ STATS GRID
		_statsContainer = new VBoxContainer();
		_statsContainer.AddThemeConstantOverride("separation", 12);
		mainVBox.AddChild(_statsContainer);

		for (int i = 0; i < 6; i++)
		{
			string statName = _statNames[i];
			Color color = _statColors[i];
			int index = i;

			var statRow = new HBoxContainer();
			statRow.AddThemeConstantOverride("separation", 15);
			statRow.CustomMinimumSize = new Vector2(0, 90);
			_statsContainer.AddChild(statRow);

			// ✨ ICON + NAME (fixed width)
			var nameLabel = new Label { Text = $"{_statIcons[i]} {statName}" };
			nameLabel.AddThemeColorOverride("font_color", color);
			nameLabel.AddThemeFontSizeOverride("font_size", 26);
			nameLabel.CustomMinimumSize = new Vector2(220, 0);
			statRow.AddChild(nameLabel);

			// ✨ POINTS (fixed width)
			var pointsLabel = new Label { Text = "Pts: 0" };
			pointsLabel.AddThemeColorOverride("font_color", Colors.White);
			pointsLabel.AddThemeFontSizeOverride("font_size", 22);
			pointsLabel.CustomMinimumSize = new Vector2(140, 0);
			_pointLabels[i] = pointsLabel;
			statRow.AddChild(pointsLabel);

			// ✨ BONUS (fixed width, expandable)
			var bonusLabel = new Label { Text = "Bonus: +0" };
			bonusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			bonusLabel.AddThemeFontSizeOverride("font_size", 22);
			bonusLabel.CustomMinimumSize = new Vector2(300, 0);
			_bonusLabels[i] = bonusLabel;
			statRow.AddChild(bonusLabel);

			// ✨ HUGE BUTTON
			var btn = new Button { Text = "+ ALLOCATE" };
			btn.CustomMinimumSize = new Vector2(220, 90);
			btn.AddThemeFontSizeOverride("font_size", 26);
			btn.AddThemeColorOverride("font_color", Colors.Black);

			var normalStyle = new StyleBoxFlat { BgColor = color };
			btn.AddThemeStyleboxOverride("normal", normalStyle);

			var hoverStyle = new StyleBoxFlat { BgColor = color * 1.5f };
			btn.AddThemeStyleboxOverride("hover", hoverStyle);

			var pressStyle = new StyleBoxFlat { BgColor = color * 1.8f };
			btn.AddThemeStyleboxOverride("focus", pressStyle);
			btn.AddThemeStyleboxOverride("pressed", pressStyle);

			btn.Pressed += () => {
				if (_levelSystem != null && _levelSystem.AvailableStatPoints > 0)
				{
					_levelSystem.AllocateStatPoint(statName);
				}
			};
			
			_allocateButtons[i] = btn;
			statRow.AddChild(btn);
		}

		// ✨ FOOTER
		var footerLabel = new Label { Text = "Press Z to close | Click button to allocate point | Updates in REAL-TIME!" };
		footerLabel.AddThemeColorOverride("font_color", Colors.White);
		footerLabel.AddThemeFontSizeOverride("font_size", 18);
		footerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(footerLabel);

		AddChild(_panel);
	}

	public void ToggleStatsPanel()
	{
		if (_levelSystem == null) return;

		Visible = !Visible;
		if (Visible)
		{
			UpdateDisplay();
		}
	}

	private void UpdateDisplay()
	{
		if (_levelSystem == null) return;

		// ✨ Update level
		_levelLabel.Text = $"⭐ STAT ALLOCATION - LEVEL {_levelSystem.CurrentLevel}";

		// ✨ Update available and spent points
		int totalSpent = _levelSystem.HealthPoints + _levelSystem.DamagePoints + 
						 _levelSystem.AttackSpeedPoints + _levelSystem.StaminaPoints + 
						 _levelSystem.DodgeChancePoints + _levelSystem.CritChancePoints;
		
		_availablePointsLabel.Text = $"🟢 AVAILABLE: {_levelSystem.AvailableStatPoints}";
		_spentPointsLabel.Text = $"📊 SPENT: {totalSpent}";

		// ✨ Update each stat - REAL VALUES from LevelingSystem
		for (int i = 0; i < 6; i++)
		{
			int points = i switch
			{
				0 => _levelSystem.HealthPoints,
				1 => _levelSystem.DamagePoints,
				2 => _levelSystem.AttackSpeedPoints,
				3 => _levelSystem.StaminaPoints,
				4 => _levelSystem.DodgeChancePoints,
				5 => _levelSystem.CritChancePoints,
				_ => 0
			};

			_pointLabels[i].Text = $"Pts: {points}";

			// ✨ REAL-TIME bonus calculations
			string bonusText = i switch
			{
				0 => $"Bonus: +{_levelSystem.GetHealthBonus():F0} HP",
				1 => $"×{Mathf.Pow(1.6f, points):F2} Damage",
				2 => $"Speed: ×{_levelSystem.GetAttackSpeedMultiplier():F2}",  // ✨ ACTUAL multiplier!
				3 => $"Bonus: +{_levelSystem.GetStaminaBonus():F0}",
				4 => $"Bonus: +{(_levelSystem.GetDodgeChanceBonus() * 100):F1}%",
				5 => $"Bonus: +{(_levelSystem.GetCritChanceBonus() * 100):F1}%",
				_ => "ERROR"
			};

			_bonusLabels[i].Text = bonusText;

			// ✨ Disable button if no points available
			_allocateButtons[i].Disabled = _levelSystem.AvailableStatPoints <= 0;
		}
	}

	private void ShowLevelUpPanel()
	{
		// ✨ Don't auto-open! User must press Z to open stats
		// Just update in background
		UpdateDisplay();
	}

	public override void _Process(double delta)
	{
		// ✨ UPDATE EVERY SINGLE FRAME!
		if (Visible && _levelSystem != null)
		{
			UpdateDisplay();
		}

		// Check for Z key press
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
	}
}
