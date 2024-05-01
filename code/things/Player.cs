using Sandbox;
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
	[Property] public SpriteRenderer Sprite { get; set; }
	[Property] public GameObject ArrowAimerPrefab { get; set;  }

	[Sync] public float Health { get; set; }
	[Sync] public Vector2 Velocity { get; set; }

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

		ArrowAimer = ArrowAimerPrefab.Clone(Transform.Position);
		ArrowAimer.SetParent( GameObject );
		ArrowAimer.NetworkMode = NetworkMode.Never;
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
		Radius = 10f;
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
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"Position2D: {Position2D}\nGridPos: {GridPos}", new global::Transform( Transform.Position + new Vector3(0f, -35f, 0f) ) );

		//Gizmo.Draw.LineSphere( Transform.Position, Radius );

		if ( Velocity.x > 0f )
			Sprite.FlipHorizontal = true;
		else if ( Velocity.x < 0f )
			Sprite.FlipHorizontal = false;

		if ( IsProxy )
			return;

		float dt = Time.Delta;

		Vector2 inputVector = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x );

		if ( inputVector.LengthSquared > 0f )
			Velocity += inputVector.Normal * Stats[PlayerStat.MoveSpeed] * BASE_MOVE_SPEED * Globals.MOVE_FACTOR * dt;

		Transform.Position += (Vector3)Velocity * dt;

		if ( IsDashing )
			Transform.Position += (Vector3)DashVelocity * Globals.MOVE_FACTOR * dt;

		Velocity = Utils.DynamicEaseTo( Velocity, Vector2.Zero, 0.2f, dt );
		TempWeight *= (1f - dt * 4.7f);

		HandleBounds();

		Manager.Instance.Camera2D.TargetPos = Position2D;

		if(Input.UsingController)
		{

		}
		else
		{
			AimDir = (Manager.Instance.MouseWorldPos - Position2D).Normal;
		}

		if ( ArrowAimer != null )
		{
			//ArrowAimer.LocalRotation = (MathF.Atan2( AimDir.y, AimDir.x ) * (180f / MathF.PI));
			ArrowAimer.Transform.LocalPosition = AimDir * 35f;
		}

		if ( !IsDead )
		{
			HandleDashing( dt );
		}

		var gridPos = Manager.Instance.GetGridSquareForPos( Position2D );
		if ( gridPos != GridPos )
		{
			Manager.Instance.DeregisterThingGridSquare( this, GridPos );
			Manager.Instance.RegisterThingGridSquare( this, gridPos );
			GridPos = gridPos;
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
			//HandleStatuses( dt );
			//HandleShooting( dt );
			//HandleFlashing( dt );
			HandleRegen( dt );
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
				Sprite.Color = Color.White;
				DashFinished();
			}
			else
			{
				if ( IsInvulnerable )
					Sprite.Color = new Color( Sandbox.Game.Random.Float( 0.1f, 0.25f ), Sandbox.Game.Random.Float( 0.1f, 0.25f ), 1f );

				if ( _dashCloudTime > Sandbox.Game.Random.Float( 0.1f, 0.2f ) )
				{
					//SpawnCloudClient();
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

		//Game.PlaySfxNearby( "player.dash", Position + dashDir * 0.5f, pitch: Utils.Map( NumDashesAvailable, 0, 5, 1f, 0.9f ), volume: 1f, maxDist: 4f );
		//SpawnCloudClient();
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

		//Game.PlaySfxTarget( To.Single( Client ), "player.dash.recharge", Position, pitch: Utils.Map( NumDashesAvailable, 1, numDashes, 1f, 1.2f ), volume: 0.2f );
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

		Sprite.Color = new Color( 1f, 0f, 0f );
		_isFlashing = true;
		_flashTimer = time;
	}

	public void Heal( float amount, float flashTime )
	{
		Sprite.Color = new Color( 0f, 1f, 0f );
		_isFlashing = true;
		_flashTimer = flashTime;

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
				Sprite.Color = Color.White;
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

	public void AddExperience( int xp )
	{
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

	public void LevelUp()
	{
		ExperienceCurrent -= ExperienceRequired;

		Level++;
		ExperienceRequired = GetExperienceReqForLevel( Level + 1 );
		NumRerollAvailable += (int)Stats[PlayerStat.NumRerollsPerLevel];

		ForEachStatus( status => status.OnLevelUp() );

		IsChoosingLevelUpReward = true;
	}

	public void Die()
	{
		if ( IsDead )
			return;

		IsDead = true;
		//AnimationPath = $"textures/sprites/player_ghost_idle.frames";
		Manager.Instance.PlayerDied( this );
		//EnableDrawing = false;
		Sprite.Color = new Color( 1f, 1f, 1f, 0.05f );
		//ShadowOpacity = 0.1f;
		_isFlashing = false;
		IsReloading = false;

		//Game.PlaySfxNearby( "die", Position, pitch: Sandbox.Game.Random.Float( 1f, 1.2f ), volume: 1.5f, maxDist: 12f );
		//DieClient();
		//DieClientSingle( To.Single( Client ) );
	}

	//[Broadcast]
	//public void DieClient()
	//{
	//	Nametag.SetVisible( false );

	//	if ( ArrowAimer != null )
	//		ArrowAimer.Opacity = 0f;
	//}

	//[Broadcast]
	//public void DieClientSingle()
	//{
	//	Game.Hud.RemoveChoicePanel();
	//}

	public void Revive()
	{
		if ( !IsDead )
			return;

		IsChoosingLevelUpReward = false;
		IsDashing = false;
		IsReloading = false;
		ReloadProgress = 0f;
		DashProgress = 0f;
		ExperienceCurrent = 0;

		Health = Stats[PlayerStat.MaxHp] * 0.33f;
		Sprite.Color = Color.White;

		IsDead = false;
		//ReviveClient();
	}

	//[Broadcast]
	//public void ReviveClient()
	//{
	//	Nametag.SetVisible( true );

	//	if ( ArrowAimer != null )
	//		ArrowAimer.Opacity = 1f;
	//}

	public void ForEachStatus( Action<Status> action )
	{
		if ( IsProxy )
			return;

		foreach ( var (_, status) in Statuses )
		{
			action( status );
		}
	}
}