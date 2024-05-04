using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class BloodSplatter : Component
{
	[Property] public SpriteRenderer Sprite { get; set; }

	private TimeSince _spawnTime;
	public float Lifetime { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Sprite.FlipHorizontal = Game.Random.Float( 0f, 1f ) < 0.5f;
		Sprite.Size = new Vector2( Game.Random.Float( 0.4f, 0.6f ), Game.Random.Float( 0.15f, 0.25f ) );
		_spawnTime = 0f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var opacity = Utils.Map( _spawnTime, 0f, 0.14f, 0f, 1f ) * Utils.Map( _spawnTime, 0f, Lifetime - 0.02f, 1f, 0f );
		Sprite.Color = Color.White.WithAlpha( opacity );

		if ( _spawnTime > Lifetime )
		{
			Manager.Instance.RemoveBloodSplatter( this );
			GameObject.Destroy();
		}
	}
}
