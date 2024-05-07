﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 7, 2, 1f )]
public class CritMultiplierStatus : Status
{
	public CritMultiplierStatus()
	{
		Title = "Deadly Criticals";
		IconPath = "textures/icons/crit_multiplier.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );

		Player.Modify( this, PlayerStat.CritMultiplier, GetAddForLevel( Level ), ModifierType.Add );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "Increase critical multiplier by {0}%", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "Increase critical multiplier by {0}%→{1}%", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public float GetAddForLevel( int level )
	{
		return 0.30f * level;
	}

	public float GetPercentForLevel( int level )
	{
		return 30f * level;
	}
}
