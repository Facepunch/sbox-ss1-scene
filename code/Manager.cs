using Sandbox;
using Sandbox.Network;

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }

	[Property] public Camera2D Camera2D { get; set; }

	public record struct GridSquare( int x, int y );
	public Dictionary<GridSquare, List<Thing>> ThingGridPositions = new Dictionary<GridSquare, List<Thing>>();

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;
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
		return new GridSquare( (int)MathF.Floor( pos.x ), (int)MathF.Floor( pos.y ) );
	}
}
