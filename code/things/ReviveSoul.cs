using Sandbox;
using static Manager;

public class ReviveSoul : Thing
{
	public Vector2 Velocity { get; set; }

	public TimeSince SpawnTime { get; private set; }

	public float Lifetime { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		OffsetY = -0.14f;

		Scale = 0.4f;

		ShadowOpacity = 0.8f;
		ShadowScale = 0.8f;
		SpawnShadow( ShadowScale, ShadowOpacity );

		if ( IsProxy )
			return;

		//BasePivotY = 0.225f;

		SpawnTime = 0f;
		Lifetime = 60f;
		Radius = 0.175f;

		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Manager.Instance.IsGameOver )
			return;

		Sprite.Size = new Vector2( 0.6f + Utils.FastSin( SpawnTime * 8f ) * 0.025f, 0.6f + MathF.Cos( SpawnTime * 8f ) * 0.025f );
		//ShadowScale = 0.8f + Utils.FastSin( SpawnTime * 8f ) * 0.025f;

		float opacity = 0.3f + Utils.FastSin( SpawnTime * 5f ) * 0.2f;
		Sprite.Color = Color.White.WithAlpha( opacity );
		ShadowSprite.Color = Color.Black.WithAlpha( opacity );

		if ( IsProxy )
			return;

		float dt = Time.Delta;

		Position2D += Velocity * dt;
		Position2D = new Vector2( MathX.Clamp( Position2D.x, Manager.Instance.BOUNDS_MIN.x + Radius, Manager.Instance.BOUNDS_MAX.x - Radius ), MathX.Clamp( Position2D.y, Manager.Instance.BOUNDS_MIN.y + Radius, Manager.Instance.BOUNDS_MAX.y - Radius ) );
		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Position2D.y ) );
		Velocity *= (1f - dt * 3.5f);

		// todo: blink near lifetime end
		if ( SpawnTime > Lifetime )
		{
			Remove();
			return;
		}

		if ( SpawnTime > 0.1f )
		{
			foreach ( Player player in Scene.GetAllComponents<Player>().Where(x => x.IsDead) )
			{
				var dist_sqr = (Position2D - player.Position2D).LengthSquared;
				var req_dist_sqr = MathF.Pow( player.Stats[PlayerStat.CoinAttractRange], 2f );
				if ( dist_sqr < req_dist_sqr )
				{
					Velocity += (player.Position2D - Position2D).Normal * Utils.Map( dist_sqr, req_dist_sqr, 0f, 0f, 1f, EasingType.Linear ) * player.Stats[PlayerStat.CoinAttractStrength] * dt;
				}
			}
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

		if ( other is Player player && SpawnTime > 0.1f )
		{
			if ( player.IsDead )
			{
				player.Revive();
				Manager.Instance.PlaySfxNearby( "heal", Position2D, pitch: Utils.Map( player.Health / player.Stats[PlayerStat.MaxHp], 0f, 1f, 1.5f, 1f ), volume: 1.5f, maxDist: 5f );
				Remove();
			}
			else
			{
				Velocity += (Position2D - player.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * player.Stats[PlayerStat.PushStrength] * (1f + player.TempWeight) * dt;
			}
		}
	}
}
