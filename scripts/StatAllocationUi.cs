using Godot;
using System;

public partial class StatAllocationUI : CanvasLayer
{
	private LevelingSystem _levelSystem;
	private PanelContainer _panel;
	private Label _levelLabel;
	private Label _availablePointsLabel;
	private Label _spentPointsLabel;
	private VBoxContainer _statsContainer;
	private Button[] _allocateButtons = new Button[4];
	private Label[] _pointLabels = new Label[4];
	private Label[] _bonusLabels = new Label[4];

	private bool _lastZPressed = false;

	private readonly string[] _statNames = { "Health", "Damage", "AttackSpeed", "Crit" };
	private readonly string[] _statIcons = { "❤️", "⚔️", "⚡", "💥" };
	
	private readonly Color[] _statColors = new Color[]
	{
		new Color(0.2f, 1f, 0.2f, 1),      // Green - Health
		new Color(1f, 0.3f, 0.3f, 1),      // Red - Damage
		new Color(1f, 1f, 0.2f, 1),        // Yellow - Speed
		new Color(1f, 0.6f, 0.2f, 1)       // Orange - Crit
	};

	private Vector2 _screenSize;
	private float _uiScale = 1.0f;

	public override void _Ready()
	{
		Layer = 100;
		_screenSize = GetViewport().GetVisibleRect().Size;
		CalculateUIScale();
	}

	private void CalculateUIScale()
	{
		float referenceWidth = 1920f;
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		_uiScale = viewportSize.X / referenceWidth;
		_uiScale = Mathf.Clamp(_uiScale, 0.5f, 2.0f);
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
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0.1f;
		_panel.AnchorTop = 0.1f;
		_panel.AnchorRight = 0.9f;
		_panel.AnchorBottom = 0.9f;
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.15f, 0.98f) };
		bgStyle.SetBorderWidthAll((int)(6 * _uiScale));
		bgStyle.BorderColor = new Color(1, 1, 0, 1);
		_panel.AddThemeStyleboxOverride("panel", bgStyle);

		var mainVBox = new VBoxContainer();
		mainVBox.AddThemeConstantOverride("separation", (int)(15 * _uiScale));
		_panel.AddChild(mainVBox);

		_levelLabel = new Label { Text = "⭐ STAT ALLOCATION - LEVEL 1" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", (int)(48 * _uiScale));
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(_levelLabel);

		var infoBox = new HBoxContainer();
		infoBox.AddThemeConstantOverride("separation", (int)(30 * _uiScale));
		infoBox.CustomMinimumSize = new Vector2(0, 60 * _uiScale);
		mainVBox.AddChild(infoBox);

		_availablePointsLabel = new Label { Text = "🟢 AVAILABLE: 0" };
		_availablePointsLabel.AddThemeColorOverride("font_color", new Color(0, 1, 0, 1));
		_availablePointsLabel.AddThemeFontSizeOverride("font_size", (int)(32 * _uiScale));
		infoBox.AddChild(_availablePointsLabel);

		_spentPointsLabel = new Label { Text = "📊 SPENT: 0" };
		_spentPointsLabel.AddThemeColorOverride("font_color", new Color(1, 0.7f, 0, 1));
		_spentPointsLabel.AddThemeFontSizeOverride("font_size", (int)(32 * _uiScale));
		infoBox.AddChild(_spentPointsLabel);

		var separator = new HSeparator();
		mainVBox.AddChild(separator);

		_statsContainer = new VBoxContainer();
		_statsContainer.AddThemeConstantOverride("separation", (int)(12 * _uiScale));
		mainVBox.AddChild(_statsContainer);

		for (int i = 0; i < 4; i++)
		{
			string statName = _statNames[i];
			Color color = _statColors[i];
			int index = i;

			var statRow = new HBoxContainer();
			statRow.AddThemeConstantOverride("separation", (int)(15 * _uiScale));
			statRow.CustomMinimumSize = new Vector2(0, 90 * _uiScale);
			_statsContainer.AddChild(statRow);

			var nameLabel = new Label { Text = $"{_statIcons[i]} {statName}" };
			nameLabel.AddThemeColorOverride("font_color", color);
			nameLabel.AddThemeFontSizeOverride("font_size", (int)(26 * _uiScale));
			nameLabel.CustomMinimumSize = new Vector2(220 * _uiScale, 0);
			statRow.AddChild(nameLabel);

			var pointsLabel = new Label { Text = "Pts: 0" };
			pointsLabel.AddThemeColorOverride("font_color", Colors.White);
			pointsLabel.AddThemeFontSizeOverride("font_size", (int)(22 * _uiScale));
			pointsLabel.CustomMinimumSize = new Vector2(140 * _uiScale, 0);
			_pointLabels[i] = pointsLabel;
			statRow.AddChild(pointsLabel);

			var bonusLabel = new Label { Text = "Bonus: +0" };
			bonusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
			bonusLabel.AddThemeFontSizeOverride("font_size", (int)(22 * _uiScale));
			bonusLabel.CustomMinimumSize = new Vector2(300 * _uiScale, 0);
			_bonusLabels[i] = bonusLabel;
			statRow.AddChild(bonusLabel);

			var btn = new Button { Text = "+ ALLOCATE" };
			btn.CustomMinimumSize = new Vector2(220 * _uiScale, 90 * _uiScale);
			btn.AddThemeFontSizeOverride("font_size", (int)(26 * _uiScale));
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

		var footerLabel = new Label { Text = "Press Z to close | Click button to allocate point | Updates in REAL-TIME!" };
		footerLabel.AddThemeColorOverride("font_color", Colors.White);
		footerLabel.AddThemeFontSizeOverride("font_size", (int)(18 * _uiScale));
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

		_levelLabel.Text = $"⭐ STAT ALLOCATION - LEVEL {_levelSystem.CurrentLevel}";

		int totalSpent = _levelSystem.HealthPoints + _levelSystem.DamagePoints + 
						 _levelSystem.AttackSpeedPoints + _levelSystem.CritChancePoints;
		
		_availablePointsLabel.Text = $"🟢 AVAILABLE: {_levelSystem.AvailableStatPoints}";
		_spentPointsLabel.Text = $"📊 SPENT: {totalSpent}";

		for (int i = 0; i < 4; i++)
		{
			int points = i switch
			{
				0 => _levelSystem.HealthPoints,
				1 => _levelSystem.DamagePoints,
				2 => _levelSystem.AttackSpeedPoints,
				3 => _levelSystem.CritChancePoints,
				_ => 0
			};

			_pointLabels[i].Text = $"Pts: {points}";

			string bonusText = i switch
			{
				0 => $"Bonus: +{_levelSystem.GetHealthBonus():F0} HP",
				1 => $"×{Mathf.Pow(1.6f, points):F2} Damage",
				2 => $"Speed: ×{_levelSystem.GetAttackSpeedMultiplier():F2}",
				3 => $"Bonus: +{(_levelSystem.GetCritChanceBonus() * 100):F1}%",
				_ => "ERROR"
			};

			_bonusLabels[i].Text = bonusText;

			_allocateButtons[i].Disabled = _levelSystem.AvailableStatPoints <= 0;
		}
	}

	private void ShowLevelUpPanel()
	{
		UpdateDisplay();
	}

	public override void _Process(double delta)
	{
		Vector2 newScreenSize = GetViewport().GetVisibleRect().Size;
		if (newScreenSize != _screenSize)
		{
			_screenSize = newScreenSize;
			CalculateUIScale();
		}

		if (Visible && _levelSystem != null)
		{
			UpdateDisplay();
		}

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
