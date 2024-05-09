using Sandbox;

public class FloatingDamageNumber : Component
{
	private RealTimeSince _timeSince;

	protected override void OnAwake()
	{
		base.OnAwake();

		_timeSince = 0f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( _timeSince > 1f )
			GameObject.Destroy();
	}
}
