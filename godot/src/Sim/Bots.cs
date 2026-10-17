using System;
using System.Collections.Generic;
using System.Linq;
using Brenda.Data;
using Godot;

namespace Brenda.Sim;

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
		var def = Catalog.Turret(Plan[_next % Plan.Length]);
		if (m.Money < def.Cost)
			return;
		var cell = BestCell(m, def);
		if (cell.X >= 0 && m.Build(def, cell, BestDirection(m, def, cell)))
			_next++;
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
/// A simple versus attacker: saves up for a randomly chosen unlocked bug, sometimes
/// floods cheap ones. Seeded so tests are repeatable.
/// </summary>
public sealed class AttackerBot
{
	readonly Random _rng;
	EnemyDef _goal;
	int _burst;

	public AttackerBot(int seed = 1) => _rng = new Random(seed);

	public void Step(Match m)
	{
		if (m.State != MatchState.Playing)
			return;
		var unlocked = Catalog.EnemyList.Where(e => m.Time >= e.VersusUnlock).ToList();
		if (_burst > 0)
		{
			var cheap = unlocked.OrderBy(e => e.VersusCost).First();
			if (m.SendBug(cheap))
				_burst--;
			return;
		}
		if (_goal == null || m.Time < _goal.VersusUnlock)
		{
			if (_rng.NextDouble() < 0.25)
			{
				_burst = 3 + _rng.Next(5);
				return;
			}
			// Favour the newest unlocks so the pressure grows during the match.
			var weights = unlocked.Select((e, i) => (e, w: 1.0 + i * i)).ToList();
			var roll = _rng.NextDouble() * weights.Sum(x => x.w);
			foreach (var (e, w) in weights)
			{
				roll -= w;
				if (roll <= 0)
				{
					_goal = e;
					break;
				}
			}
			_goal ??= unlocked.Last();
		}
		if (m.SendBug(_goal))
			_goal = null;
	}
}
