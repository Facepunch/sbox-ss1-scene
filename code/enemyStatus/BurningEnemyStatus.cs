using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using static Sandbox.Connection;

public class BurningEnemyStatus : EnemyStatus
{
	private TimeSince _sinceDamageTime;
	private const float DAMAGE_INTERVAL = 0.4f;

	public float Lifetime { get; set; }
	public float SpreadChance { get; set; }

	public Player Player { get; set; }

	public float Damage { get; set; }

	private TimeSince _damageOtherTime;

	public BurningEnemyStatus()
	{

	}

	public override void Init( Enemy enemy )
	{
		base.Init( enemy );

		Enemy.CreateBurningVfx();
	}

	public override void Update( float dt )
	{
		if ( Enemy == null || !Enemy.IsValid )
			return;

		if ( ElapsedTime > Lifetime )
			Enemy.RemoveEnemyStatus( this );

		if ( _sinceDamageTime > DAMAGE_INTERVAL )
		{
			Enemy.DamageFire( Damage, Player );
			_sinceDamageTime = 0f;
		}
	}

	public override void Remove()
	{
		Enemy.RemoveBurningVfx();
	}

	public override void Refresh()
	{
		ElapsedTime = 0f;
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		bool didDamage = false;

		if ( other is Enemy enemy && !enemy.IsDying && !enemy.HasEnemyStatus( this ) )
		{
			if ( _damageOtherTime > DAMAGE_INTERVAL )
			{
				enemy.DamageFire( Damage, Player );

				if ( !enemy.HasEnemyStatus<BurningEnemyStatus>() && Game.Random.Float( 0f, 1f ) < SpreadChance )
				{
					enemy.Burn( Player, Damage, Lifetime, SpreadChance );
					//Manager.Instance.PlaySfxNearby.PlaySfxNearby( "burn", enemy.Position, pitch: Game.Random.Float( 1.15f, 1.35f ), volume: 0.7f, maxDist: 4f );
				}

				didDamage = true;
			}
		}
		else if ( other is Player player && !player.IsDead )
		{
			if ( _damageOtherTime > DAMAGE_INTERVAL )
			{
				//player.Damage( Damage, DamageType.Fire );
				didDamage = true;
			}
		}

		if ( didDamage )
			_damageOtherTime = 0f;
	}
}
