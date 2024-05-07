﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 7, 0, 1f, typeof( FreezeShootStatus ), typeof( FreezeArmorStatus ) )]
public class FreezeLifetimeStatus : Status
{
	public FreezeLifetimeStatus()
	{
		Title = "Long Winter";
		IconPath = "textures/icons/freeze_lifetime.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );

		Player.Modify( this, PlayerStat.FreezeLifetime, GetMultForLevel( Level ), ModifierType.Mult );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "You freeze enemies for {0}% longer", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "You freeze enemies for {0}%→{1}% longer", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public float GetMultForLevel( int level )
	{
		return 1f + 0.35f * level;
	}

	public float GetPercentForLevel( int level )
	{
		return 35 * level;
	}
}
