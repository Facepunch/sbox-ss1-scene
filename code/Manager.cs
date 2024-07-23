using Battlebugs;
using Sandbox;
using Sandbox.Network;

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject ShadowPrefab { get; set; }
	[Property] public GameObject CoinPrefab { get; set; }
	[Property] public GameObject MagnetPrefab { get; set; }
	[Property] public GameObject BloodSplatterPrefab { get; set; }
	[Property] public GameObject CloudPrefab { get; set; }
	[Property] public GameObject BurningVfxPrefab { get; set; }
	[Property] public GameObject FrozenVfxPrefab { get; set; }
	[Property] public GameObject FearVfxPrefab { get; set; }
	[Property] public GameObject GrenadePrefab { get; set; }
	[Property] public GameObject ExplosionEffectPrefab { get; set; }
	[Property] public GameObject ReviveSoulPrefab { get; set; }
	[Property] public GameObject HealthPackPrefab { get; set; }
	[Property] public GameObject EnemyBulletPrefab { get; set; }
	[Property] public GameObject EnemySpikePrefab { get; set; }
	[Property] public GameObject EnemySpikeBgPrefab { get; set; }
	[Property] public GameObject EnemySpikeElitePrefab { get; set; }
	[Property] public GameObject FirePrefab { get; set; }
	[Property] public GameObject ShieldVfxPrefab { get; set; }
	[Property] public GameObject CratePrefab { get; set; }
	[Property] public GameObject ZombiePrefab { get; set; }
	[Property] public GameObject ZombieElitePrefab { get; set; }
	[Property] public GameObject ExploderPrefab { get; set; }
	[Property] public GameObject ExploderElitePrefab { get; set; }
	[Property] public GameObject SpitterPrefab { get; set; }
	[Property] public GameObject SpitterElitePrefab { get; set; }
	[Property] public GameObject SpikerPrefab { get; set; }
	[Property] public GameObject SpikerElitePrefab { get; set; }
	[Property] public GameObject ChargerPrefab { get; set; }
	[Property] public GameObject ChargerElitePrefab { get; set; }
	[Property] public GameObject RunnerPrefab { get; set; }
	[Property] public GameObject RunnerElitePrefab { get; set; }
	[Property] public GameObject BossPrefab { get; set; }

	[Property] public CameraComponent Camera { get; private set; }
	[Property] public Camera2D Camera2D { get; set; }

	public int EnemyCount { get; private set; }
	public const float MAX_ENEMY_COUNT = 350;

	public int CrateCount { get; private set; }
	public const float MAX_CRATE_COUNT = 7;

	public int CoinCount { get; private set; }
	public const float MAX_COIN_COUNT = 200;

	public record struct GridSquare( int x, int y );
	public Dictionary<GridSquare, List<Thing>> ThingGridPositions = new Dictionary<GridSquare, List<Thing>>();

	public float GRID_SIZE = 1f;
	public Vector2 BOUNDS_MIN;
	public Vector2 BOUNDS_MAX;
	public Vector2 BOUNDS_MIN_SPAWN;
	public Vector2 BOUNDS_MAX_SPAWN;

	private TimeSince _enemySpawnTime;
	[Sync] public TimeSince ElapsedTime { get; set; }

	[Sync] public bool IsGameOver { get; private set; }
	[Sync] public bool IsVictory { get; private set; }

	public Vector2 MouseWorldPos { get; private set; }

	public bool HasSpawnedBoss { get; private set; }
	public Boss Boss { get; set; }

	public TimeSince TimeSinceMagnet { get; set; }

	public List<BloodSplatter> _bloodSplatters = new();
	public List<Cloud> _clouds = new();
	public List<ExplosionEffect> _explosions = new();

	public Status HoveredStatus { get; set; }

	private int _numEnemyDeathSfxs;

	public int NumPlayers { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;

		BOUNDS_MIN = new Vector2( -16f, -12.35f );
		BOUNDS_MAX = new Vector2( 16f, 12f );
		BOUNDS_MIN_SPAWN = new Vector2( -15.5f, -11.75f );
		BOUNDS_MAX_SPAWN = new Vector2( 15.5f, 11.5f );

		ElapsedTime = 0f;

		for ( float x = BOUNDS_MIN.x; x < BOUNDS_MAX.x; x += GRID_SIZE )
		{
			for ( float y = BOUNDS_MIN.y; y < BOUNDS_MAX.y; y += GRID_SIZE )
			{
				ThingGridPositions.Add( GetGridSquareForPos( new Vector2( x, y ) ), new List<Thing>() );
			}
		}

		if ( IsProxy )
			return;
	}

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			if ( MainMenu.IsSingleplayerGame )
				CreatePlayer( Connection.Local );
			else
				GameNetworkSystem.CreateLobby();
		}

		if ( IsProxy )
			return;

		SpawnStartingThings();
	}

	public void SpawnStartingThings()
	{
		for ( int i = 0; i < 3; i++ )
		{
			var pos = new Vector2( Game.Random.Float( BOUNDS_MIN_SPAWN.x, BOUNDS_MAX_SPAWN.x ), Game.Random.Float( BOUNDS_MIN_SPAWN.y, BOUNDS_MAX_SPAWN.y ) );
			SpawnEnemy( TypeLibrary.GetType( typeof( Crate ) ), pos );
		}

		//for( int i = 0; i < 22; i++ )
		//	SpawnEnemy( TypeLibrary.GetType( typeof( Zombie ) ), new Vector2( Game.Random.Float( -1, 1f ), Game.Random.Float( -1, 1f ) ), forceSpawn: true );

		//SpawnEnemy( TypeLibrary.GetType( typeof( Exploder ) ), new Vector2( -2f, 0f ), forceSpawn: true );

		//SpawnBoss( new Vector2( 3f, 3f ) );
		//HasSpawnedBoss = true;
	}

	protected override void OnUpdate()
	{
		//for ( float x = BOUNDS_MIN.x; x < BOUNDS_MAX.x; x += GRID_SIZE )
		//{
		//	for ( float y = BOUNDS_MIN.y; y < BOUNDS_MAX.y; y += GRID_SIZE )
		//	{
		//		var pos = new Vector2( x, y );
		//		Gizmo.Draw.LineBBox( new BBox( pos, new Vector2( x + GRID_SIZE, y + GRID_SIZE ) ) );

		//		//var gridSquare = GetGridSquareForPos( pos );
		//		//Gizmo.Draw.Text( (new Vector2( gridSquare.x, gridSquare.y )).ToString(), new global::Transform( new Vector3( x + 7f, y + 7f, 0f ) ) );
		//	}
		//}

		NumPlayers = Scene.GetAllComponents<Player>().Count();

		if ( IsGameOver )
			return;

		var tr = Scene.Trace.Ray( Camera.ScreenPixelToRay( Mouse.Position ), 1500f ).Run();
		if ( tr.Hit )
		{
			MouseWorldPos = (Vector2)tr.HitPosition;
		}

		if ( _numEnemyDeathSfxs > 0 )
			_numEnemyDeathSfxs--;

		if ( IsProxy )
			return;

		HandleEnemySpawn();

		if ( !HasSpawnedBoss && !IsGameOver && ElapsedTime > 15f * 60f )
		{
			SpawnBoss( new Vector2( 0f, 0f ) );
			HasSpawnedBoss = true;
		}
	}

	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is becoming active (local = {channel == Connection.Local}) (host = {channel.IsHost})" );

		CreatePlayer( channel );
	}

	void CreatePlayer( Connection channel )
	{
		var playerObj = PlayerPrefab.Clone( new Vector3( Game.Random.Float( -3f, 3f ), Game.Random.Float( -3f, 3f ), Globals.GetZPos( 0f ) ) );
		var player = playerObj.Components.Get<Player>();

		playerObj.NetworkSpawn( channel );
	}

	void HandleEnemySpawn()
	{
		var spawnTime = Utils.Map( EnemyCount, 0, MAX_ENEMY_COUNT, 0.05f, 0.3f, EasingType.QuadOut )
			* Utils.Map( ElapsedTime, 0f, 80f, 1.5f, 1f )
			* Utils.Map( ElapsedTime, 0f, 250f, 3f, 1f )
			* Utils.Map( ElapsedTime, 0f, 700f, 1.2f, 1f );

		if ( _enemySpawnTime > spawnTime )
		{
			SpawnEnemy();
			_enemySpawnTime = 0f;
		}
	}

	void SpawnEnemy()
	{
		if ( EnemyCount >= MAX_ENEMY_COUNT )
			return;

		var pos = new Vector2( Game.Random.Float( BOUNDS_MIN_SPAWN.x, BOUNDS_MAX_SPAWN.x ), Game.Random.Float( BOUNDS_MIN_SPAWN.y, BOUNDS_MAX_SPAWN.y ) );

		//// ZOMBIE (DEFAULT)
		TypeDescription type = TypeLibrary.GetType( typeof( Zombie ) );

		// CRATE
		if ( CrateCount < MAX_CRATE_COUNT )
		{
			float crateChance = ElapsedTime < 20f ? 0f : Utils.Map( ElapsedTime, 20f, 200f, 0.005f, 0.01f );
			float additionalCrateChance = 0f;
			foreach ( Player player in Scene.GetAllComponents<Player>().Where( x => !x.IsDead ) )
			{
				if ( player.Stats[PlayerStat.CrateChanceAdditional] > 0f )
					additionalCrateChance += player.Stats[PlayerStat.CrateChanceAdditional];
			}
			crateChance *= (1f + additionalCrateChance);

			if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < crateChance )
				type = TypeLibrary.GetType( typeof( Crate ) );
		}

		// EXPLODER
		float exploderChance = ElapsedTime < 35f ? 0f : Utils.Map( ElapsedTime, 35f, 700f, 0.022f, 0.08f );
		if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < exploderChance )
		{
			float eliteChance = ElapsedTime < 480f ? 0f : Utils.Map( ElapsedTime, 480f, 1200f, 0.025f, 1f, EasingType.SineIn );
			type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( ExploderElite ) ) : TypeLibrary.GetType( typeof( Exploder ) );
		}

		// SPITTER
		float spitterChance = ElapsedTime < 100f ? 0f : Utils.Map( ElapsedTime, 100f, 800f, 0.015f, 0.1f );
		if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < spitterChance )
		{
			float eliteChance = ElapsedTime < 540f ? 0f : Utils.Map( ElapsedTime, 540f, 1200f, 0.025f, 1f, EasingType.QuadIn );
			type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( SpitterElite ) ) : TypeLibrary.GetType( typeof( Spitter ) );
		}

		// SPIKER
		float spikerChance = ElapsedTime < 320f ? 0f : Utils.Map( ElapsedTime, 320f, 800f, 0.018f, 0.1f, EasingType.SineIn );
		if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < spikerChance )
		{
			float eliteChance = ElapsedTime < 580f ? 0f : Utils.Map( ElapsedTime, 580f, 1300f, 0.008f, 1f, EasingType.SineIn );
			type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( SpikerElite ) ) : TypeLibrary.GetType( typeof( Spiker ) );
		}

		// CHARGER
		float chargerChance = ElapsedTime < 420f ? 0f : Utils.Map( ElapsedTime, 420f, 800f, 0.022f, 0.075f );
		if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < chargerChance )
		{
			float eliteChance = ElapsedTime < 660f ? 0f : Utils.Map( ElapsedTime, 660f, 1400f, 0.008f, 1f, EasingType.SineIn );
			type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( ChargerElite ) ) : TypeLibrary.GetType( typeof( Charger ) );
		}

		// RUNNER
		float runnerChance = ElapsedTime < 500f ? 0f : Utils.Map( ElapsedTime, 500f, 900f, 0.035f, 0.15f, EasingType.QuadIn );
		if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < runnerChance )
		{
			float eliteChance = ElapsedTime < 720f ? 0f : Utils.Map( ElapsedTime, 720f, 1500f, 0.01f, 1f, EasingType.QuadIn );
			type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( RunnerElite ) ) : TypeLibrary.GetType( typeof( Runner ) );
		}

		// ZOMBIE ELITE
		var zombieEliteChance = ElapsedTime < 400f ? 0f : Utils.Map( ElapsedTime, 400f, 1200f, 0.0175f, 1f, EasingType.SineIn );
		if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < zombieEliteChance )
		{
			type = TypeLibrary.GetType( typeof( ZombieElite ) );
		}

		SpawnEnemy( type, pos );
	}

	void SpawnEnemy( TypeDescription type, Vector2 pos, bool forceSpawn = false )
	{
		if ( EnemyCount >= MAX_ENEMY_COUNT && !forceSpawn )
			return;

		GameObject enemyObj;
		Enemy enemy;
		var pos3 = new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) );
		if ( type == TypeLibrary.GetType( typeof( Crate ) ) )
		{
			enemyObj = CratePrefab.Clone( pos3 );
			CrateCount++;
		}
		else if ( type == TypeLibrary.GetType( typeof( Zombie ) ) ) { enemyObj = ZombiePrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( ZombieElite ) ) ) { enemyObj = ZombieElitePrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( Exploder ) ) ) { enemyObj = ExploderPrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( ExploderElite ) ) ) { enemyObj = ExploderElitePrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( Spitter ) ) ) { enemyObj = SpitterPrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( SpitterElite ) ) ) { enemyObj = SpitterElitePrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( Spiker ) ) ) { enemyObj = SpikerPrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( SpikerElite ) ) ) { enemyObj = SpikerElitePrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( Charger ) ) ) { enemyObj = ChargerPrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( ChargerElite ) ) ) { enemyObj = ChargerElitePrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( Runner ) ) ) { enemyObj = RunnerPrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( RunnerElite ) ) ) { enemyObj = RunnerElitePrefab.Clone( pos3 ); }
		else if ( type == TypeLibrary.GetType( typeof( Boss ) ) ) { enemyObj = BossPrefab.Clone( pos3 ); }
		else
		{
			Log.Info( $"Enemy {type} not implemented yet!" );
			return;
		}

		enemy = enemyObj.Components.Get<Enemy>();

		var closestPlayer = GetClosestPlayer( pos );
		if ( closestPlayer?.Position2D.x > pos.x )
			enemy.FlipX = true;

		enemyObj.Name = type.ToString();
		enemyObj.NetworkSpawn( null );

		AddThing( enemy );
		EnemyCount++;

		PlaySfxNearby( "zombie.dirt", pos, pitch: Game.Random.Float( 0.6f, 0.8f ), volume: 0.7f, maxDist: 7.5f );
	}

	[Authority]
	public void SpawnCoin( Vector2 pos, Vector2 vel, int value = 1 )
	{
		// todo: spawn larger amounts less often if reaching max coin cap
		if ( CoinCount >= MAX_COIN_COUNT )
			return;

		var coinObj = CoinPrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
		var coin = coinObj.Components.Get<Coin>();
		coin.Velocity = vel;
		coin.SetValue( value );

		coinObj.NetworkSpawn( null );

		AddThing( coin );
		CoinCount++;

		return;
	}

	public Magnet SpawnMagnet( Vector2 pos, Vector2 vel )
	{
		var magnetObj = MagnetPrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
		var magnet = magnetObj.Components.Get<Magnet>();
		magnet.Velocity = vel;
		magnetObj.NetworkSpawn( null );

		TimeSinceMagnet = 0f;

		AddThing( magnet );

		return magnet;
	}

	public void SpawnReviveSoul( Vector2 pos, Vector2 vel )
	{
		var reviveObj = ReviveSoulPrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
		var revive = reviveObj.Components.Get<ReviveSoul>();
		revive.Velocity = vel;

		reviveObj.NetworkSpawn( null );
		AddThing( revive );
	}

	public void SpawnHealthPack( Vector2 pos, Vector2 vel )
	{
		var healthPackObj = HealthPackPrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
		var healthPack = healthPackObj.Components.Get<HealthPack>();
		healthPack.Velocity = vel;

		healthPackObj.NetworkSpawn( null );
		AddThing( healthPack );
	}

	public EnemyBullet SpawnEnemyBullet( Vector2 pos, Vector2 dir, float speed )
	{
		var enemyBulletObj = EnemyBulletPrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
		var enemyBullet = enemyBulletObj.Components.Get<EnemyBullet>();
		enemyBullet.Direction = dir;
		enemyBullet.Speed = speed;

		if ( dir.x < 0f )
			enemyBullet.Sprite.SpriteFlags = SpriteFlags.HorizontalFlip;

		enemyBulletObj.NetworkSpawn( null );
		AddThing( enemyBullet );

		return enemyBullet;
	}

	public void SpawnEnemySpike( Vector2 pos, bool elite = false )
	{
		if ( elite )
		{
			var spikeObj = EnemySpikeElitePrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
			var spike = spikeObj.Components.Get<EnemySpikeElite>();
			spikeObj.NetworkSpawn( null );
			AddThing( spike );
		}
		else
		{
			var spikeObj = EnemySpikePrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
			var spike = spikeObj.Components.Get<EnemySpike>();
			spikeObj.NetworkSpawn( null );
			AddThing( spike );
		}
	}

	[Authority]
	public void SpawnFire( Vector2 pos, Guid playerId )
	{
		var playerObj = Scene.Directory.FindByGuid( playerId );
		Player player = playerObj?.Components.Get<Player>() ?? null;
		if ( player == null )
			return;

		var fireObj = FirePrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ) );
		var fire = fireObj.Components.Get<Fire>();
		fire.Shooter = player;
		fire.Lifetime = player.Stats[PlayerStat.FireLifetime];

		fireObj.NetworkSpawn( null );
		AddThing( fire );
	}

	public void SpawnBoss( Vector2 pos )
	{
		SpawnEnemy( TypeLibrary.GetType( typeof( Boss ) ), pos, forceSpawn: true );
		PlaySfxNearby( "boss.fanfare", pos, pitch: 1.0f, volume: 1.3f, maxDist: 30f );
	}

	private T GetClosest<T>( IEnumerable<T> enumerable, Vector3 pos, float maxRange, bool ignoreZ, T except )
		where T : Thing
	{
		var dists = ignoreZ
			? enumerable.Select( x => (Thing: x, DistSq: (x.Transform.Position - pos).WithZ( 0f ).LengthSquared) )
			: enumerable.Select( x => (Thing: x, DistSq: (x.Transform.Position - pos).LengthSquared) );

		return dists.OrderBy( x => x.DistSq )
			//.FirstOrDefault( x => x.DistSq <= maxRange * maxRange && x.Thing != except && (!ignoreZ || x.Thing.Parent == null) )
			.FirstOrDefault( x => x.DistSq <= maxRange * maxRange && x.Thing != except )
			.Thing;
	}

	// todo: cache this
	public Player GetClosestPlayer( Vector3 pos, float maxRange = float.PositiveInfinity, bool alive = true, bool ignoreZ = true, Player except = null )
	{
		var players = alive
			? Scene.GetAllComponents<Player>().Where( x => !x.IsDead )
			: Scene.GetAllComponents<Player>();

		return GetClosest( players, pos, maxRange, ignoreZ, except );
	}

	public GridSquare GetGridSquareForPos( Vector2 pos )
	{
		return new GridSquare( (int)MathF.Floor( pos.x ), (int)MathF.Floor( pos.y ) );
	}

	public List<Thing> GetThingsInGridSquare( GridSquare gridSquare )
	{
		if ( ThingGridPositions.ContainsKey( gridSquare ) )
		{
			return ThingGridPositions[gridSquare];
		}

		return null;
	}

	public bool IsGridSquareInArena( GridSquare gridSquare )
	{
		return ThingGridPositions.ContainsKey( gridSquare );
	}

	public void RegisterThingGridSquare( Thing thing, GridSquare gridSquare )
	{
		if ( IsGridSquareInArena( gridSquare ) )
			ThingGridPositions[gridSquare].Add( thing );
	}

	public void DeregisterThingGridSquare( Thing thing, GridSquare gridSquare )
	{
		if ( ThingGridPositions.ContainsKey( gridSquare ) && ThingGridPositions[gridSquare].Contains( thing ) )
		{
			ThingGridPositions[gridSquare].Remove( thing );
		}
	}

	public void AddThing( Thing thing )
	{
		//_things.Add( thing );
		thing.GridPos = GetGridSquareForPos( thing.Position2D );
		RegisterThingGridSquare( thing, thing.GridPos );
	}

	public void RemoveThing( Thing thing )
	{
		if ( ThingGridPositions.ContainsKey( thing.GridPos ) )
			ThingGridPositions[thing.GridPos].Remove( thing );

		if ( thing is Enemy ) // counts Crate too
		{
			EnemyCount--;

			if ( thing is Crate )
				CrateCount--;
		}
		else if ( thing is Coin )
		{
			CoinCount--;
		}
	}

	public void HandleThingCollisionForGridSquare( Thing thing, GridSquare gridSquare, float dt )
	{
		if ( !ThingGridPositions.ContainsKey( gridSquare ) )
			return;

		var things = ThingGridPositions[gridSquare];
		if ( things.Count == 0 )
			return;

		for ( int i = things.Count - 1; i >= 0; i-- )
		{
			if ( i >= things.Count )
				continue;

			if ( thing == null || !thing.IsValid || thing.IsRemoved )
				return;

			var other = things[i];
			if ( other == thing || other.IsRemoved || !other.IsValid )
				continue;

			bool isValidType = false;
			foreach ( var t in thing.CollideWith )
			{
				if ( t.IsAssignableFrom( other.GetType() ) )
				{
					isValidType = true;
					break;
				}
			}

			if ( !isValidType )
				continue;

			var dist_sqr = (thing.Position2D - other.Position2D).LengthSquared;
			var total_radius_sqr = MathF.Pow( thing.Radius + other.Radius, 2f );
			if ( dist_sqr < total_radius_sqr )
			{
				float percent = Utils.Map( dist_sqr, total_radius_sqr, 0f, 0f, 1f );
				thing.Colliding( other, percent, dt * thing.TimeScale );
			}
		}
	}

	public void AddThingsInGridSquare( GridSquare gridSquare, List<Thing> things )
	{
		if ( !ThingGridPositions.ContainsKey( gridSquare ) )
			return;

		things.AddRange( ThingGridPositions[gridSquare] );
	}

	[Authority]
	public void PlayerDied( Player player )
	{
		int numPlayersAlive = Scene.GetAllComponents<Player>().Where( x => !x.IsDead ).Count();
		if ( numPlayersAlive == 0 )
			GameOver();
	}

	public void GameOver()
	{
		if ( IsGameOver )
			return;

		IsGameOver = true;
		IsVictory = false;

		//Sandbox.Services.Stats.Increment( "failures", 1 );
		//Sandbox.Services.Stats.SetValue( "failure-time", ElapsedTime.Relative );
	}

	public void Victory()
	{
		if ( IsGameOver )
			return;

		IsGameOver = true;
		IsVictory = true;

		//Sandbox.Services.Stats.Increment( "victories", 1 );
		//Sandbox.Services.Stats.SetValue( "victory-time", ElapsedTime.Relative );
	}

	public BloodSplatter SpawnBloodSplatter( Vector2 pos )
	{
		var bloodObj = BloodSplatterPrefab.Clone( new Vector3( pos.x, pos.y, Globals.BLOOD_DEPTH ) );
		var bloodSplatter = bloodObj.Components.Get<BloodSplatter>();
		bloodSplatter.Lifetime = Utils.Map( _bloodSplatters.Count, 0, 100, 10f, 1f ) * Game.Random.Float( 0.8f, 1.2f );

		_bloodSplatters.Add( bloodSplatter );
		return bloodSplatter;
	}

	public void RemoveBloodSplatter( BloodSplatter blood )
	{
		if ( _bloodSplatters.Contains( blood ) )
			_bloodSplatters.Remove( blood );
	}

	public Cloud SpawnCloud( Vector2 pos )
	{
		var cloudObj = CloudPrefab.Clone( new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) ), new Angles( 0f, -90f, 0f ) );
		var cloud = cloudObj.Components.Get<Cloud>();
		cloud.Lifetime = 0.7f * Game.Random.Float( 0.8f, 1.2f );

		_clouds.Add( cloud );
		return cloud;
	}

	public void RemoveCloud( Cloud cloud )
	{
		if ( _clouds.Contains( cloud ) )
			_clouds.Remove( cloud );
	}

	public ExplosionEffect SpawnExplosionEffectLocal( Vector2 pos, float scaleModifier = 1f )
	{
		var explosionObj = ExplosionEffectPrefab.Clone( new Vector3( pos.x, pos.y, 100f ) );
		var explosion = explosionObj.Components.Get<ExplosionEffect>();
		explosion.Lifetime = 0.5f;
		explosion.Transform.LocalScale *= scaleModifier;

		_explosions.Add( explosion );
		return explosion;
	}

	public void RemoveExplosionEffect( ExplosionEffect explosion )
	{
		if ( _explosions.Contains( explosion ) )
			_explosions.Remove( explosion );
	}

	[Broadcast]
	public void Restart()
	{
		foreach ( var blood in _bloodSplatters )
			blood.GameObject.Destroy();
		_bloodSplatters.Clear();

		foreach ( var cloud in _clouds )
			cloud.GameObject.Destroy();
		_clouds.Clear();

		foreach ( var explosion in _explosions )
			explosion.GameObject.Destroy();
		_explosions.Clear();

		foreach ( KeyValuePair<GridSquare, List<Thing>> pair in ThingGridPositions )
			pair.Value.Clear();

		EnemyCount = 0;
		CrateCount = 0;
		CoinCount = 0;
		_enemySpawnTime = 0f;
		ElapsedTime = 0f;
		IsGameOver = false;
		HasSpawnedBoss = false;
		Boss = null;
		TimeSinceMagnet = 0f;
		Hud.Instance?.FadeIn();

		Components.Get<PauseMenu>().IsOpen = false;

		if ( IsProxy )
			return;

		foreach ( Thing thing in Scene.GetAllComponents<Thing>() )
		{
			if ( thing is Player player )
				player.Restart();
			else
				thing.DestroyCmd();
		}

		foreach ( var number in Scene.GetAllComponents<LegacyParticleSystem>() )
			number.GameObject.Destroy();

		SpawnStartingThings();
	}

	public void PlayEnemyDeathSfxLocal( Vector3 worldPos )
	{
		if ( _numEnemyDeathSfxs >= 3 )
			return;

		PlaySfxNearby( "enemy.die", worldPos, pitch: Game.Random.Float( 0.85f, 1.15f ), volume: 1f, maxDist: 5.5f );

		_numEnemyDeathSfxs++;
	}

	public void PlaySfxNearby( string name, Vector2 worldPos, float pitch, float volume, float maxDist )
	{
		maxDist *= Globals.SFX_DIST_MODIFIER;

		var player = GetLocalPlayer();
		var playerPos = player.Position2D;

		var distSqr = (player.Position2D - worldPos).LengthSquared;
		if ( distSqr < maxDist * maxDist )
		{
			var dist = (player.Position2D - worldPos).Length;
			var falloff = Utils.Map( dist, 0f, maxDist, 1f, 0f, EasingType.SineIn );
			var pos = playerPos + (worldPos - playerPos) * 0.1f;

			player.PlaySfx( name, pos, pitch * Globals.SFX_PITCH_MODIFIER, volume * falloff );
		}
	}

	public Player GetLocalPlayer()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			if ( player.Network.IsOwner )
				return player;
		}

		return null;
	}
}
