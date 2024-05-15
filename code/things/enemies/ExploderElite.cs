﻿using Sandbox;
using static Manager;

public class ExploderElite : Enemy
{
	private TimeSince _damageTime;
	private const float DAMAGE_TIME = 0.75f;

	private const float EXPLOSION_RADIUS = 1.45f;
	private const float EXPLOSION_DAMAGE = 40f;

	[Sync] public bool IsExploding { get; set; }
	private TimeSince _explodeStartTime;
	private bool _hasExploded;
	private bool _hasStartedLooping;

	private Player _playerWhoKilledUs;

	protected override void OnAwake()
	{
		OffsetY = -0.56f;
		ShadowScale = 1.08f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		base.OnAwake();

		//AnimSpeed = 2f;
		Sprite.Texture = Texture.Load("textures/sprites/exploder_elite.vtex");

		Scale = 1.2f;
		Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 12f;
		Deceleration = 1.87f;
		DecelerationAttacking = 1.53f;

		Radius = 0.275f;

		Health = 75f;
		MaxHealth = Health;
		DamageToPlayer = 16f;

		DeathTime = 0.2f;

		CoinValueMin = 2;
		CoinValueMax = 6;

		if ( IsProxy )
			return;
		
		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		_damageTime = DAMAGE_TIME;

		//AnimationPath = AnimSpawnPath;
	}

	protected override void OnUpdate()
	{
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"IsExploding: {IsExploding}\n", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.7f, 0f ) ) );

		if ( Manager.Instance.IsGameOver )
			return;

		if ( IsExploding )
		{
			Sprite.Color = Color.Lerp( Color.White, Color.Blue, 0.5f + Utils.FastSin( Time.Now * 24f ) * 0.5f );
		}

		if ( IsProxy )
			return;

		if ( IsExploding )
		{
			if ( !_hasStartedLooping && _explodeStartTime > 0.5f )
			{
				//AnimationPath = "textures/sprites/exploder_explode_loop.frames";
				_hasStartedLooping = true;
			}

			if ( !_hasExploded && _explodeStartTime > 3.5f )
				Explode();
		}

		base.OnUpdate();
	}

	protected override void UpdatePosition( float dt )
	{
		base.UpdatePosition( dt );

		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		Velocity += (closestPlayer.Position2D - Position2D).Normal * 1.0f * dt * (IsFeared ? -1f : 1f);

		if ( IsExploding )
		{
			Transform.Position += (Vector3)Velocity * 4f * dt;
		}
		else
		{
			float speed = IsAttacking ? 2f : 1.3f;
			Transform.Position += (Vector3)Velocity * speed * dt;
		}
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

	public override void StartDying( Player player )
	{
		if ( !IsExploding )
		{
			StartExploding();
			_playerWhoKilledUs = player;
		}
	}

	public void StartExploding()
	{
		IsExploding = true;
		_explodeStartTime = 0f;
		//AnimationPath = "textures/sprites/exploder_explode_start.frames";
		CanAttack = false;
		CanTurn = false;
	}

	public void Explode()
	{
		base.StartDying( _playerWhoKilledUs );

		Manager.Instance.SpawnExplosionEffect( Position2D );
		//Game.PlaySfxNearby( "explode", Position, pitch: Sandbox.Game.Random.Float( 0.9f, 1.1f ), volume: 1f, maxDist: 6f );

		_hasExploded = true;
		IsExploding = false;
	}

	public override void FinishDying()
	{
		List<Thing> nearbyThings = new List<Thing>();

		for ( int dx = -2; dx <= 2; dx++ )
			for ( int dy = -2; dy <= 2; dy++ )
				Manager.Instance.AddThingsInGridSquare( new GridSquare( GridPos.x + dx, GridPos.y + dy ), nearbyThings );

		foreach ( Thing thing in nearbyThings )
		{
			if ( thing == this )
				continue;

			if ( thing is Enemy enemy && !enemy.IsDying && (!enemy.IsSpawning || enemy.ElapsedTime > 0.75f) )
			{
				var dist_sqr = (thing.Position2D - Position2D).LengthSquared;
				if ( dist_sqr < MathF.Pow( EXPLOSION_RADIUS, 2f ) )
				{
					var addVel = Vector2.Zero; // todo
					var addTempWeight = 0f;

					enemy.Damage( EXPLOSION_DAMAGE, Guid.Empty, addVel, addTempWeight, false );
				}
			}
			else if ( thing is Player player && !player.IsDead && !player.IsInvulnerable )
			{
				var dist_sqr = (thing.Position2D - Position2D).LengthSquared;
				if ( dist_sqr < MathF.Pow( EXPLOSION_RADIUS, 2f ) * 0.95f )
				{
					var dmg = player.CheckDamageAmount( EXPLOSION_DAMAGE, DamageType.Explosion );
					player.Damage( dmg );
				}
			}
		}

		base.FinishDying();
	}
}