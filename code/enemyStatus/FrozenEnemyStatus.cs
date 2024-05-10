using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using static Sandbox.Connection;

public class FrozenEnemyStatus : EnemyStatus
{
	public float Lifetime { get; set; }
	public float TimeScale { get; set; }

	public Player Player { get; set; }

	public override void Init( Enemy enemy )
	{
		base.Init( enemy );

		enemy.CreateFrozenVfx();
		enemy.IsFrozen = true;
		TimeScale = float.MaxValue;
	}

	public void SetLifetime( float lifetime )
	{
		if ( lifetime > Lifetime )
		{
			Lifetime = lifetime;
		}
	}

	public void SetTimeScale( float timeScale )
	{
		timeScale = MathF.Max( timeScale, 0.01f );

		if ( timeScale < TimeScale )
		{
			//Enemy.AnimSpeedModifier = timeScale;
			Enemy.TimeScale = timeScale;
		}
	}

	public override void Update( float dt )
	{
		if ( Enemy == null || !Enemy.IsValid )
			return;

		if ( ElapsedTime > Lifetime )
			Enemy.RemoveEnemyStatus( this );

		Enemy.Velocity *= (1f - Utils.Map( TimeScale, 0.6f, 0f, 3f, 15f ) * dt);
	}

	public override void StartDying()
	{
		//Enemy.AnimSpeedModifier = 1f;
		Enemy.TimeScale = 1f;
		Enemy.IsFrozen = false;

		if ( Player != null && Player.Stats[PlayerStat.FrozenShardsNum] > 0f )
		{
			int maxShardsNum = (int)Player.Stats[PlayerStat.FrozenShardsNum];
			int numShards = Game.Random.Int( 1, maxShardsNum );
			Vector2 aimDir = (new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) )).Normal;
			Player.SpawnBulletRing( Enemy.Position2D, numShards, aimDir );
		}
	}

	public override void Remove()
	{
		//Enemy.AnimSpeedModifier = 1f;
		Enemy.TimeScale = 1f;
		Enemy.IsFrozen = false;

		Enemy.RemoveFrozenVfx();
	}

	public override void Refresh()
	{
		ElapsedTime = 0f;
	}
}
