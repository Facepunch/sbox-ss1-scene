using Sandbox;
using SpriteTools;

public class ShieldVfx : Component
{
	[Property] public SpriteComponent Sprite { get; set; }

	private TimeSince _timeSince;
	private float _timeOffset;

	protected override void OnAwake()
	{
		base.OnAwake();

		_timeSince = 0f;
		_timeOffset = Game.Random.Float( 0f, 10f );

		Sprite.Transform.LocalScale = new Vector3( 1f ) * Globals.SPRITE_SCALE;
		Sprite.Tint = Color.White.WithAlpha( 0f );
	}

	protected override void OnUpdate()
	{
		bool flip = Utils.FastSin( _timeOffset + _timeSince * 4f ) < 0f;
		Sprite.SpriteFlags = flip ? SpriteFlags.HorizontalFlip : SpriteFlags.None;

		Sprite.Transform.LocalPosition = new Vector3( 0f, -0.01f + Utils.FastSin( _timeOffset + _timeSince * 4f) * 0.02f, 0.1f );
		Sprite.Transform.LocalScale = new Vector3( (1f + Utils.FastSin( _timeOffset + _timeSince * 5f ) * 0.03f), 1f + Utils.FastSin( _timeOffset + _timeSince * 7f ) * 0.07f, 1f ) * 1.8f * Globals.SPRITE_SCALE;
		Sprite.Tint = Color.Yellow.WithAlpha(0.6f + Utils.FastSin( _timeOffset + _timeSince * 12f ) * 0.4f);
	}
}
