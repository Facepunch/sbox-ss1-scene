using Sandbox;

public class Camera2D : Component
{
	public Vector2 TargetPos { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Vector2 newPos = Vector2.Lerp( (Vector2)Transform.Position, TargetPos, 0.075f );
		var XDIST = 216f;
		var YDIST = 280f;
		newPos = new Vector2( MathX.Clamp( newPos.x, -XDIST, XDIST ), MathX.Clamp( newPos.y, -YDIST, YDIST ) );


		Transform.Position = ((Vector3)newPos).WithZ( Transform.Position.z );
	}
}
