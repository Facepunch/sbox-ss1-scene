using Sandbox;
using Sandbox.Utility;
using System.Drawing;

public class DamageNumberData
{
	public int amount;
	public Vector2 pos;
	public RealTimeSince timeSince;
	public Color color;
	public Vector2 velocity;
}

public class DamageNumbers : Component
{
	[RequireComponent] public CameraComponent Camera { get; set; }

	IDisposable renderHook;

	protected override void OnEnabled()
	{
		renderHook?.Dispose();

		renderHook = Camera.AddHookAfterUI( "DamageNumbers", 700, RenderEffect );
	}

	protected override void OnDisabled()
	{
		renderHook?.Dispose();
		renderHook = null;
	}

	static List<DamageNumberData> Numbers = new();

	public static void Add( float amount, Vector2 pos, Color color )
	{
		if ( amount < 1f )
		{
			amount = MathF.Ceiling( amount );
		}
		else
		{
			float fractional = amount - MathF.Floor( amount );
			if ( fractional > 0f && Game.Random.Float( 0f, 1f ) > fractional )
				amount = MathF.Floor( amount );
			else
				amount = MathF.Ceiling( amount );
		}

		Numbers.Add( new DamageNumberData() { 
			amount = (int)amount, 
			pos = pos, 
			timeSince = 0,
			color = color,
			velocity = new Vector2(Game.Random.Float(-3f, 3f), Game.Random.Float(7f, 10f)),
		} );
	}

	protected override void OnUpdate()
	{
		var dt = Time.Delta;

		for ( int i = Numbers.Count - 1; i >= 0; i-- )
		{
			var data = Numbers[i];
			if ( data.timeSince >= TimeToLive )
			{
				Numbers.RemoveAt( i );
			}
			else
			{
				data.pos += (data.velocity + Vector2.Down * 3f) * dt;
				data.velocity *= (1f - dt * 4f);
			}
		}
	}

	public const float TimeToLive = 1f;
	//public float RiseAmount => 64.0f;

	public void RenderEffect( SceneCamera camera )
	{
		foreach ( var data in Numbers )
		{
			var screenPos = camera.ToScreen( (Vector3)data.pos ) + new Vector2( -60f, -60f );

			// Piss around with different Easing methods
			//var ease = Easing.ExpoOut( data.timeSince / TimeToLive );
			//screenPos += Vector2.Up * ease * -64f + new Vector2(-60f, -60f);

			var color = data.color.WithAlpha( 1.0f - Easing.EaseInOut( data.timeSince / TimeToLive ) );
			float fontSize = Utils.Map( data.amount, 1f, 20f, 32f, 40f) * Utils.Map( data.amount, 20f, 150f, 1f, 1.4f, EasingType.QuadIn );


			var bgColor = Color.Black.WithAlpha( 1.0f - Easing.EaseInOut( data.timeSince / TimeToLive ) );
			Graphics.DrawText( new Rect( screenPos + new Vector2(-3f, 3f), 120f ), data.amount.ToString(), bgColor, "Poppins", (int)fontSize, 800, TextFlag.Center );

			Graphics.DrawText( new Rect( screenPos, 120f ), data.amount.ToString(), color, "Poppins", (int)fontSize, 800, TextFlag.Center );
		}
	}
}
