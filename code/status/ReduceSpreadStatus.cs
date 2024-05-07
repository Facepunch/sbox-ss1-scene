﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status(3, 0, 1f, typeof(NumProjectileStatus))]
public class ReduceSpreadStatus : Status
{
	public ReduceSpreadStatus()
    {
		Title = "Tighter Grouping";
		IconPath = "textures/icons/reduce_spread.png";
	}

	public override void Init(Player player)
	{
		base.Init(player);
	}

	public override void Refresh()
    {
		Description = GetDescription(Level);

		Player.Modify(this, PlayerStat.BulletSpread, GetMultForLevel(Level), ModifierType.Mult);
	}

	public override string GetDescription(int newLevel)
	{
		return string.Format("Reduce bullet spread by {0}%", GetPercentForLevel(Level));
	}

	public override string GetUpgradeDescription(int newLevel)
    {
		return newLevel > 1 ? string.Format("Reduce bullet spread by {0}%→{1}%", GetPercentForLevel(newLevel - 1), GetPercentForLevel(newLevel)) : GetDescription(newLevel);
	}

	public float GetMultForLevel(int level)
    {
		return 1f - level * 0.3f;
    }

	public float GetPercentForLevel(int level)
	{
		return 30 * level;
	}
}
