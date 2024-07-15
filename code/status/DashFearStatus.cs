using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 1, 0, 111f )]
public class DashFearStatus : Status
{
	public DashFearStatus()
	{
		Title = "Terror Dash";
		IconPath = "textures/icons/dash_fear.png";
	}

	public override void Init( Player player )
	{
		base.Init( player );
	}

	public override void Refresh()
	{
		Description = GetDescription( Level );
	}

	public override string GetDescription( int newLevel )
	{
		return string.Format( "Touching enemies while dashing will scare them" );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return string.Format( "Touching enemies while dashing will scare them" );
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( Player.IsDashing && other is Enemy enemy )
		{
			if ( !enemy.HasEnemyStatus<FearEnemyStatus>() )
			{
				var pos = enemy.Position2D + (!enemy.Position2D.Equals( Player.Position2D ) ? (enemy.Position2D - Player.Position2D).Normal * 2f : Vector2.Zero);
				Manager.Instance.PlaySfxNearby( "fear", pos, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 0.6f, maxDist: 5f );
			}

			enemy.Fear( Player );
		}
	}
}
