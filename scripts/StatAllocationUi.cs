using Godot;
using System;

/// <summary>
/// ✨ STAT ALLOCATION UI - Shows when player levels up
/// Allows player to choose which stat to boost
/// </summary>
public partial class StatAllocationUI : CanvasLayer
{
	private LevelingSystem _levelSystem;
	private PanelContainer _panel;
	private Label _levelLabel;
	private HBoxContainer _buttonContainer;

	// ✨ NEW: Key press tracking (only Z now)
	private bool _lastZPressed = false;

	private readonly string[] _statNames = { "Health", "Damage", "AttackSpeed", "Stamina", "Dodge", "Crit" };
	private readonly Color[] _statColors = new Color[]
	{
		new Color(0.2f, 1f, 0.2f, 1),    // Green - Health
		new Color(1f, 0.3f, 0.3f, 1),    // Red - Damage
		new Color(1f, 1f, 0.2f, 1),      // Yellow - Speed
		new Color(0.2f, 0.8f, 1f, 1),    // Blue - Stamina
		new Color(0.8f, 0.2f, 1f, 1),    // Purple - Dodge
		new Color(1f, 0.6f, 0.2f, 1)     // Orange - Crit
	};

	public override void _Ready()
	{
		Layer = 100;  // Above everything else
		// Initialization will be called via InitializeDirectly() from Player3D using CallDeferred
	}

	// ✨ NEW: Called via CallDeferred from Player3D
	public void InitializeDirectly(LevelingSystem levelingSystem)
	{
		if (levelingSystem == null)
		{
			return;
		}

		Initialize(levelingSystem);
	}

	// ✨ Original Initialize method
	public void Initialize(LevelingSystem levelingSystem)
	{
		_levelSystem = levelingSystem;
		if (_levelSystem == null)
		{
			return;
		}

		CreateUI();
		Visible = false;

		// ✨ Connect to level up callback (C# Action)
		_levelSystem.LevelUp += ShowLevelUpPanel;
	}

	private void CreateUI()
	{
		// ✨ Main panel (bottom center, NOT centered)
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0.25f;
		_panel.AnchorTop = 0.75f;  // ✨ BOTTOM instead of center
		_panel.AnchorRight = 0.75f;
		_panel.AnchorBottom = 0.95f;  // ✨ Near bottom
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.1f, 0.95f) };
		bgStyle.SetBorderWidthAll(3);
		bgStyle.BorderColor = new Color(1, 1, 0, 1);  // Gold border
		_panel.AddThemeStyleboxOverride("panel", bgStyle);

		// ✨ Main container
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		_panel.AddChild(vbox);

		// ✨ Title with level and available points
		_levelLabel = new Label { Text = "⭐ STATS" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", 32);
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_levelLabel);

		// ✨ Stat buttons (smaller, horizontal layout)
		_buttonContainer = new HBoxContainer();  // ✨ CHANGED: HBox for horizontal layout
		_buttonContainer.AddThemeConstantOverride("separation", 8);
		_buttonContainer.Alignment = BoxContainer.AlignmentMode.Center;

		for (int i = 0; i < _statNames.Length; i++)
		{
			string statName = _statNames[i];
			Color color = _statColors[i];
			int index = i;

			var btn = new Button { Text = $"+{statName[0]}" };  // ✨ Single letter for space
			btn.CustomMinimumSize = new Vector2(60, 40);
			btn.AddThemeFontSizeOverride("font_size", 16);
			btn.AddThemeColorOverride("font_color", Colors.White);

			var normalStyle = new StyleBoxFlat { BgColor = color };
			btn.AddThemeStyleboxOverride("normal", normalStyle);

			var hoverStyle = new StyleBoxFlat { BgColor = color * 1.3f };
			btn.AddThemeStyleboxOverride("hover", hoverStyle);

			var pressStyle = new StyleBoxFlat { BgColor = color * 1.5f };
			btn.AddThemeStyleboxOverride("focus", pressStyle);
			btn.AddThemeStyleboxOverride("pressed", pressStyle);

			btn.Pressed += () => AllocateStatPoint(statName, index);

			_buttonContainer.AddChild(btn);
		}

		vbox.AddChild(_buttonContainer);
		AddChild(_panel);
	}

	private void ShowLevelUpPanel()
	{
		// ✨ CHANGED: Just notify player, don't auto-show
		// Player can open stats from bottom button
	}

	private void AllocateStatPoint(string statName, int index)
	{
		if (_levelSystem == null) return;

		_levelSystem.AllocateStatPoint(statName);

		// Hide panel
		Visible = false;
		// ✨ REMOVED: GetTree().Paused = false; - no longer pausing
	}

	// ✨ NEW: Public method to toggle the stats panel (called from SkillUIManager)
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
			_levelLabel.Text = $"⭐ LEVEL {_levelSystem.CurrentLevel}!\nAvailable Points: {_levelSystem.AvailableStatPoints}";
			Visible = true;
		}
	}

	public override void _Process(double delta)
	{
		// ✨ HOTKEY: Only Z opens stats (X key removed)
		
		// Check for Z key press
		if (Input.IsKeyPressed(Key.Z))
		{
			if (!_lastZPressed)  // Only toggle once per press
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
