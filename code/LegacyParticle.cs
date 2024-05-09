using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class LegacyParticle
{
	public GameObject GameObject;
	public LegacyParticleSystem LegacyParticleSystem;
	public string particleName;
	public Vector3 Position { get => GameObject.Transform.Position; set => GameObject.Transform.Position = value; }
	public Rotation Rotation { get => GameObject.Transform.Rotation; set => GameObject.Transform.Rotation = value; }
	public static LegacyParticle Create( string name, Vector3 position = default, Rotation rotation = default, List<ParticleControlPoint> controlPoints = null )
	{
		var lp = new LegacyParticle();
		lp.GameObject = Game.ActiveScene.CreateObject();
		lp.GameObject.Transform.Position = position;
		lp.GameObject.Transform.Rotation = rotation;
		lp.GameObject.Name = "particles";

		lp.LegacyParticleSystem = lp.GameObject.Components.GetOrCreate<LegacyParticleSystem>();
		lp.LegacyParticleSystem.ControlPoints = controlPoints ?? new List<ParticleControlPoint>()
		{
			new ParticleControlPoint() { Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = position }
		};

		lp.particleName = name;
		lp.LegacyParticleSystem.Particles = ParticleSystem.Load( lp.particleName );

		lp.GameObject.Components.Create<FloatingDamageNumber>();

		return lp;
	}
	public void SetGameObject( int index, GameObject obj )
	{
		var cpv = new ParticleControlPoint() { Value = ParticleControlPoint.ControlPointValueInput.GameObject, GameObjectValue = obj };
		if ( LegacyParticleSystem.ControlPoints.Count < index + 1 )
			LegacyParticleSystem.ControlPoints.Add( cpv );
		else
			LegacyParticleSystem.ControlPoints[index] = cpv;

		LegacyParticleSystem.SceneObject.SetControlPoint( index, obj.Transform.World );
	}
	public void SetVector( int index, Vector3 vec )
	{
		var cpv = new ParticleControlPoint() { Value = ParticleControlPoint.ControlPointValueInput.Vector3, VectorValue = vec };
		if ( LegacyParticleSystem.ControlPoints.Count < index + 1 )
			LegacyParticleSystem.ControlPoints.Add( cpv );
		else
			LegacyParticleSystem.ControlPoints[index] = cpv;

		LegacyParticleSystem.SceneObject.SetControlPoint( index, vec );
	}
	public void SetFloat( int index, float flt )
	{
		var cpv = new ParticleControlPoint() { Value = ParticleControlPoint.ControlPointValueInput.Float, FloatValue = flt };
		if ( LegacyParticleSystem.ControlPoints.Count < index + 1 )
			LegacyParticleSystem.ControlPoints.Add( cpv );
		else
			LegacyParticleSystem.ControlPoints[index] = cpv;

		LegacyParticleSystem.SceneObject.SetControlPoint( index, flt );
	}

	public void SetNamedValue( string name, float value )
	{
		LegacyParticleSystem.SceneObject.SetNamedValue( name, value );
	}
	public void SetNamedValue( string name, Vector3 value )
	{
		LegacyParticleSystem.SceneObject.SetNamedValue( name, value );
	}
}
