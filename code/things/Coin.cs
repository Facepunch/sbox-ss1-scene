using Sandbox;
using static Manager;

public class Coin : Thing
{
	public Vector2 Velocity { get; set; }

	public TimeSince SpawnTime { get; private set; }

	public bool IsMagnetized { get; private set; }
	public Player PlayerMagnetized { get; private set; }
	public TimeSince MagnetizeTime { get; private set; }
	private const float MAGNETIZE_DURATION = 12f;

	[Property] public int Value { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		//OffsetY = -0.12f;
		Sprite.Transform.LocalScale = new Vector3( 0.4f * Globals.SPRITE_SCALE, 0.4f * Globals.SPRITE_SCALE, 1f );
		Sprite.Transform.LocalRotation = new Angles( 0f, -90f, 0f );

		Scale = 0.4f;

		ShadowOpacity = 0.8f;
		ShadowScale = 0.4f;

		SpawnShadow( ShadowScale, ShadowOpacity );

		//BasePivotY = 0.225f;

		//Scale = new Vector2( 1f, 1f ) * 0.4f;
		SpawnTime = 0f;
		Radius = 0.125f;

		if ( IsProxy )
			return;

		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );
		CollideWith.Add( typeof( Coin ) );
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

		if ( IsProxy )
			return;

		float dt = Time.Delta;

		if ( IsMagnetized )
		{
			if ( MagnetizeTime > MAGNETIZE_DURATION || PlayerMagnetized == null || !PlayerMagnetized.IsValid || PlayerMagnetized.IsDead )
			{
				IsMagnetized = false;
				PlayerMagnetized = null;
			}
			else
			{
				if ( !PlayerMagnetized.Position2D.Equals( Position2D ) )
					Velocity += (PlayerMagnetized.Position2D - Position2D).Normal * 0.125f * Utils.Map( MagnetizeTime, 0f, MAGNETIZE_DURATION, 1f, 0f, EasingType.QuadIn );
			}
		}

		Position2D += Velocity * dt;
		Position2D = new Vector2( MathX.Clamp( Position2D.x, Manager.Instance.BOUNDS_MIN.x + Radius, Manager.Instance.BOUNDS_MAX.x - Radius ), MathX.Clamp( Position2D.y, Manager.Instance.BOUNDS_MIN.y + Radius, Manager.Instance.BOUNDS_MAX.y - Radius ) );
		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Position2D.y ) );
		Velocity *= (1f - dt * 0.92f);

		foreach ( Player player in Scene.GetAllComponents<Player>().Where(x => !x.IsDead))
		{
			var dist_sqr = (Position2D - player.Position2D).LengthSquared;
			var req_dist_sqr = MathF.Pow( player.Stats[PlayerStat.CoinAttractRange], 2f );
			if ( dist_sqr < req_dist_sqr )
			{
				if ( !player.Position2D.Equals( Position2D ) )
					Velocity += (player.Position2D - Position2D).Normal * Utils.Map( dist_sqr, req_dist_sqr, 0f, 0f, 1f, EasingType.Linear ) * player.Stats[PlayerStat.CoinAttractStrength] * dt;
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

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			if ( !Position2D.Equals( other.Position2D ) )
				Velocity += (Position2D - other.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * 20f * (1f + other.TempWeight) * dt;
		}
		else if ( other is Player player )
		{
			if ( !player.IsDead )
			{
				player.AddExperience( Value );
				Manager.Instance.PlaySfxNearby( "xp", Position2D, pitch: Utils.Map( Value, 1, 10, 0.7f, 1.5f, EasingType.QuadIn ), volume: 1f, maxDist: 6f );
				Remove();
			}
		}
		else if ( other is Coin coin )
		{
			SetValue( Value + coin.Value );
			SpawnCloudClient( coin.Position2D, Vector2.Zero );

			if ( !IsMagnetized && coin.IsMagnetized )
			{
				PlayerMagnetized = coin.PlayerMagnetized;
				IsMagnetized = true;
				MagnetizeTime = coin.MagnetizeTime;
			}

			coin.Remove();
		}
	}

	public void SetValue( int value )
	{
		Value = value;

		int tier_shape = 1 + MathX.FloorToInt( (value - 1) / 5f );
		int tier_color = value % 5;

		switch ( tier_shape )
		{
			case 1:
				Sprite.PlayAnimation( "xp_1" );
				//BasePivotY = 0.225f;
				break;
			case 2:
				Sprite.PlayAnimation( "xp_2" );
				//BasePivotY = 0.225f;
				break;
			case 3:
				Sprite.PlayAnimation( "xp_3" );
				//BasePivotY = 0.15f;
				break;
			case 4:
				Sprite.PlayAnimation( "xp_4" );
				//BasePivotY = 0.1f;
				break;
			case 5:
			default:
				Sprite.PlayAnimation( "xp_5" );
				//BasePivotY = 0.05f;
				break;
		}

		switch ( tier_color )
		{
			case 1:
				Sprite.Tint = new Color( 0.2f, 0.2f, 1f );
				break;
			case 2:
				Sprite.Tint = new Color( 1f, 0.2f, 0.2f );
				break;
			case 3:
				Sprite.Tint = new Color( 1f, 1f, 0.2f );
				break;
			case 4:
				Sprite.Tint = new Color( 0.2f, 1f, 0.3f );
				break;
			case 5:
			default:
				Sprite.Tint = new Color( 1f, 1f, 1f ) * 2f;
				break;
		}
	}

	public void Magnetize( Player player )
	{
		PlayerMagnetized = player;
		IsMagnetized = true;
		MagnetizeTime = 0f;
	}
}
