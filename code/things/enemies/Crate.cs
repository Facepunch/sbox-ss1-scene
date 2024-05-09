using Sandbox;

public class Crate : Enemy
{
	public override bool CanBleed => false;

	protected override void OnStart()
	{
		OffsetY = -0.4f;
		ShadowScale = 1.3f;
		ShadowOpacity = 0f;

		base.OnStart();

		//AnimSpeed = 2f;
		//BasePivotY = 0.05f;

		Sprite.Texture = Texture.Load("textures/sprites/crate.vtex");

		ScaleFactor = 0.95f;
		Sprite.Size = new Vector2( 1f, 1f ) * ScaleFactor;

		if ( IsProxy )
			return;
		
		PushStrength = 5f;
		Deceleration = 10f;

		Radius = 0.25f;
		Health = 45f;
		MaxHealth = Health;

		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );
	}

	protected override void UpdatePosition( float dt )
	{
		base.UpdatePosition( dt );

		Transform.Position += (Vector3)Velocity * dt;
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			var spawnFactor = Utils.Map( enemy.ElapsedTime, 0f, enemy.SpawnTime, 0f, 1f, EasingType.QuadIn );
			Velocity += (Position2D - enemy.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * enemy.PushStrength * (1f + enemy.TempWeight) * spawnFactor * dt;
		}
		else if ( other is Player player )
		{
			if ( !player.IsDead )
			{
				Velocity += (Position2D - player.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 1f ) * player.Stats[PlayerStat.PushStrength] * (1f + player.TempWeight) * dt;
			}
		}
	}

	public override void DropLoot( Player player )
	{
		float RAND_POS = 0.2f;

		int num_coins = Game.Random.Int( 2, 3 );
		for ( int i = 0; i < num_coins; i++ )
		{
			var coin = Manager.Instance.SpawnCoin( Position2D + new Vector2( Game.Random.Float( -RAND_POS, RAND_POS ), Game.Random.Float( -RAND_POS, RAND_POS ) ), Game.Random.Int( CoinValueMin, CoinValueMax ) );

			if ( coin != null )
				coin.Velocity = (coin.Position2D - Position2D) * Game.Random.Float( 2f, 6f );
		}

		//var health_pack_chance = player != null ? Utils.Map( player.Health, player.Stats[PlayerStat.MaxHp], 0f, 0.2f, 0.75f ) : 0.1f;
		//if ( Game.Random.Float( 0f, 1f ) < health_pack_chance )
		//{
		//	var healthPack = new HealthPack() { Position = Position + new Vector2( Game.Random.Float( -RAND_POS, RAND_POS ), Game.Random.Float( -RAND_POS, RAND_POS ) ) };
		//	healthPack.Velocity = (healthPack.Position - Position) * Game.Random.Float( 2f, 6f );
		//	Game.AddThing( healthPack );
		//}

		if ( Manager.Instance.TimeSinceMagnet > 50f )
		{
			var magnet_chance = 0.09f * Utils.Map( Manager.Instance.TimeSinceMagnet, 50f, 480f, 1f, 5.5f, EasingType.Linear );
			if ( Game.Random.Float( 0f, 1f ) < magnet_chance )
			{
				var pos = Position2D + new Vector2( Game.Random.Float( -RAND_POS, RAND_POS ), Game.Random.Float( -RAND_POS, RAND_POS ) );
				var vel = (pos - Position2D) * Game.Random.Float( 2f, 6f );
				Manager.Instance.SpawnMagnet( pos, vel );
			}
		}

		//var revive_chance = Game.DeadPlayers.ToList().Count * 0.4f;
		//if ( Game.Random.Float( 0f, 1f ) < revive_chance )
		//{
		//	var reviveSoul = new ReviveSoul() { Position = Position + new Vector2( Game.Random.Float( -RAND_POS, RAND_POS ), Game.Random.Float( -RAND_POS, RAND_POS ) ) };
		//	reviveSoul.Velocity = (reviveSoul.Position - Position) * Game.Random.Float( 2f, 6f );
		//	Game.AddThing( reviveSoul );
		//}

		//var grenade_chance = 0.15f;
		//if ( player != null && Game.Random.Float( 0f, 1f ) < grenade_chance )
		//{
		//	player.SpawnGrenade( Position, Vector2.Zero );
		//}
	}
}
