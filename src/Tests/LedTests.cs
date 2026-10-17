using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.Tests;

public sealed class LedTests : TestSuite
{
	[Test]
	public void BuiltInLedsCoverRouteWithoutTakingMostTurretCells()
	{
		foreach (var level in Catalog.Levels)
		{
			var route = level.PathCells();
			int Distance(Vector2I cell) => route.Min(p => Math.Abs(p.X - cell.X) + Math.Abs(p.Y - cell.Y));
			var leds = level.Components.Where(c => ComponentTypes.IsLed(c.Type)).ToList();
			Check(leds.Count >= (int)Math.Ceiling(route.Count / 3f), $"{level.Title}: route lighting density");
			Check(leds.Any(c => c.Type == "led") && leds.Any(c => c.Type == "led_5mm"), $"{level.Title}: both packages");
			Check(leds.Count(c => Distance(c.CellV) is >= 1 and <= 3) >= (int)Math.Ceiling(route.Count / 4f),
				$"{level.Title}: LEDs near the route");
			foreach (var led in leds)
				CheckEq(led.SizeV, Vector2I.One, $"{level.Title}: single-cell LED");
			// Existing SMD components are preserved, including distant indicator LEDs. The new
			// through-hole package is always placed within the requested route-side band.
			foreach (var led in leds.Where(c => c.Type == "led_5mm"))
				Check(Distance(led.CellV) is >= 1 and <= 3, $"{level.Title}: 5 mm LED within 1–3 cells");
			var blockedWithoutLeds = new HashSet<Vector2I>(route.Concat(level.CpuCells()));
			foreach (var c in level.Components.Where(c => c.Kind.Blocks && !ComponentTypes.IsLed(c.Type)))
				blockedWithoutLeds.UnionWith(c.Cells());
			var adjacentSites = 0;
			for (var y = 0; y < Grid.Rows; y++)
				for (var x = 0; x < Grid.Cols; x++)
				{
					var cell = new Vector2I(x, y);
					if (!blockedWithoutLeds.Contains(cell) && Distance(cell) == 1)
						adjacentSites++;
				}
			Check(leds.Count(c => Distance(c.CellV) == 1) * 2 < adjacentSites,
				$"{level.Title}: majority of route-adjacent turret sites preserved");
		}
	}

	[Test]
	public void MatchComponentsAreExactlyTheLevelComponents()
	{
		foreach (var level in Catalog.Levels)
		{
			var match = new Match(level);
			CheckEq(match.Components.Count, level.Components.Count, $"{level.Title}: simulation component count");
			CheckEq(Snapshot.From(match).Components.Count, level.Components.Count, $"{level.Title}: snapshot component count");
			for (var i = 0; i < level.Components.Count; i++)
				Check(ReferenceEquals(match.Components[i].Data, level.Components[i]), $"{level.Title}: component order {i}");
		}
	}

	static LevelData TestLevel(params string[] types) => new()
	{
		Path = new() { new[] { 0, 2 }, new[] { 10, 2 }, new[] { 10, 10 } },
		Components = types.Select((type, i) => new ComponentData { Type = type, Cell = new[] { 3 + i, 3 } }).ToList(),
	};

	[Test]
	public void FiveMillimetreLedIsChewedAndRewardsVersusAttacker()
	{
		var match = new Match(TestLevel("led_5mm"), MatchMode.Versus);
		var led = match.Components[0];
		Check(led.Chewable && led.Data.Kind.Blocks, "LED is chewable and blocks construction");
		CheckNear(led.MaxHp, ComponentTypes.Get("led").HpPerCell, "same health as SMD LED");
		var bug = match.SpawnBug("brute");
		bug.Progress = 3 * Grid.Cell;
		var reward = 0f;
		for (var step = 0; step < 600 && !led.Destroyed; step++)
		{
			var before = match.Energy;
			match.Step(Match.StepTime);
			if (led.Destroyed)
				reward = match.Energy - before;
		}
		Check(led.Destroyed, "bug destroyed the 5 mm LED");
		CheckNear(match.BoardIntegrity, 0, "destroyed LED reduces board integrity");
		CheckNear(match.LightLevel, 0, "destroyed LED turns off the light fraction");
		Check(reward >= led.MaxHp * match.Rules.ComponentEnergy - 0.001f, "component energy awarded to attacker");
		Check(match.Events.Any(e => e.Kind == EventKind.ComponentDestroyed && e.Type == "led_5mm"), "ordinary component destruction event");
	}

	[Test]
	public void SnapshotAndSimulationUseTheSameLivingLedFraction()
	{
		var match = new Match(TestLevel("led", "led_5mm", "chip"));
		float SnapshotLight()
		{
			var snapshot = Snapshot.From(match);
			return ComponentTypes.LightFraction(match.Level.Components.Select((c, i) => (c.Type, snapshot.Components[i] >= 0)));
		}
		CheckNear(match.LightLevel, 1, "all LEDs alive");
		match.Components[0].Hp *= 0.3f;
		CheckNear(match.LightLevel, 1, "damaged but living LEDs count");
		match.Components[0].Hp = 0;
		match.Components[0].Destroyed = true;
		CheckNear(match.LightLevel, 0.5f, "one LED destroyed");
		CheckNear(SnapshotLight(), match.LightLevel, "snapshot fraction matches simulation");
		match.Components[2].Destroyed = true;
		CheckNear(match.LightLevel, 0.5f, "non-LED destruction doesn't affect light");
		match.Components[1].Destroyed = true;
		CheckNear(match.LightLevel, 0, "all LEDs destroyed");
		CheckNear(SnapshotLight(), match.LightLevel, "dark snapshot matches simulation");
		match.Components[0].Hp = match.Components[0].MaxHp;
		match.Components[0].Destroyed = false;
		CheckNear(match.LightLevel, 0.5f, "restored LED counts again");
		CheckNear(new Match(TestLevel("chip")).LightLevel, 1, "no LEDs uses lit fallback");
	}

	[Test]
	public void LedViewTurnsOffAndRelightsAfterRestoration()
	{
		foreach (var type in new[] { "led", "led_5mm" })
			foreach (var theme in new[] { "blue", "white" })
			{
				var boardTheme = BoardTheme.Get(theme);
				var view = new ComponentView(new ComponentData { Type = type }, boardTheme, 0);
				SceneTests.Tree.Root.AddChild(view);
				var light = view.GetChildren().OfType<PointLight2D>().Single();
				Check(light.Texture == FxUtil.WideLightTexture, $"{type}/{theme}: wide light texture");
				CheckNear(light.Energy, 0.45f * boardTheme.LedEnergy, $"{type}/{theme}: theme energy");
				view.SetHp(-1, 3);
				Check(!light.Visible && light.Energy == 0 && !view.IsProcessing(), $"{type}/{theme}: destroyed light off");
				view.SetHp(1, 3);
				Check(light.Visible && light.Energy > 0 && view.IsProcessing(), $"{type}/{theme}: snapshot restoration relights");
				view.SetHp(-1, 3);
				view.Restore();
				Check(light.Visible && light.Energy > 0 && view.IsProcessing(), $"{type}/{theme}: explicit restoration relights");
				view.QueueFree();
			}
	}
}
