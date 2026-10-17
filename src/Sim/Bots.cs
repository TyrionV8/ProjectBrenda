using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Sim;

/// <summary>
/// A simple defender: whenever it can afford the next turret in its plan, it builds it on
/// the free cell that covers the most route. Used by tests, screenshots and versus vs AI.
/// </summary>
public sealed class DefenderBot
{
	public string[] Plan = { "blaster", "burst", "blaster", "freezer", "mortar", "burst", "laser" };
	public float Interval = 0.4f;
	int _next;
	float _timer;

	public void Step(Match m, float dt)
	{
		if (m.State != MatchState.Playing)
			return;
		_timer -= dt;
		if (_timer > 0)
			return;
		_timer = Interval;
		// Establish weapons first; reserve another basic weapon before buying support.
		var counter = CounterFor(m);
		if (_next >= 7 && counter != null && !m.Turrets.Any(t => t.Def.Id == counter.Id))
		{
			// Save a bounded budget instead of spending every small reward on another gun.
			if (m.Money < counter.Cost + 10) return;
			var counterCell = BestCell(m, counter);
			if (counterCell.X >= 0 && m.Build(counter, counterCell)) return;
		}
		var def = Catalog.Turret(Plan[_next % Plan.Length]);
		if (m.Money < def.Cost)
			return;
		var cell = BestCell(m, def);
		if (cell.X >= 0 && m.Build(def, cell, BestDirection(m, def, cell)))
			_next++;
	}

	static TurretDef CounterFor(Match m)
	{
		var boss = m.Bugs.FirstOrDefault(b => !b.Gone && b.Def.Boss)?.Def
			?? m.Waves.Upcoming().Select(s => Catalog.Enemy(s.Enemy)).FirstOrDefault(b => b?.Boss == true);
		return Catalog.Turret(boss?.Ability switch
		{
			"trojan" or "rootkit" => "antivirus",
			"mitm" or "ddos" => "firewall",
			"ransomware" or "panic" => "backup",
			_ => null,
		});
	}

	public static Vector2I BestCell(Match m, TurretDef def)
	{
		var stats = def.ComputeStats(null);
		var reach = def.NeedsDirection ? stats.GetValueOrDefault("beam_length", 48) + 24 : stats.GetValueOrDefault("range");
		var points = m.Path.Samples(12).ToArray();
		var best = new Vector2I(-1, -1);
		var bestScore = 0;
		for (var x = 0; x < Grid.Cols; x++)
			for (var y = 0; y < Grid.Rows; y++)
			{
				var cell = new Vector2I(x, y);
				if (!m.CanBuild(cell))
					continue;
				var center = Grid.CellCenter(cell);
				var score = points.Count(p => center.DistanceTo(p) <= reach);
				if (def.Behavior is "backup" or "firewall")
					score += m.Turrets.Count(t => center.DistanceTo(t.Pos) <= reach) * 8;
				// Prefer covering the end of the route, where bugs are about to hit the CPU.
				if (score > bestScore)
				{
					bestScore = score;
					best = cell;
				}
			}
		return best;
	}

	public static float BestDirection(Match m, TurretDef def, Vector2I cell)
	{
		if (!def.NeedsDirection)
			return 0;
		var route = new HashSet<Vector2I>(m.Level.PathCells());
		var best = 0f;
		var bestScore = -1;
		for (var d = 0; d < 4; d++)
		{
			var dir = new Vector2I(Mathf.RoundToInt(Mathf.Cos(d * Mathf.Pi / 2)), Mathf.RoundToInt(Mathf.Sin(d * Mathf.Pi / 2)));
			var score = Enumerable.Range(1, 3).Count(s => route.Contains(cell + dir * s));
			if (score > bestScore)
			{
				bestScore = score;
				best = d * Mathf.Pi / 2;
			}
		}
		return best;
	}
}

/// <summary>
/// A simple versus attacker: saves energy into a budget that grows during the match, then
/// releases a whole wave at once (mostly the strongest unlocked bugs, padded with cheap ones).
/// Seeded so tests are repeatable.
/// </summary>
public sealed class AttackerBot
{
	readonly Random _rng;
	readonly Queue<EnemyDef> _wave = new();
	float _budget;

	public AttackerBot(int seed = 1)
	{
		_rng = new Random(seed);
		_budget = 40;
	}

	public void Step(Match m)
	{
		if (m.State != MatchState.Playing)
			return;
		if (_wave.Count > 0)
		{
			if (m.SendBug(_wave.Peek()))
				_wave.Dequeue();
			else if (m.Energy < _wave.Peek().VersusCost && m.CannotSend(_wave.Peek()) == "Not enough energy")
				_wave.Clear();
			return;
		}
		if (m.Energy < _budget)
			return;
		PlanWave(m);
		_budget = Mathf.Min(40 + m.Time * (0.8f + (float)_rng.NextDouble() * 0.8f), m.Rules.EnergyCap * 0.9f);
	}

	void PlanWave(Match m)
	{
		var unlocked = Catalog.VersusEnemies.Where(e => m.Time >= e.VersusUnlock).OrderByDescending(e => e.VersusCost).ToList();
		var energy = m.Energy;
		// Spend about two thirds on the heavy hitters and the rest on a swarm.
		var heavy = unlocked.Take(Math.Min(2, unlocked.Count)).ToList();
		var heavyBudget = energy * (0.5f + (float)_rng.NextDouble() * 0.3f);
		while (heavy.Count > 0)
		{
			var pick = heavy[_rng.Next(heavy.Count)];
			if (pick.VersusCost > heavyBudget)
			{
				heavy.Remove(pick);
				continue;
			}
			heavyBudget -= pick.VersusCost;
			energy -= pick.VersusCost;
			_wave.Enqueue(pick);
		}
		var cheap = unlocked.Last();
		while (energy >= cheap.VersusCost)
		{
			energy -= cheap.VersusCost;
			_wave.Enqueue(cheap);
		}
	}
}
