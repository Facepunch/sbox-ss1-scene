using Sandbox.UI;
using SpriteTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class BloodSplatter : Component
{
	[Property] public SpriteComponent Sprite { get; set; }

	private TimeSince _spawnTime;
	public float Lifetime { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Sprite.PlayAnimation( $"blood_splatter_{Game.Random.Int( 0, 3 )}" );
		Sprite.PlaybackSpeed = Sandbox.Game.Random.Float( 4f, 6.5f );

		if ( Game.Random.Float( 0f, 1f ) < 0.5f )
			Sprite.SpriteFlags = SpriteFlags.HorizontalFlip;

		Transform.LocalRotation = new Angles( 0f, -90f, 0f );
		//Transform.LocalScale = new Vector3( Game.Random.Float( 0.4f, 0.6f ), Game.Random.Float( 0.15f, 0.25f ), 1f ) * Globals.SPRITE_SCALE;
		Transform.LocalScale = new Vector3( Game.Random.Float( 0.33f, 0.48f ), Game.Random.Float( 0.4f, 0.63f ), 1f ) * 2.2f * Globals.SPRITE_SCALE;

		_spawnTime = 0f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var opacity = Utils.Map( _spawnTime, 0f, 0.14f, 0f, 1f ) * Utils.Map( _spawnTime, 0f, Lifetime - 0.02f, 1f, 0f );
		Sprite.Tint = Color.White.WithAlpha( opacity );

		if ( _spawnTime > Lifetime )
		{
			Manager.Instance.RemoveBloodSplatter( this );
			GameObject.Destroy();
		}
	}
}
