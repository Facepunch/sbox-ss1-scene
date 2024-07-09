using Sandbox;
using SpriteTools;

public sealed class ExplosionEffect : Component
{
	[Property] public SpriteComponent Sprite { get; set; }

	private TimeSince _spawnTime;
	public float Lifetime { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Transform.LocalScale = new Vector3( 1f, 0.95f, 1f ) * 2.85f * Globals.SPRITE_SCALE;
		//Sprite.Size = new Vector2( 1f, 0.95f ) * 2.85f;
		_spawnTime = 0f;
		Sprite.Tint = Color.Red.WithAlpha( 0.8f );
		Sprite.PlaybackSpeed = 1.5f;
	}

	protected override void OnUpdate()
	{
		Sprite.Tint = Color.Lerp(Color.Yellow, Color.Red, Utils.Map( _spawnTime, 0f, Lifetime, 0f, 1f, EasingType.QuartOut ) ).WithAlpha( Utils.Map( _spawnTime, 0f, Lifetime, 0.8f, 0f, EasingType.Linear ) );

		if ( _spawnTime > Lifetime )
		{
			Manager.Instance.RemoveExplosionEffect( this );
			GameObject.Destroy();
		}
	}
}
