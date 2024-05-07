﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 3, 0, 1f )]
public class CrateChanceStatus : Status
{
	public CrateChanceStatus()
	{
		Title = "More Crates";
		IconPath = "textures/icons/more_crates.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );

		Player.Modify( this, PlayerStat.CrateChanceAdditional, GetAddForLevel( Level ), ModifierType.Add );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "{0}% greater chance for a crate to spawn instead of an enemy", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "{0}%→{1}% greater chance for a crate to spawn instead of an enemy", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public float GetAddForLevel( int level )
	{
		return level * 0.2f;
	}

	public float GetPercentForLevel( int level )
	{
		return level * 20;
	}
}
