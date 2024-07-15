using Sandbox;

public class SpitterElite : Enemy
{
	private TimeSince _damageTime;
	private const float DAMAGE_TIME = 0.75f;

	private float _shootDelayTimer;
	private const float SHOOT_DELAY_MIN = 2f;
	private const float SHOOT_DELAY_MAX = 3f;

	public bool IsShooting { get; private set; }

	private TimeSince _prepareShootTime;
	private int _numVolleysShot;
	private float _currShootDelay;

	public override float HeightVariance => 0.03f;
	public override float WidthVariance => 0.02f;

	protected override void OnAwake()
	{
		//OffsetY = -0.47f;
		ShadowScale = 1.125f;
		ShadowFullOpacity = 0.8f;
		ShadowOpacity = 0f;

		Scale = 1.05f;

		base.OnAwake();

		//Sprite.Texture = Texture.Load("textures/sprites/spitter_elite.vtex");

		//Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		PushStrength = 8f;

		Radius = 0.26f;

		Health = 100f;
		MaxHealth = Health;
		DamageToPlayer = 11f;

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
		//Gizmo.Draw.Text( $"Sprite.PlaybackSpeed: {Sprite.PlaybackSpeed}\n{Sprite.CurrentAnimation.Name}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.7f, 0f ) ) ); 

		base.UpdatePosition( dt );

		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		if ( IsShooting )
		{
			if ( _numVolleysShot < 3 && _prepareShootTime > (_numVolleysShot == 0 ? 1.0f : _currShootDelay) )
				Shoot();
			else if ( _numVolleysShot >= 3 && _prepareShootTime > 0.6f )
				FinishShooting();

			return;
		}
		else
		{
			Velocity += (closestPlayer.Position2D - Position2D).Normal * 1.0f * dt * (IsFeared ? -1f : 1f);
		}

		float speed = 0.6f * (IsAttacking ? 1.3f : 0.7f) + Utils.FastSin( MoveTimeOffset + Time.Now * (IsAttacking ? 15f : 7.5f) ) * (IsAttacking ? 0.5f : 0.2f);
		Transform.Position += (Vector3)Velocity * speed * dt;

		var player_dist_sqr = (closestPlayer.Position2D - Position2D).LengthSquared;
		if ( !IsShooting && player_dist_sqr < 10f * 10f )
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
		Sprite.PlayAnimation( "shoot" );
		DontChangeAnimSpeed = true;
		AnimSpeed = 1f;
		Manager.Instance.PlaySfxNearby( "spitter.prepare", Position2D, pitch: Game.Random.Float( 1f, 1.1f ), volume: 0.6f, maxDist: 2.75f );
		CanAttack = false;
		CanAttackAnim = false;
		_numVolleysShot = 0;
		_currShootDelay = Game.Random.Float( 0.1f, 0.5f );
	}

	public void Shoot()
	{
		var closestPlayer = Manager.Instance.GetClosestPlayer( Position2D );
		if ( closestPlayer == null )
			return;

		var target_pos = closestPlayer.Position2D + closestPlayer.Velocity * Game.Random.Float( 0.5f, 1.85f );
		var dir = Utils.RotateVector( (target_pos - Position2D).Normal, Game.Random.Float( -14f, 14f ) );
		//var enemyBullet = Manager.Instance.SpawnEnemyBullet( Position2D + new Vector2(0f, 0.55f) + dir * 0.03f, dir, speed: 2.15f );
		var enemyBullet = Manager.Instance.SpawnEnemyBullet( Position2D + dir * 0.03f, dir, speed: 2.15f );
		enemyBullet.SetColor( new Color( 1f, 0.2f, 0f ) );
		enemyBullet.Lifetime = 8f;

		Velocity *= 0.25f;
		_numVolleysShot++;
		_prepareShootTime = 0f;
		_currShootDelay = Game.Random.Float( 0.1f, 0.5f );

		Manager.Instance.PlaySfxNearby( "spitter.shoot", Position2D, pitch: Game.Random.Float( 1.0f, 1.1f ), volume: 0.9f, maxDist: 5f );

		if ( _numVolleysShot >= 3 )
			Sprite.PlayAnimation( "shoot_reverse" );
	}

	public void FinishShooting()
	{
		Sprite.PlayAnimation( AnimIdlePath );
		CanAttack = true;
		CanAttackAnim = true;
		_shootDelayTimer = Game.Random.Float( SHOOT_DELAY_MIN, SHOOT_DELAY_MAX );
		IsShooting = false;
		DontChangeAnimSpeed = false;
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			var spawnFactor = Utils.Map( enemy.TimeSinceSpawn, 0f, enemy.SpawnTime, 0f, 1f, EasingType.QuadIn );
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
