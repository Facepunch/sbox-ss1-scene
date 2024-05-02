using Microsoft.VisualBasic;
using Sandbox;
using Sandbox.Network;
using System.Runtime.InteropServices;
using System.Text;

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject EnemyPrefab { get; set; }
	[Property] public GameObject ShadowPrefab { get; set; }
	[Property] public GameObject CoinPrefab { get; set; }
	[Property] public GameObject MagnetPrefab { get; set; }

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

	public Vector2 MouseWorldPos { get; private set; }

	public bool HasSpawnedBoss { get; private set; }

	public TimeSince TimeSinceMagnet { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;

		BOUNDS_MIN = new Vector2( -16f, -12f );
		BOUNDS_MAX = new Vector2( 16f, 12f );
		BOUNDS_MIN_SPAWN = new Vector2( -15.5f, -11.5f );
		BOUNDS_MAX_SPAWN = new Vector2( 15.5f, 11.5f );

		for ( float x = BOUNDS_MIN.x; x < BOUNDS_MAX.x; x += GRID_SIZE )
		{
			for ( float y = BOUNDS_MIN.y; y < BOUNDS_MAX.y; y += GRID_SIZE )
			{
				ThingGridPositions.Add( GetGridSquareForPos( new Vector2( x, y ) ), new List<Thing>() );
			}
		}
	}

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
			GameNetworkSystem.CreateLobby();

		if ( Networking.IsHost )
			Network.TakeOwnership();

		SpawnMagnet( new Vector2( 3f, 3f ) );
		SpawnCoin( new Vector2( 4f, 4f ) );
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

		var tr = Scene.Trace.Ray( Camera.ScreenPixelToRay( Mouse.Position ), 1500f ).Run();
		if ( tr.Hit )
		{
			MouseWorldPos = (Vector2)tr.HitPosition;
		}

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

		var playerObj = PlayerPrefab.Clone( new Vector3( 0f, 0f, 0f) );
		var player = playerObj.Components.Get<Player>();

		playerObj.NetworkSpawn( channel );
	}

	void HandleEnemySpawn()
	{
		var spawnTime = Utils.Map( EnemyCount, 0, MAX_ENEMY_COUNT, 0.05f, 0.33f, EasingType.QuadOut ) * Utils.Map( ElapsedTime, 0f, 80f, 1.5f, 1f ) * Utils.Map( ElapsedTime, 0f, 250f, 3f, 1f ) * Utils.Map( ElapsedTime, 0f, 900f, 1.2f, 1f );
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
		//TypeDescription type = TypeLibrary.GetType( typeof( Zombie ) );

		//// CRATE
		//if ( CrateCount < MAX_CRATE_COUNT )
		//{
		//	float crateChance = ElapsedTime < 20f ? 0f : Utils.Map( ElapsedTime, 20f, 200f, 0.005f, 0.01f );
		//	float additionalCrateChance = 0f;
		//	foreach ( PlayerCitizen player in AlivePlayers )
		//	{
		//		if ( player.Stats[PlayerStat.CrateChanceAdditional] > 0f )
		//			additionalCrateChance += player.Stats[PlayerStat.CrateChanceAdditional];
		//	}
		//	crateChance *= (1f + additionalCrateChance);

		//	if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < crateChance )
		//		type = TypeLibrary.GetType( typeof( Crate ) );
		//}

		//// EXPLODER
		//float exploderChance = ElapsedTime < 35f ? 0f : Utils.Map( ElapsedTime, 35f, 700f, 0.022f, 0.08f );
		//if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < exploderChance )
		//{
		//	float eliteChance = ElapsedTime < 480f ? 0f : Utils.Map( ElapsedTime, 480f, 1200f, 0.025f, 1f, EasingType.SineIn );
		//	type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( ExploderElite ) ) : TypeLibrary.GetType( typeof( Exploder ) );
		//}

		//// SPITTER
		//float spitterChance = ElapsedTime < 100f ? 0f : Utils.Map( ElapsedTime, 100f, 800f, 0.015f, 0.1f );
		//if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < spitterChance )
		//{
		//	float eliteChance = ElapsedTime < 540f ? 0f : Utils.Map( ElapsedTime, 540f, 1200f, 0.025f, 1f, EasingType.QuadIn );
		//	type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( SpitterElite ) ) : TypeLibrary.GetType( typeof( Spitter ) );
		//}

		//// SPIKER
		//float spikerChance = ElapsedTime < 320f ? 0f : Utils.Map( ElapsedTime, 320f, 800f, 0.018f, 0.1f, EasingType.SineIn );
		//if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < spikerChance )
		//{
		//	float eliteChance = ElapsedTime < 580f ? 0f : Utils.Map( ElapsedTime, 580f, 1300f, 0.008f, 1f, EasingType.SineIn );
		//	type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( SpikerElite ) ) : TypeLibrary.GetType( typeof( Spiker ) );
		//}

		//// CHARGER
		//float chargerChance = ElapsedTime < 420f ? 0f : Utils.Map( ElapsedTime, 420f, 800f, 0.022f, 0.075f );
		//if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < chargerChance )
		//{
		//	float eliteChance = ElapsedTime < 660f ? 0f : Utils.Map( ElapsedTime, 660f, 1400f, 0.008f, 1f, EasingType.SineIn );
		//	type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( ChargerElite ) ) : TypeLibrary.GetType( typeof( Charger ) );
		//}

		//// RUNNER
		//float runnerChance = ElapsedTime < 500f ? 0f : Utils.Map( ElapsedTime, 500f, 900f, 0.035f, 0.15f, EasingType.QuadIn );
		//if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < runnerChance )
		//{
		//	float eliteChance = ElapsedTime < 720f ? 0f : Utils.Map( ElapsedTime, 720f, 1500f, 0.01f, 1f, EasingType.QuadIn );
		//	type = Game.Random.Float( 0f, 1f ) < eliteChance ? TypeLibrary.GetType( typeof( RunnerElite ) ) : TypeLibrary.GetType( typeof( Runner ) );
		//}

		//// ZOMBIE ELITE
		//var zombieEliteChance = ElapsedTime < 400f ? 0f : Utils.Map( ElapsedTime, 400f, 1200f, 0.0175f, 1f, EasingType.SineIn );
		//if ( type == TypeLibrary.GetType( typeof( Zombie ) ) && Game.Random.Float( 0f, 1f ) < zombieEliteChance )
		//{
		//	type = TypeLibrary.GetType( typeof( ZombieElite ) );
		//}

		//type = Game.Random.Int(0, 2) == 0 ? TypeLibrary.GetType(typeof(RunnerElite)) : TypeLibrary.GetType(typeof(Runner));

		//SpawnEnemy( type, pos );
		SpawnEnemy( pos );
	}

	//void SpawnEnemy( TypeDescription type, Vector2 pos, bool forceSpawn = false )
	void SpawnEnemy( Vector2 pos, bool forceSpawn = false )
	{
		if ( EnemyCount >= MAX_ENEMY_COUNT && !forceSpawn )
			return;

		var enemyObj = EnemyPrefab.Clone( new Vector3( pos.x, pos.y, 0f ) );
		enemyObj.Name = "zombie";
		var enemy = enemyObj.Components.Create<Zombie>();

		//var enemy = type.Create<Enemy>();

		//var closestPlayer = GetClosestPlayer( pos );
		//if ( closestPlayer?.Position2D.x > pos.x )
		//	enemy.Scale = new Vector2( -1f, 1f ) * enemy.ScaleFactor;

		AddThing( enemy );
		EnemyCount++;

		//if ( type == TypeLibrary.GetType( typeof( Crate ) ) )
		//	CrateCount++;

		//PlaySfxNearby( "zombie.dirt", pos, pitch: Game.Random.Float( 0.6f, 0.8f ), volume: 0.7f, maxDist: 7.5f );

		enemyObj.NetworkSpawn();
	}

	public Coin SpawnCoin( Vector2 pos, int value = 1 )
	{
		// todo: spawn larger amounts less often if reaching max coin cap
		if ( CoinCount >= MAX_COIN_COUNT )
			return null;

		var coinObj = CoinPrefab.Clone( new Vector3( pos.x, pos.y, 0f ) );
		var coin = coinObj.Components.Get<Coin>();
		coin.SetValue( value );

		AddThing( coin );
		CoinCount++;

		return coin;
	}

	public Magnet SpawnMagnet( Vector2 pos )
	{
		var magnetObj = MagnetPrefab.Clone( new Vector3( pos.x, pos.y, 0f ) );
		var magnet = magnetObj.Components.Get<Magnet>();

		AddThing( magnet );

		return magnet;
	}

	public void SpawnBoss( Vector2 pos )
	{
		//SpawnEnemy( TypeLibrary.GetType( typeof( Boss ) ), pos, forceSpawn: true );
		//PlaySfxNearby( "boss.fanfare", pos, pitch: 1.0f, volume: 1.3f, maxDist: 30f );
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
		{
			ThingGridPositions[thing.GridPos].Remove( thing );
		}

		//if ( thing is Enemy ) // counts Crate too
		//{
		//	EnemyCount--;

		//	if ( thing is Crate )
		//		CrateCount--;
		//}
		//else if ( thing is Coin )
		//{
		//	CoinCount--;
		//}
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
			//Log.Info("!!! " + thing.Name + " --- " + i.ToString() + " count: " + things.Count);

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
				//thing.Velocity += (thing.Position - other.Position).Normal * Utils.Map(dist_sqr, total_radius_sqr, 0f, 0f, 10f) * (1f + other.TempWeight) * dt;
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

	public void PlayerDied( Player player )
	{
		//int numPlayersAlive = Players.Where( x => !x.IsDead ).Count();
		//if ( numPlayersAlive == 0 )
		//	GameOver();
	}

	public void GameOver()
	{
		if ( IsGameOver )
			return;

		IsGameOver = true;
		//GameOverClient();
	}
}
