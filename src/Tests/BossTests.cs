using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.Tests;

/// <summary>Every boss does its thing: each one is dropped on its own level and watched for a while.</summary>
public sealed class BossTests : TestSuite
{
	/// <summary>A level with no waves, a boss part-way along the route and turrets beside it.</summary>
	static (Match Match, Bug Boss) Arena(string bossId, int turrets = 4, float share = 0.3f)
	{
		var level = Catalog.Levels.First(l => l.Waves.SelectMany(w => w).Any(s => s.Enemy == bossId)).Clone();
		level.Waves.Clear();
		level.Waves.Add(new List<SpawnData> { new() { Enemy = "bug", Time = 999 } });
		var m = new Match(level) { Money = 10000 };
		var boss = m.SpawnBug(bossId, m.Path.Length * share);
		// Blasters on the free cells nearest to the boss.
		var cells = new List<Vector2I>();
		for (var x = 0; x < Grid.Cols; x++)
			for (var y = 0; y < Grid.Rows; y++)
				if (m.CanBuild(new Vector2I(x, y)))
					cells.Add(new Vector2I(x, y));
		foreach (var cell in cells.OrderBy(c => Grid.CellCenter(c).DistanceTo(boss.Pos)).Take(turrets))
			m.Build(Catalog.Turret("blaster"), cell);
		m.Events.Clear();
		return (m, boss);
	}

	static List<SimEvent> Run(Match m, float seconds)
	{
		var events = new List<SimEvent>();
		for (var t = 0f; t < seconds && m.State == MatchState.Playing; t += Match.StepTime)
		{
			m.Step(Match.StepTime);
			events.AddRange(m.Events);
			m.Events.Clear();
		}
		return events;
	}

	static int Count(List<SimEvent> events, string type) => events.Count(e => e.Kind == EventKind.BossAbility && e.Type == type);

	[Test]
	public void EveryLevelHasItsBoss()
	{
		foreach (var level in Catalog.Levels)
			Check(level.Waves.SelectMany(w => w).Any(s => Catalog.Enemy(s.Enemy)?.Boss == true), $"{level.Title} has a boss");
		var abilities = Catalog.EnemyList.Where(e => e.Boss).Select(e => e.Ability).ToHashSet();
		CheckEq(abilities.Count, 8, "different boss abilities");
	}

	[Test]
	public void TrojanRevealsAndDropsItsPayload()
	{
		var (m, boss) = Arena("trojan", 0);
		m.DamageBug(boss, boss.Def.MaxHp * 0.3f);
		Check(!boss.Revealed, "still disguised at 70%");
		m.DamageBug(boss, boss.Def.MaxHp * 0.3f);
		Check(boss.Revealed, "revealed below half health");
		m.DamageBug(boss, boss.Def.MaxHp);
		var events = Run(m, 0.1f);
		Check(boss.Dead, "dead");
		CheckEq(m.Bugs.Count(b => b.Def.Id == boss.Def.Spawn), boss.Def.SpawnCount, "payload bugs on the board");
		Check(m.Bugs.All(b => b.Progress > 0), "payload appears where it died");
		Check(events.Count(e => e.Kind == EventKind.BugSpawned) == boss.Def.SpawnCount, "spawn events");
	}

	[Test]
	public void KeyloggerStealsMoney()
	{
		var (m, _) = Arena("keylogger", 0);
		var before = m.Money;
		var events = Run(m, 5.5f);
		Check(Count(events, "steal") >= 5, $"steals every second ({Count(events, "steal")})");
		Check(m.Money < before, "money went down");
	}

	[Test]
	public void ManInTheMiddleInterceptsShots()
	{
		var (m, _) = Arena("mitm", 6);
		var events = Run(m, 8);
		var hits = events.Count(e => e.Kind == EventKind.BulletHit);
		var intercepted = Count(events, "intercept");
		Check(intercepted > 0, "some shots intercepted");
		Check(hits > 0, "some shots still hit");
		Note = $"{intercepted} intercepted, {hits} hits";
	}

	[Test]
	public void DdosFloodsPackets()
	{
		var (m, boss) = Arena("ddos", 0);
		var events = Run(m, 7);
		var packets = events.Count(e => e.Kind == EventKind.BugSpawned && e.Type == "packet");
		CheckEq(packets, 2 * boss.Def.SpawnCount, "packets after two floods");
		Check(m.CpuHp == Match.CpuMaxHp || m.Bugs.All(b => b.Def.Id != "packet"), "packets don't hurt the CPU");
	}

	[Test]
	public void RootkitHidesFromTurrets()
	{
		var (m, boss) = Arena("rootkit", 6, 0.2f);
		Run(m, boss.Def.AbilityEvery + 0.1f);
		Check(boss.Cloaked, "cloaked after its visible phase");
		var hp = boss.Hp;
		Run(m, boss.Def.AbilityTime - 0.3f);
		Check(boss.Cloaked && Mathf.IsEqualApprox(boss.Hp, hp), "no damage while hidden");
		Check(!m.BugsInRange(boss.Pos, 500).Contains(boss), "turrets can't see it");
		Run(m, 0.5f);
		Check(!boss.Cloaked, "visible again");
		Note = $"progress {boss.Progress:0}/{m.Path.Length:0}, hp {boss.Hp:0}, cpu {m.CpuHp}";
	}

	[Test]
	public void RansomwareEncryptsNearbyTurrets()
	{
		var (m, boss) = Arena("ransomware", 4);
		Run(m, boss.Def.AbilityEvery + 0.05f);
		var locked = m.Turrets.Count(t => t.Locked > 0);
		Check(locked > 0, "turrets near it are encrypted");
		var shots = Run(m, boss.Def.AbilityTime * 0.8f).Count(e => e.Kind == EventKind.TurretFired && m.Turrets.Any(t => t.Id == e.Id && t.Locked > 0));
		CheckEq(shots, 0, "encrypted turrets don't fire");
	}

	[Test]
	public void WormForksTwice()
	{
		var (m, boss) = Arena("worm", 0);
		m.DamageBug(boss, boss.Def.MaxHp * 0.4f);
		m.DamageBug(boss, boss.Def.MaxHp * 0.35f);
		var events = Run(m, 0.1f);
		CheckEq(boss.Forks, 2, "forks");
		CheckEq(m.Bugs.Count(b => b.Def.Id == "worm_copy"), 2 * boss.Def.SpawnCount, "copies on the board");
		Check(events.Count == 0 || m.Bugs.Count == 3, "boss and its copies");
	}

	[Test]
	public void KernelPanicFreezesEveryTurret()
	{
		var (m, boss) = Arena("boss", 5);
		Run(m, boss.Def.AbilityEvery + 0.05f);
		Check(m.Turrets.Count > 0 && m.Turrets.All(t => t.Locked > 0), "all turrets crashed");
		Run(m, boss.Def.AbilityTime + 0.1f);
		Check(m.Turrets.All(t => t.Locked <= 0), "they come back");
	}
}
