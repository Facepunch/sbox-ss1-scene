using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class EnemySpike : Thing
{
	public TimeSince SpawnTime { get; private set; }

	public Enemy Shooter { get; set; }
	public float Damage { get; set; }
	public float Lifetime { get; set; }
	public SpriteRenderer BgSprite { get; set; }
	//public SpikeBackground Background { get; set; }

	public List<Thing> _hitThings = new List<Thing>();

	private bool _playedSfx;

	protected override void OnAwake()
	{
		base.OnAwake();

		OffsetY = -0.2f;

		Radius = 0.225f;

		Scale = 0.65f;
		Sprite.Size = new Vector2( 1f, 1f ) * Scale;
		Sprite.FlipHorizontal = Game.Random.Float( 0f, 1f ) < 0.5f;

		Lifetime = 2.1f;
		Damage = 10f;
		SpawnTime = 0f;

		// spawn background
		var bgObj = Manager.Instance.EnemySpikeBgPrefab.Clone( Transform.Position );
		bgObj.SetParent( GameObject );
		bgObj.Transform.LocalPosition = new Vector3( 0f, OffsetY, Globals.SHADOW_DEPTH_OFFSET );
		bgObj.NetworkMode = NetworkMode.Never;

		BgSprite = bgObj.Components.Get<SpriteRenderer>();
		BgSprite.FlipHorizontal = Game.Random.Float( 0f, 1f ) < 0.5f;
		BgSprite.Size = new Vector2( 1f, 1f ) * 1f;

		Sprite.Color = Color.White.WithAlpha( 0f );
		BgSprite.Color = Color.White.WithAlpha( 0f );

		if ( IsProxy )
			return;

		CollideWith.Add( typeof( Player ) );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		if ( Manager.Instance.IsGameOver )
			return;

		Sprite.Color = Color.White.WithAlpha( Utils.Map( SpawnTime, 0.8f, 1.25f, 0f, 1f, EasingType.SineOut ) * Utils.Map( SpawnTime, 1.25f, 1.4f, 1f, 0f, EasingType.SineOut ) );
		BgSprite.Color = Color.White.WithAlpha( Utils.Map( SpawnTime, 0f, 1f, 0f, 1f, EasingType.SineOut ) * Utils.Map( SpawnTime, Lifetime - 0.3f, Lifetime, 1f, 0f ) );

		if ( IsProxy )
			return;

		float dt = Time.Delta;

		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Position2D.y ) );

		if ( !_playedSfx && SpawnTime > 1.15f )
		{
			Manager.Instance.PlaySfxNearby( "spike.thrust", Position2D, pitch: Game.Random.Float( 1.15f, 1.3f ), volume: 1.5f, maxDist: 6f );
			_playedSfx = true;
		}

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

		if ( SpawnTime < 1.25f || SpawnTime > 1.6f )
			return;

		if ( other is Player player && !player.IsDead )
		{
			if ( _hitThings.Contains( player ) )
				return;

			float dmg = player.CheckDamageAmount( Damage, DamageType.Ranged );

			if ( !player.IsInvulnerable )
			{
				Manager.Instance.PlaySfxNearby( "spike.stab", player.Position2D, pitch: Game.Random.Float( 0.85f, 0.9f ), volume: 1.6f, maxDist: 6f );
				player.Damage( dmg );
				player.AddVelocity( (player.Position2D - Position2D).Normal * Game.Random.Float(1f, 2f));
			}

			_hitThings.Add( player );
		}
	}
}
