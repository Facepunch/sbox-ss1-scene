using SpriteTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MenuEnemy : Component
{
	[Property] public SpriteComponent Sprite { get; set; }


	public Vector3 Velocity { get; set; }

	public bool IsAttacking { get; set; }

	public bool IsFlipped { get; set; }

	public float SpeedModifier { get; set; }

	float _heightMod;
	float _widthMod;

	public virtual float HeightVariance => 0f;
	public virtual float WidthVariance => 0f;

	private TimeSince _timeSinceSpawn;

	private float _angerThreshold;
	private float _angerChangeSpeed;

	public float AnimSpeedOffset = Game.Random.Float( 0f, 99f );

	protected override void OnAwake()
	{
		base.OnAwake();

		AnimSpeedOffset = Game.Random.Float( 0f, 99f );

		_heightMod = Game.Random.Float( 1f - HeightVariance, 1f + HeightVariance );
		_widthMod = Game.Random.Float( 1f - WidthVariance, 1f + WidthVariance );

		//Sprite.Tint = Color.White.WithAlpha( 0f );

		_timeSinceSpawn = 0f;

		_angerThreshold = Utils.Map(Game.Random.Float( 0f, 1f ), 0f, 1f, 0f, 1f, EasingType.QuadIn);
		_angerChangeSpeed = Game.Random.Float( 0.2f, 0.8f );

		Transform.LocalScale = Vector3.Zero;
	}

	protected override void OnStart()
	{
		base.OnStart();

		Transform.LocalRotation = new Angles( 0f, -90f, 0f );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Sprite.Tint = Color.White.WithAlpha( Utils.Map( _timeSinceSpawn, 0f, 1f, 0f, 1f ) );

		Transform.Position = Transform.Position.WithZ( Utils.Map( Transform.Position.y, MenuManager.Y_MIN, MenuManager.Y_MAX, MenuManager.Z_NEAR, MenuManager.Z_FAR ) );

		var shouldAttack = _timeSinceSpawn > 0.5f && ( 0.5f + Utils.FastSin( AnimSpeedOffset + Time.Now * _angerChangeSpeed )) > _angerThreshold;

		if ( !IsAttacking && shouldAttack )
		{
			var sfx = Sound.Play( "zombie.spawn0", Transform.Position.WithZ(100f) );
			if ( sfx != null )
			{
				sfx.Volume = Utils.Map(Transform.Position.y, MenuManager.Y_MIN, MenuManager.Y_MAX, 0.9f, 0.25f) * Utils.Map(MenuManager.Instance.ElapsedTime, 0f, 1.2f, 0f, 1f);
				sfx.Pitch = Game.Random.Float( 0.9f, 1.1f );
			}
		}

		IsAttacking = shouldAttack;
		Sprite.PlayAnimation( IsAttacking ? "attack" : "walk" );

		var scale = Utils.Map( Transform.Position.z, MenuManager.Z_NEAR, MenuManager.Z_FAR, 1f, 0.1f );
		Transform.LocalScale = new Vector3( scale * _heightMod, scale * _widthMod, 1f );

		Sprite.FlashTint = Color.Black.WithAlpha( Utils.Map( Transform.Position.z, MenuManager.Z_NEAR, MenuManager.Z_FAR, 0f, 0.998f, EasingType.QuadOut ) );
		Sprite.SpriteFlags = IsFlipped ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
	}
}
