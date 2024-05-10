﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status(7, 0, 1f)]
public class XpAttractStrengthStatus : Status
{
	public XpAttractStrengthStatus()
    {
		Title = "XP Magnet";
		IconPath = "textures/icons/xp_strength.png";
	}

	public override void Init(Player player)
	{
		base.Init(player);
	}

	public override void Refresh()
    {
		Description = GetDescription(Level);

		Player.Modify(this, PlayerStat.CoinAttractStrength, GetMultForLevel(Level), ModifierType.Mult);
	}

	public override string GetDescription(int newLevel)
	{
		return string.Format("Increase XP attract strength by {0}%", GetPercentForLevel(Level));
	}

	public override string GetUpgradeDescription(int newLevel)
    {
		return newLevel > 1 ? string.Format("Increase XP attract strength by {0}%→{1}%", GetPercentForLevel(newLevel - 1), GetPercentForLevel(newLevel)) : GetDescription(newLevel);
	}

	public float GetMultForLevel(int level)
    {
		return 1f + 0.45f * level;
    }

	public float GetPercentForLevel(int level)
	{
		return 45 * level;
	}
}