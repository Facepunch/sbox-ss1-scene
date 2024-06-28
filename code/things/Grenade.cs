using Sandbox;
using static Manager;

public class Grenade : Thing
{
	public Vector2 Velocity { get; set; }

	public TimeSince SpawnTime { get; private set; }

	public float Lifetime { get; set; }
	public float Damage { get; set; }
	public float ExplosionRadius { get; set; }
	private const float BASE_EXPLOSION_MODIFIER = 0.6f;
	public float ExplosionSizeMultiplier { get; set; }
	public float Friction { get; set; }
	public Player Player { get; set; }
	public float StickyPercent { get; set; }
	public float FearChance { get; set; }
	public float CriticalChance { get; set; }
	public float CriticalMultiplier { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		OffsetY = -0.14f;

		Scale = 0.3f;

		ShadowOpacity = 0.8f;
		ShadowScale = 0.6f;
		SpawnShadow( ShadowScale, ShadowOpacity );

		if ( IsProxy )
			return;

		//BasePivotY = 0.225f;

		SpawnTime = 0f;
		Lifetime = 3f;
		Radius = 0.175f;

		Damage = 25f;
		ExplosionRadius = 1.45f;

		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );
		//CollideWith.Add( typeof( Coin ) );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"Stats[BulletStat.Damage]: {Stats[BulletStat.Damage]}\nStats[BulletStat.Lifetime]: {Stats[BulletStat.Lifetime]}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.4f, 0f ) ) );

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		if ( Manager.Instance.IsGameOver )
			return;

		Sprite.Tint = Color.Lerp( StickyPercent <= 0f ? Color.Red : Color.Magenta, new Color( 0f, 0.01f, 0f ), 0.5f + MathF.Sin( SpawnTime.Relative * Utils.Map( SpawnTime, 0f, Lifetime, 1f, 16f, EasingType.QuadIn ) ) * 0.5f );

		if ( IsProxy )
			return;

		float dt = Time.Delta;

		Position2D += Velocity * dt;

		float BUFFER = -0.175f;
		if ( Position2D.x < Manager.Instance.BOUNDS_MIN.x + BUFFER )
			Velocity = new Vector2( MathF.Abs( Velocity.x ), Velocity.y );
		else if ( Position2D.x > Manager.Instance.BOUNDS_MAX.x - BUFFER )
			Velocity = new Vector2( -MathF.Abs( Velocity.x ), Velocity.y );

		if ( Position2D.y < Manager.Instance.BOUNDS_MIN.y + BUFFER )
			Velocity = new Vector2( Velocity.x, MathF.Abs( Velocity.y ) );
		else if ( Position2D.y > Manager.Instance.BOUNDS_MAX.y - BUFFER )
			Velocity = new Vector2( Velocity.x, -MathF.Abs( Velocity.y ) );

		//Position2D = new Vector2( MathX.Clamp( Position2D.x, Manager.Instance.BOUNDS_MIN.x + Radius, Manager.Instance.BOUNDS_MAX.x - Radius ), MathX.Clamp( Position2D.y, Manager.Instance.BOUNDS_MIN.y + Radius, Manager.Instance.BOUNDS_MAX.y - Radius ) );
		Transform.Position = Transform.Position.WithZ( Globals.GetZPos(Position2D.y) );
		Velocity *= (1f - dt * 0.95f);

		//Scale = new Vector2( 0.6f + Utils.FastSin( SpawnTime * 8f ) * 0.025f, 0.6f + MathF.Cos( SpawnTime * 8f ) * 0.025f );
		//ShadowScale = 0.8f + Utils.FastSin( SpawnTime * 8f ) * 0.025f;

		for ( int dx = -1; dx <= 1; dx++ )
		{
			for ( int dy = -1; dy <= 1; dy++ )
			{
				Manager.Instance.HandleThingCollisionForGridSquare( this, new GridSquare( GridPos.x + dx, GridPos.y + dy ), dt );

				if ( IsRemoved )
					return;
			}
		}

		if ( SpawnTime > Lifetime )
		{
			Explode();
		}
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		float repelAmount = (SpawnTime < 0.1f || StickyPercent <= 0f) ? 30f : Utils.Map( StickyPercent, 0f, 1f, 10f, -40f );

		if ( (other is Enemy enemy && !enemy.IsDying) || (other is Player player && !player.IsDead) )
		{
			Velocity += (Position2D - other.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * repelAmount * (1f + other.TempWeight) * dt;
		}
	}

	[Broadcast]
	public void Explode()
	{
		float scaleModifier = BASE_EXPLOSION_MODIFIER * ExplosionSizeMultiplier;
		Manager.Instance.SpawnExplosionEffectLocal( Position2D, scaleModifier );
		Manager.Instance.PlaySfxNearbyLocal( "explode", Position2D, pitch: Game.Random.Float( 0.9f, 1.1f ), volume: 1f, maxDist: 6f );

		if ( IsProxy )
			return;

		List<Thing> nearbyThings = new List<Thing>();

		for ( int dx = -2; dx <= 2; dx++ )
			for ( int dy = -2; dy <= 2; dy++ )
				Manager.Instance.AddThingsInGridSquare( new GridSquare( GridPos.x + dx, GridPos.y + dy ), nearbyThings );

		foreach ( Thing thing in nearbyThings )
		{
			if ( thing == this )
				continue;

			float radius = ExplosionRadius * BASE_EXPLOSION_MODIFIER * ExplosionSizeMultiplier;
			float damage = Damage * Player?.Stats[PlayerStat.ExplosionDamageMultiplier] ?? 1f;
			bool isCrit = Game.Random.Float( 0f, 1f ) < CriticalChance;
			if ( isCrit )
				damage *= CriticalMultiplier;

			if ( thing is Enemy enemy && !enemy.IsDying && (!enemy.IsSpawning || enemy.ElapsedTime > 0.75f) )
			{
				var dist_sqr = (thing.Position2D - Position2D).LengthSquared;

				if ( dist_sqr < MathF.Pow( radius, 2f ) )
				{
					var addVel = Vector2.Zero; // todo
					var addTempWeight = 0f;

					// todo: should player source be counted?
					enemy.Damage( damage, Guid.Empty, addVel, addTempWeight, isCrit );

					if ( Game.Random.Float( 0f, 1f ) < FearChance && !enemy.IsDying )
					{
						if ( !enemy.HasEnemyStatus<FearEnemyStatus>() )
							Manager.Instance.PlaySfxNearby( "fear", enemy.Position2D, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 0.6f, maxDist: 6f );

						enemy.Fear( Player );
					}
				}
			}
			else if ( thing is Player player && !player.IsDead && !player.IsInvulnerable )
			{
				var dist_sqr = (thing.Position2D - Position2D).LengthSquared;
				if ( dist_sqr < MathF.Pow( radius, 2f ) * 0.94f )
				{
					var dmg = player.CheckDamageAmount( damage, DamageType.Explosion );
					player.Damage( dmg );
				}
			}
		}

		Remove();
	}
}
