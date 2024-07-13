using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class Fire : Thing
{
	public TimeSince SpawnTime { get; private set; }

	public Player Shooter { get; set; }

	public float Lifetime { get; set; }

	private TimeSince _sinceDamageTime;
	private const float DAMAGE_INTERVAL = 0.4f;

	protected override void OnAwake()
	{
		base.OnAwake();

		//OffsetY = -0.16f;

		Radius = 0.27f;

		Scale = 0.9f;
		Sprite.Transform.LocalScale = new Vector3( Scale * Globals.SPRITE_SCALE, Scale * Globals.SPRITE_SCALE, 1f );
		Sprite.Transform.LocalRotation = new Angles( 0f, -90f, 0f );

		//ShadowOpacity = 0.8f;
		//ShadowScale = 0.3f;
		//SpawnShadow( ShadowScale, ShadowOpacity );

		//Speed = 2f;
		//Lifetime = 2f;
		SpawnTime = 0f;

		if ( IsProxy )
			return;

		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );
	}

	[Broadcast]
	public void SetColor(Color color)
	{
		Sprite.Tint = color;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		if ( Manager.Instance.IsGameOver )
			return;

		//Sprite.FlipHorizontal = Utils.FastSin( SpawnTime * 6f ) < 0f;
		//Sprite.Size = new Vector2( (0.9f + Utils.FastSin( SpawnTime * 28f ) * 0.09f), 0.9f + Utils.FastSin( SpawnTime * 15f ) * 0.065f );

		float opacity = 0.6f + Utils.FastSin( SpawnTime * 5f ) * 0.4f * Utils.Map( SpawnTime, Lifetime - 0.25f, Lifetime, 1f, 0f );
		Sprite.Tint = Color.White.WithAlpha( opacity );

		if ( IsProxy )
			return;

		float dt = Time.Delta;

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

		if ( Shooter == null )
			return;

		if ( other is Enemy enemy && !enemy.IsDying && ( !enemy.IsSpawning || enemy.TimeSinceSpawn > 1.0f ) )
		{
			if ( !enemy.HasEnemyStatus<BurningEnemyStatus>() )
			{
				enemy.Burn( Shooter, Shooter.Stats[PlayerStat.FireDamage] * Shooter.GetDamageMultiplier(), Shooter.Stats[PlayerStat.FireLifetime], Shooter.Stats[PlayerStat.FireSpreadChance] );
				Manager.Instance.PlaySfxNearby( "burn", Position2D, pitch: Sandbox.Game.Random.Float( 0.95f, 1.15f ), volume: 1f, maxDist: 5f );
			}
		}
		else if ( other is Player player && !player.IsDead )
		{
			if ( _sinceDamageTime > DAMAGE_INTERVAL )
			{
				float dmg = player.CheckDamageAmount( Shooter.Stats[PlayerStat.FireDamage] * Shooter.GetDamageMultiplier(), DamageType.Fire );

				if ( !player.IsInvulnerable )
				{
					Manager.Instance.PlaySfxNearby( "splash", Position2D, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 1f, maxDist: 4f );
					player.Damage( dmg );
					player.AddVelocity( Game.Random.Float(0f, 0.3f) );
				}

				_sinceDamageTime = 0f;
			}
		}
	}
}
