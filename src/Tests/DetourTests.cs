using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.Tests;

public sealed class DetourTests : TestSuite
{
	static LevelData Arena(params ComponentData[] components) => new()
	{
		Path = new() { new[] { 0, 2 }, new[] { 18, 2 } },
		StartMoney = 10000,
		Components = components.Length == 0
			? new() { new() { Type = "smd", Cell = new[] { 3, 4 } } }
			: components.ToList(),
	};

	static Bug Spawn(Match match, string type = "termite", float progress = 3 * Grid.Cell)
		=> match.SpawnBug(type, progress);

	static EnemyDef PrivateDef(Bug bug)
	{
		bug.Def = JsonIO.Read<EnemyDef>(JsonIO.Write(bug.Def));
		return bug.Def;
	}

	static void Run(Match match, int steps)
	{
		for (var i = 0; i < steps && match.State == MatchState.Playing; i++)
			match.Step(Match.StepTime);
	}

	[Test]
	public void TermiteWalksToEdgeChewsReturnsExactlyAndEscapes()
	{
		var match = new Match(Arena(), MatchMode.Versus);
		var bug = Spawn(match);
		var exit = bug.Pos;
		var progress = bug.Progress;
		var states = new HashSet<DetourState>();
		var movedOffRoute = false;
		var chewingAtEdge = false;
		var exactReturn = false;
		var wrongProgress = false;
		for (var i = 0; i < 60 * 40 && !bug.Gone; i++)
		{
			var before = bug.Detour;
			match.Step(Match.StepTime);
			states.Add(bug.Detour);
			if (bug.OffRoute)
			{
				wrongProgress |= bug.Progress != progress || bug.Escaped;
				movedOffRoute |= bug.Pos.DistanceTo(exit) > Grid.Cell;
			}
			if (bug.Detour == DetourState.Chewing)
			{
				chewingAtEdge = Mathf.IsEqualApprox(match.Components[0].DistanceTo(bug.Pos), bug.Radius);
				CheckEq(bug.ChewTarget, 0, "only the chewing state owns ChewTarget");
				var snapshot = Snapshot.From(match).Bugs.Single();
				Check(snapshot.Chewing && snapshot.OffRoute, "snapshot reports actual chewing off route");
			}
			else
				CheckEq(bug.ChewTarget, -1, "travelling bug never claims to chew");
			if (before == DetourState.Returning && !bug.OffRoute)
				exactReturn = bug.Pos == exit && bug.Progress == progress;
		}
		Check(states.Contains(DetourState.Going) && states.Contains(DetourState.Chewing)
			&& states.Contains(DetourState.Returning), "all detour phases observed");
		Check(movedOffRoute && chewingAtEdge, "walked off route to the component edge");
		Check(!wrongProgress, "route progress frozen and no escape throughout detour");
		Check(match.Components[0].Destroyed, "termite destroyed its component");
		Check(exactReturn, "returned to the exact saved position and progress");
		Check(bug.Escaped && match.CpuHp == Match.CpuMaxHp - bug.Def.CpuDamage, "resumed route and reached CPU");
		CheckEq(match.Events.Count(e => e.Kind == EventKind.ComponentDestroyed), 1, "single component destruction");
	}

	[Test]
	public void RouteHasNoAutomaticChewingAndDefaultsDisableDetours()
	{
		var match = new Match(Arena(new ComponentData() { Type = "led", Cell = new[] { 3, 3 } }), MatchMode.Versus);
		var bug = Spawn(match);
		var def = PrivateDef(bug);
		var defaults = new EnemyDef();
		Check(defaults.DetourChance == 0 && defaults.DetourRange == 0
			&& defaults.DetourChewTime == 0 && defaults.DetourCooldown == 0, "missing fields safely disable detours");
		def.DetourChance = 0;
		Run(match, 600);
		CheckNear(match.Components[0].Hp, match.Components[0].MaxHp, "nearby route component remains untouched");
		Check(!bug.OffRoute && bug.ChewTarget == -1, "remained on route");
		Check(!match.Events.Any(e => e.Kind is EventKind.Chew or EventKind.ComponentDestroyed), "no route chewing events");
	}

	[Test]
	public void ChewBudgetExpiresWithoutRetryingSameComponent()
	{
		var match = new Match(Arena(), MatchMode.Versus);
		var bug = Spawn(match);
		var def = PrivateDef(bug);
		def.DetourChance = 1;
		def.DetourChewTime = 0.25f;
		def.DetourCooldown = 0;
		var initial = match.Components[0].Hp;
		Run(match, 60 * 15);
		Check(!match.Components[0].Destroyed, "time budget stops before destruction");
		CheckNear(initial - match.Components[0].Hp, def.ChewDps * def.DetourChewTime, "damage bounded by chewing seconds");
		CheckEq(bug.DetourConsidered.Count, 1, "successful component tried only once, even with no cooldown");
		Check(!bug.OffRoute && bug.Progress > 3 * Grid.Cell, "returned and resumed despite undestroyed target");
	}

	[Test]
	public void DestroyedTargetAbortsApproachAndChewing()
	{
		foreach (var phase in new[] { DetourState.Going, DetourState.Chewing })
		{
			var match = new Match(Arena(), MatchMode.Versus);
			var bug = Spawn(match);
			PrivateDef(bug).DetourChance = 1;
			for (var i = 0; i < 600 && bug.Detour != phase; i++)
				match.Step(Match.StepTime);
			CheckEq(bug.Detour, phase, "reached abort phase");
			var exit = bug.DetourExit;
			match.Components[0].Destroyed = true;
			match.Components[0].Hp = 0;
			match.Events.Clear();
			match.Step(Match.StepTime);
			Check(bug.Detour == DetourState.Returning || !bug.OffRoute, "immediately abandons missing target");
			CheckEq(bug.ChewTarget, -1, "chewing cleared on abort");
			for (var i = 0; i < 600 && bug.OffRoute; i++)
				match.Step(Match.StepTime);
			Check(bug.Pos == exit && !bug.OffRoute, "abort returns exactly to route");
			Check(!match.Events.Any(e => e.Kind is EventKind.Chew or EventKind.ComponentDestroyed), "no damage or duplicate reward after abort");
		}
	}

	[Test]
	public void SharedTargetDestroyedByAnotherBugRewardsOnlyOnce()
	{
		var rules = new VersusRules { StartEnergy = 0, EnergyRate = 0, EnergyRateGrowth = 0 };
		var match = new Match(Arena(), MatchMode.Versus, rules);
		var first = Spawn(match);
		var second = Spawn(match);
		PrivateDef(first).DetourChance = 1;
		PrivateDef(second).DetourChance = 1;
		match.Step(Match.StepTime);
		Check(first.OffRoute && second.OffRoute && first.DetourTarget == second.DetourTarget,
			"two bugs genuinely approach the same living target");
		for (var i = 0; i < 600 && !match.Components[0].Destroyed; i++)
			match.Step(Match.StepTime);
		Check(match.Components[0].Destroyed, "shared target destroyed by a chewing bug");
		// A bug earlier in the fixed-step order observes a later bug's destruction next step.
		match.Step(Match.StepTime);
		Check(first.Detour == DetourState.Returning && second.Detour == DetourState.Returning,
			"both return by the next step after the shared destruction");
		Check(first.ChewTarget == -1 && second.ChewTarget == -1, "both stop chewing");
		Run(match, 300);
		CheckEq(match.Events.Count(e => e.Kind == EventKind.ComponentDestroyed), 1, "one destruction event");
		CheckNear(match.Energy, match.Components[0].MaxHp * rules.ComponentEnergy, "one component reward");
	}

	[Test]
	public void NearestEligibleComponentUsesRectangleDistance()
	{
		var match = new Match(Arena(
			new() { Type = "chip", Cell = new[] { 5, 4 }, Size = new[] { 4, 1 } },
			new() { Type = "smd", Cell = new[] { 4, 4 } },
			new() { Type = "hole", Cell = new[] { 3, 3 } },
			new() { Type = "led", Cell = new[] { 3, 3 } }), MatchMode.Versus);
		match.Components[3].Destroyed = true;
		var bug = Spawn(match);
		PrivateDef(bug).DetourChance = 1;
		match.Step(Match.StepTime);
		CheckEq(bug.DetourTarget, 1, "nearest living chewable component chosen");
		Check(bug.DetourConsidered.SetEquals(new[] { 1 }), "no roll for destroyed or unchewable components");
		var rectangleMatch = new Match(Arena(new ComponentData() { Type = "chip", Cell = new[] { 3, 4 }, Size = new[] { 8, 1 } }), MatchMode.Versus);
		var rectangleBug = Spawn(rectangleMatch);
		PrivateDef(rectangleBug).DetourChance = 1;
		rectangleMatch.Step(Match.StepTime);
		Check(rectangleBug.OffRoute, "near rectangle edge eligible even when centre lies outside range");
		var distantMatch = new Match(Arena(new ComponentData() { Type = "led", Cell = new[] { 3, 6 } }), MatchMode.Versus);
		var distantBug = Spawn(distantMatch);
		PrivateDef(distantBug).DetourChance = 1;
		distantMatch.Step(Match.StepTime);
		Check(!distantBug.OffRoute && distantBug.DetourConsidered.Count == 0, "no decision outside rectangle range");
	}

	[Test]
	public void CooldownDefersNewComponentsUntilAfterReturn()
	{
		var match = new Match(Arena(
			new() { Type = "led", Cell = new[] { 3, 4 } },
			new() { Type = "led", Cell = new[] { 4, 4 } }), MatchMode.Versus);
		var bug = Spawn(match);
		var def = PrivateDef(bug);
		def.DetourChance = 1;
		def.DetourCooldown = 2;
		match.Step(Match.StepTime);
		for (var i = 0; i < 600 && bug.OffRoute; i++)
			match.Step(Match.StepTime);
		CheckNear(bug.DetourCooldown, 2, "cooldown begins at return");
		Run(match, 60);
		Check(!bug.OffRoute && bug.DetourConsidered.Count == 1, "no new roll during cooldown");
		Run(match, 62);
		Check(bug.OffRoute && bug.DetourTarget == 1, "new component can attract bug once cooldown expires");
	}

	[Test]
	public void RunnerAndRoachRarelyDetourWithOneRollPerBug()
	{
		foreach (var type in new[] { "runner", "roach" })
		{
			var match = new Match(Arena(), MatchMode.Versus, seed: 71);
			var bugs = Enumerable.Range(0, 1000).Select(_ => Spawn(match, type)).ToList();
			match.Step(Match.StepTime);
			var selected = bugs.Count(b => b.OffRoute);
			Check(selected is >= 25 and <= 80, $"{type}: rare selections from 1000 single opportunities ({selected})");
			var failures = bugs.Where(b => !b.OffRoute).ToList();
			Run(match, 60);
			Check(failures.All(b => !b.OffRoute && b.DetourConsidered.SetEquals(new[] { 0 })), "failed roll never repeats while in range");
			Note += $"{type}: {selected}/1000; ";
		}
	}

	[Test]
	public void BossesAndPacketsNeverDetourEvenWithMisconfiguredChance()
	{
		foreach (var enemy in Catalog.EnemyList.Where(e => e.Boss || e.Id == "packet"))
		{
			var match = new Match(Arena(), MatchMode.Versus);
			var bug = Spawn(match, enemy.Id);
			var def = PrivateDef(bug);
			def.DetourChance = 1;
			def.DetourRange = 3;
			def.DetourChewTime = 8;
			Run(match, 120);
			Check(!bug.OffRoute && bug.DetourConsidered.Count == 0, $"{enemy.Id}: no detour decisions");
			CheckNear(match.Components[0].Hp, match.Components[0].MaxHp, $"{enemy.Id}: never chews route components");
		}
	}

	[Test]
	public void SlowAffectsDetourMovementAndBitesWithRealAngles()
	{
		var match = new Match(Arena(), MatchMode.Versus);
		var bug = Spawn(match);
		PrivateDef(bug).DetourChance = 1;
		bug.ApplySlow(0.5f, 30);
		var initial = bug.Pos;
		match.Step(Match.StepTime);
		CheckNear(bug.Pos.DistanceTo(initial), bug.Def.Speed * 0.5f * Match.StepTime, "slowed outbound speed");
		CheckNear(bug.Angle, (bug.Pos - initial).Angle(), "outbound angle is actual direction");
		Check(match.FurthestInRange(bug.Pos, 20) == bug, "off-route bug remains targetable");
		for (var i = 0; i < 600 && bug.Detour != DetourState.Chewing; i++)
			match.Step(Match.StepTime);
		CheckEq(bug.Detour, DetourState.Chewing, "arrived for slowed chewing");
		var hp = match.Components[0].Hp;
		match.Step(Match.StepTime);
		CheckNear(hp - match.Components[0].Hp, bug.Def.ChewDps * 0.5f * Match.StepTime, "slowed chewing damage");
		for (var i = 0; i < 600 && bug.Detour != DetourState.Returning; i++)
			match.Step(Match.StepTime);
		initial = bug.Pos;
		match.Step(Match.StepTime);
		CheckNear(bug.Pos.DistanceTo(initial), bug.Def.Speed * 0.5f * Match.StepTime, "slowed return speed");
		CheckNear(bug.Angle, (bug.Pos - initial).Angle(), "return angle is actual direction");
	}

	[Test]
	public void TurretHitsBugWhileItIsOffRoute()
	{
		var match = new Match(Arena(), MatchMode.Versus);
		var bug = Spawn(match);
		PrivateDef(bug).DetourChance = 1;
		bug.Hp = 1000;
		Run(match, 60);
		Check(bug.OffRoute && bug.Pos.DistanceTo(match.Path.Sample(bug.Progress)) > 10, "bug left route before turret construction");
		Check(match.Build(Catalog.Turret("blaster"), new Vector2I(2, 3)), "nearby turret built");
		var hitOffRoute = false;
		for (var i = 0; i < 120; i++)
		{
			var hp = bug.Hp;
			var wasOffRoute = bug.OffRoute;
			match.Step(Match.StepTime);
			hitOffRoute |= wasOffRoute && bug.OffRoute && bug.Hp < hp;
		}
		Check(hitOffRoute, "turret damaged the bug during its detour");
		Check(match.Events.Any(e => e.Kind == EventKind.BulletHit), "actual projectile hit recorded");
	}

	[Test]
	public void SeededMatchesProduceIdenticalSnapshotsAndEvents()
	{
		var level = Arena(
			new() { Type = "led", Cell = new[] { 3, 4 } },
			new() { Type = "chip", Cell = new[] { 8, 4 }, Size = new[] { 2, 2 } });
		var first = new Match(level, MatchMode.Versus, seed: 314);
		var second = new Match(level.Clone(), MatchMode.Versus, seed: 314);
		var differentSeed = new Match(level.Clone(), MatchMode.Versus, seed: 315);
		var sawDifference = false;
		var sawDetour = false;
		foreach (var match in new[] { first, second, differentSeed })
			for (var i = 0; i < 20; i++)
				Spawn(match, i % 2 == 0 ? "termite" : "bug", i * 10);
		for (var i = 0; i < 60 * 15; i++)
		{
			first.Step(Match.StepTime);
			second.Step(Match.StepTime);
			differentSeed.Step(Match.StepTime);
			CheckEq(JsonIO.Write(Snapshot.From(first)), JsonIO.Write(Snapshot.From(second)), "identical snapshot at fixed step");
			Check(first.Events.SequenceEqual(second.Events), "identical event order and data");
			sawDifference |= JsonIO.Write(Snapshot.From(first)) != JsonIO.Write(Snapshot.From(differentSeed));
			sawDetour |= first.Bugs.Any(b => b.OffRoute);
			first.Events.Clear();
			second.Events.Clear();
			differentSeed.Events.Clear();
		}
		Check(sawDetour && sawDifference, "seed drives detours while identical seeds reproduce them");
	}
}
