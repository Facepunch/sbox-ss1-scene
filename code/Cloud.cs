using Sandbox.UI;
using SpriteTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class Cloud : Component
{
	[Property] public SpriteComponent Sprite { get; set; }

	private TimeSince _spawnTime;
	public float Lifetime { get; set; }
	public Vector2 Velocity { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Transform.LocalRotation = new Angles( 0f, -90f, 0f );
		Transform.LocalScale = new Vector3( Game.Random.Float( 0.65f, 0.75f ), Game.Random.Float( 0.7f, 0.9f ), 1f ) * Globals.SPRITE_SCALE;
		_spawnTime = 0f;

		if ( Game.Random.Float( 0f, 1f ) < 0.5f )
			Sprite.SpriteFlags = SpriteFlags.HorizontalFlip;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Transform.Position += (Vector3)Velocity * Time.Delta;
		Velocity *= (1f - Time.Delta * 1.5f);
		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Transform.Position.y ) );

		var opacity = Utils.Map( _spawnTime, 0f, 0.2f, 0f, 1f ) * Utils.Map( _spawnTime, 0f, Lifetime - 0.03f, 1f, 0f );
		Sprite.Tint = Color.White.WithAlpha( opacity );

		if ( _spawnTime > Lifetime )
		{
			Manager.Instance.RemoveCloud( this );
			GameObject.Destroy();
		}
	}
}
