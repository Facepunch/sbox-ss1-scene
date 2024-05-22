using Sandbox.UI;
using SpriteTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public class Thing : Component
{
	[Property] public SpriteComponent Sprite { get; set; }
	[Sync] public float Scale { get; set; }

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

	[Sync] public bool SpriteDirty { get; set; }

	public Vector2 Position2D
	{
		get { return (Vector2)Transform.Position + new Vector2(0f, OffsetY); }
		set { Transform.Position = new Vector3( value.x, value.y - OffsetY, Transform.Position.z ); }
	}

	public Thing()
	{
		TimeScale = 1f;
		SpriteDirty = true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"{GameObject.Name}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.7f, 0f ) ) );

		// todo: optimize?
		UpdateGridPos();

		if( SpriteDirty && ShadowSprite != null )
		{
			ShadowSprite.Size = new Vector2( ShadowScale );

			SpriteDirty = false;
		}

		if(Sprite != null)
		{
			Sprite.Transform.Scale = new Vector3( Scale, Scale, 1f );
		}
	}

	public virtual void Colliding( Thing other, float percent, float dt )
	{

	}

	[Broadcast]
	public virtual void Remove()
	{
		IsRemoved = true;
		Manager.Instance.DeregisterThingGridSquare( this, GridPos );

		if ( IsProxy )
			return;

		GameObject.Destroy();
	}

	[Broadcast]
	public void DestroyCmd()
	{
		if ( IsProxy )
			return;

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

	[Broadcast]
	public void SpawnCloudClient( Vector2 pos, Vector2 vel )
	{
		var cloud = Manager.Instance.SpawnCloud( pos );
		cloud.Velocity = vel;
	}
}
