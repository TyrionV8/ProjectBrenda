using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Sim;

public enum MatchMode
{
	Campaign,
	Versus,
}

public enum MatchState
{
	Playing,
	DefenderWon,
	AttackerWon,
}

public enum PlayerSide
{
	Defender,
	Attacker,
}

/// <summary>
/// The whole game simulation, independent of rendering and networking. Step it with a
/// fixed time step; read its state (or a <see cref="Snapshot"/>) and drain <see cref="Events"/>.
/// </summary>
public sealed partial class Match
{
	public const int CpuMaxHp = 10;
	public const float StepTime = 1f / 60f;

	public readonly LevelData Level;
	public readonly MatchMode Mode;
	public readonly VersusRules Rules;
	public readonly PathLine Path;
	public readonly WaveRunner Waves;
	public readonly HashSet<Vector2I> Blocked;
	public readonly List<Bug> Bugs = new();
	public readonly List<Turret> Turrets = new();
	public readonly Dictionary<Vector2I, Turret> TurretAt = new();
	public readonly List<Projectile> Projectiles = new();
	public readonly List<BoardComponent> Components = new();
	public readonly List<SimEvent> Events = new();

	public int Money;
	public float Energy;
	public int CpuHp = CpuMaxHp;
	public float Time;
	public MatchState State = MatchState.Playing;

	readonly Func<TurretDef, IReadOnlyDictionary<string, int>> _upgrades;
	readonly Random _detourRandom;
	readonly Dictionary<int, Bug> _bugById = new();
	readonly Dictionary<int, Projectile> _projById = new();
	int _nextId = 1;
	/// <summary>Bugs released by bosses during a step, added once the step's loops are done.</summary>
	readonly List<(string Enemy, float Progress)> _spawnQueue = new();
	float _income;
	float _lastSend = -99;
	float _stepAccumulator;

	/// <param name="upgrades">Upgrade levels per turret (campaign); null means base stats.</param>
	/// <param name="seed">Seed for deterministic, match-local detour decisions.</param>
	public Match(LevelData level, MatchMode mode = MatchMode.Campaign, VersusRules rules = null,
		Func<TurretDef, IReadOnlyDictionary<string, int>> upgrades = null, int seed = 1)
	{
		Level = level;
		Mode = mode;
		Rules = rules ?? Catalog.Versus;
		_upgrades = upgrades;
		_detourRandom = new Random(seed);
		Path = new PathLine(level.PathPoints());
		Blocked = level.BlockedCells();
		Money = level.StartMoney + (mode == MatchMode.Versus ? Rules.DefenderBonusMoney : 0);
		Energy = mode == MatchMode.Versus ? Rules.StartEnergy : 0;
		Waves = new WaveRunner(mode == MatchMode.Campaign ? level.Waves : new List<List<SpawnData>>());
		for (var i = 0; i < level.Components.Count; i++)
		{
			var data = level.Components[i];
			var kind = data.Kind;
			var hp = kind.Chewable ? kind.HpPerCell * data.Size[0] * data.Size[1] : 0;
			Components.Add(new BoardComponent
			{
				Index = i, Data = data, Rect = Grid.CellRect(data.CellV, data.SizeV), MaxHp = hp, Hp = hp,
			});
		}
	}

	/// <summary>Share of LED components still working, 1 when there are none.</summary>
	public float LightLevel => ComponentTypes.LightFraction(Components.Select(c => (c.Data.Type, !c.Destroyed)));

	public float TimeLeft => Mode == MatchMode.Versus ? Mathf.Max(Rules.Duration - Time, 0) : 0;
	public float EnergyRate => Rules.EnergyRate + Rules.EnergyRateGrowth * Time;
	public int AliveBugs => Bugs.Count;

	/// <summary>Share of component HP still standing (1 = untouched).</summary>
	public float BoardIntegrity
	{
		get
		{
			var max = Components.Sum(c => c.MaxHp);
			return max <= 0 ? 1 : Components.Sum(c => c.Destroyed ? 0 : c.Hp) / max;
		}
	}

	/// <summary>Campaign rating: 3 stars when the board is almost untouched.</summary>
	public int Stars => State != MatchState.DefenderWon ? 0 : BoardIntegrity >= 0.9f ? 3 : BoardIntegrity >= 0.6f ? 2 : 1;

	public Bug FindBug(int id) => _bugById.TryGetValue(id, out var b) && !b.Gone ? b : null;

	public Projectile FindProjectile(int id) => _projById.TryGetValue(id, out var p) && !p.Done ? p : null;

	/// <summary>Advance by <paramref name="delta"/> seconds using fixed sub-steps.</summary>
	public void Advance(float delta)
	{
		_stepAccumulator += delta;
		var steps = 0;
		while (_stepAccumulator >= StepTime && steps < 64)
		{
			Step(StepTime);
			_stepAccumulator -= StepTime;
			steps++;
		}
	}

	public void Step(float dt)
	{
		if (State != MatchState.Playing)
			return;
		Time += dt;

		if (Mode == MatchMode.Campaign)
		{
			Waves.Step(dt, Bugs.Count, id => SpawnBug(id),
				(kind, index) => Events.Add(new SimEvent(kind, index)));
		}
		else
		{
			Energy = Mathf.Min(Energy + EnergyRate * dt, Rules.EnergyCap);
			_income += Rules.DefenderIncome * dt;
			if (_income >= 1)
			{
				Money += (int)_income;
				_income -= (int)_income;
			}
		}

		foreach (var t in Turrets)
			StepTurret(t, dt);
		foreach (var p in Projectiles)
			StepProjectile(p, dt);
		foreach (var b in Bugs)
			StepBug(b, dt);
		foreach (var (enemy, progress) in _spawnQueue)
			SpawnBug(enemy, progress);
		_spawnQueue.Clear();

		Projectiles.RemoveAll(p =>
		{
			if (p.Done)
				_projById.Remove(p.Id);
			return p.Done;
		});
		Bugs.RemoveAll(b =>
		{
			if (b.Gone)
				_bugById.Remove(b.Id);
			return b.Gone;
		});

		if (CpuHp <= 0)
			End(MatchState.AttackerWon);
		else if (Mode == MatchMode.Campaign && Waves.State == WaveState.Done && Bugs.Count == 0)
			End(MatchState.DefenderWon);
		else if (Mode == MatchMode.Versus && Time >= Rules.Duration)
			End(MatchState.DefenderWon);
	}

	void End(MatchState state)
	{
		State = state;
		Events.Add(new SimEvent(EventKind.MatchEnded, (int)state, Value: Stars));
	}

	// ---------------------------------------------------------------- commands

	/// <summary>A player left the match: the other side wins.</summary>
	public void Forfeit(PlayerSide loser)
	{
		if (State == MatchState.Playing)
			End(loser == PlayerSide.Attacker ? MatchState.DefenderWon : MatchState.AttackerWon);
	}

	public bool CanBuild(Vector2I cell) => Grid.InBounds(cell) && !Blocked.Contains(cell) && !TurretAt.ContainsKey(cell);

	public bool Build(TurretDef def, Vector2I cell, float direction = 0)
	{
		if (def == null || State != MatchState.Playing || !CanBuild(cell) || Money < def.Cost)
			return false;
		var stats = def.ComputeStats(_upgrades?.Invoke(def));
		var turret = new Turret
		{
			Id = _nextId++, Def = def, Cell = cell, Pos = Grid.CellCenter(cell), Direction = direction,
			Stats = stats, HeadAngle = def.NeedsDirection ? direction : 0,
		};
		turret.Cooldown = turret.Stat("cooldown") * 0.5f;
		Turrets.Add(turret);
		TurretAt[cell] = turret;
		Money -= def.Cost;
		Events.Add(new SimEvent(EventKind.TurretBuilt, turret.Id, turret.Pos, Type: def.Id));
		return true;
	}

	/// <summary>Versus: why the attacker can't send this bug right now, or null if it can.</summary>
	public string CannotSend(EnemyDef def)
	{
		if (Mode != MatchMode.Versus)
			return "Only in versus mode";
		if (State != MatchState.Playing)
			return "The match is over";
		if (def == null)
			return "Unknown bug";
		if (!def.InVersus)
			return "Not available in versus";
		if (Time < def.VersusUnlock)
			return $"Unlocks in {Mathf.CeilToInt(def.VersusUnlock - Time)}s";
		if (Energy < def.VersusCost)
			return "Not enough energy";
		if (Time - _lastSend < Rules.SendInterval)
			return "Too fast";
		return null;
	}

	public bool SendBug(EnemyDef def)
	{
		if (CannotSend(def) != null)
			return false;
		Energy -= def.VersusCost;
		_lastSend = Time;
		SpawnBug(def.Id);
		return true;
	}

	/// <param name="progress">Where along the route it appears (bosses release bugs where they are).</param>
	public Bug SpawnBug(string enemyId, float progress = 0)
	{
		var def = Catalog.Enemy(enemyId);
		if (def == null)
		{
			GD.PushError($"Unknown enemy '{enemyId}'");
			return null;
		}
		progress = Mathf.Clamp(progress, 0, Path.Length);
		var bug = new Bug
		{
			Id = _nextId++, Def = def, Hp = def.MaxHp, Progress = progress, Pos = Path.Sample(progress), Angle = Path.AngleAt(progress),
			AbilityTimer = def.AbilityEvery,
		};
		Bugs.Add(bug);
		_bugById[bug.Id] = bug;
		Events.Add(new SimEvent(EventKind.BugSpawned, bug.Id, bug.Pos, Type: def.Id));
		return bug;
	}

	// ---------------------------------------------------------------- bugs

	void StepBug(Bug b, float dt)
	{
		if (b.Gone)
			return;
		if (b.SlowTime > 0)
		{
			b.SlowTime -= dt;
			if (b.SlowTime <= 0)
				b.SlowFactor = 1;
		}
		b.DetourCooldown = Mathf.Max(b.DetourCooldown - dt, 0);
		if (b.OffRoute)
			StepDetour(b, dt);
		else
		{
			b.Pos = Path.Sample(b.Progress);
			if (TryStartDetour(b))
				StepDetour(b, dt);
			else
			{
				b.Progress += b.Def.Speed * b.SlowFactor * dt;
				b.Pos = Path.Sample(b.Progress);
				b.Angle = Path.AngleAt(b.Progress);
				if (b.Progress >= Path.Length)
				{
					b.Escaped = true;
					Events.Add(new SimEvent(EventKind.BugEscaped, b.Id, b.Pos, Type: b.Def.Id));
					// DDoS packets just clog things up; everything else hurts the CPU.
					if (b.Def.CpuDamage > 0)
					{
						CpuHp = Mathf.Max(CpuHp - b.Def.CpuDamage, 0);
						Events.Add(new SimEvent(EventKind.CpuHit, Value: CpuHp, Pos: b.Pos));
					}
					return;
				}
			}
		}
		if (!string.IsNullOrEmpty(b.Def.Ability))
			StepAbility(b, dt);
	}

	public void DamageBug(Bug b, float amount)
	{
		if (!b.Targetable)
			return;
		b.Hp -= amount;
		OnBossHurt(b);
		if (b.Hp > 0)
			return;
		b.Dead = true;
		Money += Mode == MatchMode.Versus ? Mathf.RoundToInt(b.Def.Reward * Rules.KillRewardFactor) : b.Def.Reward;
		Events.Add(new SimEvent(EventKind.BugDied, b.Id, b.Pos, b.Def.Reward, b.Def.Id));
		if (b.Def.Ability == "trojan")
		{
			// The payload spills out where the horse fell.
			for (var i = 0; i < b.Def.SpawnCount; i++)
				_spawnQueue.Add((b.Def.Spawn, b.Progress - i * 9));
			Events.Add(new SimEvent(EventKind.BossAbility, b.Id, b.Pos, b.Def.SpawnCount, "payload"));
		}
	}

	// ---------------------------------------------------------------- targeting helpers

	public IEnumerable<Bug> BugsInRange(Vector2 from, float range) =>
		Bugs.Where(b => b.Targetable && from.DistanceTo(b.Pos) <= range + b.Radius * 0.5f);

	/// <summary>The bug closest to the CPU within range.</summary>
	public Bug FurthestInRange(Vector2 from, float range)
	{
		Bug best = null;
		foreach (var b in BugsInRange(from, range))
			if (best == null || b.Progress > best.Progress)
				best = b;
		return best;
	}

	public Bug Nearest(Vector2 from, float maxRange = float.MaxValue)
	{
		Bug best = null;
		var bestD = maxRange;
		foreach (var b in Bugs)
		{
			if (!b.Targetable)
				continue;
			var d = from.DistanceTo(b.Pos);
			if (d <= bestD)
			{
				best = b;
				bestD = d;
			}
		}
		return best;
	}

	Projectile AddProjectile(Projectile p)
	{
		p.Id = _nextId++;
		Projectiles.Add(p);
		_projById[p.Id] = p;
		return p;
	}
}
