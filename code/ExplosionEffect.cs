using Sandbox;

public sealed class ExplosionEffect : Component
{
	[Property] public SpriteRenderer Sprite { get; set; }
	private TimeSince _spawnTime;
	public float Lifetime { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Sprite.Size = new Vector2( 1f, 0.95f ) * 2.85f;
		_spawnTime = 0f;
		Sprite.Color = Color.Red.WithAlpha( 0.8f );
	}

	protected override void OnUpdate()
	{
		Sprite.Color = Color.Red.WithAlpha( Utils.Map( _spawnTime, 0f, Lifetime, 0.8f, 0f ) );

		if ( _spawnTime > Lifetime )
		{
			Manager.Instance.RemoveExplosionEffect( this );
			GameObject.Destroy();
		}
	}
}
