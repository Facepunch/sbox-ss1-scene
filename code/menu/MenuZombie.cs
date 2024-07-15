using SpriteTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MenuZombie : MenuEnemy
{
	public override float HeightVariance => 0.065f;
	public override float WidthVariance => 0.035f;

	protected override void OnAwake()
	{
		base.OnAwake();
		
	}

	protected override void OnStart()
	{
		base.OnStart();


	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		float animSpeed = Utils.Map( Utils.FastSin( AnimSpeedOffset + Time.Now * 7.5f ), -1f, 1f, 0.75f, 3f, EasingType.ExpoIn );
		Sprite.PlaybackSpeed = animSpeed;

		Velocity += new Vector3( IsFlipped ? 1f : -1f, 0f, 0f ) * Time.Delta;

		float speed = (IsAttacking ? 1.3f : 0.7f) + Utils.FastSin( AnimSpeedOffset + Time.Now * (IsAttacking ? 15f : 7.5f) ) * (IsAttacking ? 0.66f : 0.35f);
		Transform.Position += Velocity * speed * SpeedModifier * Time.Delta;

		if ( Math.Abs( Transform.Position.x ) > 150f )
		{
			GameObject.Destroy();
			return;
		}
	}
}
