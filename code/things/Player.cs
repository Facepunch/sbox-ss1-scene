using Sandbox;
using SpriteTools;
using System.Drawing;
using System.Numerics;
using System.Runtime.Serialization.Formatters;
using System.Xml.Linq;
using static Manager;

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
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject ArrowAimerPrefab { get; set;  }
	[Property] public GameObject BulletPrefab { get; set; }

	[Sync] public float Health { get; set; }
	[Sync] public Vector2 Velocity { get; set; }
	[Sync] public Vector2 InputVector { get; set; }

	public GameObject ArrowAimer { get; private set; }
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
	public List<Status> LevelUpChoices { get; private set; }

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
	public TimeSince TimeSinceHurt { get; private set; }

	//public Nametag Nametag { get; private set; }

	private GameObject _shieldVfx;

	[Sync] public int NumRerollAvailable { get; set; }

	// STATS
	[Sync] public NetDictionary<PlayerStat, float> Stats { get; private set; } = new();

	// STATUS
	public Dictionary<int, Status> Statuses { get; private set; }

	private Dictionary<Status, Dictionary<PlayerStat, ModifierData>> _modifiers_stat = new Dictionary<Status, Dictionary<PlayerStat, ModifierData>>();
	private Dictionary<PlayerStat, float> _original_properties_stat = new Dictionary<PlayerStat, float>();

	private bool _doneFirstUpdate;

	protected override void OnAwake()
	{
		base.OnAwake();

		//OffsetY = -0.42f;
		//OffsetY = 0f;

		Scale = 1f;

		ShadowOpacity = 0.8f;
		ShadowScale = 1.12f;
		//SpawnShadow( ShadowScale, ShadowOpacity );

		Statuses = new Dictionary<int, Status>();
		LevelUpChoices = new List<Status>();
		InitializeStats();

		if ( IsProxy )
			return;

		CollideWith.Add( typeof( Enemy ) );
		CollideWith.Add( typeof( Player ) );

		ArrowAimer = ArrowAimerPrefab.Clone(Transform.Position);
		ArrowAimer.SetParent( GameObject );
		ArrowAimer.NetworkMode = NetworkMode.Never;
	}

	public void InitializeStats()
	{
		_original_properties_stat.Clear();

		if(Network.Active)
		{
			RemoveShieldVfx();
		}
		else
		{
			if ( _shieldVfx != null )
			{
				_shieldVfx.Destroy();
				_shieldVfx = null;
			}
		}

		Level = 0;
		ExperienceRequired = GetExperienceReqForLevel( Level + 1 );
		ExperienceTotal = 0;
		ExperienceCurrent = 0;
		Stats[PlayerStat.AttackTime] = 0.15f;
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
		Radius = 0.10f;
		GridPos = Manager.Instance.GetGridSquareForPos( Position2D );
		AimDir = new Vector2(0f, 1f);
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
		Stats[PlayerStat.PushStrength] = 30f;
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
		Sprite.FlashTint = Color.White.WithAlpha( 0f );
		Sprite.Tint = Color.White;
		//EnableDrawing = true;
		IsChoosingLevelUpReward = false;
		IsDashing = false;
		IsReloading = true;
		Timer = Stats[PlayerStat.ReloadTime];
		ReloadProgress = 0f;
		DashProgress = 0f;
		DashRechargeProgress = 1f;
		TempWeight = 0f;
		_shotNum = 0;
		TimeSinceHurt = 999f;
		ShadowOpacity = 0.8f;
		//ShadowScale = 1.12f;

		//AddStatus( TypeLibrary.GetType( typeof( DamageStatus) ) );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//Gizmo.Draw.Color = Color.White.WithAlpha(0.5f);
		//Gizmo.Draw.Text( $"IsGameOver: {Manager.Instance.IsGameOver}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.7f, 0f ) ) );

		string debug = "";

		if (!_doneFirstUpdate)
		{
			SpawnShadow( ShadowScale, ShadowOpacity );
			Manager.Instance.Camera2D.SetPos( Position2D );

			_doneFirstUpdate = true;
		}

		if (!IsProxy )
		{
			InputVector = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x );

			if ( Input.Pressed( "Menu" ) )
			{
				Manager.Instance.Restart();
				return;
			}

			foreach ( KeyValuePair<int, Status> pair in Statuses )
			{
				Status status = pair.Value;
				debug += status.ToString() + "\n";
			}

			//Gizmo.Draw.Color = Color.White.WithAlpha(0.5f);
			//Gizmo.Draw.Text( $"{debug}\nIsGameOver: {Manager.Instance.IsGameOver}\nIsReloading: {IsReloading}\nHealth: {Health}/{Stats[PlayerStat.MaxHp]}\nExperienceTotal: {ExperienceTotal}\nGridPos: {GridPos}\nRadius: {Radius}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.7f, 0f ) ) );
		}

		//Gizmo.Draw.Color = Color.White.WithAlpha(0.05f);
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		//if ( ShadowSprite != null )
		//{
		//	ShadowSprite.Tint = Color.Black.WithAlpha( ShadowOpacity );
		//	ShadowSprite.Transform.LocalScale = new Vector2( ShadowScale * Globals.SPRITE_SCALE );
		//}

		if ( Manager.Instance.IsGameOver )
			return;

		float dt = Time.Delta;

		//SetFlipHorizontal( Velocity.x > 0f );
		Sprite.SpriteFlags = Velocity.x > 0f ? SpriteFlags.HorizontalFlip : SpriteFlags.None;

		bool hurting = TimeSinceHurt < 0.25f;
		bool attacking = !IsReloading;
		bool moving = Velocity.LengthSquared > 0.01f && InputVector.LengthSquared > 0.1f;

		string stateStr = "";
		if ( hurting && attacking )
			stateStr = "hurt_attack_";
		else if ( hurting )
			stateStr = "hurt_";
		else if ( attacking )
			stateStr = "attack_";

		Sprite.PlayAnimation( $"{stateStr}{(moving ? "walk" : "idle")}" );
		Sprite.PlaybackSpeed = moving ? Utils.Map( Velocity.Length, 0f, 2f, 1.5f, 2f ) : 0.66f;

		Sprite.Transform.LocalRotation = new Angles( 0f, -90f + (Velocity.Length * Utils.FastSin( Time.Now * MathF.PI * 6f ) * 1.6f) * (Sprite.SpriteFlags.HasFlag(SpriteFlags.HorizontalFlip) ? -1f : 1f), 0f );

		if ( !IsDead )
		{
			HandleFlashing( dt );
		}

		if ( IsProxy )
			return;

		if ( InputVector.LengthSquared > 0f )
			Velocity += InputVector.Normal * Stats[PlayerStat.MoveSpeed] * BASE_MOVE_SPEED * dt;

		var velocity = Velocity + (IsDashing ? DashVelocity : Vector2.Zero);
		Position2D += velocity * dt;

		Transform.Position = Transform.Position.WithZ( Globals.GetZPos(Position2D.y) );

		Velocity = Utils.DynamicEaseTo( Velocity, Vector2.Zero, 0.2f, dt );
		TempWeight *= (1f - dt * 4.7f);

		HandleBounds();

		Manager.Instance.Camera2D.TargetPos = Position2D;

		if(Input.UsingController)
		{

		}
		else
		{
			AimDir = (Manager.Instance.MouseWorldPos - (Position2D + new Vector2(0f, 0.5f))).Normal;
		}

		if ( ArrowAimer != null )
		{
			ArrowAimer.Transform.LocalRotation = new Angles(0f, MathF.Atan2( AimDir.y, AimDir.x ) * (180f / MathF.PI) - 180f, 0f);
			//ArrowAimer.Transform.LocalPosition = new Vector2( 0f, 0.4f + OffsetY ) + AimDir * 0.7f;
			ArrowAimer.Transform.LocalPosition = new Vector2( 0f, 0.4f ) + AimDir * 0.7f;
		}

		for ( int dx = -1; dx <= 1; dx++ )
		{
			for ( int dy = -1; dy <= 1; dy++ )
			{
				Manager.Instance.HandleThingCollisionForGridSquare( this, new GridSquare( GridPos.x + dx, GridPos.y + dy ), dt );
			}
		}

		if ( !IsDead )
		{
			HandleDashing( dt );
			HandleStatuses( dt );
			HandleShooting( dt );
			HandleRegen( dt );
		}

		if(IsChoosingLevelUpReward)
		{
			if ( Input.Pressed( "reload" ) )		UseReroll();
			else if ( Input.Pressed( "Slot1" ) )	UseChoiceHotkey( 1 );
			else if ( Input.Pressed( "Slot2" ) )	UseChoiceHotkey( 2 );
			else if ( Input.Pressed( "Slot3" ) )	UseChoiceHotkey( 3 );
			else if ( Input.Pressed( "Slot4" ) )	UseChoiceHotkey( 4 );
			else if ( Input.Pressed( "Slot5" ) )	UseChoiceHotkey( 5 );
			else if ( Input.Pressed( "Slot6" ) )	UseChoiceHotkey( 6 );
		}

		if(Input.Pressed("use"))
		{
			AddExperience( 1 );
		}
	}

	void HandleRegen( float dt )
	{
		if ( Math.Abs( Stats[PlayerStat.HealthRegen] ) > 0f )
			RegenHealth( Stats[PlayerStat.HealthRegen] * dt );

		if ( Stats[PlayerStat.HealthRegenStill] > 0f && !IsMoving )
			RegenHealth( Stats[PlayerStat.HealthRegenStill] * dt );
	}

	public void RegenHealth( float amount )
	{
		Health += amount;
		if ( Health > Stats[PlayerStat.MaxHp] )
			Health = Stats[PlayerStat.MaxHp];

		if ( Health <= 0f )
			Die();
	}

	void HandleDashing( float dt )
	{
		int numDashes = (int)MathF.Round( Stats[PlayerStat.NumDashes] );
		if ( NumDashesAvailable < numDashes )
		{
			DashTimer -= dt;
			DashRechargeProgress = Utils.Map( DashTimer, Stats[PlayerStat.DashCooldown], 0f, 0f, 1f );
			if ( DashTimer <= 0f )
			{
				DashRecharged();
			}
		}

		if ( DashInvulnTimer > 0f )
		{
			DashInvulnTimer -= dt;
			DashProgress = Utils.Map( DashInvulnTimer, Stats[PlayerStat.DashInvulnTime], 0f, 0f, 1f );
			if ( DashInvulnTimer <= 0f )
			{
				IsDashing = false;
				Sprite.Tint = Color.White;
				DashFinished();
			}
			else
			{
				if ( IsInvulnerable )
					Sprite.Tint = new Color( Game.Random.Float( 0.1f, 0.25f ), Game.Random.Float( 0.1f, 0.25f ), 1f );

				if ( _dashCloudTime > Game.Random.Float( 0.1f, 0.2f ) )
				{
					SpawnDashCloudClient();
					_dashCloudTime = 0f;
				}
			}
		}

		if ( Input.Pressed( "Jump" ) || Input.Pressed( "attack1" ) )
			Dash();
	}

	public void Dash()
	{
		if ( NumDashesAvailable <= 0 )
			return;

		Vector2 dashDir = Velocity.LengthSquared > 0f ? Velocity.Normal : AimDir;
		DashVelocity = dashDir * Stats[PlayerStat.DashStrength];
		TempWeight = 2f;

		if ( NumDashesAvailable == (int)Stats[PlayerStat.NumDashes] )
			DashTimer = Stats[PlayerStat.DashCooldown];

		NumDashesAvailable--;
		IsDashing = true;
		DashInvulnTimer = Stats[PlayerStat.DashInvulnTime];
		DashProgress = 0f;
		DashRechargeProgress = 0f;

		Manager.Instance.PlaySfxNearby( "player.dash", Position2D + dashDir * 0.5f, pitch: Utils.Map( NumDashesAvailable, 0, 5, 1f, 0.9f ), volume: 1f, maxDist: 4f );
		SpawnDashCloudClient();
		_dashCloudTime = 0f;

		ForEachStatus( status => status.OnDashStarted() );
	}

	public void DashFinished()
	{
		ForEachStatus( status => status.OnDashFinished() );
	}

	public void DashRecharged()
	{
		NumDashesAvailable++;
		var numDashes = (int)MathF.Round( Stats[PlayerStat.NumDashes] );
		if ( NumDashesAvailable > numDashes )
			NumDashesAvailable = numDashes;

		if ( NumDashesAvailable < numDashes )
		{
			DashTimer = Stats[PlayerStat.DashCooldown];
			DashRechargeProgress = 0f;
		}
		else
		{
			DashRechargeProgress = 1f;
		}

		ForEachStatus( status => status.OnDashRecharged() );

		Manager.Instance.PlaySfxNearbyLocal( "player.dash.recharge", Position2D, pitch: Utils.Map( NumDashesAvailable, 1, numDashes, 1f, 1.2f ), volume: 0.2f, maxDist: 5f );
	}

	void HandleBounds()
	{
		var x_min = Manager.Instance.BOUNDS_MIN.x + Radius;
		var x_max = Manager.Instance.BOUNDS_MAX.x - Radius;
		var y_min = Manager.Instance.BOUNDS_MIN.y;
		var y_max = Manager.Instance.BOUNDS_MAX.y - Radius;

		if ( Position2D.x < x_min )
		{
			Position2D = new Vector2( x_min, Position2D.y );
			Velocity = new Vector2( Velocity.x * -1f, Velocity.y );
		}
		else if ( Position2D.x > x_max )
		{
			Position2D = new Vector2( x_max, Position2D.y );
			Velocity = new Vector2( Velocity.x * -1f, Velocity.y );
		}

		if ( Position2D.y < y_min )
		{
			Position2D = new Vector2( Position2D.x, y_min );
			Velocity = new Vector2( Velocity.x, Velocity.y * -1f );
		}
		else if ( Position2D.y > y_max )
		{
			Position2D = new Vector2( Position2D.x, y_max );
			Velocity = new Vector2( Velocity.x, Velocity.y * -1f );
		}
	}

	public int GetExperienceReqForLevel( int level )
	{
		return (int)MathF.Round( Utils.Map( level, 1, 150, 3f, 340f, EasingType.SineIn ) );
	}

	public void Flash( float time )
	{
		if ( _isFlashing )
			return;

		Sprite.Tint = new Color( 1f, 0f, 0f );
		_isFlashing = true;
		_flashTimer = time;
	}

	[Broadcast]
	public void Heal( float amount, float flashTime )
	{
		Sprite.Tint = new Color( 0f, 1f, 0f );
		_isFlashing = true;
		_flashTimer = flashTime;

		if ( IsProxy )
			return;

		Health += amount;
		if ( Health > Stats[PlayerStat.MaxHp] )
			Health = Stats[PlayerStat.MaxHp];
	}

	void HandleFlashing( float dt )
	{
		if ( _isFlashing )
		{
			_flashTimer -= dt;
			if ( _flashTimer < 0f )
			{
				_isFlashing = false;
				Sprite.Tint = Color.White;
			}
		}
	}

	public void AddStatus( TypeDescription type )
	{
		Status status = null;
		var typeIdentity = type.Identity;

		if ( Statuses.ContainsKey( typeIdentity ) )
		{
			status = Statuses[typeIdentity];
			status.Level++;
		}

		if ( status == null )
		{
			status = StatusManager.CreateStatus( type );
			Statuses.Add( typeIdentity, status );
			status.Init( this );
		}

		//Sandbox.Services.Stats.Increment( Client, "status", 1, $"{type.Name.ToLowerInvariant()}", new { Status = type.Name.ToLowerInvariant(), Level = status.Level } );

		status.Refresh();

		//RefreshStatusHud();

		Manager.Instance.PlaySfxNearbyLocal( "click", Position2D, 0.9f, 0.75f, 5f );

		LevelUpChoices.Clear();
		IsChoosingLevelUpReward = false;

		CheckForLevelUp();
	}

	public bool HasStatus( TypeDescription type )
	{
		return Statuses.ContainsKey( type.Identity );
	}

	public Status GetStatus( TypeDescription type )
	{
		if ( Statuses.ContainsKey( type.Identity ) )
			return Statuses[type.Identity];

		return null;
	}

	public int GetStatusLevel( TypeDescription type )
	{
		if ( Statuses.ContainsKey( type.Identity ) )
			return Statuses[type.Identity].Level;

		return 0;
	}

	public void Modify( Status caller, PlayerStat statType, float value, ModifierType type, float priority = 0f, bool update = true )
	{
		if ( !_modifiers_stat.ContainsKey( caller ) )
			_modifiers_stat.Add( caller, new Dictionary<PlayerStat, ModifierData>() );

		_modifiers_stat[caller][statType] = new ModifierData( value, type, priority );

		if ( update )
			UpdateProperty( statType );
	}

	public void AdjustBaseStat( PlayerStat statType, float amount, bool update = true )
	{
		if ( !_original_properties_stat.ContainsKey( statType ) )
			_original_properties_stat.Add( statType, Stats[statType] );

		_original_properties_stat[statType] += amount;

		if ( update )
			UpdateProperty( statType );
	}

	void UpdateProperty( PlayerStat statType )
	{
		if ( !_original_properties_stat.ContainsKey( statType ) )
		{
			_original_properties_stat.Add( statType, Stats[statType] );
		}

		float curr_value = _original_properties_stat[statType];
		float curr_set = curr_value;
		bool should_set = false;
		float curr_priority = 0f;
		float total_add = 0f;
		float total_mult = 1f;

		foreach ( Status caller in _modifiers_stat.Keys )
		{
			var dict = _modifiers_stat[caller];
			if ( dict.ContainsKey( statType ) )
			{
				var mod_data = dict[statType];
				switch ( mod_data.type )
				{
					case ModifierType.Set:
						if ( mod_data.priority >= curr_priority )
						{
							curr_set = mod_data.value;
							curr_priority = mod_data.priority;
							should_set = true;
						}
						break;
					case ModifierType.Add:
						total_add += mod_data.value;
						break;
					case ModifierType.Mult:
						total_mult *= mod_data.value;
						break;
				}
			}
		}

		if ( should_set )
			curr_value = curr_set;

		curr_value += total_add;
		curr_value *= total_mult;

		Stats[statType] = curr_value;
	}

	[Broadcast]
	public void AddExperience( int xp )
	{
		if ( IsProxy )
			return;

		ExperienceTotal += xp;
		ExperienceCurrent += xp;

		ForEachStatus( status => status.OnGainExperience( xp ) );

		if ( !IsChoosingLevelUpReward )
			CheckForLevelUp();
	}

	public void CheckForLevelUp()
	{
		//Log.Info("CheckForLevelUp: " + ExperienceCurrent + " / " + ExperienceRequired + " IsServer: " + Sandbox.Game.IsServer + " Level: " + Level);
		if ( ExperienceCurrent >= ExperienceRequired && !Manager.Instance.IsGameOver )
			LevelUp();
	}

	[Broadcast]
	public void LevelUp()
	{
		if ( IsProxy )
			return;

		ExperienceCurrent -= ExperienceRequired;

		Level++;
		ExperienceRequired = GetExperienceReqForLevel( Level + 1 );
		NumRerollAvailable += (int)Stats[PlayerStat.NumRerollsPerLevel];

		Manager.Instance.PlaySfxNearbyLocal( "levelup", Position2D, Game.Random.Float( 0.95f, 1.05f ), 0.5f, 5f );

		ForEachStatus( status => status.OnLevelUp() );

		GenerateLevelUpChoices();
		IsChoosingLevelUpReward = true;
	}

	public void UseReroll()
	{
		if(NumRerollAvailable <= 0)
		{
			// todo: sfx
			return;
		}

		Manager.Instance.PlaySfxNearbyLocal( "reroll", Position2D, Utils.Map( NumRerollAvailable, 0, 20, 0.9f, 1.4f, EasingType.QuadIn ), 0.6f, 5f );

		NumRerollAvailable--;

		GenerateLevelUpChoices();

		ForEachStatus( status => status.OnReroll() );
	}

	public void UseChoiceHotkey(int num)
	{
		var index = num - 1;

		if ( !IsChoosingLevelUpReward || index >= LevelUpChoices.Count )
			return;

		AddStatus( TypeLibrary.GetType( LevelUpChoices[index].GetType() ) );
	}

	public float CheckDamageAmount( float damage, DamageType damageType )
	{
		if ( IsInvulnerable )
		{
			return 0f;
		}

		if ( HasStatus( TypeLibrary.GetType( typeof( ShieldStatus ) ) ) )
		{
			var shieldStatus = GetStatus( TypeLibrary.GetType( typeof( ShieldStatus ) ) ) as ShieldStatus;
			if ( shieldStatus != null && shieldStatus.IsShielded )
			{
				shieldStatus.LoseShield();
				return 0f;
			}
		}

		if ( Stats[PlayerStat.DamageReductionPercent] > 0f )
			damage *= (1f - MathX.Clamp( Stats[PlayerStat.DamageReductionPercent], 0f, 1f ));

		if ( damageType == DamageType.Explosion && Stats[PlayerStat.ExplosionDamageReductionPercent] > 0f )
			damage *= (1f - MathX.Clamp( Stats[PlayerStat.ExplosionDamageReductionPercent], 0f, 1f ));

		if ( damageType != DamageType.Explosion && Stats[PlayerStat.NonExplosionDamageIncreasePercent] > 0f )
			damage *= (1f + Stats[PlayerStat.NonExplosionDamageIncreasePercent]);

		return damage;
	}

	[Broadcast]
	public void Damage( float damage )
	{
		TimeSinceHurt = 0f;
		Flash( 0.125f );
		SpawnBlood( damage );
		//DamageNumbers.Add( (int)damage, Position2D + Vector2.Up * Radius * 3f + new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) ) * 0.2f, color: Color.Red );
		DamageNumbersLegacy.Create( damage, Position2D + new Vector2( 0.4f + Game.Random.Float( -0.1f, 0.1f ), Radius * 3f + Game.Random.Float( -0.2f, 0.3f ) ), color: Color.Red );

		if ( IsProxy )
			return;

		//if ( HasStatus( TypeLibrary.GetType( typeof( ShieldStatus ) ) ) )
		//{
		//	var shieldStatus = GetStatus( TypeLibrary.GetType( typeof( ShieldStatus ) ) ) as ShieldStatus;
		//	if ( shieldStatus != null && shieldStatus.IsShielded )
		//	{
		//		shieldStatus.LoseShield();
		//		return;
		//	}
		//}

		ForEachStatus( status => status.OnHurt( damage ) );

		Health -= damage;

		if ( Health <= 0f )
			Die();
	}

	[Broadcast]
	public void AddVelocity(Vector2 vel)
	{
		if ( IsProxy )
			return;

		Velocity += vel;
	}

	public void SpawnBlood( float damage )
	{
		var blood = Manager.Instance.SpawnBloodSplatter( Position2D );
		//blood.Sprite.Size *= Utils.Map( damage, 1f, 20f, 0.3f, 0.5f, EasingType.QuadIn ) * Game.Random.Float( 0.8f, 1.2f );
		blood.Transform.LocalScale *= Utils.Map( damage, 1f, 20f, 0.5f, 1.2f, EasingType.QuadIn ) * Game.Random.Float( 0.8f, 1.2f );
		blood.Lifetime *= 0.3f;
	}

	[Broadcast]
	public void Die()
	{
		if ( IsDead )
			return;

		IsDead = true;
		Sprite.Tint = new Color( 1f, 1f, 1f, 0.05f );
		ShadowOpacity = 0.2f;
		_isFlashing = false;
		IsReloading = false;

		Manager.Instance.PlaySfxNearbyLocal( "die", Position2D, pitch: Game.Random.Float( 1f, 1.2f ), volume: 1.5f, maxDist: 12f );

		Sprite.PlayAnimation( "ghost_idle" );

		if ( IsProxy )
			return;
		
		Manager.Instance.PlayerDied( this );

		//Game.Hud.RemoveChoicePanel();
	}

	[Broadcast]
	public void Revive()
	{
		if ( !IsDead )
			return;

		IsDead = false;
		IsChoosingLevelUpReward = false;
		IsDashing = false;
		IsReloading = true;
		Sprite.Tint = Color.White;
		ShadowOpacity = 0.8f;

		if ( IsProxy )
			return;

		Timer = Stats[PlayerStat.ReloadTime];
		ReloadProgress = 0f;
		DashProgress = 0f;
		ExperienceCurrent = 0;

		Health = Stats[PlayerStat.MaxHp] * 0.33f;

		//Nametag.SetVisible( true );

		//if ( ArrowAimer != null )
		//	ArrowAimer.Opacity = 1f;
	}

	public void ForEachStatus( Action<Status> action )
	{
		if ( IsProxy )
			return;

		foreach ( var (_, status) in Statuses )
		{
			action( status );
		}
	}

	void HandleStatuses( float dt )
	{
		foreach ( KeyValuePair<int, Status> pair in Statuses )
		{
			Status status = pair.Value;
			if ( status.ShouldUpdate )
				status.Update( dt );
		}
	}

	void HandleShooting( float dt )
	{
		if ( IsReloading )
		{
			ReloadProgress = Utils.Map( Timer, Stats[PlayerStat.ReloadTime], 0f, 0f, 1f );
			Timer -= dt * Stats[PlayerStat.ReloadSpeed];
			if ( Timer <= 0f )
			{
				Reload();
			}
		}
		else
		{
			Timer -= dt * Stats[PlayerStat.AttackSpeed] * (IsMoving ? 1f : Stats[PlayerStat.AttackSpeedStill]);
			if ( Timer <= 0f )
			{
				Shoot( isLastAmmo: AmmoCount == 1 );
				AmmoCount--;

				if ( AmmoCount <= 0 )
				{
					IsReloading = true;

					Timer += Stats[PlayerStat.ReloadTime];
				}
				else
				{
					Timer += Stats[PlayerStat.AttackTime];
				}
			}
		}

		//DebugText(AmmoCount.ToString() + "\nreloading: " + IsReloading + "\ntimer: " + Timer + "\nShotDelay: " + AttackTime + "\nReloadTime: " + ReloadTime + "\nAttackSpeed: " + AttackSpeed);
	}

	public void Shoot( bool isLastAmmo = false )
	{
		float start_angle = MathF.Sin( -_shotNum * 2f ) * Stats[PlayerStat.BulletInaccuracy];

		int num_bullets_int = (int)Stats[PlayerStat.NumProjectiles];
		float currAngleOffset = num_bullets_int == 1 ? 0f : -Stats[PlayerStat.BulletSpread] * 0.5f;
		float increment = num_bullets_int == 1 ? 0f : Stats[PlayerStat.BulletSpread] / (float)(num_bullets_int - 1);

		//var pos = Position2D + AimDir * 0.5f + new Vector2(0f, -OffsetY);
		var pos = Position2D + AimDir * 0.5f;

		for ( int i = 0; i < num_bullets_int; i++ )
		{
			var dir = Utils.RotateVector( AimDir, start_angle + currAngleOffset + increment * i );
				SpawnBullet( pos, dir, isLastAmmo );
		}

		Manager.Instance.PlaySfxNearby( "shoot", pos, pitch: Utils.Map( _shotNum, 0f, (float)Stats[PlayerStat.MaxAmmoCount], 1f, 1.25f ), volume: 1f, maxDist: 4f );

		Velocity -= AimDir * Stats[PlayerStat.Recoil];

		_shotNum++;
	}

	void SpawnBullet( Vector2 pos, Vector2 dir, bool isLastAmmo = false )
	{
		var damage = (Stats[PlayerStat.BulletDamage] * Stats[PlayerStat.BulletDamageMultiplier] + Stats[PlayerStat.BulletFlatDamageAddition]) * GetDamageMultiplier();
		if ( isLastAmmo )
			damage *= Stats[PlayerStat.LastAmmoDamageMultiplier];

		if ( Stats[PlayerStat.DamagePerEarlierShot] > 0f )
			damage += _shotNum * Stats[PlayerStat.DamagePerEarlierShot];

		if ( Stats[PlayerStat.DamageForSpeed] > 0f )
		{
			damage += Stats[PlayerStat.DamageForSpeed] * Velocity.Length;

			if ( IsDashing )
				damage += Stats[PlayerStat.DamageForSpeed] * DashVelocity.Length;
		}

		var bulletObj = BulletPrefab.Clone( (Vector3)pos );
		var bullet = bulletObj.Components.Get<Bullet>();

		//bullet.Depth = -1f;
		bullet.Velocity = dir * Stats[PlayerStat.BulletSpeed];
		bullet.Shooter = this;
		bullet.TempWeight = 3f;
		//bullet.BasePivotY = Utils.Map( damage, 5f, 30f, -1.2f, -0.3f );

		bullet.Stats[BulletStat.Damage] = damage;
		bullet.Stats[BulletStat.Force] = Stats[PlayerStat.BulletForce];
		bullet.Stats[BulletStat.Lifetime] = Stats[PlayerStat.BulletLifetime];
		bullet.Stats[BulletStat.NumPiercing] = (int)MathF.Round( Stats[PlayerStat.BulletNumPiercing] );
		bullet.Stats[BulletStat.FireIgniteChance] = Stats[PlayerStat.ShootFireIgniteChance];
		bullet.Stats[BulletStat.FreezeChance] = Stats[PlayerStat.ShootFreezeChance];
		bullet.Stats[BulletStat.GrowDamageAmount] = Stats[PlayerStat.BulletDamageGrow];
		bullet.Stats[BulletStat.ShrinkDamageAmount] = Stats[PlayerStat.BulletDamageShrink];
		bullet.Stats[BulletStat.DistanceDamageAmount] = Stats[PlayerStat.BulletDistanceDamage];
		bullet.Stats[BulletStat.HealTeammateAmount] = Stats[PlayerStat.BulletHealTeammateAmount];

		if ( Stats[PlayerStat.GrenadesCanCrit] <= 0f )
		{
			bullet.Stats[BulletStat.CriticalChance] = Stats[PlayerStat.CritChance];
			bullet.Stats[BulletStat.CriticalMultiplier] = Stats[PlayerStat.CritMultiplier];
		}

		bullet.Init();

		bullet.GameObject.NetworkSpawn(Network.OwnerConnection); // todo: not necessary to specify connection?
		//bullet.Transform.Position = (Vector3)pos;

		//Game.AddThing( bullet );
	}

	void Reload()
	{
		AmmoCount = (int)Stats[PlayerStat.MaxAmmoCount];
		IsReloading = false;
		_shotNum = 0;
		ReloadProgress = 0f;

		ForEachStatus( status => status.OnReload() );

		//Manager.Instance.PlaySfxNearbyLocal("reload.end", Position2D, pitch: 1f, volume: 0.5f, 5f);
	}

	public float GetDamageMultiplier()
	{
		float damageMultiplier = Stats[PlayerStat.OverallDamageMultiplier];

		if ( Stats[PlayerStat.LowHealthDamageMultiplier] > 1f )
			damageMultiplier *= Utils.Map( Health, Stats[PlayerStat.MaxHp], 0f, 1f, Stats[PlayerStat.LowHealthDamageMultiplier] );

		if ( Stats[PlayerStat.FullHealthDamageMultiplier] > 1f && !(Health < Stats[PlayerStat.MaxHp]) )
			damageMultiplier *= Stats[PlayerStat.FullHealthDamageMultiplier];

		return damageMultiplier;
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		base.Colliding( other, percent, dt );

		if ( IsDead )
			return;

		ForEachStatus( status => status.Colliding( other, percent, dt ) );

		if ( other is Enemy enemy && !enemy.IsDying )
		{
			if ( !Position2D.Equals( other.Position2D ) )
			{
				var spawnFactor = Utils.Map( enemy.ElapsedTime, 0f, enemy.SpawnTime, 0f, 1f, EasingType.QuadIn );
				Velocity += (Position2D - other.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 100f ) * (1f + other.TempWeight) * spawnFactor * dt;
			}
		}
		else if ( other is Player player )
		{
			if ( !player.IsDead && !Position2D.Equals( other.Position2D ) )
			{
				Velocity += (Position2D - other.Position2D).Normal * Utils.Map( percent, 0f, 1f, 0f, 100f ) * (1f + other.TempWeight) * dt;
			}
		}
	}

	[Broadcast]
	public void SpawnDashCloudClient()
	{
		Manager.Instance.SpawnCloud( Position2D + new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) ) * 0.05f );
	}

	public void GenerateLevelUpChoices()
	{
		LevelUpChoices.Clear();

		int numChoices = Math.Clamp( (int)MathF.Round( Stats[PlayerStat.NumUpgradeChoices] ), 1, 6 );
		List<TypeDescription> statusTypes = StatusManager.GetRandomStatuses( this, numChoices );

		for ( int i = 0; i < statusTypes.Count; i++ )
		{
			var type = statusTypes[i];
			var status = StatusManager.CreateStatus( type );
			var currLevel = GetStatusLevel( type );
			status.Level = currLevel + 1;
			LevelUpChoices.Add( status );
		}
	}

	[Broadcast]
	public void Restart()
	{
		Sprite.PlayAnimation( "idle" );
		//AnimationSpeed = 0.66f;

		if ( IsProxy )
			return;

		Position2D = new Vector3( Game.Random.Float( -3f, 3f ), Game.Random.Float( -3f, 3f ));
		Manager.Instance.Camera2D.SetPos( Position2D );

		InitializeStats();

		Manager.Instance.PlaySfxNearbyLocal("restart", Position2D, Game.Random.Float( 0.95f, 1.05f ), 0.66f, 4f );
	}

	public void SpawnBulletRing( Vector2 pos, int numBullets, Vector2 aimDir )
	{
		float increment = 360f / numBullets;

		for ( int i = 0; i < numBullets; i++ )
		{
			var dir = Utils.RotateVector( aimDir, i * increment );
			SpawnBullet( pos, dir );
		}

		Manager.Instance.PlaySfxNearby( "shoot", pos, pitch: 1f, volume: 1f, maxDist: 3f );
	}

	public Grenade SpawnGrenade( Vector2 pos, Vector2 vel )
	{
		var grenadeObj = Manager.Instance.GrenadePrefab.Clone();

		var grenade = grenadeObj.Components.Get<Grenade>();
		grenade.Velocity = vel;
		grenade.ExplosionSizeMultiplier = Stats[PlayerStat.ExplosionSizeMultiplier];
		grenade.Player = this;
		grenade.StickyPercent = Stats[PlayerStat.GrenadeStickyPercent];
		grenade.FearChance = Stats[PlayerStat.GrenadeFearChance];

		if ( Stats[PlayerStat.GrenadesCanCrit] > 0f )
		{
			grenade.CriticalChance = Stats[PlayerStat.CritChance];
			grenade.CriticalMultiplier = Stats[PlayerStat.CritMultiplier];
		}

		grenadeObj.NetworkSpawn(Network.OwnerConnection);
		grenadeObj.Transform.Position = new Vector3( pos.x, pos.y, Globals.GetZPos( pos.y ) );

		Manager.Instance.AddThing( grenade );

		return grenade;
	}

	[Broadcast]
	public void CreateShieldVfx()
	{
		_shieldVfx = Manager.Instance.ShieldVfxPrefab.Clone( Transform.Position );
		_shieldVfx.Parent = GameObject;
		_shieldVfx.Transform.LocalPosition = new Vector3( 0f, 0f, 1f );
		_shieldVfx.Components.Get<SpriteRenderer>().Size = new Vector2(1.8f);
	}

	[Broadcast]
	public void RemoveShieldVfx()
	{
		if ( _shieldVfx != null )
		{
			_shieldVfx.Destroy();
			_shieldVfx = null;
		}
	}

	[Broadcast]
	public void PlaySfx(string name, Vector2 pos, float pitch, float volume)
	{
		if ( IsProxy )
			return;

		var sfx = Sound.Play( name, new Vector3( pos.x, pos.y, Globals.SFX_DEPTH ) );
		if ( sfx != null )
		{
			sfx.Volume = volume;
			sfx.Pitch = pitch;
		}
	}

	public override void SetFlipHorizontal( bool flip )
	{
		Body.Transform.LocalRotation = new Angles( flip ? 180f : 0f, 0f, 0f );
	}
}
