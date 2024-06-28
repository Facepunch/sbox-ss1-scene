using SpriteTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PlayerTest : Component
{
	[Property] public SpriteComponent SpriteComponent { get; set; }
	[Property] public GameObject Body { get; set; }

	private bool _isFlipped;

	protected override void OnStart()
	{
		base.OnStart();

		SpriteComponent.OnBroadcastEvent += OnEvent;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (Input.Pressed("Slot1"))
		{
			SpriteComponent.PlayAnimation( "idle" );
			//SpriteComponent.Tint = Color.White.WithAlpha( 1f );
			Body.Transform.LocalRotation = new Angles( 0f, 0f, 0f );
			//Transform.LocalScale = new Vector3( 1f, 1f, 1f );
			SpriteComponent.FlashAmount = 0f;
			_isFlipped = false;
		}
		else if ( Input.Pressed( "Slot2" ) )
		{
			SpriteComponent.PlayAnimation( "walk" );
			Body.Transform.LocalRotation = new Angles( 180f, 0f, 0f );
			//SpriteComponent.Transform.Rotation = new Angles( 180f, 0f, 0f );
			//SpriteComponent.Tint = Color.White.WithAlpha(0.5f);
			//Transform.LocalScale = new Vector3( 1f, -1f, 1f );

			SpriteComponent.FlashAmount = 1f;
			_isFlipped = true;
		}

		SpriteComponent.Tint = Color.White;

		SpriteComponent.Transform.LocalRotation = new Angles( 0f, -90f + (Utils.FastSin( Time.Now * 10f ) * 4f) * (_isFlipped ? -1f : 1f), 0f );
	}

	void OnEvent(string tag)
	{
		Log.Info($"event: {tag}");
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		SpriteComponent.OnBroadcastEvent -= OnEvent;
	}
}
