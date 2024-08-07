﻿using System;
using System.Collections.Generic;
using SpriteTools;
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
	public SpriteComponent BgSprite { get; set; }
	//public SpikeBackground Background { get; set; }

	public List<Thing> _hitThings = new List<Thing>();

	private bool _playedSfx;

	protected override void OnAwake()
	{
		base.OnAwake();

		//OffsetY = -0.2f;

		Radius = 0.275f;

		Scale = 1.2f;
		//Sprite.Size = new Vector2( 1f, 1f ) * Scale;
		//Sprite.FlipHorizontal = Game.Random.Float( 0f, 1f ) < 0.5f;
		Sprite.SpriteFlags = Game.Random.Int( 0, 1 ) == 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;

		Sprite.Transform.LocalScale = new Vector3( 1f ) * Scale * Globals.SPRITE_SCALE;

		Lifetime = 1.9f;
		Damage = 10f;
		SpawnTime = 0f;

		// spawn background
		var bgObj = Manager.Instance.EnemySpikeBgPrefab.Clone( Transform.Position );
		bgObj.SetParent( GameObject );
		//bgObj.Transform.LocalPosition = new Vector3( 0f, OffsetY, Globals.SHADOW_DEPTH_OFFSET );
		bgObj.Transform.LocalPosition = new Vector3( 0f, 0f, Globals.SHADOW_DEPTH_OFFSET );
		bgObj.NetworkMode = NetworkMode.Never;

		BgSprite = bgObj.Components.Get<SpriteComponent>();
		BgSprite.SpriteFlags = Game.Random.Int(0, 1) == 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		BgSprite.Transform.LocalScale = new Vector3( 1f ) * 1.3f * Globals.SPRITE_SCALE;
		BgSprite.Transform.LocalRotation = new Angles( 0f, -90f, 0f );

		if ( IsProxy )
			return;

		CollideWith.Add( typeof( Player ) );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Gizmo.Draw.Color = SpawnTime < 1.25f || SpawnTime > 1.6f ? Color.White.WithAlpha( 0.05f ) : Color.Red.WithAlpha( 0.1f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		if ( Manager.Instance.IsGameOver )
			return;

		if ( IsProxy )
			return;

		float dt = Time.Delta;

		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Position2D.y ) );

		if ( !_playedSfx && SpawnTime > 1.15f )
		{
			Manager.Instance.PlaySfxNearby( "spike.thrust", Position2D, pitch: Game.Random.Float( 1.15f, 1.3f ), volume: 1.2f, maxDist: 6f );
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
