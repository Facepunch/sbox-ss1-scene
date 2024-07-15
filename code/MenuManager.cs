using Battlebugs;
using Sandbox;
using Sandbox.Network;

public sealed class MenuManager : Component, Component.INetworkListener
{
	public static MenuManager Instance { get; private set; }

	[Property] public GameObject MenuZombiePrefab { get; set; }

	[Sync] public TimeSince ElapsedTime { get; set; }

	private TimeSince _timeSinceEnemySpawn;
	private float _nextSpawnDelay;

	public const float Y_MIN = -60f;
	public const float Y_MAX = 15f;
	public const float Z_NEAR = 0f;
	public const float Z_FAR = -100f;

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;

		ElapsedTime = 0f;

		_timeSinceEnemySpawn = 0f;
		_nextSpawnDelay = 0.01f;

		if ( IsProxy )
			return;
	}

	protected override void OnStart()
	{
		for ( int i = 0; i < Game.Random.Int(3, 15); i++ )
			SpawnRandomStartingEnemy();
	}

	protected override void OnUpdate()
	{
		if( _timeSinceEnemySpawn > _nextSpawnDelay )
		{
			SpawnRandomEnemy();

			_timeSinceEnemySpawn = 0f;
			_nextSpawnDelay = Game.Random.Float( 0.1f, 2f );
		}	
	}

	void SpawnRandomStartingEnemy()
	{
		bool flipped = Game.Random.Int( 0, 1 ) == 0;
		float y = GetRandomYPos();
		float x = Game.Random.Float( -80f, 80f );
		SpawnEnemy( TypeLibrary.GetType( typeof( MenuZombie ) ), new Vector3( x, y, 0f ), flipped);
	}

	void SpawnRandomEnemy()
	{
		bool flipped = Game.Random.Int( 0, 1 ) == 0;
		float y = GetRandomYPos();
		float x = Game.Random.Float( 100f, 120f ) * Utils.Map( y, Y_MIN, Y_MAX, 1.5f, 1f ) * (flipped ? -1f : 1f);
		SpawnEnemy( TypeLibrary.GetType( typeof( MenuZombie ) ), new Vector3( x, y, 0f ), flipped);

		var sfx = Sound.Play( "zombie.dirt", new Vector3(x, y, 100f) );
		if ( sfx != null )
		{
			sfx.Volume = Utils.Map( y, MenuManager.Y_MIN, MenuManager.Y_MAX, 0.7f, 0.2f ) * Utils.Map( ElapsedTime, 0f, 1.25f, 0f, 1f);
			sfx.Pitch = Game.Random.Float( 0.6f, 0.8f );
		}
	}

	public float GetRandomYPos()
	{
		return Utils.Map( Game.Random.Float( 0f, 1f ), 0f, 1f, Y_MAX, Y_MIN, EasingType.QuartIn ); 
	}

	void SpawnEnemy( TypeDescription type, Vector3 pos, bool flipped )
	{
		GameObject enemyObj = null;
		MenuEnemy enemy;
		if ( type == TypeLibrary.GetType( typeof( MenuZombie ) ) ) { enemyObj = MenuZombiePrefab.Clone( pos ); }

		enemy = enemyObj.Components.Get<MenuEnemy>();
		enemy.Transform.Position = pos;
		enemyObj.Name = type.ToString();

		enemy.Sprite.SpriteFlags = flipped ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		enemy.IsFlipped = flipped;
		enemy.SpeedModifier = Game.Random.Float( 4f, 5f ) * Utils.Map(pos.y, Y_MIN, Y_MAX, 1f, 0.1f);
	}
}
