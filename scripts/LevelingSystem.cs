using Godot;
using System;
using System.Collections.Generic;

public partial class LevelingSystem : Node
{
	private const int BASE_XP_PER_KILL = 100;
	private const int SOFT_CAP_LEVEL = 30;
	private const float SOFT_CAP_REDUCTION = 0.90f;
	private const int MAX_LEVEL = 100;

	private const float HEALTH_PER_POINT = 5f;
	private const float DAMAGE_PER_POINT = 2f;
	private const float ATTACK_SPEED_PER_POINT = 0.05f;
	private const float CRIT_CHANCE_PER_POINT = 0.01f;

	public int CurrentLevel { get; private set; } = 1;
	public float CurrentXP { get; private set; } = 0f;
	public float XPRequiredForNextLevel { get; private set; } = BASE_XP_PER_KILL;
	public int AvailableStatPoints { get; private set; } = 0;
	public bool JustLeveledUp { get; private set; } = false;

	public int HealthPoints { get; private set; } = 0;
	public int DamagePoints { get; private set; } = 0;
	public int AttackSpeedPoints { get; private set; } = 0;
	public int CritChancePoints { get; private set; } = 0;

	public Action LevelUp { get; set; }
	public Action<int> XPGained { get; set; }
	public Action<string, int> StatAllocated { get; set; }

	public override void _Ready()
	{
	}

	public void AddXP(int xpAmount)
	{
		CurrentXP += xpAmount;
		XPGained?.Invoke(xpAmount);

		while (CurrentXP >= XPRequiredForNextLevel && CurrentLevel < MAX_LEVEL)
		{
			LevelUp_();
		}
	}

	private void LevelUp_()
	{
		CurrentLevel++;
		CurrentXP -= XPRequiredForNextLevel;
		AvailableStatPoints++;
		JustLeveledUp = true;

		CalculateXPForNextLevel();

		LevelUp?.Invoke();
	}

	private void CalculateXPForNextLevel()
	{
		float baseXP = BASE_XP_PER_KILL * CurrentLevel;

		if (CurrentLevel >= SOFT_CAP_LEVEL)
		{
			int levelsAboveCap = CurrentLevel - SOFT_CAP_LEVEL;
			float capMultiplier = Mathf.Pow(SOFT_CAP_REDUCTION, levelsAboveCap);
			baseXP *= capMultiplier;
		}

		XPRequiredForNextLevel = baseXP;
	}

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
			case "crit":
				CritChancePoints++;
				break;
			default:
				return;
		}

		AvailableStatPoints--;
		JustLeveledUp = false;

		int newValue = GetStatValue(statName);
		StatAllocated?.Invoke(statName, newValue);
	}

	private int GetStatValue(string statName)
	{
		return statName.ToLower() switch
		{
			"health" => HealthPoints,
			"damage" => DamagePoints,
			"attackspeed" => AttackSpeedPoints,
			"crit" => CritChancePoints,
			_ => 0
		};
	}

	public float GetDamageMultiplier()
	{
		return 1.0f + (DamagePoints * DAMAGE_PER_POINT) / 15f;
	}

	public float GetHealthBonus()
	{
		return HealthPoints * HEALTH_PER_POINT;
	}

	public float GetAttackSpeedMultiplier()
	{
		return 1.0f - (AttackSpeedPoints * ATTACK_SPEED_PER_POINT);
	}

	public float GetCritChanceBonus()
	{
		return CritChancePoints * CRIT_CHANCE_PER_POINT;
	}

	public Dictionary<string, Variant> SaveData()
	{
		return new Dictionary<string, Variant>
		{
			{ "level", CurrentLevel },
			{ "xp", CurrentXP },
			{ "health_points", HealthPoints },
			{ "damage_points", DamagePoints },
			{ "attack_speed_points", AttackSpeedPoints },
			{ "crit_chance_points", CritChancePoints }
		};
	}

	public void LoadData(Dictionary<string, Variant> data)
	{
		if (data == null) return;

		CurrentLevel = (int)(data.ContainsKey("level") ? data["level"] : 1);
		CurrentXP = (float)(data.ContainsKey("xp") ? data["xp"] : 0f);
		HealthPoints = (int)(data.ContainsKey("health_points") ? data["health_points"] : 0);
		DamagePoints = (int)(data.ContainsKey("damage_points") ? data["damage_points"] : 0);
		AttackSpeedPoints = (int)(data.ContainsKey("attack_speed_points") ? data["attack_speed_points"] : 0);
		CritChancePoints = (int)(data.ContainsKey("crit_chance_points") ? data["crit_chance_points"] : 0);

		CalculateXPForNextLevel();
	}
}
