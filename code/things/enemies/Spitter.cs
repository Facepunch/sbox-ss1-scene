﻿using Sandbox;

public class Spitter : Enemy
{
	private TimeSince _damageTime;
	private const float DAMAGE_TIME = 0.75f;

	private float _shootDelayTimer;
	private const float SHOOT_DELAY_MIN = 2f;
	private const float SHOOT_DELAY_MAX = 3f;

	public bool IsShooting { get; private set; }
	private bool _hasShot;

	private TimeSince _prepareShootTime;

	protected override void OnAwake()
	{
		//OffsetY = -0.45f;
		ShadowScale = 1.1f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		Scale = 1f;

		base.OnAwake();

		//Sprite.Texture = Texture.Load("textures/sprites/spitter.vtex");

		//Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 8f;

		Radius = 0.25f;

		Health = 50f;
		MaxHealth = Health;
		DamageToPlayer = 10f;

		CoinValueMin = 1;
		CoinValueMax = 2;

		Sprite.PlayAnimation( AnimSpawnPath );

		if ( IsProxy )
			return;
		
		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		_damageTime = DAMAGE_TIME;
		_shootDelayTimer = Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX );
	}

	protected override void UpdatePosition( float dt )
	{
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"Sprite.PlaybackSpeed: {Sprite.PlaybackSpeed}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.7f, 0f ) ) );

		base.UpdatePosition( dt );

		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		if ( IsShooting )
		{
			if ( !_hasShot && _prepareShootTime > 1.0f )
				Shoot();

			if ( _prepareShootTime > 1.6f )
				FinishShooting();

			return;
		}
		else
		{
			Velocity += (closestPlayer.Position2D - Position2D).Normal * 1.0f * dt * (IsFeared ? -1f : 1f);
		}

		float speed = 0.9f * (IsAttacking ? 1.3f : 0.7f) + Utils.FastSin( MoveTimeOffset + Time.Now * (IsAttacking ? 15f : 7.5f) ) * (IsAttacking ? 0.66f : 0.35f);
		Transform.Position += (Vector3)Velocity * speed * dt;

		var player_dist_sqr = (closestPlayer.Position2D - Position2D).LengthSquared;
		if ( !IsShooting && !IsAttacking && player_dist_sqr < 5f * 5f )
		{
			_shootDelayTimer -= dt;
			if ( _shootDelayTimer < 0f )
			{
				PrepareToShoot();
			}
		}
	}

	public void PrepareToShoot()
	{
		_prepareShootTime = 0f;
		IsShooting = true;
		_hasShot = false;
		DontChangeSpritePlaybackSpeed = true;
		Sprite.PlaybackSpeed = 1f;
		Sprite.PlayAnimation( "shoot" );
		Manager.Instance.PlaySfxNearby( "spitter.prepare", Position2D, pitch: Game.Random.Float( 1f, 1.1f ), volume: 0.6f, maxDist: 2.75f );
		CanAttack = false;
		CanAttackAnim = false;
	}

	public void Shoot()
	{
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		var target_pos = closestPlayer.Position2D + closestPlayer.Velocity * Game.Random.Float( 0.5f, 1.5f );
		var dir = Utils.RotateVector( (target_pos - Position2D).Normal, Game.Random.Float( -10f, 10f ) );
		//Manager.Instance.SpawnEnemyBullet( Position2D + new Vector2( 0f, 0.45f ) + dir * 0.05f, dir, speed: 2f );
		Manager.Instance.SpawnEnemyBullet( Position2D + dir * 0.05f, dir, speed: 2f );

		Velocity *= 0.25f;
		_hasShot = true;

		Manager.Instance.PlaySfxNearby( "spitter.shoot", Position2D, pitch: Game.Random.Float( 0.8f, 0.9f ), volume: 0.9f, maxDist: 5f );
		Sprite.PlayAnimation( "shoot_reverse" );
	}

	public void FinishShooting()
	{
		Sprite.PlayAnimation( AnimIdlePath );
		CanAttack = true;
		CanAttackAnim = true;
		_shootDelayTimer = Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX );
		IsShooting = false;
		DontChangeSpritePlaybackSpeed = false;
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			var spawnFactor = Utils.Map( enemy.ElapsedTime, 0f, enemy.SpawnTime, 0f, 1f, EasingType.QuadIn );
			Velocity += (Position2D - enemy.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * enemy.PushStrength * (1f + enemy.TempWeight) * spawnFactor * dt;
		}
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
						Manager.Instance.PlaySfxNearby( "zombie.attack.player", Position2D, pitch: Utils.Map( player.Health, player.Stats[PlayerStat.MaxHp], 0f, 0.95f, 1.15f, EasingType.QuadIn ), volume: 1f, maxDist: 5.5f );

						player.Damage( dmg );

						if ( dmg > 0f )
							OnDamagePlayer( player, dmg );
					}

					_damageTime = 0f;
				}
			}
		}
	}
}
