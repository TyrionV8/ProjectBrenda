using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.Tests;

/// <summary>Plays the simulation headless — no rendering, so whole levels take milliseconds.</summary>
public sealed class SimTests : TestSuite
{
	public static Match Play(LevelData level, bool bot, float limit = 900)
	{
		var m = new Match(level);
		var defender = bot ? new DefenderBot() : null;
		while (m.State == MatchState.Playing && m.Time < limit)
		{
			m.Step(Match.StepTime);
			defender?.Step(m, Match.StepTime);
		}
		return m;
	}

	[Test]
	public void LostWithoutTurrets()
	{
		var m = Play(Catalog.Levels[0], false);
		CheckEq(m.State, MatchState.AttackerWon, "state");
	}

	[Test]
	public void BotClearsEveryLevel()
	{
		var notes = new System.Collections.Generic.List<string>();
		for (var i = 0; i < Catalog.Levels.Count; i++)
		{
			var m = Play(Catalog.Levels[i], true);
			Check(m.State == MatchState.DefenderWon, $"level {i + 1} not cleared (cpu {m.CpuHp})");
			notes.Add($"L{i + 1}: cpu {m.CpuHp} board {m.BoardIntegrity:P0} {m.Stars}*");
		}
		Note = string.Join(" | ", notes);
	}

	[Test]
	public void EveryTurretAffectsBugs()
	{
		foreach (var def in Catalog.Turrets.Where(t => t.Behavior is "blaster" or "mortar" or "burst" or "laser" or "freezer"))
		{
			var level = Catalog.Levels[0].Clone();
			level.Waves.Clear();
			var m = new Match(level) { Money = 999 };
			var cell = DefenderBot.BestCell(m, def);
			Check(m.Build(def, cell, DefenderBot.BestDirection(m, def, cell)), $"{def.Id} built");
			var bug = m.SpawnBug("tank");
			bug.Hp = 1000;
			// Start the bug a little before the point of the route nearest to the turret.
			var center = Grid.CellCenter(cell);
			var nearest = 0f;
			for (var d = 0f; d < m.Path.Length; d += 4)
				if (m.Path.Sample(d).DistanceTo(center) < m.Path.Sample(nearest).DistanceTo(center))
					nearest = d;
			bug.Progress = Mathf.Max(nearest - 60, 0);
			var slowed = false;
			for (var i = 0; i < 60 * 40 && !bug.Gone; i++)
			{
				m.Step(Match.StepTime);
				slowed |= bug.Slowed;
			}
			if (def.Behavior == "freezer")
				Check(slowed, "freezer slowed the bug");
			else
				Check(bug.Hp < 1000, $"{def.Id} did no damage");
		}
	}

	[Test]
	public void BugsChewComponents()
	{
		var level = new LevelData
		{
			Path = new() { new[] { 0, 2 }, new[] { 18, 2 } },
			Components = new() { new() { Type = "smd", Cell = new[] { 3, 4 } } },
		};
		var m = new Match(level, MatchMode.Versus, seed: 1);
		m.SpawnBug("termite", 3 * Grid.Cell);
		var leftRoute = false;
		var destroyed = false;
		for (var i = 0; i < 60 * 20 && !destroyed; i++)
		{
			m.Step(Match.StepTime);
			leftRoute |= m.Bugs.Any(b => b.OffRoute && b.Pos.DistanceTo(m.Path.Sample(b.Progress)) > 1);
			destroyed |= m.Events.Any(e => e.Kind == EventKind.ComponentDestroyed);
		}
		Check(leftRoute, "termite physically left the route before chewing");
		Check(destroyed, "detouring termite destroyed the component");
		Check(m.Components[0].Destroyed, "component flagged destroyed");
		Check(m.BoardIntegrity < 1, "integrity dropped");
	}

	[Test]
	public void VersusBotsFinish()
	{
		var results = new System.Collections.Generic.List<string>();
		var defenderWins = 0;
		var games = 0;
		foreach (var level in Catalog.Levels)
			for (var seed = 1; seed <= 2; seed++)
			{
				var m = new Match(level, MatchMode.Versus);
				var defender = new DefenderBot();
				var attacker = new AttackerBot(seed);
				while (m.State == MatchState.Playing)
				{
					m.Step(Match.StepTime);
					defender.Step(m, Match.StepTime);
					attacker.Step(m);
				}
				games++;
				if (m.State == MatchState.DefenderWon)
					defenderWins++;
				results.Add($"{(m.State == MatchState.DefenderWon ? "D" : "A")}@{m.Time:0}s");
			}
		Note = $"defender won {defenderWins}/{games}: {string.Join(" ", results)}";
	}

	[Test]
	public void VersusRulesAreEnforced()
	{
		var m = new Match(Catalog.Levels[0], MatchMode.Versus);
		var boss = Catalog.Enemy("boss");
		CheckEq(m.CannotSend(boss)?.StartsWith("Unlocks") ?? false, true, "boss locked at start");
		var bug = Catalog.Enemy("bug");
		Check(m.SendBug(bug), "can send a bug");
		Check(!m.SendBug(bug), "send interval enforced");
		m.Energy = 0;
		m.Step(1);
		CheckEq(m.CannotSend(Catalog.Enemy("spider")), "Not enough energy", "energy enforced");
		var campaign = new Match(Catalog.Levels[0]);
		Check(!campaign.SendBug(bug), "no sending in campaign");
	}
}
