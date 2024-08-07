﻿using Sandbox.Diagnostics;
using SpriteTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Manager;

public abstract class Enemy : Thing
{
	[Sync] public float Health { get; set; }
	public Vector2 Velocity { get; set; }
	[Sync] public bool FlipX { get; set; }
	public float MoveTimeOffset { get; set; }

	private float _flashTimer;
	private bool _isFlashing;

	[Sync] public float MaxHealth { get; protected set; }

	public bool IsSpawning { get; private set; }
	public TimeSince TimeSinceSpawn { get; private set; }
	public bool IsDying { get; private set; }
	public float DeathTimeElapsed { get; private set; }
	public float DeathTime { get; protected set; }
	public float DeathProgress { get; private set; }
	private Vector3 _deathScale;

	[Sync] public bool IsAttacking { get; private set; }
	private float _aggroTimer;
	public bool CanAttack { get; set; }
	public bool CanAttackAnim { get; set; }
	public bool CanTurn { get; set; }
	public bool DontChangeAnimSpeed { get; set; }
	public virtual bool CanBleed => true;
	public float AggroRange { get; protected set; }
	protected const float AGGRO_START_TIME = 0.2f;
	protected const float AGGRO_LOSE_TIME = 0.4f;

	public float DamageToPlayer { get; protected set; }

	public float ScaleFactor { get; protected set; }
	public float PushStrength { get; protected set; }

	public float SpawnTime { get; protected set; }
	public float ShadowFullOpacity { get; protected set; }
	public virtual float FullOpacity => 1f;

	public string AnimSpawnPath { get; protected set; }
	public string AnimIdlePath { get; protected set; }
	public string AnimAttackPath { get; protected set; }
	public string AnimDiePath { get; protected set; }

	public float Deceleration { get; protected set; }
	public float DecelerationAttacking { get; protected set; }

	private TimeSince _spawnCloudTime;

	public Dictionary<TypeDescription, EnemyStatus> EnemyStatuses = new Dictionary<TypeDescription, EnemyStatus>();

	private BurningVfx _burningVfx;
	private FrozenVfx _frozenVfx;
	private FearVfx _fearVfx;
	public bool IsFrozen { get; set; }
	public bool IsFeared { get; set; }

	private float _animSpeed;
	public float AnimSpeed { get { return _animSpeed; } set { _animSpeed = value; Sprite.PlaybackSpeed = _animSpeed * _animSpeedModifier; } }
	private float _animSpeedModifier;
	public float AnimSpeedModifier { get { return _animSpeedModifier; } set { _animSpeedModifier = value; Sprite.PlaybackSpeed = _animSpeed * _animSpeedModifier; } }

	public int CoinValueMin { get; protected set; }
	public int CoinValueMax { get; protected set; }

	public virtual float HeightVariance => 0f;
	public virtual float WidthVariance => 0f;

	protected override void OnAwake()
	{
		base.OnAwake();

		Sprite.Transform.LocalScale = new Vector3( Scale * Game.Random.Float( 1f - HeightVariance, 1f + HeightVariance ), Scale * Game.Random.Float( 1f - WidthVariance, 1f + WidthVariance ), 1f ) * Globals.SPRITE_SCALE;

		AnimSpawnPath = "spawn";
		AnimIdlePath = "walk";
		AnimAttackPath = "attack";
		AnimDiePath = "die";

		_animSpeed = 1f;
		_animSpeedModifier = 1f;

		SpawnShadow( ShadowScale, ShadowOpacity );

		IsSpawning = true;
		TimeSinceSpawn = 0f;
		SpawnTime = 1.75f;

		MoveTimeOffset = Game.Random.Float( 0f, 4f );
		Deceleration = 1.47f;
		DecelerationAttacking = 1.33f;
		DeathTime = 0.3f;
		AggroRange = 1.4f;
		CanAttack = true;
		CanAttackAnim = true;
		CanTurn = true;

		CoinValueMin = 1;
		CoinValueMax = 1;

		if ( IsProxy )
			return;
	}

	protected override void OnUpdate()
	{
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"Anim: {Sprite.CurrentAnimation.Name}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.7f, 0f ) ) );

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.05f );
		//Gizmo.Draw.LineSphere( (Vector3)Position2D, Radius );

		//Gizmo.Draw.Color = Color.Black.WithAlpha( 0.2f );
		//Gizmo.Draw.Text( $"IsSpawning: {IsSpawning}", new global::Transform( (Vector3)Position2D + new Vector3( 0f, -0.2f, 0f ) ) );

		if ( Manager.Instance.IsGameOver )
			return;

		base.OnUpdate();

		float dt = Time.Delta;

		var closestPlayer = Manager.Instance.GetClosestPlayer( (Vector3)Position2D );
		if ( closestPlayer == null )
			return;

		UpdateSprite( closestPlayer );

		if ( IsSpawning )
		{
			HandleSpawning();
			return;
		}

		HandleFlashing( dt );

		if ( IsDying )
		{
			HandleDying( dt );
			return;
		}

		Sprite.SpriteFlags = FlipX ? SpriteFlags.HorizontalFlip : SpriteFlags.None;

		if ( IsProxy )
			return;

		HandleStatuses( dt );

		UpdatePosition( dt );
		Transform.Position = Transform.Position.WithZ( Globals.GetZPos( Position2D.y ) );

		ClampToBounds();
		HandleDeceleration( dt );
		//Depth = -Position.y * 10f;

		CheckCollisions( dt );

		TempWeight *= (1f - dt * 4.7f);

		HandleAttacking( closestPlayer, dt );
	}

	protected virtual void HandleStatuses( float dt )
	{
		for ( int i = EnemyStatuses.Count - 1; i >= 0; i-- )
		{
			var status = EnemyStatuses.Values.ElementAt( i );
			if ( status.ShouldUpdate )
				status.Update( dt );
		}
	}

	protected virtual void HandleDeceleration( float dt )
	{
		Velocity *= (1f - dt * (IsAttacking ? DecelerationAttacking : Deceleration));
	}

	protected virtual void HandleAttacking( Player targetPlayer, float dt )
	{
		float dist_sqr = (targetPlayer.Position2D - Position2D).LengthSquared;
		float attack_dist_sqr = MathF.Pow( AggroRange, 2f );

		if ( !IsAttacking )
		{
			if ( CanAttack )
			{
				if ( dist_sqr < attack_dist_sqr )
				{
					_aggroTimer += dt;
					if ( _aggroTimer > AGGRO_START_TIME )
					{
						StartAttacking();
						_aggroTimer = 0f;
					}
				}
				else
				{
					_aggroTimer = 0f;
				}
			}
		}
		else
		{
			if ( dist_sqr > attack_dist_sqr )
			{
				_aggroTimer += dt;
				if ( _aggroTimer > AGGRO_LOSE_TIME )
				{
					IsAttacking = false;
				}
			}
			else
			{
				if ( !DontChangeAnimSpeed )
					AnimSpeed = Utils.Map( dist_sqr, attack_dist_sqr, 0f, 1f, 4f, EasingType.Linear );

				_aggroTimer = 0f;
			}
		}
	}

	public virtual void StartAttacking()
	{
		IsAttacking = true;
	}

	protected virtual void UpdateSprite( Player targetPlayer )
	{
		if ( IsDying ) return;

		else if ( !IsAttacking )
		{
			if ( IsSpawning ) Sprite.PlayAnimation( AnimSpawnPath );
			else Sprite.PlayAnimation( AnimIdlePath );

			if ( !DontChangeAnimSpeed )
				AnimSpeed = Utils.Map( Utils.FastSin( MoveTimeOffset + Time.Now * 7.5f ), -1f, 1f, 0.75f, 3f, EasingType.ExpoIn );

			if ( !IsProxy && CanTurn && !IsFrozen )
			{
				if ( MathF.Abs( Velocity.x ) > 0.175f )
					FlipX = Velocity.x > 0f;
			}
		}
		else
		{
			Sprite.PlayAnimation( AnimAttackPath );
			if ( !DontChangeAnimSpeed )
			{
				float dist_sqr = (targetPlayer.Position2D - Position2D).LengthSquared;
				float attack_dist_sqr = MathF.Pow( AggroRange, 2f );
				AnimSpeed = Utils.Map( dist_sqr, attack_dist_sqr, 0f, 1f, 4f, EasingType.Linear );
			}

			if ( !IsProxy && CanTurn && !IsFrozen )
			{
				if ( IsFeared )
					FlipX = targetPlayer.Position2D.x < Position2D.x;
				else
					FlipX = targetPlayer.Position2D.x > Position2D.x;
			}
		}
	}

	void HandleFlashing( float dt )
	{
		if ( _isFlashing )
		{
			_flashTimer -= dt;
			if ( _flashTimer < 0f )
			{
				_isFlashing = false;
				Sprite.FlashTint = Color.White.WithAlpha( 0f );
				Sprite.Tint = Color.Lerp( Color.White, Color.Black, Utils.Map( Health, MaxHealth, 0f, 0f, 0.7f ) ).WithAlpha( FullOpacity );
			}
		}
	}

	void HandleDying( float dt )
	{
		DeathTimeElapsed += dt;

		Sprite.Transform.LocalScale = _deathScale * Utils.Map( DeathTimeElapsed, 0f, DeathTime, 1f, 1.2f );

		if ( DeathTimeElapsed > DeathTime )
		{
			DeathProgress = 1f;
			FinishDying();
		}
		else
		{
			DeathProgress = Utils.Map( DeathTimeElapsed, 0f, DeathTime, 0f, 1f );
			ShadowOpacity = Utils.Map( DeathProgress, 0f, 1f, ShadowFullOpacity, 0f, EasingType.QuadIn );
			SpriteDirty = true;
		}
	}

	void HandleSpawning()
	{
		if ( TimeSinceSpawn > SpawnTime )
		{
			IsSpawning = false;
			ShadowOpacity = ShadowFullOpacity;
		}
		else
		{
			//Sprite.PlayAnimation( AnimSpawnPath );

			if ( _spawnCloudTime > (0.3f / TimeScale) )
			{
				var cloud = Manager.Instance.SpawnCloud( Position2D + new Vector2( 0f, 0.05f ) );
				cloud.Velocity = new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) ).Normal * Game.Random.Float( 0.2f, 0.6f );
				_spawnCloudTime = Game.Random.Float( 0f, 0.15f );
			}

			ShadowOpacity = Utils.Map( TimeSinceSpawn, 0f, SpawnTime, 0f, ShadowFullOpacity );
		}

		ShadowSprite.Tint = Color.Black.WithAlpha( ShadowOpacity );
	}

	void ClampToBounds()
	{
		var x_min = Manager.Instance.BOUNDS_MIN.x + Radius;
		var x_max = Manager.Instance.BOUNDS_MAX.x - Radius;
		var y_min = Manager.Instance.BOUNDS_MIN.y;
		var y_max = Manager.Instance.BOUNDS_MAX.y - Radius;
		Position2D = new Vector2( MathX.Clamp( Position2D.x, x_min, x_max ), MathX.Clamp( Position2D.y, y_min, y_max ) );
	}

	protected virtual void UpdatePosition( float dt )
	{

	}

	[Broadcast]
	public virtual void Damage( float damage, Guid playerId, Vector2 addVel, float addTempWeight, bool isCrit = false )
	{
		if ( IsDying )
			return;

		Player player = null;
		if ( playerId != Guid.Empty )
		{
			var playerObj = Scene.Directory.FindByGuid( playerId );
			player = playerObj?.Components.Get<Player>() ?? null;

			if ( player != null )
			{
				if ( IsFeared )
				{
					damage *= player.Stats[PlayerStat.FearDamageMultiplier];
				}
			}
		}

		Flash( 0.12f );

		//DamageNumbers.Add( (int)damage, Position2D + Vector2.Up * Radius * 3f + new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) ) * 0.2f, color: isCrit ? Color.Yellow : Color.White );
		DamageNumbersLegacy.Create( damage, Position2D + new Vector2( 0.4f + Game.Random.Float( -0.1f, 0.1f ), Radius * 3f + Game.Random.Float( -0.2f, 0.3f ) ), color: isCrit ? Color.Yellow : Color.White );

		if ( IsProxy )
			return;

		if ( player != null )
		{
			if ( IsFeared )
			{
				if ( player.Stats[PlayerStat.FearDrainPercent] > 0f )
					player.RegenHealth( damage * player.Stats[PlayerStat.FearDrainPercent] );
			}
		}

		Velocity += addVel;
		TempWeight += addTempWeight;

		Health -= damage;

		if ( Health <= 0f )
			StartDying( player?.GameObject.Id ?? Guid.Empty );
	}

	public virtual void DamageFire( float damage, Player player )
	{
		if ( IsFrozen )
			damage *= player?.Stats[PlayerStat.FreezeFireDamageMultiplier] ?? 1f;

		Damage( damage, player?.GameObject.Id ?? Guid.Empty, addVel: Vector2.Zero, addTempWeight: 0f );
	}

	[Broadcast]
	public virtual void StartDying( Guid playerId )
	{
		IsDying = true;
		DeathProgress = 0f;
		DeathTimeElapsed = 0f;
		Sprite.PlayAnimation( AnimDiePath );
		AnimSpeed = Game.Random.Float( 6.5f, 9f );

		_isFlashing = false;

		Sprite.FlashTint = Color.White.WithAlpha( 0f );

		_deathScale = Sprite.Transform.LocalScale;

		if ( CanBleed )
			Manager.Instance.SpawnBloodSplatter( Position2D );

		Manager.Instance.PlayEnemyDeathSfxLocal( Position2D );

		if ( IsProxy )
			return;

		Sandbox.Services.Stats.Increment( "zombies_killed", 1 );

		Player player = null;
		if ( playerId != Guid.Empty )
		{
			var playerObj = Scene.Directory.FindByGuid( playerId );
			player = playerObj?.Components.Get<Player>() ?? null;
			if ( player is not null )
			{
				player.ForEachStatus( status => status.OnKill( this ) );

				//if ( this is not Crate )
				//{
				//	Sandbox.Services.Stats.Increment( player.Client, "kills", 1, $"{GetType().Name.ToLowerInvariant()}" );
				//}
				//else
				//{
				//	Sandbox.Services.Stats.Increment( player.Client, "crates", 1 );
				//}
			}
		}

		DropLoot( player );

		for ( int i = EnemyStatuses.Count - 1; i >= 0; i-- )
			EnemyStatuses.Values.ElementAt( i ).StartDying();
	}

	public virtual void DropLoot( Player player )
	{
		var coin_chance = player != null ? Utils.Map( player.Stats[PlayerStat.Luck], 0f, 10f, 0.5f, 1f ) : 0.5f;
		if ( Game.Random.Float( 0f, 1f ) < coin_chance )
		{
			Manager.Instance.SpawnCoin( Position2D, Game.Random.Int( CoinValueMin, CoinValueMax ) );
		}
		else
		{
			var lowest_hp_percent = 1f;
			foreach ( Player p in Scene.GetAllComponents<Player>().Where( x => !x.IsDead ) )
				lowest_hp_percent = MathF.Min( lowest_hp_percent, p.Health / p.Stats[PlayerStat.MaxHp] );

			var health_pack_chance = Utils.Map( lowest_hp_percent, 1f, 0f, 0f, 0.1f );
			if ( Game.Random.Float( 0f, 1f ) < health_pack_chance )
			{
				Manager.Instance.SpawnHealthPack( Position2D, vel: Vector2.Zero );
			}
		}
	}

	public virtual void FinishDying()
	{
		Remove();
	}

	public override void Remove()
	{
		if ( !IsProxy )
		{
			for ( int i = EnemyStatuses.Count - 1; i >= 0; i-- )
				EnemyStatuses.Values.ElementAt( i ).Remove();

			EnemyStatuses.Clear();
		}

		base.Remove();
	}

	public void Flash( float time )
	{
		if ( _isFlashing )
			return;

		Sprite.Tint = Color.White.WithAlpha( 1f );
		Sprite.FlashTint = Color.White.WithAlpha( 1f );
		_isFlashing = true;
		_flashTimer = time;
	}

	void CheckCollisions( float dt )
	{
		for ( int dx = -1; dx <= 1; dx++ )
		{
			for ( int dy = -1; dy <= 1; dy++ )
			{
				Manager.Instance.HandleThingCollisionForGridSquare( this, new GridSquare( GridPos.x + dx, GridPos.y + dy ), dt );
			}
		}
	}

	public override void Colliding( Thing other, float percent, float dt )
	{
		for ( int i = EnemyStatuses.Count - 1; i >= 0; i-- )
			EnemyStatuses.Values.ElementAt( i ).Colliding( other, percent, dt );
	}

	public TStatus AddEnemyStatus<TStatus>()
		where TStatus : EnemyStatus
	{
		var type = TypeLibrary.GetType<TStatus>();

		if ( EnemyStatuses.TryGetValue( type, out var status ) )
		{
			status.Refresh();
			return (TStatus)status;
		}
		else
		{
			status = type.Create<EnemyStatus>();
			EnemyStatuses.Add( type, status );
			status.Init( this );
			return (TStatus)status;
		}
	}

	public void RemoveEnemyStatus<TStatus>( TStatus status )
		where TStatus : EnemyStatus
	{
		if ( EnemyStatuses.Remove( TypeLibrary.GetType<TStatus>(), out var existing ) )
		{
			Assert.AreEqual( existing, status );
			status.Remove();
		}
	}

	private void RemoveEnemyStatus<TStatus>()
		where TStatus : EnemyStatus
	{
		if ( EnemyStatuses.Remove( TypeLibrary.GetType<TStatus>(), out var status ) )
		{
			status.Remove();
		}
	}

	private TStatus GetEnemyStatus<TStatus>()
		where TStatus : EnemyStatus
	{
		return EnemyStatuses.TryGetValue( TypeLibrary.GetType<TStatus>(), out var status )
			? (TStatus)status
			: null;
	}

	public bool HasEnemyStatus<TStatus>( TStatus status )
		where TStatus : EnemyStatus
	{
		return EnemyStatuses.TryGetValue( TypeLibrary.GetType<TStatus>(), out var existing ) && existing == status;
	}

	public bool HasEnemyStatus<TStatus>()
		where TStatus : EnemyStatus
	{
		return EnemyStatuses.ContainsKey( TypeLibrary.GetType<TStatus>() );
	}

	[Broadcast]
	public void CreateBurningVfx()
	{
		var obj = Manager.Instance.BurningVfxPrefab.Clone( Transform.Position );
		obj.Parent = GameObject;
		obj.Transform.LocalPosition = new Vector3( 0f, 0f, 1f );

		_burningVfx = obj.Components.Get<BurningVfx>();
		_burningVfx.Enemy = this;
	}

	[Broadcast]
	public void RemoveBurningVfx()
	{
		if ( _burningVfx != null )
		{
			_burningVfx.GameObject.Destroy();
			_burningVfx = null;
		}
	}

	[Broadcast]
	public void CreateFrozenVfx()
	{
		var obj = Manager.Instance.FrozenVfxPrefab.Clone( Transform.Position );
		obj.Parent = GameObject;
		obj.Transform.LocalPosition = new Vector3( 0f, 0f, 2f );

		_frozenVfx = obj.Components.Get<FrozenVfx>();
		_frozenVfx.Enemy = this;
	}

	[Broadcast]
	public void RemoveFrozenVfx()
	{
		if ( _frozenVfx != null )
		{
			_frozenVfx.GameObject.Destroy();
			_frozenVfx = null;
		}
	}

	[Broadcast]
	public void CreateFearVfx()
	{
		var obj = Manager.Instance.FearVfxPrefab.Clone( Transform.Position );
		obj.Parent = GameObject;
		obj.Transform.LocalPosition = new Vector3( 0f, 0f, 3f );

		_fearVfx = obj.Components.Get<FearVfx>();
		_fearVfx.Enemy = this;
	}

	[Broadcast]
	public void RemoveFearVfx()
	{
		if ( _fearVfx != null )
		{
			_fearVfx.GameObject.Destroy();
			_fearVfx = null;
		}
	}

	public void Burn( Player player, float damage, float lifetime, float spreadChance )
	{
		var burning = AddEnemyStatus<BurningEnemyStatus>();
		burning.Player = player;
		burning.Damage = damage;
		burning.Lifetime = lifetime;
		burning.SpreadChance = spreadChance;

		if ( player != null )
			player.ForEachStatus( status => status.OnBurn( this ) );
	}

	public void Freeze( Player player )
	{
		if ( IsDying )
			return;

		var frozen = AddEnemyStatus<FrozenEnemyStatus>();
		frozen.Player = player;
		frozen.SetLifetime( player.Stats[PlayerStat.FreezeLifetime] );
		frozen.SetTimeScale( player.Stats[PlayerStat.FreezeTimeScale] );

		if ( player != null )
			player.ForEachStatus( status => status.OnFreeze( this ) );
	}

	public void Fear( Player player )
	{
		if ( IsDying )
			return;

		var fear = AddEnemyStatus<FearEnemyStatus>();
		fear.Player = player;
		fear.SetLifetime( player?.Stats[PlayerStat.FearLifetime] ?? 4f );

		if ( player != null )
		{
			if ( player.Stats[PlayerStat.FearPainPercent] > 0f )
				fear.PainPercent = player.Stats[PlayerStat.FearPainPercent];

			player.ForEachStatus( status => status.OnFear( this ) );
		}
	}

	protected virtual void OnDamagePlayer( Player player, float damage )
	{
		if ( player.Stats[PlayerStat.ThornsPercent] > 0f )
			Damage( damage * player.Stats[PlayerStat.ThornsPercent] * player.GetDamageMultiplier(), player.GameObject.Id, addVel: Vector2.Zero, addTempWeight: 0f, isCrit: false );

		if ( Game.Random.Float( 0f, 1f ) < player.Stats[PlayerStat.FreezeOnMeleeChance] )
		{
			if ( !HasEnemyStatus<FrozenEnemyStatus>() )
				Manager.Instance.PlaySfxNearby( "frozen", Position2D, pitch: Game.Random.Float( 1.1f, 1.2f ), volume: 1.5f, maxDist: 5f );

			Freeze( player );
		}

		if ( Game.Random.Float( 0f, 1f ) < player.Stats[PlayerStat.FearOnMeleeChance] )
		{
			if ( !HasEnemyStatus<FearEnemyStatus>() )
				Manager.Instance.PlaySfxNearby( "fear", Position2D, pitch: Game.Random.Float( 0.95f, 1.05f ), volume: 0.6f, maxDist: 5f );

			Fear( player );
		}
	}
}
