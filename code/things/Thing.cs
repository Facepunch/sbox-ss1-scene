using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class Thing : Component
{
	public Vector2 Velocity { get; set; }
	[Sync] public float Radius { get; set; }
	public float TempWeight { get; set; }
	public GridSquare GridPos { get; set; }
	public bool IsRemoved { get; private set; }
	public List<Type> CollideWith = new List<Type>();
	public float TimeScale { get; set; }

	public Vector2 Position2D => (Vector2)Transform.Position;

	public Thing()
	{
		TimeScale = 1f;
	}

	public virtual void Update( float dt )
	{

	}

	public virtual void Colliding( Thing other, float percent, float dt )
	{

	}

	public virtual void Remove()
	{
		IsRemoved = true;
		//Game.RemoveThing( this );
		GameObject.Destroy();
	}
}
