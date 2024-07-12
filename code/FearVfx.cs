using Sandbox;
using SpriteTools;

public sealed class FearVfx : Component
{
	[Property] public SpriteComponent Sprite { get; set; }

	public Enemy Enemy { get; set; }

	private TimeSince _timeSince;
	private float _timeOffset;

	protected override void OnAwake()
	{
		base.OnAwake();

		_timeSince = 0f;
		_timeOffset = Game.Random.Float( 0f, 10f );

		Transform.LocalRotation = new Angles( 0f, -90f, 0f );
		Transform.LocalPosition = new Vector3( 0f, 0f, 0.1f );
	}

	protected override void OnUpdate()
	{
		bool flip = Utils.FastSin( _timeOffset + _timeSince * 3f ) < 0f;
		Sprite.SpriteFlags = flip ? SpriteFlags.HorizontalFlip : SpriteFlags.None;

		float scaleFactor = Utils.Map( Enemy.Scale, 0.85f, 2.5f, 0.9f, 1.9f );
		Sprite.Transform.LocalScale = new Vector3( (1f + Utils.FastSin( _timeOffset + _timeSince * 18f ) * 0.03f), 1f + Utils.FastSin( _timeOffset + _timeSince * 13f ) * 0.025f, 1f ) * scaleFactor * Globals.SPRITE_SCALE;
		Sprite.Tint = Color.White.WithAlpha( 0.9f + Utils.FastSin( _timeOffset + _timeSince * 15f ) * 0.1f ) * Utils.Map( Enemy.DeathProgress, 0f, 1f, 1f, 0f );
	}
}
