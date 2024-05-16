using Sandbox;

public class Spiker : Enemy
{
	private TimeSince _damageTime;
	private const float DAMAGE_TIME = 0.75f;

	private float _shootDelayTimer;
	private const float SHOOT_DELAY_MIN = 2f;
	private const float SHOOT_DELAY_MAX = 3f;

	public bool IsShooting { get; private set; }
	private float _shotTimer;
	private const float SHOOT_TIME = 4f;
	private bool _hasShot;
	private TimeSince _prepareStartTime;
	private bool _hasReversed;

	protected override void OnAwake()
	{
		OffsetY = -0.58f;
		ShadowScale = 1.15f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		base.OnAwake();

		//AnimSpeed = 4f;
		Sprite.Texture = Texture.Load("textures/sprites/spiker.vtex");

		Scale = 1.4f;
		Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 8f;
		Deceleration = 2.57f;
		DecelerationAttacking = 2.35f;
		AggroRange = 0.75f;

		Radius = 0.27f;

		Health = 80f;
		MaxHealth = Health;
		DamageToPlayer = 14f;

		CoinValueMin = 1;
		CoinValueMax = 4;

		if ( IsProxy )
			return;
		
		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		_damageTime = DAMAGE_TIME;
		//AnimationPath = AnimSpawnPath;
	}

	protected override void UpdatePosition( float dt )
	{
		base.UpdatePosition( dt );

		// todo: optimize
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		if ( IsShooting )
		{
			Velocity *= (1f - dt * (IsAttacking ? DecelerationAttacking : Deceleration));
			if ( !_hasShot && _prepareStartTime > 1f )
			{
				CreateSpike();
				_hasShot = true;
			}

			if ( !_hasReversed && _prepareStartTime > 3f )
			{
				_hasReversed = true;
				//AnimationPath = "textures/sprites/spiker_shoot_reverse.frames";
			}

			Velocity *= (1f - dt * 4f);

			_shotTimer -= dt;
			if ( _shotTimer < 0f )
			{
				FinishShooting();
				return;
			}
		}
		else
		{
			Velocity += (closestPlayer.Position2D - Position2D).Normal * 1.0f * dt * (IsFeared ? -1f : 1f);
		}

		float speed = 0.9f * (IsAttacking ? 1.3f : 0.7f) + Utils.FastSin( MoveTimeOffset + Time.Now * (IsAttacking ? 15f : 7.5f) ) * (IsAttacking ? 0.66f : 0.35f);
		Transform.Position += (Vector3)Velocity * speed * dt;

		var player_dist_sqr = (closestPlayer.Position2D - Position2D).LengthSquared;
		if ( !IsShooting && !IsAttacking && player_dist_sqr < MathF.Pow( 6f, 2f ) )
		{
			_shootDelayTimer -= dt;
			if ( _shootDelayTimer < 0f )
			{
				StartShooting();
			}
		}
	}

	public void StartShooting()
	{
		_shotTimer = SHOOT_TIME;
		IsShooting = true;
		CanAttack = false;
		CanTurn = false;
		_hasShot = false;
		_hasReversed = false;
		_prepareStartTime = 0f;
		Velocity *= 0.25f;
		//AnimationPath = "textures/sprites/spiker_shoot.frames";
	}

	public void CreateSpike()
	{
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		var target_pos = closestPlayer.Position2D + closestPlayer.Velocity * Game.Random.Float( 0.2f, 2f ) + new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) ) * 0.4f;
		var BUFFER = 0.3f;

		Manager.Instance.SpawnEnemySpike( new Vector2( Math.Clamp( target_pos.x, Manager.Instance.BOUNDS_MIN.x + BUFFER, Manager.Instance.BOUNDS_MAX.x - BUFFER ), Math.Clamp( target_pos.y, Manager.Instance.BOUNDS_MIN.y + BUFFER, Manager.Instance.BOUNDS_MAX.y - BUFFER ) ) );

		//Game.PlaySfxNearby( "spike.prepare", target_pos, pitch: Sandbox.Game.Random.Float( 0.95f, 1.05f ), volume: 1.5f, maxDist: 5f );
	}

	public void FinishShooting()
	{
		_shootDelayTimer = Sandbox.Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX );
		IsShooting = false;
		CanAttack = true;
		CanTurn = true;
		//AnimationPath = AnimIdlePath;
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
