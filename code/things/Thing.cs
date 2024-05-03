using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class Thing : Component
{
	[Sync] public float Radius { get; set; }
	public float TempWeight { get; set; }
	public GridSquare GridPos { get; set; }
	public bool IsRemoved { get; private set; }
	public List<Type> CollideWith = new List<Type>();
	public float TimeScale { get; set; }

	public float OffsetY { get; set; }
	[Sync] public float ShadowOpacity { get; set; }
	[Sync] public float ShadowScale { get; set; }
	public SpriteRenderer ShadowSprite { get; set; }

	public Vector2 Position2D
	{
		get
		{
			return (Vector2)Transform.Position + new Vector2(0f, OffsetY);
		}
		set
		{
			Transform.Position = new Vector3( value.x, value.y - OffsetY, Transform.Position.z );
		}
	}

	public Thing()
	{
		TimeScale = 1f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// todo: optimize?
		UpdateGridPos();
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

	protected void SpawnShadow( float size, float opacity )
	{
		var shadowObj = Manager.Instance.ShadowPrefab.Clone( Transform.Position );
		shadowObj.SetParent( GameObject );
		shadowObj.Transform.LocalPosition = new Vector3(0f, OffsetY, Globals.SHADOW_DEPTH_OFFSET );
		shadowObj.NetworkMode = NetworkMode.Never;

		ShadowSprite = shadowObj.Components.Get<SpriteRenderer>();
		ShadowSprite.Size = new Vector2( size );
		ShadowSprite.Color = Color.Black.WithAlpha( opacity );
	}

	protected void UpdateGridPos()
	{
		var gridPos = Manager.Instance.GetGridSquareForPos( Position2D );
		if ( gridPos != GridPos )
		{
			Manager.Instance.DeregisterThingGridSquare( this, GridPos );
			Manager.Instance.RegisterThingGridSquare( this, gridPos );
			GridPos = gridPos;
		}
	}
}
