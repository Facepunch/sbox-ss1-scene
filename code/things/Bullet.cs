using System;
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
	public Vector2 Velocity { get; set; }

	public TimeSince TimeSinceSpawn { get; private set; }
	public Player Shooter { get; set; }
	public int NumHits { get; private set; }

	public Dictionary<BulletStat, float> Stats { get; private set; }

	public List<Thing> _hitThings = new List<Thing>();
	//private float _scaleFactor;

	protected override void OnAwake()
	{
		base.OnAwake();

		OffsetY = -0.45f;

		Radius = 0.1f;

		ShadowOpacity = 0.8f;
		ShadowScale = 0.3f;
		SpawnShadow( ShadowScale, ShadowOpacity );

		if ( IsProxy )
			return;

		//Scale = new Vector2( 0.1f, 0.1f );
		TimeSinceSpawn = 0f;
		NumHits = 0;

		Stats = new Dictionary<BulletStat, float>();

		Stats[BulletStat.Damage] = 1f;
		Stats[BulletStat.AddTempWeight] = 2f;
		Stats[BulletStat.Force] = 0.75f;
		Stats[BulletStat.Lifetime] = 1f;
		Stats[BulletStat.NumPiercing] = 0;
		Stats[BulletStat.CriticalChance] = 0;
		Stats[BulletStat.CriticalMultiplier] = 1f;
		Stats[BulletStat.HealTeammateAmount] = 0f;

		CollideWith.Add( typeof( Enemy ) );
	}

	public void Init()
	{
		//_scaleFactor = Utils.Map( Stats[BulletStat.Damage], 10f, 120f, 0.015f, 0.003f, EasingType.Linear );
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

		Scale = scale;
		//Sprite.Size = new Vector2( scale );
		
		Radius = 0.07f + scale * 0.2f;
		ShadowScale = scale * 1.3f;

		SpriteDirty = true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Log.Info( $"Stats: {Stats}" );

		if (!IsProxy)
		{
			//Gizmo.Draw.Color = Color.White;
			//Gizmo.Draw.Text( $"TimeSinceSpawn: {TimeSinceSpawn}\nStats[BulletStat.Damage]: {Stats[BulletStat.Damage]}\nStats[BulletStat.Lifetime]: {Stats[BulletStat.Lifetime]}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.2f, 0f ) ) );
		}

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		if ( Manager.Instance.IsGameOver )
			return;

		if ( IsProxy )
			return;

		if ( Shooter == null || Shooter.IsDead )
		{
			Remove();
			return;
		}

		float dt = Time.Delta;

		Position2D += Velocity * dt;
		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Position2D.y ) );

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

		if ( TimeSinceSpawn > Stats[BulletStat.Lifetime] )
		{
			Remove();
			return;
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

		if ( other is Enemy enemy )
		{
			if ( !enemy.IsDying && (!enemy.IsSpawning || enemy.ElapsedTime > 1.5f) )
			{
				if ( _hitThings.Contains( enemy ) )
					return;

				Manager.Instance.PlaySfxNearby( "enemy.hit", Position2D, pitch: Utils.Map( enemy.Health, enemy.MaxHealth, 0f, 0.9f, 1.3f, EasingType.SineIn ), volume: 1f, maxDist: 4f );

				if ( Game.Random.Float( 0f, 1f ) < Stats[BulletStat.FireIgniteChance] )
				{
					//if ( !enemy.HasEnemyStatus<BurningEnemyStatus>() )
					//	Game.PlaySfxNearby( "burn", Position, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 1f, maxDist: 5f );

					enemy.Burn( Shooter, Shooter.Stats[PlayerStat.FireDamage] * Shooter.GetDamageMultiplier(), Shooter.Stats[PlayerStat.FireLifetime], Shooter.Stats[PlayerStat.FireSpreadChance] );
				}

				if ( Game.Random.Float( 0f, 1f ) < Stats[BulletStat.FreezeChance] )
				{
					//if ( !enemy.HasEnemyStatus<FrozenEnemyStatus>() )
					//	Game.PlaySfxNearby( "frozen", Position, pitch: Game.Random.Float( 1.2f, 1.3f ), volume: 1.6f, maxDist: 6f );

					enemy.Freeze( Shooter );
				}

				bool isCrit = Game.Random.Float( 0f, 1f ) < Stats[BulletStat.CriticalChance];
				float damage = Stats[BulletStat.Damage] * (isCrit ? Stats[BulletStat.CriticalMultiplier] : 1f);
				var addVel = Velocity.Normal * Stats[BulletStat.Force] * (8f / enemy.PushStrength);
				enemy.Damage( damage, Shooter.GameObject.Id, addVel, Stats[BulletStat.AddTempWeight], isCrit );

				NumHits++;

				if ( NumHits > (int)Stats[BulletStat.NumPiercing] )
				{
					Remove();
					return;
				}
				else
				{
					_hitThings.Add( enemy );
				}
			}
		}
		else if ( other is Player player && player != Shooter && !player.IsDead )
		{
			if ( _hitThings.Contains( player ) )
				return;

			player.Heal( Stats[BulletStat.HealTeammateAmount], 0.05f );

			NumHits++;

			if ( NumHits > (int)Stats[BulletStat.NumPiercing] )
			{
				Remove();
				return;
			}
			else
			{
				_hitThings.Add( player );
			}
		}
	}
}
