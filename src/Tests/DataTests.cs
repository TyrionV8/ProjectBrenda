using System.Linq;
using Brenda.Autoload;
using Brenda.Data;
using Brenda.Sim;
using Godot;

namespace Brenda.Tests;

public sealed class DataTests : TestSuite
{
	[Test]
	public void LevelsLoadAndValidate()
	{
		Check(Catalog.Levels.Count >= 3, "at least 3 levels");
		for (var i = 0; i < Catalog.Levels.Count; i++)
		{
			var problems = Catalog.Levels[i].Validate(Catalog.Enemies.Keys.ToList());
			Check(problems.Count == 0, $"level {i + 1}: {string.Join("; ", problems)}");
		}
	}

	[Test]
	public void BlockedCellsCoverRouteAndCpu()
	{
		var level = Catalog.Levels[0];
		var blocked = level.BlockedCells();
		foreach (var cell in level.PathCells())
			Check(blocked.Contains(cell), $"route cell {cell} not blocked");
		Check(blocked.Contains(level.CpuCell + new Vector2I(1, 1)), "cpu footprint");
	}

	[Test]
	public void LevelJsonRoundTrip()
	{
		var level = Catalog.Levels[0];
		var copy = JsonIO.Read<LevelData>(JsonIO.Write(level));
		CheckEq(copy.Components.Count, level.Components.Count, "components");
		CheckEq(copy.Path.Count, level.Path.Count, "path");
		CheckEq(copy.Waves.Sum(w => w.Count), level.Waves.Sum(w => w.Count), "spawns");
		CheckEq(copy.Theme, level.Theme, "theme");
	}

	[Test]
	public void ValidationFindsProblems()
	{
		var level = Catalog.Levels[0].Clone();
		level.Components.Add(new ComponentData { Type = "chip", Cell = new[] { level.Path[1][0], level.Path[1][1] }, Size = new[] { 1, 1 } });
		level.Path.Add(new[] { level.Path[^1][0] + 2, level.Path[^1][1] + 3 });
		var problems = level.Validate();
		Check(problems.Any(p => p.Contains("on the route")), "component on route detected");
		Check(problems.Any(p => p.Contains("not horizontal")), "diagonal segment detected");
	}

	[Test]
	public void UpgradesChangeStats()
	{
		var blaster = Catalog.Turret("blaster");
		var b = blaster.ComputeStats(null);
		CheckNear(b["damage"], 1, "base damage");
		CheckNear(b["cooldown"], 2, "base cooldown");
		var up = blaster.ComputeStats(new System.Collections.Generic.Dictionary<string, int> { ["damage"] = 3, ["speed"] = 25, ["range"] = 15 });
		CheckNear(up["damage"], 4, "upgraded damage");
		CheckNear(up["cooldown"], 2 - 25f / 30f, "upgraded cooldown");
		CheckNear(up["range"], 125, "upgraded range");
		var freezer = Catalog.Turret("freezer");
		CheckNear(freezer.ComputeStats(new System.Collections.Generic.Dictionary<string, int> { ["freeze"] = 50 })["slow_factor"], 0.1f, "slow clamp");
	}

	[Test]
	public void SaveRoundTripAndPoints()
	{
		GameState.Reset();
		var mortar = Catalog.Turret("mortar");
		GameState.Points = 2;
		Check(GameState.Buy(mortar, mortar.FindUpgrade("bomb_area")), "buy 1");
		Check(GameState.Buy(mortar, mortar.FindUpgrade("bomb_area")), "buy 2");
		Check(!GameState.Buy(mortar, mortar.FindUpgrade("bomb_area")), "no points left");
		GameState.CurrentLevel = 0;
		Check(!GameState.CompleteCurrentLevel(2), "not finished");
		CheckEq(GameState.Points, GameState.PointsPerLevel, "points after first clear");
		GameState.CurrentLevel = 0;
		GameState.CompleteCurrentLevel(3);
		CheckEq(GameState.Points, GameState.PointsPerLevel, "no points for replays");
		CheckEq(GameState.Stars[0], 3, "best stars kept");
		GameState.Reset();
		Check(GameState.Load(), "load");
		CheckEq(GameState.UpgradeLevel("mortar", "bomb_area"), 2, "upgrade saved");
		Check(GameState.Unlocked[1], "level 2 unlocked");
		CheckEq(GameState.CurrentLevel, 1, "continue from last unlocked");
		DirAccess.RemoveAbsolute(GameState.SavePath);
		GameState.Reset();
	}

	[Test]
	public void SnapshotJsonRoundTrip()
	{
		var m = new Match(Catalog.Levels[0]);
		m.Build(Catalog.Turret("blaster"), DefenderBot.BestCell(m, Catalog.Turret("blaster")));
		for (var i = 0; i < 600; i++)
			m.Step(Match.StepTime);
		var snap = Snapshot.From(m);
		var copy = JsonIO.Read<Snapshot>(JsonIO.Write(snap));
		CheckEq(copy.Bugs.Count, snap.Bugs.Count, "bugs");
		CheckEq(copy.Turrets.Count, 1, "turrets");
		CheckEq(copy.Components.Count, m.Components.Count, "components");
		CheckEq(copy.State, "playing", "state");
	}
}
