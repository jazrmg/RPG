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
	private VBoxContainer _buttonContainer;

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
		GD.Print("[StatAllocationUI] _Ready() called");
		// Initialization will be called via InitializeDirectly() from Player3D using CallDeferred
	}

	// ✨ NEW: Called via CallDeferred from Player3D
	public void InitializeDirectly(LevelingSystem levelingSystem)
	{
		GD.Print($"[StatAllocationUI] InitializeDirectly() called!");
		
		if (levelingSystem == null)
		{
			GD.PrintErr("[StatAllocationUI] ERROR: LevelingSystem is null!");
			return;
		}

		Initialize(levelingSystem);
	}

	// ✨ Original Initialize method
	public void Initialize(LevelingSystem levelingSystem)
	{
		GD.Print($"[StatAllocationUI] Initialize() called with LevelingSystem");
		
		_levelSystem = levelingSystem;
		if (_levelSystem == null)
		{
			GD.PrintErr("[StatAllocationUI] ERROR: LevelingSystem parameter is null!");
			return;
		}

		CreateUI();
		Visible = false;

		// ✨ Connect to level up callback (C# Action)
		GD.Print($"[StatAllocationUI] Connecting to LevelingSystem.LevelUp callback...");
		_levelSystem.LevelUp += ShowLevelUpPanel;
		GD.Print($"[StatAllocationUI] ✅ Connected! ShowLevelUpPanel will be called on level up");
	}

	private void CreateUI()
	{
		// ✨ Main panel (centered, semi-transparent)
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0.25f;
		_panel.AnchorTop = 0.25f;
		_panel.AnchorRight = 0.75f;
		_panel.AnchorBottom = 0.75f;
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.1f, 0.95f) };
		bgStyle.SetBorderWidthAll(3);
		bgStyle.BorderColor = new Color(1, 1, 0, 1);  // Gold border
		_panel.AddThemeStyleboxOverride("panel", bgStyle);

		// ✨ Main container
		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 16);
		_panel.AddChild(vbox);

		// ✨ Title
		_levelLabel = new Label { Text = "⭐ LEVEL UP!" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", 48);
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_levelLabel);

		// ✨ Subtitle
		var subtitle = new Label { Text = "Choose a stat to improve" };
		subtitle.AddThemeColorOverride("font_color", Colors.White);
		subtitle.AddThemeFontSizeOverride("font_size", 24);
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(subtitle);

		// ✨ Stat buttons
		_buttonContainer = new VBoxContainer();
		_buttonContainer.AddThemeConstantOverride("separation", 8);

		for (int i = 0; i < _statNames.Length; i++)
		{
			string statName = _statNames[i];
			Color color = _statColors[i];
			int index = i;  // Capture for closure

			var btn = new Button { Text = $"+1 {statName}" };
			btn.CustomMinimumSize = new Vector2(300, 50);
			btn.AddThemeFontSizeOverride("font_size", 20);
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
		try
		{
			GD.Print($"[StatAllocationUI] ShowLevelUpPanel() called!");
			
			if (_levelSystem == null) 
			{
				GD.PrintErr("[StatAllocationUI] ERROR: _levelSystem is NULL!");
				return;
			}

			GD.Print($"[StatAllocationUI] Setting level label...");
			if (_levelLabel == null)
			{
				GD.PrintErr("[StatAllocationUI] ERROR: _levelLabel is NULL!");
				return;
			}

			_levelLabel.Text = $"⭐ LEVEL {_levelSystem.CurrentLevel}!";
			GD.Print($"[StatAllocationUI] Level label set");
			
			GD.Print($"[StatAllocationUI] Setting Visible = true...");
			Visible = true;
			GD.Print($"[StatAllocationUI] Visible set to true");
			
			// ✨ FIXED: Check if GetTree() exists before pausing
			GD.Print($"[StatAllocationUI] Pausing game...");
			if (GetTree() != null)
			{
				GetTree().Paused = true;
				GD.Print($"[StatAllocationUI] Game paused");
			}
			else
			{
				GD.PrintErr("[StatAllocationUI] WARNING: GetTree() is null, skipping pause");
			}
			
			GD.Print($"✨ LEVEL UP UI SHOWN - Player can now spend stat point!");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[StatAllocationUI] ERROR in ShowLevelUpPanel: {ex.Message}");
			GD.PrintErr($"[StatAllocationUI] Stack trace: {ex.StackTrace}");
		}
	}

	private void AllocateStatPoint(string statName, int index)
	{
		if (_levelSystem == null) return;

		_levelSystem.AllocateStatPoint(statName);

		// Hide panel
		Visible = false;
		
		// ✨ FIXED: Check if GetTree() exists before unpausing
		if (GetTree() != null)
		{
			GetTree().Paused = false;
		}

		GD.Print($"✅ Stat point allocated to {statName}");
	}

	public override void _Process(double delta)
	{
		// Allow ESC to close (optional)
		if (Visible && Input.IsActionJustPressed("ui_cancel"))
		{
			Visible = false;
			// ✨ FIXED: Check if GetTree() exists before unpausing
			if (GetTree() != null)
			{
				GetTree().Paused = false;
			}
		}
	}
}
