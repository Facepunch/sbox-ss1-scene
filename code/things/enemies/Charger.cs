using Sandbox;

public class Charger : Enemy
{
	private TimeSince _damageTime;
	private const float DAMAGE_TIME = 1f;

	private float _chargeDelayTimer;
	private const float CHARGE_DELAY_MIN = 2f;
	private const float CHARGE_DELAY_MAX = 3f;

	public bool IsPreparingToCharge { get; private set; }
	public bool IsCharging { get; private set; }
	private float _prepareTimer;
	private const float PREPARE_TIME = 1f;
	private float _chargeTimer;
	private const float CHARGE_TIME = 3f;

	private Vector2 _chargeDir;
	private Vector2 _chargeVel;
	private TimeSince _chargeCloudTimer;

	protected override void OnAwake()
	{
		OffsetY = -0.57f;
		ShadowScale = 1.25f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		base.OnAwake();

		Sprite.Texture = Texture.Load("textures/sprites/charger.vtex");

		//ScaleFactor = 0.85f;
		Scale = 1.25f;
		Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 25f;

		Radius = 0.275f;

		Health = 75f;
		MaxHealth = Health;
		DamageToPlayer = 15f;

		CoinValueMin = 2;
		CoinValueMax = 5;

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
			if ( _chargeTimer < 0f )
			{
				IsCharging = false;
				//AnimationPath = AnimIdlePath;
				CanTurn = true;
			}
			else
			{
				_chargeVel += _chargeDir * 4f * Utils.MapReturn( _chargeTimer, CHARGE_TIME, 0f, 0f, 1f, EasingType.Linear ) * dt;
				TempWeight += Utils.MapReturn( _chargeTimer, CHARGE_TIME, 0f, 1f, 6f, EasingType.Linear ) * dt;
			}

			Transform.Position += (Vector3)(_chargeVel + Velocity) * dt;

			if ( _chargeCloudTimer > 0.25f )
			{
				SpawnCloudClient( Position2D + new Vector2( 0f, 0.25f ), -_chargeDir * Game.Random.Float( 0.2f, 0.8f ) );
				_chargeCloudTimer = Game.Random.Float( 0f, 0.075f );
			}
		}
		else
		{
			Velocity += (closestPlayer.Position2D - Position2D).Normal * dt * (IsFeared ? -1f : 1f);

			float speed = 0.75f * (IsAttacking ? 1.3f : 0.7f) + Utils.FastSin( MoveTimeOffset + Time.Now * (IsAttacking ? 15f : 7.5f) ) * (IsAttacking ? 0.66f : 0.35f);
			Transform.Position += (Vector3)Velocity * speed * dt;
		}

		var player_dist_sqr = (closestPlayer.Position2D - Position2D).LengthSquared;
		if ( !IsPreparingToCharge && !IsCharging && !IsAttacking && player_dist_sqr < 8f * 8f )
		{
			_chargeDelayTimer -= dt;
			if ( _chargeDelayTimer < 0f )
			{
				PrepareToCharge();
			}
		}
	}

	protected override void HandleDeceleration( float dt )
	{
		if ( IsCharging )
		{
			Velocity *= (1f - dt * 1.75f);
			_chargeVel *= (1f - dt * 0.5f);
		}
		else
		{
			base.HandleDeceleration( dt );
		}
	}

	protected override void UpdateSprite( Player targetPlayer )
	{
		if ( !IsCharging )
			base.UpdateSprite( targetPlayer );
	}

	protected override void HandleAttacking( Player targetPlayer, float dt )
	{
		if ( !IsPreparingToCharge && !IsCharging )
			base.HandleAttacking( targetPlayer, dt );
	}

	public void PrepareToCharge()
	{
		_prepareTimer = PREPARE_TIME;
		IsPreparingToCharge = true;
		//Game.PlaySfxNearby( "enemy.roar.prepare", Position, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 1f, maxDist: 5f );
		//AnimationPath = "textures/sprites/charger_charge_start.frames";
		CanTurn = false;
		CanAttack = false;
	}

	public void Charge()
	{
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		var target_pos = closestPlayer.Position2D + closestPlayer.Velocity * Game.Random.Float( 0.5f, 1.75f );
		_chargeDir = Utils.RotateVector( (target_pos - Position2D).Normal, Game.Random.Float( -10f, 10f ) );

		IsPreparingToCharge = false;
		IsCharging = true;
		_chargeTimer = CHARGE_TIME;
		CanAttack = true;

		_chargeDelayTimer = Game.Random.Float( CHARGE_DELAY_MIN, CHARGE_DELAY_MAX );
		_chargeVel = Vector2.Zero;
		//AnimationPath = "textures/sprites/charger_charge_loop.frames";

		//AnimSpeed = 3f;
		Sprite.FlipHorizontal = target_pos.x > Position2D.x;

		//Game.PlaySfxNearby( "enemy.roar", Position, pitch: Game.Random.Float( 0.925f, 1.075f ), volume: 1f, maxDist: 8f );
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

				if ( (IsAttacking || IsCharging) && _damageTime > (DAMAGE_TIME / TimeScale) )
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
