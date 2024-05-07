﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 7, 0, 1f )]
public class BulletLifetimeStatus : Status
{
	public BulletLifetimeStatus()
	{
		Title = "Long Bullets";
		IconPath = "textures/icons/bullet_lifetime.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );

		Player.Modify( this, PlayerStat.BulletLifetime, GetMultForLevel( Level ), ModifierType.Mult );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "Increase bullet lifetime by {0}%", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "Increase bullet lifetime by {0}%→{1}%", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public float GetMultForLevel( int level )
	{
		return 1f + 0.25f * level;
	}

	public float GetPercentForLevel( int level )
	{
		return 25 * level;
	}
}
