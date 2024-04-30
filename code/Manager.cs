using Sandbox;
using Sandbox.Network;

public sealed class Manager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

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
}
