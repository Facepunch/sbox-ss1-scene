using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class Cloud : Component
{
	[Property] public SpriteRenderer Sprite { get; set; }

	private TimeSince _spawnTime;
	public float Lifetime { get; set; }
	public Vector2 Velocity { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Sprite.FlipHorizontal = Game.Random.Float( 0f, 1f ) < 0.5f;
		Sprite.Size = new Vector2( Game.Random.Float( 0.7f, 0.95f ), Game.Random.Float( 0.5f, 0.65f ) ) * 0.55f;
		_spawnTime = 0f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Transform.Position += (Vector3)Velocity * Time.Delta;
		Velocity *= (1f - Time.Delta * 1.5f);
		Transform.Position = Transform.Position.WithZ( -Transform.Position.y * 10f );

		Sprite.Color = Color.White.WithAlpha( Utils.Map( _spawnTime, 0f, Lifetime, 1f, 0f ) );

		if ( _spawnTime > Lifetime )
		{
			Manager.Instance.RemoveCloud( this );
			GameObject.Destroy();
		}
	}
}
