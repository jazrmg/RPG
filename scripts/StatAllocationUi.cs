using Godot;
using System;

public partial class StatAllocationUI : CanvasLayer
{
	private LevelingSystem _levelSystem;
	private PanelContainer _panel;
	private Label _levelLabel;
	private Label _pointsLabel;
	private VBoxContainer _statsContainer;

	private readonly string[] _statNames = { "Health", "Damage", "AttackSpeed", "Crit" };
	private readonly string[] _icons = { "❤️", "⚔️", "⚡", "💥" };
	
	private readonly Color[] _colors = new Color[]
	{
		new Color(0.2f, 1f, 0.2f, 1),
		new Color(1f, 0.3f, 0.3f, 1),
		new Color(1f, 1f, 0.2f, 1),
		new Color(1f, 0.6f, 0.2f, 1)
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
		_levelSystem.LevelUp += ShowPanel;
	}

	private void CreateUI()
	{
		_panel = new PanelContainer();
		_panel.AnchorLeft = 0.2f;
		_panel.AnchorTop = 0.3f;
		_panel.AnchorRight = 0.8f;
		_panel.AnchorBottom = 0.8f;
		
		var bgStyle = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.15f, 0.98f) };
		bgStyle.SetBorderWidthAll(4);
		bgStyle.BorderColor = new Color(1, 1, 0, 1);
		_panel.AddThemeStyleboxOverride("panel", bgStyle);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 12);
		_panel.AddChild(vbox);

		_levelLabel = new Label { Text = "⭐ STAT ALLOCATION" };
		_levelLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
		_levelLabel.AddThemeFontSizeOverride("font_size", 32);
		_levelLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_levelLabel);

		_pointsLabel = new Label { Text = "Available Points: 0" };
		_pointsLabel.AddThemeColorOverride("font_color", Colors.White);
		_pointsLabel.AddThemeFontSizeOverride("font_size", 20);
		_pointsLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_pointsLabel);

		_statsContainer = new VBoxContainer();
		_statsContainer.AddThemeConstantOverride("separation", 10);
		vbox.AddChild(_statsContainer);

		for (int i = 0; i < 4; i++)
		{
			string statName = _statNames[i];
			Color color = _colors[i];
			int index = i;

			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			row.CustomMinimumSize = new Vector2(0, 60);
			_statsContainer.AddChild(row);

			var label = new Label { Text = $"{_icons[i]} {statName}" };
			label.AddThemeColorOverride("font_color", color);
			label.AddThemeFontSizeOverride("font_size", 18);
			label.CustomMinimumSize = new Vector2(150, 0);
			row.AddChild(label);

			var btn = new Button { Text = "+1" };
			btn.CustomMinimumSize = new Vector2(100, 60);
			btn.AddThemeFontSizeOverride("font_size", 18);
			btn.AddThemeColorOverride("font_color", Colors.Black);
			
			var normalStyle = new StyleBoxFlat { BgColor = color };
			btn.AddThemeStyleboxOverride("normal", normalStyle);
			
			var hoverStyle = new StyleBoxFlat { BgColor = color * 1.3f };
			btn.AddThemeStyleboxOverride("hover", hoverStyle);
			
			btn.Pressed += () => {
				if (_levelSystem != null && _levelSystem.AvailableStatPoints > 0)
				{
					_levelSystem.AllocateStatPoint(statName);
					UpdateDisplay();
				}
			};
			row.AddChild(btn);
		}

		AddChild(_panel);
	}

	public void ToggleStatsPanel()
	{
		if (_levelSystem == null) return;
		Visible = !Visible;
		if (Visible)
			UpdateDisplay();
	}

	public void OpenFromButton()
	{
		ToggleStatsPanel();
	}

	private void UpdateDisplay()
	{
		if (_levelSystem == null) return;

		_levelLabel.Text = $"⭐ LEVEL {_levelSystem.CurrentLevel}";
		_pointsLabel.Text = $"Available Points: {_levelSystem.AvailableStatPoints}";

		for (int i = 0; i < 4; i++)
		{
			var row = _statsContainer.GetChild(i) as HBoxContainer;
			if (row == null) continue;

			var btn = row.GetChild(1) as Button;
			if (btn == null) continue;

			int points = i switch
			{
				0 => _levelSystem.HealthPoints,
				1 => _levelSystem.DamagePoints,
				2 => _levelSystem.AttackSpeedPoints,
				3 => _levelSystem.CritChancePoints,
				_ => 0
			};

			btn.Text = $"+1 (x{points})";
		}
	}

	private void ShowPanel()
	{
		UpdateDisplay();
	}

	public override void _Process(double delta)
	{
		if (Visible && _levelSystem != null)
		{
			UpdateDisplay();
		}
	}
}
