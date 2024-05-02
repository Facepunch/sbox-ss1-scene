﻿using Sandbox;

[Status( 7, 0, 1f )]
public class MovespeedStatus : Status
{
	public MovespeedStatus()
	{
		Title = "Fast Shoes";
		IconPath = "textures/icons/shoe.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );

		Player.Modify( this, PlayerStat.MoveSpeed, GetMultForLevel( Level ), ModifierType.Mult );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "Increase movespeed by {0}%", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "Increase movespeed by {0}%→{1}%", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public float GetMultForLevel( int level )
	{
		return 1f + 0.2f * level;
	}

	public float GetPercentForLevel( int level )
	{
		return 20 * level;
	}
}
