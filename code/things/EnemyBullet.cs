using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class EnemyBullet : Thing
{
	public Vector2 Velocity { get; set; }

	public TimeSince SpawnTime { get; private set; }

	public float Damage { get; set; }
	public float Lifetime { get; set; }
	public Vector2 Direction { get; set; }
	public float Speed { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		OffsetY = -0.4f;

		Radius = 0.066f;

		Scale = 0.35f;
		//Sprite.Size = new Vector2( 1f, 1f ) * Scale;

		ShadowOpacity = 0.8f;
		ShadowScale = 0.6f;
		SpawnShadow( ShadowScale, ShadowOpacity );

		//Speed = 2f;
		Lifetime = 6f;
		Damage = 12f;
		SpawnTime = 0f;

		if ( IsProxy )
			return;

		CollideWith.Add( typeof( Player ) );
	}

	[Broadcast]
	public void SetColor(Color color)
	{
		Sprite.Color = color;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		if ( Manager.Instance.IsGameOver )
			return;

		if ( IsProxy )
			return;

		float dt = Time.Delta;

		// todo: flip horizontal if moving left

		var speed = Speed * Utils.Map( SpawnTime, 0f, 0.5f, 0f, 1f, EasingType.QuadInOut );
		Position2D += Direction * speed * dt;
		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Position2D.y ) );

		if ( SpawnTime > Lifetime )
		{
			Remove();
			return;
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

		if ( other is Player player && !player.IsDead )
		{
			float dmg = player.CheckDamageAmount( Damage, DamageType.Ranged );

			if ( !player.IsInvulnerable )
			{
				//Game.PlaySfxNearby( "splash", Position, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 1f, maxDist: 4f );
				player.Damage( dmg );
				player.AddVelocity( Direction * 2f );
			}

			Remove();
		}
	}
}
