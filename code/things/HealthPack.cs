using Sandbox;
using static Manager;

public class HealthPack : Thing
{
	public Vector2 Velocity { get; set; }

	public TimeSince SpawnTime { get; private set; }

	public float Lifetime { get; set; }

	public const float HP_AMOUNT = 20f;

	protected override void OnAwake()
	{
		base.OnAwake();

		OffsetY = -0.16f;

		Scale = 0.6f;

		ShadowOpacity = 0.8f;
		ShadowScale = 0.8f;
		SpawnShadow( ShadowScale, ShadowOpacity );

		Sprite.Transform.LocalScale = new Vector3( 0.6f, 0.6f, 1f ) * Globals.SPRITE_SCALE;

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
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"Stats[BulletStat.Damage]: {Stats[BulletStat.Damage]}\nStats[BulletStat.Lifetime]: {Stats[BulletStat.Lifetime]}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.4f, 0f ) ) );

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		if ( Manager.Instance.IsGameOver )
			return;

		Sprite.Transform.LocalScale = new Vector3( 0.6f + Utils.FastSin( SpawnTime * 8f ) * 0.025f, 0.6f + MathF.Cos( SpawnTime * 8f ) * 0.025f, 1f ) * Globals.SPRITE_SCALE;
		//ShadowScale = 0.8f + Utils.FastSin( SpawnTime * 8f ) * 0.025f;

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
			foreach ( Player player in Scene.GetAllComponents<Player>().Where( x => !x.IsDead ) )
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

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			Velocity += (Position2D - other.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * 20f * (1f + other.TempWeight) * dt;
		}
		else if ( other is Player player )
		{
			if ( !player.IsDead && SpawnTime > 0.1f )
			{
				player.Heal( HP_AMOUNT, 0.2f );
				Manager.Instance.PlaySfxNearby( "heal", Position2D, pitch: Utils.Map( player.Health / player.Stats[PlayerStat.MaxHp], 0f, 1f, 1.5f, 1f ), volume: 1.5f, maxDist: 5f );

				Remove();
			}
		}
	}
}
