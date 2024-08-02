global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;

public class Globals
{
	public const float SHADOW_DEPTH_OFFSET = -200f;
	public const float BLOOD_DEPTH = -400f;
	public const float DAMAGE_NUMBER_DEPTH = 200f;

	public const float SFX_DEPTH = 100f;
	public const float SFX_DIST_MODIFIER = 1.3f;
	public const float SFX_PITCH_MODIFIER = 0.775f;

	public const float SPRITE_SCALE = 0.01f;

	public static float GetZPos( float yPos )
	{
		return -yPos * 10f;
	}

	private static List<Color> _nameColors = new() {
		new Color( 1f, 1f, 1f ),
		new Color( 1f, 0.5f, 0.5f ),
		new Color(0.5f, 0.5f, 1f ),
		new Color( 1f, 1f, 0.5f )
	};
}
