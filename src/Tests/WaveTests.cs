using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Sim;

namespace HardwareDefence.Tests;

/// <summary>Campaign pacing, boss consequences and measured live-population budgets.</summary>
public sealed class WaveTests : TestSuite
{
	static readonly int[] Counts = { 3, 4, 6, 8, 11, 15, 20, 25 };
	static readonly string[] Bosses = { "trojan", "keylogger", "mitm", "ddos", "rootkit", "ransomware", "worm", "boss" };
	static readonly int[] Damage = { 5, 7, 9, 10, 10, 10, 10, 10 };

	[Test]
	public void CampaignCountsBossOrderAndSpacing()
	{
		for (var i = 0; i < Counts.Length; i++)
		{
			var level = Catalog.Levels[i];
			CheckEq(level.Waves.Count, Counts[i], $"L{i + 1} waves");
			CheckEq(level.Waves[^1][^1].Enemy, Bosses[i], $"L{i + 1} final spawn");
			CheckEq(level.Waves.SelectMany(w => w).Count(s => Catalog.Enemy(s.Enemy).Boss), 1, $"L{i + 1} single boss");
			foreach (var wave in level.Waves)
			{
				Check(wave.Count is >= 5 and <= 9, $"L{i + 1} compact wave");
				for (var spawn = 1; spawn < wave.Count; spawn++)
					Check(wave[spawn].Time - wave[spawn - 1].Time >= 0.5f, $"L{i + 1} minimum spacing");
			}
		}
		Check(!Catalog.Levels[0].Waves.SelectMany(w => w).Any(s => s.Enemy == "termite"), "termite introduced after board one");
		Check(Catalog.Levels[1].Waves.SelectMany(w => w).Any(s => s.Enemy == "termite"), "termite introduced on board two");
		Check(Catalog.Levels[3].Waves.SelectMany(w => w).Any(s => s.Enemy == "roach"), "roach introduced on board four");
		Check(Catalog.Levels.SelectMany(l => l.Waves).SelectMany(w => w).Select(s => s.Enemy).Distinct().Count(id => !Catalog.Enemy(id).Boss) == 8,
			"all eight ordinary species appear");
	}

	[Test]
	public void BossCpuDamageAndSameStepDefeat()
	{
		for (var i = 0; i < Bosses.Length; i++)
		{
			CheckEq(Catalog.Enemy(Bosses[i]).CpuDamage, Damage[i], $"{Bosses[i]} damage");
			foreach (var mode in new[] { MatchMode.Campaign, MatchMode.Versus })
			{
				var match = new Match(Catalog.Levels[i], mode);
				match.SpawnBug(Bosses[i], match.Path.Length - 0.01f);
				match.Step(Match.StepTime);
				CheckEq(match.CpuHp, Math.Max(0, Match.CpuMaxHp - Damage[i]), $"{Bosses[i]} {mode} remaining CPU");
				CheckEq(match.State, i >= 3 ? MatchState.AttackerWon : MatchState.Playing, $"{Bosses[i]} {mode} same-step result");
			}
		}
		foreach (var id in new[] { "bug", "worm_copy", "packet" })
		{
			// Remove detour targets from this cloned arena so this tests escape damage,
			// rather than a seeded decision to leave the route beside the CPU.
			var level = Catalog.Levels[0].Clone();
			level.Components.Clear();
			var match = new Match(level);
			match.SpawnBug(id, match.Path.Length - 0.01f);
			match.Step(Match.StepTime);
			CheckEq(match.CpuHp, Match.CpuMaxHp - (id == "packet" ? 0 : 1), $"{id} ordinary damage unchanged");
		}
	}


	[Test]
	public void DefenderAnticipatesPublicBossAndBuysUrgentSupportAtRealCost()
	{
		foreach (var active in new[] { false, true })
		{
			var level = Catalog.Levels[2].Clone();
			level.Waves = new()
			{
				new() { new() { Enemy = "bug", Time = 1000 } },
				new() { new() { Enemy = "mitm", Time = 1000 } },
			};
			var match = new Match(level) { Money = 10000 };
			var bot = new DefenderBot();
			for (var gun = 0; gun < 7; gun++) bot.Step(match, 1);
			var cost = Catalog.Turret("firewall").Cost;
			match.Money = cost + (active ? 0 : 10);
			if (active) match.SpawnBug("mitm");
			bot.Step(match, 1);
			CheckEq(match.Turrets.Count(t => t.Def.Id == "firewall"), 1, active ? "urgent support" : "public future boss support");
			CheckEq(match.Money, active ? 0 : 10, "support charged its real cost");
		}
	}

	[Test]
	public void MeasuredPopulationStaysReadable()
	{
		const int populationLimit = 36;
		var notes = new List<string>();
		for (var i = 0; i < Catalog.Levels.Count; i++)
		{
			// Real fixed-step runs count ordinary bugs AND boss-created minions, not just JSON spawns.
			// Test both the reference defender and an idle player; stop each at its real outcome.
			var peaks = new List<int>();
			foreach (var scenario in new[] { "bot", "idle", "boss-idle" })
			{
				var level = Catalog.Levels[i].Clone();
				if (scenario == "boss-idle")
					level.Waves = new() { new() { new() { Enemy = Bosses[i], Time = 0 } } };
				var match = new Match(level);
				var bot = scenario == "bot" ? new DefenderBot() : null;
				var peak = 0;
				while (match.State == MatchState.Playing && match.Time < 4800)
				{
					match.Step(Match.StepTime);
					bot?.Step(match, Match.StepTime);
					peak = Math.Max(peak, match.Bugs.Count(b => !b.Gone));
					match.Events.Clear();
				}
				Check(match.State != MatchState.Playing, $"L{i + 1} {scenario} finishes");
				Check(peak <= populationLimit, $"L{i + 1} real peak {peak} exceeds {populationLimit}");
				peaks.Add(peak);
			}
			notes.Add(FormattableString.Invariant($"L{i + 1}: bot {peaks[0]}, idle {peaks[1]}, boss-idle {peaks[2]}"));
		}
		Note = $"measured alive (limit {populationLimit}): " + string.Join(" | ", notes);
	}
}
