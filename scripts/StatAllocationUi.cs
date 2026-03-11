using Godot;
using System;

/// <summary>
/// ✨ STAT ALLOCATION UI - Beautiful redesigned version
/// Shows when player levels up
/// Allows player to choose which stat to boost
/// </summary>
public partial class StatAllocationUI : CanvasLayer
{
	private LevelingSystem _levelSystem;
	private PanelContainer _panel;
	private Label _levelLabel;
	private Label _pointsLabel;
	private VBoxContainer _statsContainer;

	private bool _lastZPressed = false;

	private readonly string[] _statNames = { "Health", "Damage", "AttackSpeed", "Stamina", "Dodge", "Crit" };
	private readonly string[] _statDescriptions = new string[]
	{
		"Increase maximum HP\n+5 HP per point",
		"Increase attack damage\n+2 damage per point",
		"INSANELY FAST attacks\n60% faster per point ⚡",  // ✨ EXTREME SPEED!
		"Increase max stamina\n+5 stamina per point",
		"Increase dodge chance\n+2% dodge per point",
		"Increase critical chance\n+1% crit per point"
	};

	private readonly Color[] _statColors = new Color[]
	{
		new Color(0.2f, 1f, 0.2f, 1),      // Green - Health
		new Color(1f, 0.3f, 0.3f, 1),      // Red - Damage
		new Color(1f, 1f, 0.2f, 1),        // Yellow - Speed
		new Color(0.2f, 0.8f, 1f, 1),      // Blue - Stamina
		new Color(0.8f, 0.2f, 1f, 1),      // Purple - Dodge
		new Color(1f, 0.6f, 0.2f, 1)       // Orange - Crit
	};

	private int[] _currentStatPoints = new int[6];
	private Label[] _statPointLabels = new Label[6];

	public override void _Ready()
	{
		Layer = 100;
	}

	public void InitializeDirectly(LevelingSystem levelingSystem)
	{
		if (levelingSystem == null)
		{
			return;
		}

		Initialize(levelingSystem);
	}

	public void Initialize(LevelingSystem levelingSystem)
	{
		_levelSystem = levelingSystem;
		if (_levelSystem == null)
		{
			return;
		}

		CreateUI();
		Visible = false;

		_levelSystem.LevelUp += ShowLevelUpPanel;
	}

	private void CreateUI()
	{
		// ✨ Main panel - centered, larger, more beautiful
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0.15f;
		_panel.AnchorTop = 0.15f;
		_panel.AnchorRight = 0.85f;
		_panel.AnchorBottom = 0.85f;
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.15f, 0.98f) };
		bgStyle.SetBorderWidthAll(4);
		bgStyle.BorderColor = new Color(1f, 0.8f, 0.2f, 1);  // Gold border
		_panel.AddThemeStyleboxOverride("panel", bgStyle);

		// ✨ Main container
		var mainVBox = new VBoxContainer();
		mainVBox.AddThemeConstantOverride("separation", 20);
		_panel.AddChild(mainVBox);

		// ✨ Header section
		var headerVBox = new VBoxContainer();
		headerVBox.AddThemeConstantOverride("separation", 8);
		mainVBox.AddChild(headerVBox);

		// ✨ Level label - large and prominent
		_levelLabel = new Label { Text = "⭐ LEVEL UP!" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", 48);
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		headerVBox.AddChild(_levelLabel);

		// ✨ Points available
		_pointsLabel = new Label { Text = "Available Points: 1" };
		_pointsLabel.AddThemeColorOverride("font_color", new Color(0.8f, 1, 0.8f, 1));
		_pointsLabel.AddThemeFontSizeOverride("font_size", 24);
		_pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
		headerVBox.AddChild(_pointsLabel);

		// ✨ Divider
		var divider = new HSeparator();
		mainVBox.AddChild(divider);

		// ✨ Stats grid (3 columns x 2 rows)
		var gridContainer = new GridContainer();
		gridContainer.Columns = 3;
		gridContainer.AddThemeConstantOverride("h_separation", 16);
		gridContainer.AddThemeConstantOverride("v_separation", 16);
		mainVBox.AddChild(gridContainer);

		for (int i = 0; i < _statNames.Length; i++)
		{
			string statName = _statNames[i];
			string description = _statDescriptions[i];
			Color color = _statColors[i];
			int index = i;

			// Create stat card
			var card = new PanelContainer();
			card.CustomMinimumSize = new Vector2(220, 200);

			var cardStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.2f, 0.9f) };
			cardStyle.SetBorderWidthAll(2);
			cardStyle.BorderColor = color;
			card.AddThemeStyleboxOverride("panel", cardStyle);

			var cardVBox = new VBoxContainer();
			cardVBox.AddThemeConstantOverride("separation", 8);
			card.AddChild(cardVBox);

			// ✨ Stat name (big and bold)
			var nameLabel = new Label { Text = statName };
			nameLabel.AddThemeColorOverride("font_color", color);
			nameLabel.AddThemeFontSizeOverride("font_size", 22);
			nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
			cardVBox.AddChild(nameLabel);

			// ✨ Description (smaller, wrapping text)
			var descLabel = new Label { Text = description };
			descLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 1));
			descLabel.AddThemeFontSizeOverride("font_size", 12);
			descLabel.HorizontalAlignment = HorizontalAlignment.Center;
			descLabel.AutowrapMode = TextServer.AutowrapMode.Word;
			cardVBox.AddChild(descLabel);

			// ✨ Current points display
			var pointsLabel = new Label { Text = "+0 points" };
			pointsLabel.AddThemeColorOverride("font_color", new Color(0.7f, 1, 0.7f, 1));
			pointsLabel.AddThemeFontSizeOverride("font_size", 13);
			pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
			cardVBox.AddChild(pointsLabel);
			_statPointLabels[i] = pointsLabel;

			// ✨ Add spacer to push button down
			cardVBox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

			// ✨ Button to allocate point
			var btn = new Button { Text = "Allocate Point" };
			btn.CustomMinimumSize = new Vector2(0, 45);
			btn.AddThemeFontSizeOverride("font_size", 14);
			btn.AddThemeColorOverride("font_color", Colors.White);

			var normalStyle = new StyleBoxFlat { BgColor = color * 0.6f };
			btn.AddThemeStyleboxOverride("normal", normalStyle);

			var hoverStyle = new StyleBoxFlat { BgColor = color };
			btn.AddThemeStyleboxOverride("hover", hoverStyle);

			var pressStyle = new StyleBoxFlat { BgColor = color * 1.3f };
			btn.AddThemeStyleboxOverride("focus", pressStyle);
			btn.AddThemeStyleboxOverride("pressed", pressStyle);

			btn.Pressed += () => AllocateStatPoint(statName, index);

			cardVBox.AddChild(btn);
			gridContainer.AddChild(card);
		}

		AddChild(_panel);
	}

	private void ShowLevelUpPanel()
	{
		// Notified - player can open stats with Z key
	}

	private void AllocateStatPoint(string statName, int index)
	{
		if (_levelSystem == null) return;

		_levelSystem.AllocateStatPoint(statName);
		UpdateCurrentStatPoints();
		UpdatePointsLabel();

		// Close panel if no more points
		if (_levelSystem.AvailableStatPoints <= 0)
		{
			Visible = false;
		}
	}

	private void UpdateCurrentStatPoints()
	{
		_currentStatPoints[0] = _levelSystem.HealthPoints;
		_currentStatPoints[1] = _levelSystem.DamagePoints;
		_currentStatPoints[2] = _levelSystem.AttackSpeedPoints;
		_currentStatPoints[3] = _levelSystem.StaminaPoints;
		_currentStatPoints[4] = _levelSystem.DodgeChancePoints;
		_currentStatPoints[5] = _levelSystem.CritChancePoints;

		for (int i = 0; i < _statPointLabels.Length; i++)
		{
			int points = _currentStatPoints[i];
			if (points > 0)
			{
				_statPointLabels[i].Text = $"+{points} points";
				_statPointLabels[i].AddThemeColorOverride("font_color", new Color(1, 1, 0.3f, 1));
			}
			else
			{
				_statPointLabels[i].Text = "+0 points";
				_statPointLabels[i].AddThemeColorOverride("font_color", new Color(0.7f, 1, 0.7f, 1));
			}
		}
	}

	private void UpdatePointsLabel()
	{
		int available = _levelSystem.AvailableStatPoints;
		string color = available > 0 ? "[color=#00ff00]" : "[color=#ff6666]";
		string endColor = "[/color]";
		
		_pointsLabel.Text = $"Available Points: {color}{available}{endColor}";
	}

	public void ToggleStatsPanel()
	{
		if (_levelSystem == null) 
		{
			return;
		}

		if (Visible)
		{
			// Close panel
			Visible = false;
		}
		else
		{
			// Open panel
			UpdateCurrentStatPoints();
			_levelLabel.Text = $"⭐ LEVEL {_levelSystem.CurrentLevel}";
			UpdatePointsLabel();
			Visible = true;
		}
	}

	public override void _Process(double delta)
	{
		// ✨ REAL-TIME UPDATE: Update level and points while panel is open
		if (Visible && _levelSystem != null)
		{
			_levelLabel.Text = $"⭐ LEVEL {_levelSystem.CurrentLevel}";
			UpdatePointsLabel();
			UpdateCurrentStatPoints();
		}

		// Hotkey: Z key opens/closes stats
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
