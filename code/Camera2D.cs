using Sandbox;

public class Camera2D : Component
{
	public Vector2 TargetPos { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Vector2 newPos = Vector2.Lerp( (Vector2)Transform.Position, TargetPos, 0.075f );
		var XDIST = 10.75f;
		var Y_MIN = -8.3f;
		var Y_MAX = 8.9f;
		newPos = new Vector2( MathX.Clamp( newPos.x, -XDIST, XDIST ), MathX.Clamp( newPos.y, Y_MIN, Y_MAX ) );

		Transform.Position = ((Vector3)newPos).WithZ( Transform.Position.z );
	}

	public void SetPos( Vector2 pos )
	{
		Transform.Position = ((Vector3)pos).WithZ( Transform.Position.z );
		TargetPos = pos;
	}
}
