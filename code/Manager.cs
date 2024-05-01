using Microsoft.VisualBasic;
using Sandbox;
using Sandbox.Network;

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }

	[Property] public CameraComponent Camera { get; private set; }
	[Property] public Camera2D Camera2D { get; set; }

	public record struct GridSquare( int x, int y );
	public Dictionary<GridSquare, List<Thing>> ThingGridPositions = new Dictionary<GridSquare, List<Thing>>();

	public float GRID_SIZE = 100f;
	public Vector2 BOUNDS_MIN;
	public Vector2 BOUNDS_MAX;
	public Vector2 BOUNDS_MIN_SPAWN;
	public Vector2 BOUNDS_MAX_SPAWN;

	private TimeSince _enemySpawnTime;
	[Sync] public TimeSince ElapsedTime { get; set; }

	[Sync] public bool IsGameOver { get; private set; }

	public Vector2 MouseWorldPos { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;

		BOUNDS_MIN = new Vector2( -610f, -474f );
		BOUNDS_MAX = new Vector2( 610f, 474f );
		BOUNDS_MIN_SPAWN = BOUNDS_MIN * 0.9f;
		BOUNDS_MAX_SPAWN = BOUNDS_MAX * 0.9f;

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

		var tr = Scene.Trace.Ray( Camera.ScreenPixelToRay( Mouse.Position ), 1000f ).Run();
		if ( tr.Hit )
		{
			MouseWorldPos = (Vector2)tr.HitPosition;
		}
	}

	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is becoming active (local = {channel == Connection.Local}) (host = {channel.IsHost})" );

		var playerObj = PlayerPrefab.Clone( new Vector3( 0f, 0f, 0f) );
		var player = playerObj.Components.Get<Player>();

		playerObj.NetworkSpawn( channel );
	}

	public GridSquare GetGridSquareForPos( Vector2 pos )
	{
		return new GridSquare( (int)MathF.Floor( pos.x / GRID_SIZE ), (int)MathF.Floor( pos.y / GRID_SIZE ) );
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
