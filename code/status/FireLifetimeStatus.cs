﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 4, 0, 1f, typeof( FireIgniteStatus ), typeof( DashFireStatus ) )]
public class FireLifetimeStatus : Status
{
	public FireLifetimeStatus()
	{
		Title = "Longer Fires";
		IconPath = "textures/icons/fire_lifetime.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );

		Player.Modify( this, PlayerStat.FireLifetime, GetMultForLevel( Level ), ModifierType.Mult );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "Your fires last {0}% longer", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "Your fires last {0}%→{1}% longer", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public float GetMultForLevel( int level )
	{
		return 1 + 0.35f * level;
	}

	public float GetPercentForLevel( int level )
	{
		return 35 * level;
	}
}
