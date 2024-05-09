﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using static Sandbox.Connection;

public class FearEnemyStatus : EnemyStatus
{
	public float Lifetime { get; set; }
	public float TimeScale { get; set; }

	public Player Player { get; set; }

	public TimeSince TimeSincePain { get; set; }
	public float PainPercent { get; set; }

	public override void Init( Enemy enemy )
	{
		base.Init( enemy );

		enemy.CreateFearVfx();
		enemy.IsFeared = true;
		TimeScale = float.MaxValue;
		TimeSincePain = 0f;
	}

	public void SetLifetime( float lifetime )
	{
		if ( lifetime > Lifetime )
		{
			Lifetime = lifetime;
		}
	}

	public override void Update( float dt )
	{
		if ( Enemy == null || !Enemy.IsValid )
			return;

		if ( ElapsedTime > Lifetime )
			Enemy.RemoveEnemyStatus( this );

		if ( TimeSincePain > 1f )
		{
			TimeSincePain = 0f;

			if ( PainPercent > 0f )
			{
				float damage = PainPercent * Enemy.Health;
				Enemy.Damage( damage, Guid.Empty, addVel: Vector2.Zero, addTempWeight: 0f );
			}
		}
	}

	public override void StartDying()
	{
		Enemy.IsFeared = false;

		if ( Player != null && Sandbox.Game.Random.Float( 0f, 1f ) < Player.Stats[PlayerStat.FearDropGrenadeChance] )
		{
			//Player.SpawnGrenade(
			//	pos: Enemy.Position + new Vector2( Enemy.Radius * Sandbox.Game.Random.Float( -1f, 1f ), Enemy.Radius * Sandbox.Game.Random.Float( -1f, 1f ) ),
			//	velocity: new Vector2( 0.5f * Sandbox.Game.Random.Float( -1f, 1f ), 0.5f * Sandbox.Game.Random.Float( -1f, 1f ) )
			//);
		}
	}

	public override void Remove()
	{
		Enemy.IsFeared = false;

		Enemy.RemoveFearVfx();
	}

	public override void Refresh()
	{
		ElapsedTime = 0f;
	}
}
