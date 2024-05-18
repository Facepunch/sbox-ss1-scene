using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status(7, 0, 1f)]
public class XpDamageStatus : Status
{
	public XpDamageStatus()
    {
		Title = "XP Shrapnel";
		IconPath = "textures/icons/xp_damage.png";
	}

	public override void Init(Player player)
	{
		base.Init(player);
	}

	public override void Refresh()
    {
		Description = GetDescription(Level);
    }

	public override string GetDescription(int newLevel)
	{
		return string.Format("When you gain XP, hurt nearby enemies for {0} per point", GetPrintAmountForLevel( Level));
	}

	public override string GetUpgradeDescription(int newLevel)
	{
		return newLevel > 1 ? string.Format("When you gain XP, hurt nearby enemies for {0}→{1} per point", GetPrintAmountForLevel( newLevel - 1), GetPrintAmountForLevel( newLevel)) : GetDescription(newLevel);
	}

    public override void OnGainExperience(int xp)
    {
		List<Thing> nearbyThings = new List<Thing>();

		for ( int dx = -2; dx <= 2; dx++ )
			for ( int dy = -2; dy <= 2; dy++ )
				Manager.Instance.AddThingsInGridSquare( new Manager.GridSquare( Player.GridPos.x + dx, Player.GridPos.y + dy ), nearbyThings );

		float radius = 2f;
		float damage = xp * GetAmountForLevel( Level ) * Player.GetDamageMultiplier();

		Manager.Instance.PlaySfxNearby( "shoot", Player.Position2D, pitch: Utils.Map( xp, 1, 16, 0.85f, 1.4f ), volume: Utils.Map( xp, 1, 16, 0.6f, 1f ), maxDist: 3f );

		foreach ( Thing thing in nearbyThings )
		{
			if ( thing is Enemy enemy && !enemy.IsDying && (!enemy.IsSpawning || enemy.ElapsedTime > 0.75f) )
			{
				var dist_sqr = (thing.Position2D - Player.Position2D).LengthSquared;
				if ( dist_sqr < MathF.Pow( radius, 2f ) )
				{
					var addVel = Vector2.Zero; // todo
					var addTempWeight = 0f;
					enemy.Damage( damage, Player.GameObject.Id, addVel, addTempWeight, false );
				}
			}
		}
	}

    public float GetAmountForLevel(int level)
	{
		return level * 0.85f;
	}

    public string GetPrintAmountForLevel(int level)
    {
        return string.Format("{0:0.00}", GetAmountForLevel(level));
    }
}
