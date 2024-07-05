using Editor;
using Sandbox;
using System.Threading;
using System.Threading.Tasks;

namespace SpriteTools;

[DropObject( "sprite", "sprite" )]
partial class SpriteDropObject : BaseDropObject
{
	Texture texture;
	float aspect = 1f;

	protected override async Task Initialize ( string dragData, CancellationToken token )
	{
		Asset asset = await InstallAsset( dragData, token );

		if ( asset is null )
			return;

		if ( token.IsCancellationRequested )
			return;

		PackageStatus = "Loading Texture";
		texture = Texture.Invalid;
		PackageStatus = null;

		aspect = (float)texture.Height / texture.Width;
		if ( texture.HasAnimatedSequences ) aspect = 0f;
	}

	public override void OnUpdate ()
	{
		using var scope = Gizmo.Scope( "DropObject", traceTransform );

		Gizmo.Draw.Color = Color.White;
		if ( texture is not null && aspect != 0 )
		{
			Gizmo.Draw.Sprite( Vector3.Zero, new Vector2( 25f, 25f * aspect ), texture, true );
		}
		else
		{
			Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
			Gizmo.Draw.Sprite( Bounds.Center, 16, "materials/gizmo/downloads.png" );
		}
	}

	public override async Task OnDrop ()
	{
		await WaitForLoad();

		if ( texture is null )
			return;

		var DragObject = new GameObject();
		DragObject.Name = texture.ResourceName;
		DragObject.Transform.World = traceTransform;

		GameObject = DragObject;

		var spriteComponent = GameObject.Components.GetOrCreate<SpriteRenderer>();
		spriteComponent.Texture = texture;
		spriteComponent.Size = new Vector2( 25f, 25f * ( aspect == 0 ? 1 : aspect ) );

		EditorScene.Selection.Clear();
		EditorScene.Selection.Add( DragObject );
	}
}
