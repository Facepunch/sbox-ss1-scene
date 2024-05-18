using Sandbox;

public class Boss : Enemy
{
	private TimeSince _damageTime;
	private const float DAMAGE_TIME = 0.5f;

	private float _shootDelayTimer;
	private const float SHOOT_DELAY_MIN = 1.25f;
	private const float SHOOT_DELAY_MAX = 3f;

	public bool IsShooting { get; private set; }
	private bool _hasShot;

	private TimeSince _prepareShootTime;

	public bool IsCharging { get; private set; }
	private float _chargeTimer;
	private const float CHARGE_TIME_MIN = 1f;
	private const float CHARGE_TIME_MAX = 2.4f;
	private float _chargeTime;
	private Vector2 _chargeDir;
	private Vector2 _chargeVel;
	private TimeSince _chargeCloudTimer;

	public bool IsPreparingToCharge { get; private set; }
	private float _prepareTimer;
	private const float PREPARE_TIME = 0.8f;
	private bool _hasLandedCharge;

	private float _chargeDelayTimer;
	private const float CHARGE_DELAY_MIN = 3f;
	private const float CHARGE_DELAY_MAX = 6f;

	// todo: charge reflects off arena bounds

	protected override void OnAwake()
	{
		OffsetY = -0.95f;
		ShadowScale = 2.15f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		base.OnAwake();

		//AnimSpeed = 3f;
		//BasePivotY = 0.05f;

		Sprite.Texture = Texture.Load("textures/sprites/boss.vtex");

		//ScaleFactor = 0.85f;
		Scale = 2.5f;
		Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 50f;
		DeathTime = 3f;

		Radius = 0.42f;

		Deceleration = 1.1f;
		DecelerationAttacking = 1.1f;

		Health = 25000f;
		MaxHealth = Health;
		DamageToPlayer = 32f;

		Manager.Instance.Boss = this;

		if ( IsProxy )
			return;
		
		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		_damageTime = DAMAGE_TIME;
		_shootDelayTimer = Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX );
		_chargeDelayTimer = Game.Random.Float( CHARGE_DELAY_MIN, CHARGE_DELAY_MAX );
		//AnimationPath = AnimSpawnPath;
	}

	protected override void UpdatePosition( float dt )
	{
		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		base.UpdatePosition( dt );

		// todo: optimize
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		if ( IsPreparingToCharge )
		{
			_prepareTimer -= dt;
			if ( _prepareTimer < 0f )
			{
				Charge();
				return;
			}
		}
		else if ( IsCharging )
		{
			_chargeTimer -= dt;

			if ( !_hasLandedCharge && _chargeTimer < 0.5f )
			{
				//AnimationPath = "textures/sprites/boss_charge_reverse.frames";
				_hasLandedCharge = true;
			}

			if ( _chargeTimer < 0f )
			{
				IsCharging = false;
				//AnimationPath = AnimIdlePath;
				CanTurn = true;
				CanAttackAnim = true;
			}
			else
			{
				_chargeVel += _chargeDir * 4f * Utils.MapReturn( _chargeTimer, _chargeTime, 0f, 0f, 1f, EasingType.Linear ) * dt;
				TempWeight += Utils.MapReturn( _chargeTimer, _chargeTime, 0f, 1f, 6f, EasingType.Linear ) * dt;
			}

			if ( _chargeTimer < 0.1f )
				_chargeVel *= 0f;

			Transform.Position += (Vector3)(_chargeVel + Velocity) * dt;

			if ( _chargeCloudTimer > 0.25f )
			{
				SpawnCloudClient( Position2D + new Vector2( 0f, 0.25f ), -_chargeDir * Game.Random.Float( 0.2f, 0.8f ) );
				_chargeCloudTimer = Game.Random.Float( 0f, 0.075f );

				if ( Health < MaxHealth / 2f )
				{
					var dir = (new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) )).Normal;
					var enemyBullet = Manager.Instance.SpawnEnemyBullet( Position2D + new Vector2( 0f, 0.55f ) + dir * 0.03f, dir, speed: 3f );
					enemyBullet.SetColor( new Color( 1f, 1f, 0f ) );
				}
			}
		}
		else
		{
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
				if ( !closestPlayer.Position2D.Equals( Position2D ) )
					Velocity += (closestPlayer.Position2D - Position2D).Normal * 1.0f * dt * (IsFeared ? -1f : 1f);
			}

			float speed = 1.66f * (IsAttacking ? 1.3f : 0.7f) + Utils.FastSin( MoveTimeOffset + Time.Now * (IsAttacking ? 15f : 7.5f) );
			Transform.Position += (Vector3)Velocity * speed * dt;

			var player_dist_sqr = (closestPlayer.Position2D - Position2D).LengthSquared;

			if ( !IsPreparingToCharge && !IsCharging && !IsShooting && !IsAttacking && player_dist_sqr < MathF.Pow( 9f, 2f ) )
			{
				_shootDelayTimer -= dt;
				if ( _shootDelayTimer < 0f )
				{
					PrepareToShoot();
				}
			}

			if ( !IsPreparingToCharge && !IsCharging && !IsShooting && !IsAttacking && player_dist_sqr < 15f * 15f )
			{
				_chargeDelayTimer -= dt;
				if ( _chargeDelayTimer < 0f )
				{
					PrepareToCharge();
				}
			}
		}
	}

	public void PrepareToShoot()
	{
		_prepareShootTime = 0f;
		IsShooting = true;
		_hasShot = false;
		//AnimationPath = "textures/sprites/boss_shoot.frames";
		Manager.Instance.PlaySfxNearby( "boss.prepare", Position2D, pitch: Game.Random.Float( 0.75f, 0.85f ), volume: 1.7f, maxDist: 16f );
		CanAttack = false;
	}

	public void Shoot()
	{
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		var num_bullets = MathX.FloorToInt( Utils.Map( Health, MaxHealth, 0f, 3f, 8f, EasingType.SineIn ) ) + Game.Random.Int( 0, 1 );
		var spread = Game.Random.Float( 30f, 60f );

		float currAngleOffset = -spread * 0.5f;
		float increment = spread / (float)(num_bullets - 1);

		var target_pos = closestPlayer.Position2D + closestPlayer.Velocity * Game.Random.Float( 0.25f, 1.5f );
		Vector2 aim_dir = Utils.RotateVector( (target_pos - Position2D).Normal, Game.Random.Float( -15f, 15f ) );

		for ( int i = 0; i < num_bullets; i++ )
		{
			var dir = Utils.RotateVector( aim_dir, currAngleOffset + increment * i );
			var enemyBullet = Manager.Instance.SpawnEnemyBullet( Position2D + new Vector2( 0f, 0.55f ) + dir * 0.03f, dir, speed: 3f );
			enemyBullet.SetColor( new Color( 1f, 1f, 0f ) );
		}

		Velocity *= 0.25f;
		_hasShot = true;

		//AnimationPath = "textures/sprites/boss_shoot_reverse.frames";
		Manager.Instance.PlaySfxNearby( "boss.shoot", Position2D, pitch: Game.Random.Float( 0.65f, 0.75f ), volume: 1.5f, maxDist: 9f );
	}

	public void FinishShooting()
	{
		//AnimationPath = AnimIdlePath;
		CanAttack = true;
		_shootDelayTimer = Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX ) * Utils.Map( Health, MaxHealth, 0f, 1f, 0.5f, EasingType.QuadIn );
		IsShooting = false;
	}

	public void PrepareToCharge()
	{
		_prepareTimer = PREPARE_TIME;
		IsPreparingToCharge = true;
		Manager.Instance.PlaySfxNearby( "boss.prepare", Position2D, pitch: Game.Random.Float( 1.05f, 1.1f ), volume: 1.75f, maxDist: 10f );
		//AnimationPath = "textures/sprites/boss_charge.frames";
		CanTurn = false;
		CanAttack = false;
		CanAttackAnim = false;
	}

	public void Charge()
	{
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		var target_pos = closestPlayer.Position2D + closestPlayer.Velocity * Game.Random.Float( 0f, 1.66f );
		_chargeDir = Utils.RotateVector( (target_pos - Position2D).Normal, Game.Random.Float( -10f, 10f ) );

		IsPreparingToCharge = false;
		IsCharging = true;
		_chargeTime = Game.Random.Float( CHARGE_TIME_MIN, CHARGE_TIME_MAX );
		_chargeTimer = _chargeTime;
		CanAttack = true;
		_hasLandedCharge = false;

		_chargeDelayTimer = Game.Random.Float( CHARGE_DELAY_MIN, CHARGE_DELAY_MAX ) * Utils.Map( Health, MaxHealth, 0f, 1f, 0.5f, EasingType.SineIn );
		_chargeVel = Vector2.Zero;

		Sprite.FlipHorizontal = target_pos.x > Position2D.x;

		Manager.Instance.PlaySfxNearby( "boss.charge", Position2D, pitch: Game.Random.Float( 0.9f, 1.05f ), volume: 1.6f, maxDist: 9f );
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
				Velocity += (Position2D - player.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * player.Stats[PlayerStat.PushStrength] * (1f + player.TempWeight) * dt * 0.25f;

				if ( IsAttacking && _damageTime > (DAMAGE_TIME / TimeScale) )
				{
					float dmg = player.CheckDamageAmount( DamageToPlayer, DamageType.Melee );

					if ( !player.IsInvulnerable )
					{
						Manager.Instance.PlaySfxNearby( "zombie.attack.player", Position2D, pitch: Utils.Map( player.Health, player.Stats[PlayerStat.MaxHp], 0f, 0.95f, 1.15f, EasingType.QuadIn ), volume: 1f, maxDist: 5.5f );

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
		base.StartDying( player );

		//ColorFill = new ColorHsv( 0f, 0f, 0f, 0f );

		Manager.Instance.PlaySfxNearby( "boss.die", Position2D, pitch: Game.Random.Float( 0.75f, 0.8f ), volume: 1.5f, maxDist: 15f );
		Manager.Instance.Victory();
	}
}
