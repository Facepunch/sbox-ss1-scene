using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

[Status(7, 0, 1f)]
public class ShieldStatus : Status
{
    public bool IsShielded;
    private float _timer;

	public ShieldStatus()
    {
		Title = "Fragile Shield";
		IconPath = "textures/icons/shield.png";
	}

	public override void Init(Player player)
	{
		base.Init(player);
        GainShield();
        ShouldUpdate = true;
    }

	public override void Refresh()
    {
		Description = GetDescription(Level);
        GainShield();
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (!IsShielded)
        {
            _timer += dt;
            if (_timer > GetTimeForLevel(Level))
            {
                GainShield();
            }
        }
    }

    public void GainShield()
    {
        if(!IsShielded)
        {
            IsShielded = true;
            Player.CreateShieldVfx();
            _timer = 0f;
            Manager.Instance.PlaySfxNearby( "shield_gain", Player.Position2D, pitch: Game.Random.Float(1.2f, 1.25f), volume: 0.5f, maxDist: 7.5f);
        }
    }

    public void LoseShield()
    {
        if(IsShielded)
        {
            IsShielded = false;
            Player.RemoveShieldVfx();
			Manager.Instance.PlaySfxNearby("shield_break", Player.Position2D, pitch: Game.Random.Float(0.95f, 1.05f), volume: 1f, maxDist: 7.5f);
		}
    }

    public override string GetDescription(int newLevel)
	{
		return string.Format("Gain a shield that prevents a single hit and recharges in {0}s", GetTimeForLevel(Level));
	}

	public override string GetUpgradeDescription(int newLevel)
	{
		return newLevel > 1 ? string.Format("Gain a shield that prevents a single hit and recharges in {0}s→{1}s", GetTimeForLevel(newLevel - 1), GetTimeForLevel(newLevel)) : GetDescription(newLevel);
	}

	public float GetTimeForLevel(int level)
	{
		return 58 - level * 8;
	}

    public override void Remove()
    {
        base.Remove();

        Player.RemoveShieldVfx();
    }
}
