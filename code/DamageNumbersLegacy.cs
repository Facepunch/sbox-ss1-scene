using Sandbox;
using Sandbox.Utility;
using System.Drawing;

public static class DamageNumbersLegacy
{
	public static void Create( float amount, Vector2 pos, Color color )
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

		var number = amount;

		var ps = LegacyParticle.Create( "particles/damagenumber_ss2/dmg_number_ss2.vpcf", new Vector3(pos.x, pos.y, Globals.DAMAGE_NUMBER_DEPTH), Rotation.Identity );

		var vec21 = Vector3.Zero;
		var vec22 = Vector3.Zero;

		if ( amount < 10 )
		{
			vec21 = vec21.WithX( number % 10 );
		}
		else if ( amount < 100 )
		{
			vec21 = vec21.WithY( number % 10 );
			vec22 = vec22.WithY( 1 );

			number /= 10;
			vec21 = vec21.WithX( MathF.Floor( number % 10 ) );
		}
		else
		{
			vec21 = vec21.WithZ( number % 10 );
			vec22 = vec22.WithZ( 1 );

			number /= 10;
			vec21 = vec21.WithY( MathF.Floor( number % 10 ) );
			vec22 = vec22.WithY( 1 );

			number /= 10;
			vec21 = vec21.WithX( MathF.Floor( number % 100 ) );
			vec22 = vec22.WithX( 1 );
		}

		ps.SetVector( 21, vec21 );
		ps.SetVector( 22, vec22 );

		float size = Utils.Map( amount, 1f, 20f, 0.15f, 0.18f, EasingType.Linear ) * Utils.Map( amount, 20f, 100f, 0.1f, 0.15f, EasingType.Linear ) * 1.6f;
		Vector3 velocity = new Vector3( Game.Random.Float( -1f, 1f ) * 2f, Game.Random.Float( 3.5f, 4.5f ), 0f );
		Vector3 gravity = new Vector3( 0f, -7f, 0f );

		ps.SetNamedValue( "Color", color );
		ps.SetNamedValue( "Size", size );
		ps.SetVector( 1, velocity );
		ps.SetNamedValue( "Gravity", gravity );
	}
}
