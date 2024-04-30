using Sandbox;

public enum ModifierType { Set, Add, Mult }
public class ModifierData
{
	public float value;
	public ModifierType type;
	public float priority;

	public ModifierData( float _value, ModifierType _type, float _priority = 0f )
	{
		value = _value;
		type = _type;
		priority = _priority;
	}
}

public enum PlayerStat
{
	AttackTime, AttackSpeed, ReloadTime, ReloadSpeed, MaxAmmoCount, BulletDamage, BulletForce, Recoil, MoveSpeed, NumProjectiles, BulletSpread, BulletInaccuracy, BulletSpeed, BulletLifetime,
	BulletNumPiercing, CritChance, CritMultiplier, LowHealthDamageMultiplier, NumUpgradeChoices, HealthRegen, HealthRegenStill, DamageReductionPercent, PushStrength, CoinAttractRange, CoinAttractStrength, Luck, MaxHp,
	NumDashes, DashInvulnTime, DashCooldown, DashProgress, DashStrength, ThornsPercent, ShootFireIgniteChance, FireDamage, FireLifetime, FireSpreadChance, ShootFreezeChance, FreezeLifetime,
	FreezeTimeScale, FreezeOnMeleeChance, FreezeFireDamageMultiplier, LastAmmoDamageMultiplier, FearLifetime, FearDamageMultiplier, FearOnMeleeChance, BulletDamageGrow, BulletDamageShrink,
	BulletDistanceDamage, NumRerollsPerLevel, FullHealthDamageMultiplier, DamagePerEarlierShot, DamageForSpeed, OverallDamageMultiplier, ExplosionSizeMultiplier, GrenadeVelocity, ExplosionDamageMultiplier,
	BulletDamageMultiplier, ExplosionDamageReductionPercent, NonExplosionDamageIncreasePercent, GrenadeStickyPercent, GrenadeFearChance, FearDrainPercent, FearPainPercent, CrateChanceAdditional,
	AttackSpeedStill, FearDropGrenadeChance, FrozenShardsNum, NoDashInvuln, BulletFlatDamageAddition, GrenadesCanCrit, BulletHealTeammateAmount,
}

public enum DamageType { Melee, Ranged, Explosion, Fire, }

public class Player : Thing
{
	[Property] public SpriteRenderer Sprite { get; set; }

	public float Health { get; set; }

	//public Arrow ArrowAimer { get; private set; }
	public Vector2 AimDir { get; private set; }

	[Sync] public bool IsDead { get; private set; }
	public float Timer { get; protected set; }
	[Sync] public bool IsReloading { get; protected set; }
	[Sync] public float ReloadProgress { get; protected set; }

	public const float BASE_MOVE_SPEED = 15f;
	private int _shotNum;

	[Sync] public int Level { get; protected set; }
	public int ExperienceTotal { get; protected set; }
	[Sync] public int ExperienceCurrent { get; protected set; }
	[Sync] public int ExperienceRequired { get; protected set; }
	public bool IsChoosingLevelUpReward { get; protected set; }

	[Sync] public float DashTimer { get; private set; }
	[Sync] public bool IsDashing { get; private set; }
	[Sync] public Vector2 DashVelocity { get; private set; }
	[Sync] public float DashInvulnTimer { get; private set; }
	private TimeSince _dashCloudTime;
	public float DashProgress { get; protected set; }
	[Sync] public float DashRechargeProgress { get; protected set; }
	[Sync] public int NumDashesAvailable { get; private set; }
	public int AmmoCount { get; protected set; }

	public bool IsMoving => Velocity.LengthSquared > 0.01f && !IsDashing;
	public bool IsInvulnerable => IsDashing && Stats[PlayerStat.NoDashInvuln] <= 0f;

	private float _flashTimer;
	private bool _isFlashing;
	public float TimeSinceHurt { get; private set; }

	//public Nametag Nametag { get; private set; }

	//private ShieldVfx _shieldVfx;

	[Sync] public int NumRerollAvailable { get; set; }

	// STATS
	[Sync] public IDictionary<PlayerStat, float> Stats { get; private set; }

	// STATUS
	[Sync] public IDictionary<int, Status> Statuses { get; private set; }

	private Dictionary<Status, Dictionary<PlayerStat, ModifierData>> _modifiers_stat = new Dictionary<Status, Dictionary<PlayerStat, ModifierData>>();
	private Dictionary<PlayerStat, float> _original_properties_stat = new Dictionary<PlayerStat, float>();

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
			return;

		//CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		Stats = new Dictionary<PlayerStat, float>();
		Statuses = new Dictionary<int, Status>();

		InitializeStats();
	}

	public void InitializeStats()
	{
		//AnimationPath = "textures/sprites/player_idle.frames";
		//AnimationSpeed = 0.66f;

		_original_properties_stat.Clear();

		//RemoveShieldVfx();

		Level = 0;
		ExperienceRequired = GetExperienceReqForLevel( Level + 1 );
		ExperienceTotal = 0;
		ExperienceCurrent = 0;
		Stats[PlayerStat.AttackTime] = 0.15f;
		Timer = Stats[PlayerStat.AttackTime];
		AmmoCount = 5;
		Stats[PlayerStat.MaxAmmoCount] = AmmoCount;
		Stats[PlayerStat.ReloadTime] = 1.5f;
		Stats[PlayerStat.ReloadSpeed] = 1f;
		Stats[PlayerStat.AttackSpeed] = 1f;
		Stats[PlayerStat.BulletDamage] = 5f;
		Stats[PlayerStat.BulletForce] = 0.55f;
		Stats[PlayerStat.Recoil] = 0f;
		Stats[PlayerStat.MoveSpeed] = 1f;
		Stats[PlayerStat.NumProjectiles] = 1f;
		Stats[PlayerStat.BulletSpread] = 35f;
		Stats[PlayerStat.BulletInaccuracy] = 5f;
		Stats[PlayerStat.BulletSpeed] = 4.5f;
		Stats[PlayerStat.BulletLifetime] = 0.8f;
		Stats[PlayerStat.Luck] = 1f;
		Stats[PlayerStat.CritChance] = 0.05f;
		Stats[PlayerStat.CritMultiplier] = 1.5f;
		Stats[PlayerStat.LowHealthDamageMultiplier] = 1f;
		Stats[PlayerStat.FullHealthDamageMultiplier] = 1f;
		Stats[PlayerStat.ThornsPercent] = 0f;

		Stats[PlayerStat.NumDashes] = 1f;
		NumDashesAvailable = (int)MathF.Round( Stats[PlayerStat.NumDashes] );
		Stats[PlayerStat.DashCooldown] = 3f;
		Stats[PlayerStat.DashInvulnTime] = 0.25f;
		Stats[PlayerStat.DashStrength] = 3f;
		Stats[PlayerStat.BulletNumPiercing] = 0f;

		Health = 100f;
		Stats[PlayerStat.MaxHp] = 100f;
		IsDead = false;
		Radius = 0.1f;
		GridPos = Manager.Instance.GetGridSquareForPos( Position2D );
		AimDir = Vector2.Up;
		NumRerollAvailable = 2;

		Stats[PlayerStat.FireDamage] = 1.0f;
		Stats[PlayerStat.FireLifetime] = 2.0f;
		Stats[PlayerStat.ShootFireIgniteChance] = 0f;
		Stats[PlayerStat.FireSpreadChance] = 0f;
		Stats[PlayerStat.ShootFreezeChance] = 0f;
		Stats[PlayerStat.FreezeLifetime] = 3f;
		Stats[PlayerStat.FreezeTimeScale] = 0.6f;
		Stats[PlayerStat.FreezeOnMeleeChance] = 0f;
		Stats[PlayerStat.FreezeFireDamageMultiplier] = 1f;
		Stats[PlayerStat.FearLifetime] = 4f;
		Stats[PlayerStat.FearDamageMultiplier] = 1f;
		Stats[PlayerStat.FearOnMeleeChance] = 0f;

		Stats[PlayerStat.CoinAttractRange] = 1.7f;
		Stats[PlayerStat.CoinAttractStrength] = 3.1f;

		Stats[PlayerStat.NumUpgradeChoices] = 3f;
		Stats[PlayerStat.HealthRegen] = 0f;
		Stats[PlayerStat.HealthRegenStill] = 0f;
		Stats[PlayerStat.DamageReductionPercent] = 0f;
		Stats[PlayerStat.PushStrength] = 50f;
		Stats[PlayerStat.LastAmmoDamageMultiplier] = 1f;
		Stats[PlayerStat.BulletDamageGrow] = 0f;
		Stats[PlayerStat.BulletDamageShrink] = 0f;
		Stats[PlayerStat.BulletDistanceDamage] = 0f;
		Stats[PlayerStat.NumRerollsPerLevel] = 1f;
		Stats[PlayerStat.DamagePerEarlierShot] = 0f;
		Stats[PlayerStat.DamageForSpeed] = 0f;
		Stats[PlayerStat.OverallDamageMultiplier] = 1f;
		Stats[PlayerStat.ExplosionSizeMultiplier] = 1f;
		Stats[PlayerStat.GrenadeVelocity] = 8f;
		Stats[PlayerStat.ExplosionDamageMultiplier] = 1f;
		Stats[PlayerStat.BulletDamageMultiplier] = 1f;
		Stats[PlayerStat.ExplosionDamageReductionPercent] = 0f;
		Stats[PlayerStat.NonExplosionDamageIncreasePercent] = 0f;
		Stats[PlayerStat.GrenadeStickyPercent] = 0f;
		Stats[PlayerStat.GrenadeFearChance] = 0f;
		Stats[PlayerStat.FearDrainPercent] = 0f;
		Stats[PlayerStat.FearPainPercent] = 0f;
		Stats[PlayerStat.CrateChanceAdditional] = 0f;
		Stats[PlayerStat.AttackSpeedStill] = 1f;
		Stats[PlayerStat.FearDropGrenadeChance] = 0f;
		Stats[PlayerStat.FrozenShardsNum] = 0f;
		Stats[PlayerStat.NoDashInvuln] = 0f;
		Stats[PlayerStat.BulletFlatDamageAddition] = 0f;
		Stats[PlayerStat.GrenadesCanCrit] = 0f;
		Stats[PlayerStat.BulletHealTeammateAmount] = 0f;

		Statuses.Clear();
		//_statusesToRemove.Clear();
		_modifiers_stat.Clear();

		_isFlashing = false;
		//ColorTint = Color.White;
		//EnableDrawing = true;
		IsChoosingLevelUpReward = false;
		IsDashing = false;
		IsReloading = false;
		ReloadProgress = 0f;
		DashProgress = 0f;
		DashRechargeProgress = 1f;
		TempWeight = 0f;
		_shotNum = 0;
		TimeSinceHurt = 999f;

		//ShadowOpacity = 0.8f;
		//ShadowScale = 1.12f;

		//InitializeStatsClient();
		//RefreshStatusHud( To.Single( Client ) );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		float dt = Time.Delta;

		Vector2 inputVector = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x );

		if ( inputVector.LengthSquared > 0f )
			Velocity += inputVector.Normal * Stats[PlayerStat.MoveSpeed] * BASE_MOVE_SPEED * Globals.MOVE_FACTOR * dt;

		Transform.Position += (Vector3)Velocity * dt;

		Velocity = Utils.DynamicEaseTo( Velocity, Vector2.Zero, 0.2f, dt );
		TempWeight *= (1f - dt * 4.7f);

		//HandleBounds();

		if ( Velocity.x > 0f )
			Sprite.FlipHorizontal = true;
		else if(Velocity.x < 0f)
			Sprite.FlipHorizontal = false;


		Manager.Instance.Camera2D.TargetPos = Position2D;
	}

	public int GetExperienceReqForLevel( int level )
	{
		return (int)MathF.Round( Utils.Map( level, 1, 150, 3f, 340f, EasingType.SineIn ) );
	}
}
