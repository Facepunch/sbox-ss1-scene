﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public enum BulletStat
{
	Damage, Force, AddTempWeight, Lifetime, NumPiercing, CriticalChance, CriticalMultiplier, FireIgniteChance, FreezeChance, BulletSpread, BulletInaccuracy, BulletSpeed, BulletLifetime,
	GrowDamageAmount, ShrinkDamageAmount, DistanceDamageAmount, HealTeammateAmount,
}

public class Bullet : Thing
{
	[Property] public SpriteRenderer Sprite { get; set; }

	public Vector2 Velocity { get; set; }

	public TimeSince SpawnTime { get; private set; }
	public Player Shooter { get; set; }
	public int NumHits { get; private set; }

	public IDictionary<BulletStat, float> Stats { get; private set; }

	public List<Thing> _hitThings = new List<Thing>();
	private float _scaleFactor;

	protected override void OnStart()
	{
		base.OnStart();

		//SpawnShadow( Radius * 3f );

		if ( IsProxy )
			return;

		//Scale = new Vector2( 0.1f, 0.1f );
		SpawnTime = 0f;
		NumHits = 0;
		Radius = 0.1f;

		Stats = new Dictionary<BulletStat, float>();

		Stats[BulletStat.Damage] = 1f;
		Stats[BulletStat.AddTempWeight] = 2f;
		Stats[BulletStat.Force] = 0.75f;
		Stats[BulletStat.Lifetime] = 1f;
		Stats[BulletStat.NumPiercing] = 0;
		Stats[BulletStat.CriticalChance] = 0;
		Stats[BulletStat.CriticalMultiplier] = 1f;
		Stats[BulletStat.HealTeammateAmount] = 0f;

		//ShadowOpacity = 0.8f;
		//ShadowScale = 0.3f;

		//CollideWith.Add( typeof( Enemy ) );
	}

	public void Init()
	{
		_scaleFactor = Utils.Map( Stats[BulletStat.Damage], 10f, 120f, 0.015f, 0.003f, EasingType.Linear );
		DetermineSize();

		if ( Stats[BulletStat.HealTeammateAmount] > 0f )
		{
			CollideWith.Add( typeof( Player ) );
			Sprite.Color = Color.Green;
		}
	}

	void DetermineSize()
	{
		var damage = Stats[BulletStat.Damage];
		//float scale = 0.125f + damage * _scaleFactor;
		float scale = damage < 30f
			? Utils.Map( damage, 0f, 30f, 0.1f, 0.5f, EasingType.QuadOut )
			: Utils.Map( damage, 30f, 150f, 0.5f, 1.75f, EasingType.QuadIn );

		//Scale = new Vector2( scale, scale );
		Radius = 0.07f + scale * 0.2f;
		//ShadowScale = scale * 1.2f;
	}

	public override void Update( float dt )
	{
		if ( Shooter == null || Shooter.IsDead )
		{
			Remove();
			return;
		}

		if ( Manager.Instance.IsGameOver )
			return;

		base.Update( dt );

		if ( IsProxy )
			return;

		Position2D += Velocity * dt;
		//Depth = -Position.y * 10f;

		bool changedDamage = false;

		if ( Stats[BulletStat.GrowDamageAmount] > 0f )
		{
			Stats[BulletStat.Damage] += Stats[BulletStat.GrowDamageAmount] * dt;
			changedDamage = true;
		}

		if ( Stats[BulletStat.ShrinkDamageAmount] > 0f )
		{
			Stats[BulletStat.Damage] -= Stats[BulletStat.ShrinkDamageAmount] * dt;
			changedDamage = true;

			if ( Stats[BulletStat.Damage] <= 0f )
			{
				Remove();
				return;
			}
		}

		if ( Stats[BulletStat.DistanceDamageAmount] > 0f )
		{
			Stats[BulletStat.Damage] += Stats[BulletStat.DistanceDamageAmount] * Velocity.Length * dt;
			changedDamage = true;
		}

		if ( changedDamage )
			DetermineSize();

		if ( SpawnTime > Stats[BulletStat.Lifetime] )
		{
			Remove();
			return;
		}

		var gridPos = Manager.Instance.GetGridSquareForPos( Position2D );
		if ( gridPos != GridPos )
		{
			Manager.Instance.DeregisterThingGridSquare( this, GridPos );
			Manager.Instance.RegisterThingGridSquare( this, gridPos );
			GridPos = gridPos;
		}

		for ( int dx = -1; dx <= 1; dx++ )
		{
			for ( int dy = -1; dy <= 1; dy++ )
			{
				Manager.Instance.HandleThingCollisionForGridSquare( this, new GridSquare( GridPos.x + dx, GridPos.y + dy ), dt );

				if ( IsRemoved )
					return;
			}
		}
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( Shooter == null )
			return;

		//if ( typeof( Enemy ).IsAssignableFrom( other.GetType() ) )
		//{
		//	var enemy = (Enemy)other;

		//	if ( !enemy.IsDying && (!enemy.IsSpawning || enemy.ElapsedTime > 1.5f) )
		//	{
		//		if ( _hitThings.Contains( enemy ) )
		//			return;

		//		Game.PlaySfxNearby( "enemy.hit", Position, pitch: Utils.Map( enemy.Health, enemy.MaxHealth, 0f, 0.9f, 1.3f, EasingType.SineIn ), volume: 1f, maxDist: 4f );

		//		if ( Sandbox.Game.Random.Float( 0f, 1f ) < Stats[BulletStat.FireIgniteChance] )
		//		{
		//			if ( !enemy.HasEnemyStatus<BurningEnemyStatus>() )
		//				Game.PlaySfxNearby( "burn", Position, pitch: Sandbox.Game.Random.Float( 0.95f, 1.05f ), volume: 1f, maxDist: 5f );

		//			enemy.Burn( Shooter, Shooter.Stats[PlayerStat.FireDamage] * Shooter.GetDamageMultiplier(), Shooter.Stats[PlayerStat.FireLifetime], Shooter.Stats[PlayerStat.FireSpreadChance] );
		//		}

		//		if ( Sandbox.Game.Random.Float( 0f, 1f ) < Stats[BulletStat.FreezeChance] )
		//		{
		//			if ( !enemy.HasEnemyStatus<FrozenEnemyStatus>() )
		//				Game.PlaySfxNearby( "frozen", Position, pitch: Sandbox.Game.Random.Float( 1.2f, 1.3f ), volume: 1.6f, maxDist: 6f );

		//			enemy.Freeze( Shooter );
		//		}

		//		bool isCrit = Sandbox.Game.Random.Float( 0f, 1f ) < Stats[BulletStat.CriticalChance];
		//		float damage = Stats[BulletStat.Damage] * (isCrit ? Stats[BulletStat.CriticalMultiplier] : 1f);
		//		enemy.Damage( damage, Shooter, isCrit );

		//		enemy.Velocity += Velocity.Normal * Stats[BulletStat.Force] * (8f / enemy.PushStrength);
		//		enemy.TempWeight += Stats[BulletStat.AddTempWeight];

		//		NumHits++;

		//		if ( NumHits > (int)Stats[BulletStat.NumPiercing] )
		//		{
		//			Remove();
		//			return;
		//		}
		//		else
		//		{
		//			_hitThings.Add( enemy );
		//		}
		//	}
		//}
		//else if ( other is PlayerCitizen player && player != Shooter && !player.IsDead )
		//{
		//	if ( _hitThings.Contains( player ) )
		//		return;

		//	player.Heal( Stats[BulletStat.HealTeammateAmount], 0.05f );

		//	NumHits++;

		//	if ( NumHits > (int)Stats[BulletStat.NumPiercing] )
		//	{
		//		Remove();
		//		return;
		//	}
		//	else
		//	{
		//		_hitThings.Add( player );
		//	}
		//}
	}
}