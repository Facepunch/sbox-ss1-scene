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

	public float ShadowOpacity { get; set; }
	public float ShadowScale { get; set; }
	public SpriteComponent ShadowSprite { get; set; }

	[Sync] public bool SpriteDirty { get; set; }

	public Vector2 Position2D
	{
		get { return (Vector2)Transform.Position; }
		set { Transform.Position = new Vector3( value.x, value.y, Transform.Position.z ); }
	}

	public Thing()
	{
		TimeScale = 1f;
		SpriteDirty = true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Gizmo.Draw.Color = Color.Black.WithAlpha(0.2f);
		//Gizmo.Draw.Text( $"{GameObject.Name}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.2f, 0f ) ) );

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.01f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		// todo: optimize?
		UpdateGridPos();

		if ( Sprite != null && SpriteDirty && ShadowSprite != null )
		{
			//Sprite.Size = new Vector2( Scale );
			ShadowSprite.Transform.LocalScale = new Vector3( ShadowScale * Globals.SPRITE_SCALE, ShadowScale * Globals.SPRITE_SCALE, 1f );
			ShadowSprite.Tint = Color.Black.WithAlpha( ShadowOpacity );

			SpriteDirty = false;
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

		Manager.Instance.RemoveThing( this );

		GameObject.Destroy();
	}

	[Authority]
	public void DestroyCmd()
	{
		GameObject.Destroy();
	}

	protected void SpawnShadow( float size, float opacity )
	{
		if ( ShadowSprite.IsValid() )
		{
			ShadowSprite.GameObject.Destroy();
		}

		var shadowObj = Manager.Instance.ShadowPrefab.Clone( Transform.Position );
		shadowObj.SetParent( GameObject );
		shadowObj.Transform.LocalPosition = new Vector3( 0f, 0f, Globals.SHADOW_DEPTH_OFFSET );
		shadowObj.Transform.LocalRotation = new Angles( 0f, -90f, 0f );
		shadowObj.NetworkMode = NetworkMode.Never;

		ShadowSprite = shadowObj.Components.Get<SpriteComponent>();
		ShadowSprite.Transform.LocalScale = new Vector3( size * Globals.SPRITE_SCALE, size * Globals.SPRITE_SCALE, 1f );
		ShadowSprite.Tint = Color.Black.WithAlpha( opacity );
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
