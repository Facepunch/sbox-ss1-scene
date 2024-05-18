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

	public static float GetZPos( float yPos )
	{
		return -yPos * 10f;
	}
}
