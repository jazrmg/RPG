using Godot;

public partial class SkillUIManager : Control
{
	private Label _healthValue;
	private ProgressBar _healthBar;
	private Label _levelTitle;
	private ProgressBar _xpBar;
	
	private Button _buttonLight;
	private Button _buttonHeavy;
	private Button _buttonSpecial;
	private Button _buttonAuto;
	private Button _buttonStats;
	private Button _buttonEquip;
	
	private Player3D _player;
	private StatAllocationUI _statsUI;

	public override void _Ready()
	{
		// Find Player3D
		_player = GetTree().Root.GetChild(0).FindChild("Player3D", true, false) as Player3D;
		if (_player == null)
		{
			GD.PrintErr("Player3D not found!");
			return;
		}

		// Find Stats UI
		_statsUI = _player.StatsUI;

		// Find health nodes
		_healthValue = GetNode<Label>("HealthContainer/HealthVBox/HealthValue");
		_healthBar = GetNode<ProgressBar>("HealthContainer/HealthVBox/HealthBar");
		
		// Find level nodes
		_levelTitle = GetNode<Label>("LevelContainer/LevelVBox/LevelTitle");
		_xpBar = GetNode<ProgressBar>("LevelContainer/LevelVBox/XPBar");

		// Find skill buttons
		_buttonLight = GetNode<Button>("SkillsContainer/Button_Light");
		_buttonHeavy = GetNode<Button>("SkillsContainer/Button_Heavy");
		_buttonSpecial = GetNode<Button>("SkillsContainer/Button_Special");
		_buttonAuto = GetNode<Button>("SkillsContainer/Button_Auto");
		_buttonStats = GetNode<Button>("SkillsContainer/Button_Stats");
		_buttonEquip = GetNode<Button>("SkillsContainer/Button_Equip");

		// Setup health bar
		_healthBar.MinValue = 0;
		_healthBar.MaxValue = _player.MaxPlayerHealth;
		_healthBar.Value = _player._playerHealth;

		// Setup XP bar
		_xpBar.MinValue = 0;
		_xpBar.MaxValue = 100;
		_xpBar.Value = 0;

		// Connect button signals
		_buttonLight.Pressed += () => SelectAttack(1);
		_buttonHeavy.Pressed += () => SelectAttack(2);
		_buttonSpecial.Pressed += () => SelectAttack(3);
		_buttonAuto.Pressed += () => ToggleAutoBattle();
		_buttonStats.Pressed += () => OpenStats();
		_buttonEquip.Pressed += () => ToggleSwordEquip();
	}

	public override void _Process(double delta)
	{
		if (_player == null) return;

		UpdateHealthDisplay();
		UpdateLevelDisplay();
		UpdateButtonStyles();
	}

	private void UpdateHealthDisplay()
	{
		_healthValue.Text = $"{_player._playerHealth:F0} / {_player.MaxPlayerHealth:F0}";
		_healthBar.Value = _player._playerHealth;
	}

	private void UpdateLevelDisplay()
	{
		if (_player.LevelingSystem == null) return;

		_levelTitle.Text = $"⭐ LEVEL {_player.LevelingSystem.CurrentLevel}";

		float xpPercent = _player.LevelingSystem.CurrentXP / _player.LevelingSystem.XPRequiredForNextLevel;
		_xpBar.Value = Mathf.Clamp(xpPercent * 100, 0, 100);
	}

	private void UpdateButtonStyles()
	{
		// Reset attack buttons
		ResetButtonStyle(_buttonLight, new Color(0.1f, 0.9f, 0.1f, 0.9f));  // Green
		ResetButtonStyle(_buttonHeavy, new Color(0.95f, 0.75f, 0.0f, 0.9f)); // Yellow
		ResetButtonStyle(_buttonSpecial, new Color(0.95f, 0.1f, 0.1f, 0.9f)); // Red

		// Highlight selected attack mode
		if (_player._attackModeTimer > 0)
		{
			switch (_player._currentAttackMode)
			{
				case Player3D.AttackMode.Light:
					HighlightButton(_buttonLight, new Color(0.1f, 0.9f, 0.1f, 0.9f));
					break;
				case Player3D.AttackMode.Heavy:
					HighlightButton(_buttonHeavy, new Color(0.95f, 0.75f, 0.0f, 0.9f));
					break;
				case Player3D.AttackMode.Special:
					HighlightButton(_buttonSpecial, new Color(0.95f, 0.1f, 0.1f, 0.9f));
					break;
			}
		}

		// Update auto battle button
		if (_player._isAutoBattle)
		{
			var activeStyle = new StyleBoxFlat { BgColor = new Color(0.9f, 0.2f, 0.2f, 0.9f) };
			activeStyle.SetBorderWidthAll(3);
			activeStyle.BorderColor = Colors.White;
			_buttonAuto.AddThemeStyleboxOverride("normal", activeStyle);
		}
		else
		{
			var inactiveStyle = new StyleBoxFlat { BgColor = new Color(0.7f, 0.35f, 1.0f, 0.9f) };
			inactiveStyle.SetBorderWidthAll(2);
			inactiveStyle.BorderColor = Colors.White;
			_buttonAuto.AddThemeStyleboxOverride("normal", inactiveStyle);
		}

		// Update equip button
		if (_player._isSwordEquipped)
		{
			var equippedStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.9f, 0.1f, 0.9f) };  // Bright green
			equippedStyle.SetBorderWidthAll(3);
			equippedStyle.BorderColor = Colors.White;
			_buttonEquip.AddThemeStyleboxOverride("normal", equippedStyle);
		}
		else
		{
			var unequippedStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.35f, 0.8f, 0.9f) };  // Purple
			unequippedStyle.SetBorderWidthAll(2);
			unequippedStyle.BorderColor = Colors.White;
			_buttonEquip.AddThemeStyleboxOverride("normal", unequippedStyle);
		}
	}

	private void ResetButtonStyle(Button btn, Color color)
	{
		var style = new StyleBoxFlat { BgColor = color };
		style.SetBorderWidthAll(2);
		style.BorderColor = Colors.White;
		btn.AddThemeStyleboxOverride("normal", style);
	}

	private void HighlightButton(Button btn, Color color)
	{
		var style = new StyleBoxFlat { BgColor = color * 1.3f };
		style.SetBorderWidthAll(3);
		style.BorderColor = Colors.White;
		btn.AddThemeStyleboxOverride("normal", style);
	}

	private void SelectAttack(int skillNum)
	{
		if (_player == null) return;

		_player._currentAttackMode = skillNum switch
		{
			1 => Player3D.AttackMode.Light,
			2 => Player3D.AttackMode.Heavy,
			3 => Player3D.AttackMode.Special,
			_ => Player3D.AttackMode.None
		};
		_player._attackModeTimer = _player.AttackModeWindowTime;
	}

	private void ToggleAutoBattle()
	{
		if (_player == null) return;

		_player._isAutoBattle = !_player._isAutoBattle;
	}

	private void ToggleSwordEquip()
	{
		if (_player == null) return;

		_player._isSwordEquipped = !_player._isSwordEquipped;
		
		// Show/hide sword in hand
		var swordRoot = _player.FindChild("antique_estoc_1k", true, false);
		if (swordRoot != null)
		{
			if (swordRoot is Node3D node3d)
			{
				node3d.Visible = _player._isSwordEquipped;
			}
		}
	}

	private void OpenStats()
	{
		if (_statsUI != null)
		{
			_statsUI.ToggleStatsPanel();
		}
	}
}
