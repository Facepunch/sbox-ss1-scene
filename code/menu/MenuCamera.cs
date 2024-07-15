using Sandbox;

public class MenuCamera : Component
{
	public Vector3 Velocity { get; private set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		Velocity += new Vector3( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ), 0f ) * 50f * Time.Delta;
		Velocity *= (1f - 2f * Time.Delta);

		Transform.Position += Velocity * Time.Delta;
		Transform.Position = Vector3.Lerp( Transform.Position, new Vector3( 0f, 0f, 250f ), 1f * Time.Delta );

		//Transform.Position = new Vector3( 5f * Utils.Map(Mouse.Position.x, 0f, Screen.Width, -1f, 1f), Utils.Map( Mouse.Position.y, 0f, Screen.Height, 2f, -4f), 250f);
	}
}
