﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 7, 0, 111f, typeof( DashFearStatus ), typeof( GrenadeFearStatus ) )]
public class FearDamageStatus : Status
{
	public FearDamageStatus()
	{
		Title = "Fear Damage";
		IconPath = "textures/icons/fear_damage.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );

		Player.Modify( this, PlayerStat.FearDamageMultiplier, GetMultForLevel( Level ), ModifierType.Mult );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "Deal {0}% more damage to scared enemies", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "Deal {0}%→{1}% more damage to scared enemies", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public float GetMultForLevel( int level )
	{
		return 1f + (0.30f * level);
	}

	public float GetPercentForLevel( int level )
	{
		return 30 * level;
	}
}
