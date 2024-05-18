using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status( 3, 0, 1f )]
public class DashFireStatus : Status
{
	public DashFireStatus()
	{
		Title = "Cheeky Fire";
		IconPath = "textures/icons/dash_fire.png";
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
		return string.Format( "{0}% chance to start a fire when you dash", GetPercentForLevel( Level ) );
	}

	public override string GetUpgradeDescription( int newLevel )
	{
		return newLevel > 1 ? string.Format( "{0}%→{1}% chance to start a fire when you dash", GetPercentForLevel( newLevel - 1 ), GetPercentForLevel( newLevel ) ) : GetDescription( newLevel );
	}

	public override void OnDashStarted()
	{
		if ( Game.Random.Float( 0f, 1f ) < GetChanceForLevel( Level ) )
		{
			Manager.Instance.SpawnFire( Player.Position2D + new Vector2(0f, 0.3f), Player.GameObject.Id );

			Manager.Instance.PlaySfxNearby( "ignite", Player.Position2D, pitch: Game.Random.Float( 1.05f, 1.25f ), volume: 0.5f, maxDist: 4f );
		}
	}

	public float GetChanceForLevel( int level )
	{
		return level == 1 ? 0.5f : (level == 2 ? 0.75f : 1f);
	}
	public float GetPercentForLevel( int level )
	{
		return level == 1 ? 50 : (level == 2 ? 75 : 100);
	}
}
