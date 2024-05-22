﻿using Sandbox;

public class ZombieElite : Enemy
{
	private TimeSince _damageTime;
	private const float DAMAGE_TIME = 0.5f;

	public bool HasTarget { get; private set; }
	private Vector2 _wanderPos;

	protected override void OnAwake()
	{
		OffsetY = -0.45f;
		ShadowScale = 1f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		base.OnAwake();

		//AnimSpeed = 2f;

		//ScaleFactor = 0.85f;
		Scale = 0.95f;
		//Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 12f;

		Radius = 0.27f;

		Health = 55f;
		MaxHealth = Health;
		DamageToPlayer = 10f;

		if ( IsProxy )
			return;
		
		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		//ShadowScale = 0.95f;
		_damageTime = DAMAGE_TIME;

		HasTarget = false;

		_wanderPos = new Vector2( Game.Random.Float( Manager.Instance.BOUNDS_MIN.x + 10f, Manager.Instance.BOUNDS_MAX.x - 10f ), Game.Random.Float( Manager.Instance.BOUNDS_MIN.y + 10f, Manager.Instance.BOUNDS_MAX.y - 20f ) );

		//AnimationPath = AnimSpawnPath;
	}

	protected override void UpdatePosition( float dt )
	{
		base.UpdatePosition( dt );

		// todo: optimize
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		if ( HasTarget )
		{
			Velocity += (closestPlayer.Position2D - Position2D).Normal * dt * (IsFeared ? -1f : 1f);
		}
		else
		{
			var wander_dist_sqr = (_wanderPos - Position2D).LengthSquared;
			if ( wander_dist_sqr < 0.25f )
			{
				_wanderPos = new Vector2( MathX.Clamp( closestPlayer.Position2D.x + Game.Random.Float( -30f, 30f ), Manager.Instance.BOUNDS_MIN.x + 10f, Manager.Instance.BOUNDS_MAX.x - 1f ), MathX.Clamp( closestPlayer.Position2D.y + Game.Random.Float( -30f, 30f ), Manager.Instance.BOUNDS_MIN.y + 10f, Manager.Instance.BOUNDS_MAX.y - 10f ) );
			}

			Velocity += (_wanderPos - Position2D).Normal * dt;

			var player_dist_sqr = (closestPlayer.Position2D - Position2D).LengthSquared;
			if ( player_dist_sqr < 3.5f * 3.5f )
			{
				HasTarget = true;
				//Game.PlaySfxNearby( "zombie.spawn0", Position, pitch: Game.Random.Float( 0.9f, 1.1f ), volume: 1f, maxDist: 4f );
			}
		}

		//if(!HasTarget)
		//    DebugOverlay.Line(Position, _wanderPos, 0f, false);

		float speed = (IsAttacking ? 1.9f : 1f) + Utils.FastSin( MoveTimeOffset + Time.Now * (IsAttacking ? 16f : 9.5f) ) * (IsAttacking ? 1.9f : 1f);
		Transform.Position += (Vector3)Velocity * speed * dt;
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			var spawnFactor = Utils.Map( enemy.ElapsedTime, 0f, enemy.SpawnTime, 0f, 1f, EasingType.QuadIn );
			Velocity += (Position2D - enemy.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * enemy.PushStrength * (1f + enemy.TempWeight) * spawnFactor * dt;
		}
		// todo: move collision check to player instead to prevent laggy hits?
		else if ( other is Player player )
		{
			if ( !player.IsDead )
			{
				Velocity += (Position2D - player.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * player.Stats[PlayerStat.PushStrength] * (1f + player.TempWeight) * dt;

				if ( IsAttacking && _damageTime > (DAMAGE_TIME / TimeScale) )
				{
					float dmg = player.CheckDamageAmount( DamageToPlayer, DamageType.Melee );

					if ( !player.IsInvulnerable )
					{
						//Game.PlaySfxNearby( "zombie.attack.player", Position, pitch: Utils.Map( player.Health, player.Stats[PlayerStat.MaxHp], 0f, 0.95f, 1.15f, EasingType.QuadIn ), volume: 1f, maxDist: 5.5f );

						player.Damage( dmg );

						if( dmg > 0f )
							OnDamagePlayer( player, dmg );
					}

					_damageTime = 0f;
				}
			}
		}
	}
}
