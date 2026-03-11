using Godot;
using System;
using System.Collections.Generic;  // ✨ FIX: For Dictionary<,>

/// <summary>
/// ✨ LEVELING SYSTEM - Standalone class for player progression
/// Can be imported into any character class
/// Handles: XP, levels, stat points, soft cap scaling
/// </summary>
public partial class LevelingSystem : Node
{
	// ✨ LEVEL CONSTANTS
	private const int BASE_XP_PER_KILL = 100;
	private const int SOFT_CAP_LEVEL = 30;
	private const float SOFT_CAP_REDUCTION = 0.90f;  // 10% reduction per level after 30
	private const int MAX_LEVEL = 100;

	// ✨ STAT POINT VALUES (per point spent)
	private const float HEALTH_PER_POINT = 5f;
	private const float DAMAGE_PER_POINT = 2f;
	private const float ATTACK_SPEED_PER_POINT = 0.60f;  // ✨ EXTREME: 60% per point (INSANELY FAST - 5x stronger than before!)
	private const float STAMINA_PER_POINT = 5f;
	private const float DODGE_CHANCE_PER_POINT = 0.02f;  // 2%
	private const float CRIT_CHANCE_PER_POINT = 0.01f;   // 1%

	// ✨ PLAYER PROGRESSION STATE
	public int CurrentLevel { get; private set; } = 1;
	public float CurrentXP { get; private set; } = 0f;
	public float XPRequiredForNextLevel { get; private set; } = BASE_XP_PER_KILL;
	public int AvailableStatPoints { get; private set; } = 0;
	public bool JustLeveledUp { get; private set; } = false;

	// ✨ STAT ALLOCATIONS (how many points spent on each stat)
	public int HealthPoints { get; private set; } = 0;
	public int DamagePoints { get; private set; } = 0;
	public int AttackSpeedPoints { get; private set; } = 0;
	public int StaminaPoints { get; private set; } = 0;
	public int DodgeChancePoints { get; private set; } = 0;
	public int CritChancePoints { get; private set; } = 0;

	// ✨ CALLBACKS for UI updates (C# Actions instead of Godot signals)
	public Action LevelUp { get; set; }
	public Action<int> XPGained { get; set; }
	public Action<string, int> StatAllocated { get; set; }

	public override void _Ready()
	{
		// System is ready to use
	}

	/// <summary>
	/// Add XP when enemy dies (call this from Player3D when killing enemy)
	/// </summary>
	public void AddXP(int xpAmount)
	{
		CurrentXP += xpAmount;
		XPGained?.Invoke(xpAmount);  // ✨ Invoke Action instead of EmitSignal

		// Check for level up
		while (CurrentXP >= XPRequiredForNextLevel && CurrentLevel < MAX_LEVEL)
		{
			LevelUp_();  // Call internal method
		}
	}

	/// <summary>
	/// Level up! Adds stat point and restores HP/Stamina to 100%
	/// </summary>
	private void LevelUp_()  // ✨ Renamed to avoid conflict with Action property
	{
		CurrentLevel++;
		CurrentXP -= XPRequiredForNextLevel;
		AvailableStatPoints++;
		JustLeveledUp = true;

		// Calculate XP for next level (with soft cap after level 30)
		CalculateXPForNextLevel();

		LevelUp?.Invoke();  // ✨ Invoke Action instead of EmitSignal
	}

	/// <summary>
	/// Calculate XP required for next level (with soft cap scaling)
	/// </summary>
	private void CalculateXPForNextLevel()
	{
		float baseXP = BASE_XP_PER_KILL * CurrentLevel;

		// Apply soft cap after level 30
		if (CurrentLevel >= SOFT_CAP_LEVEL)
		{
			int levelsAboveCap = CurrentLevel - SOFT_CAP_LEVEL;
			float capMultiplier = Mathf.Pow(SOFT_CAP_REDUCTION, levelsAboveCap);
			baseXP *= capMultiplier;
		}

		XPRequiredForNextLevel = baseXP;
	}

	/// <summary>
	/// Allocate a stat point to specific stat
	/// </summary>
	public void AllocateStatPoint(string statName)
	{
		if (AvailableStatPoints <= 0)
		{
			return;
		}

		switch (statName.ToLower())
		{
			case "health":
				HealthPoints++;
				break;
			case "damage":
				DamagePoints++;
				break;
			case "attackspeed":
				AttackSpeedPoints++;
				break;
			case "stamina":
				StaminaPoints++;
				break;
			case "dodge":
				DodgeChancePoints++;
				break;
			case "crit":
				CritChancePoints++;
				break;
			default:
				return;
		}

		AvailableStatPoints--;
		JustLeveledUp = false;  // User has spent the point, hide UI

		int newValue = GetStatValue(statName);
		StatAllocated?.Invoke(statName, newValue);  // ✨ Invoke Action instead of EmitSignal
	}

	/// <summary>
	/// Get current value of allocated stat
	/// </summary>
	private int GetStatValue(string statName)
	{
		return statName.ToLower() switch
		{
			"health" => HealthPoints,
			"damage" => DamagePoints,
			"attackspeed" => AttackSpeedPoints,
			"stamina" => StaminaPoints,
			"dodge" => DodgeChancePoints,
			"crit" => CritChancePoints,
			_ => 0
		};
	}

	/// <summary>
	/// Get stat multiplier for damage (used by Player3D)
	/// </summary>
	public float GetDamageMultiplier()
	{
		// Base damage + points spent
		return 1.0f + (DamagePoints * DAMAGE_PER_POINT) / 15f;  // Normalize to reasonable scale
	}

	/// <summary>
	/// Get health bonus
	/// </summary>
	public float GetHealthBonus()
	{
		return HealthPoints * HEALTH_PER_POINT;
	}

	/// <summary>
	/// Get max stamina bonus
	/// </summary>
	public float GetStaminaBonus()
	{
		return StaminaPoints * STAMINA_PER_POINT;
	}

	/// <summary>
	/// Get attack speed multiplier (cooldown reduction)
	/// ✨ EXTREME: Each point reduces cooldown by 60% - capped at 0.05 multiplier (20x faster!)
	/// </summary>
	public float GetAttackSpeedMultiplier()
	{
		// ✨ EXTREME: Calculate with massive multiplier, but cap at 0.05 minimum (20x faster at max!)
		float multiplier = 1.0f - (AttackSpeedPoints * ATTACK_SPEED_PER_POINT);
		return Mathf.Max(multiplier, 0.05f);  // ✨ Never go below 0.05 (20x faster attacks!)
	}

	/// <summary>
	/// Get dodge chance bonus
	/// </summary>
	public float GetDodgeChanceBonus()
	{
		return DodgeChancePoints * DODGE_CHANCE_PER_POINT;
	}

	/// <summary>
	/// Get crit chance bonus
	/// </summary>
	public float GetCritChanceBonus()
	{
		return CritChancePoints * CRIT_CHANCE_PER_POINT;
	}

	/// <summary>
	/// Save level data to JSON
	/// </summary>
	public Dictionary<string, Variant> SaveData()
	{
		return new Dictionary<string, Variant>
		{
			{ "level", CurrentLevel },
			{ "xp", CurrentXP },
			{ "health_points", HealthPoints },
			{ "damage_points", DamagePoints },
			{ "attack_speed_points", AttackSpeedPoints },
			{ "stamina_points", StaminaPoints },
			{ "dodge_chance_points", DodgeChancePoints },
			{ "crit_chance_points", CritChancePoints }
		};
	}

	/// <summary>
	/// Load level data from JSON
	/// </summary>
	public void LoadData(Dictionary<string, Variant> data)
	{
		if (data == null) return;

		CurrentLevel = (int)(data.ContainsKey("level") ? data["level"] : 1);
		CurrentXP = (float)(data.ContainsKey("xp") ? data["xp"] : 0f);
		HealthPoints = (int)(data.ContainsKey("health_points") ? data["health_points"] : 0);
		DamagePoints = (int)(data.ContainsKey("damage_points") ? data["damage_points"] : 0);
		AttackSpeedPoints = (int)(data.ContainsKey("attack_speed_points") ? data["attack_speed_points"] : 0);
		StaminaPoints = (int)(data.ContainsKey("stamina_points") ? data["stamina_points"] : 0);
		DodgeChancePoints = (int)(data.ContainsKey("dodge_chance_points") ? data["dodge_chance_points"] : 0);
		CritChancePoints = (int)(data.ContainsKey("crit_chance_points") ? data["crit_chance_points"] : 0);

		CalculateXPForNextLevel();
	}

	/// <summary>
	/// Debug: Print current stats
	/// </summary>
	public void PrintStats()
	{
		// Stats printing removed
	}
}
