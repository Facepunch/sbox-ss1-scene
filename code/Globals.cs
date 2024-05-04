global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;

public class Globals
{
	public const float SHADOW_DEPTH_OFFSET = -200f;
	public const float BLOOD_DEPTH = -400f;

	public static float GetZPos( float yPos )
	{
		return -yPos * 10f;
	}
}
