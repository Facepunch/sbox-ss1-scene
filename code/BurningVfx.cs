using Sandbox;

public class BurningVfx : Component
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
		bool flip = Sprite.FlipHorizontal = Utils.FastSin( _timeOffset + _timeSince * 4f ) < 0f;
		Sprite.Size = new Vector2( (1f + Utils.FastSin( _timeOffset + _timeSince * 24f ) * 0.1f) * (flip ? -1f : 1f), 1f + Utils.FastSin( _timeOffset + _timeSince * 14f ) * 0.075f );
		Sprite.Color = Color.White.WithAlpha(0.4f + Utils.FastSin( _timeOffset + _timeSince * 20f ) * 0.3f) * Utils.Map( Enemy.DeathProgress, 0f, 1f, 1f, 0f );
	}
}
