using Sandbox;

public sealed class FrozenVfx : Component
{
	[Property] public SpriteRenderer Sprite { get; set; }

	public Enemy Enemy { get; set; }

	private TimeSince _timeSince;
	private float _timeOffset;

	protected override void OnAwake()
	{
		base.OnAwake();

		_timeSince = 0f;
		_timeOffset = Game.Random.Float( 0f, 10f );
	}

	protected override void OnUpdate()
	{
		bool flip = Sprite.FlipHorizontal = Utils.FastSin( _timeOffset + _timeSince * 6f ) < 0f;
		Sprite.Size = new Vector2( (1f + Utils.FastSin( _timeOffset + _timeSince * 18f ) * 0.05f) * (flip ? -1f : 1f), 1f + Utils.FastSin( _timeOffset + _timeSince * 13f ) * 0.035f );
		Sprite.Color = Color.White.WithAlpha( 0.8f + Utils.FastSin( _timeOffset + _timeSince * 20f ) * 0.2f ) * Utils.Map( Enemy.DeathProgress, 0f, 1f, 1f, 0f );
	}
}
