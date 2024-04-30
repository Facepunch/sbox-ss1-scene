using Sandbox;

public class Player : Component
{
	[Property] public SpriteRenderer Sprite { get; set; }

	public Vector2 Velocity { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;
		var moveSpeed = 50f;
		Velocity = Utils.DynamicEaseTo( Velocity, wishMoveDir * moveSpeed, 0.5f, Time.Delta );
		Transform.Position += (Vector3)Velocity * Time.Delta;

		if( Velocity.x > 0f )
			Sprite.FlipHorizontal = true;
		else if(Velocity.x < 0f)
			Sprite.FlipHorizontal = false;

	}
}
