using Sandbox;

public class SpikerElite : Enemy
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
		//OffsetY = -0.82f;
		ShadowScale = 1.6f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		Scale = 1.9f;

		base.OnAwake();

		//AnimSpeed = 3f;
		//Sprite.Texture = Texture.Load("textures/sprites/spiker_elite.vtex");

		//Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 12f;
		Deceleration = 2.57f;
		DecelerationAttacking = 2.35f;
		AggroRange = 0.45f;

		Radius = 0.28f;

		Health = 220f;
		MaxHealth = Health;
		DamageToPlayer = 20f;

		CoinValueMin = 7;
		CoinValueMax = 15;

		if ( IsProxy )
			return;

		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		_damageTime = DAMAGE_TIME;
		_shootDelayTimer = Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX );
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
				Sprite.PlayAnimation( "shoot_reverse" );
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

		float speed = 0.5f * (IsAttacking ? 1.3f : 0.7f) + Utils.FastSin( MoveTimeOffset + Time.Now * (IsAttacking ? 12f : 4.5f) ) * (IsAttacking ? 0.66f : 0.35f);
		Transform.Position += (Vector3)Velocity * speed * dt;

		var player_dist_sqr = (closestPlayer.Position2D - Position2D).LengthSquared;
		if ( !IsShooting && !IsAttacking && player_dist_sqr < MathF.Pow( 8f, 2f ) )
		{
			_shootDelayTimer -= dt;
			if ( _shootDelayTimer < 0f )
			{
				StartShooting();
			}
		}
	}

	protected override void UpdateSprite( Player targetPlayer )
	{
		if ( Sprite.CurrentAnimation.Name.Contains( "shoot" ) ) return;

		base.UpdateSprite( targetPlayer );
	}

	public void StartShooting()
	{
		_shotTimer = SHOOT_TIME;
		IsShooting = true;
		CanAttack = false;
		CanAttackAnim = false;
		CanTurn = false;
		_hasShot = false;
		_hasReversed = false;
		_prepareStartTime = 0f;
		Velocity *= 0.25f;
		DontChangeAnimSpeed = true;
		AnimSpeed = 1f;
		BroadcastShootAnim();
	}

	[Broadcast]
	void BroadcastShootAnim()
	{
		Sprite.PlayAnimation( "shoot" );
	}

	public void CreateSpike()
	{
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		var target_pos = closestPlayer.Position2D + closestPlayer.Velocity * Game.Random.Float( 0.1f, 3f ) + new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) ) * 1.2f;
		var BUFFER = 0.3f;

		Manager.Instance.SpawnEnemySpike(
			new Vector2( Math.Clamp( target_pos.x, Manager.Instance.BOUNDS_MIN.x + BUFFER, Manager.Instance.BOUNDS_MAX.x - BUFFER ), Math.Clamp( target_pos.y, Manager.Instance.BOUNDS_MIN.y + BUFFER, Manager.Instance.BOUNDS_MAX.y - BUFFER ) ),
			elite: true
		);

		Manager.Instance.PlaySfxNearby( "spike.prepare", target_pos, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 1.5f, maxDist: 5f );
	}

	public void FinishShooting()
	{
		_shootDelayTimer = Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX );
		IsShooting = false;
		CanAttack = true;
		CanAttackAnim = true;
		CanTurn = true;
		DontChangeAnimSpeed = false;
		BroadcastIdleAnim();
	}

	[Broadcast]
	void BroadcastIdleAnim()
	{
		Sprite.PlayAnimation( AnimIdlePath );
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			var spawnFactor = Utils.Map( enemy.TimeSinceSpawn, 0f, enemy.SpawnTime, 0f, 1f, EasingType.QuadIn );
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
