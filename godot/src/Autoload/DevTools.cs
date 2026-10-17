using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Brenda.Data;
using Brenda.Game;
using Brenda.Sim;
using Brenda.Tests;
using Brenda.UI;
using Godot;

namespace Brenda.Autoload;

/// <summary>
/// Developer entry points, only active with command-line user arguments:
///   godot --headless --path . -- --test                    run all [Test] methods
///   godot --path . -- --capture=menu,game:1 --out=DIR      save screenshots
///   godot --headless --path . -- --build-theme             regenerate assets/ui/theme.tres
/// </summary>
public partial class DevTools : Node
{
	public override void _Ready()
	{
		var args = OS.GetCmdlineUserArgs();
		if (args.Contains("--build-theme"))
		{
			DirAccess.MakeDirRecursiveAbsolute("res://assets/ui");
			var err = ResourceSaver.Save(UiTheme.Build(), "res://assets/ui/theme.tres");
			GD.Print($"theme saved: {err}");
			GetTree().Quit(err == Error.Ok ? 0 : 1);
		}
		else if (args.Contains("--test"))
			Callable.From(() => { _ = RunTests(Arg(args, "--filter", "")); }).CallDeferred();
		else if (args.Any(a => a.StartsWith("--capture=")))
			Callable.From(() => { _ = Capture(Arg(args, "--capture", "").Split(','), Arg(args, "--out", "user://shots")); }).CallDeferred();
	}

	static string Arg(string[] args, string key, string fallback) =>
		args.FirstOrDefault(a => a.StartsWith(key + "="))?.Substring(key.Length + 1) ?? fallback;

	async Task RunTests(string filter)
	{
		SceneTests.Tree = GetTree();
		var failures = 0;
		var total = 0;
		var suites = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(TestSuite)) && !t.IsAbstract).OrderBy(t => t.Name);
		foreach (var type in suites)
			foreach (var method in type.GetMethods().Where(m => m.GetCustomAttribute<TestAttribute>() != null))
			{
				var name = $"{type.Name}.{method.Name}";
				if (filter != "" && !name.Contains(filter))
					continue;
				total++;
				var suite = (TestSuite)Activator.CreateInstance(type);
				if (OS.GetCmdlineUserArgs().Contains("--verbose-tests"))
					GD.Print($"  ...   {name}");
				var started = Time.GetTicksMsec();
				try
				{
					if (method.Invoke(suite, null) is Task task)
						await task;
				}
				catch (Exception e)
				{
					suite.Errors.Add((e.InnerException ?? e).ToString());
				}
				var ms = Time.GetTicksMsec() - started;
				if (suite.Errors.Count == 0)
					GD.Print($"  PASS  {name} ({ms} ms) {suite.Note}");
				else
				{
					failures++;
					GD.Print($"  FAIL  {name}");
					foreach (var e in suite.Errors)
						GD.Print($"        - {e}");
				}
			}
		GD.Print($"{total - failures}/{total} tests passed");
		Engine.TimeScale = 1;
		GetTree().Quit(failures > 0 ? 1 : 0);
	}

	async Task Capture(string[] shots, string outDir)
	{
		DirAccess.MakeDirRecursiveAbsolute(outDir);
		foreach (var shot in shots)
		{
			var parts = shot.Split(':');
			var level = parts.Length > 1 ? int.Parse(parts[1]) - 1 : 0;
			var wait = parts.Length > 2 ? float.Parse(parts[2]) : 10;
			switch (parts[0])
			{
				case "menu":
					await Goto(GameSession.MenuScene, 2.5f);
					break;
				case "levels":
					GameState.Reset();
					for (var i = 0; i < GameState.Unlocked.Count; i++)
					{
						GameState.Unlocked[i] = true;
						GameState.Stars[i] = i % 4;
					}
					await Goto("res://scenes/level_select.tscn", 1.5f);
					break;
				case "upgrades":
					GameState.Points = 9;
					for (var i = 0; i < 4; i++)
						GameState.Buy(Catalog.Turrets[i], Catalog.Turrets[i].Upgrades[0]);
					await Goto("res://scenes/upgrade_screen.tscn", 1.5f);
					break;
				case "end":
					await Goto("res://scenes/end_screen.tscn", 2);
					break;
				case "editor":
				case "waves":
					if (parts.Length > 1)
						Editor.EditorState.Load(Catalog.Levels[level].Clone(), string.Format(Catalog.LevelPath, level + 1));
					await Goto("res://scenes/level_editor.tscn", 1.0f);
					if (parts[0] == "waves")
						(GetTree().CurrentScene as Editor.LevelEditor)?.ShowWaves(true);
					await Wait(0.5f);
					break;
				case "versus":
					await Goto("res://scenes/versus_menu.tscn", 1.5f);
					break;
				case "game":
				case "attack":
				case "win":
					var attacker = parts[0] == "attack";
					var lvl = Catalog.Levels[Math.Min(level, Catalog.Levels.Count - 1)];
					GameSession.Current = attacker ? GameSession.Kind.Versus : GameSession.Kind.Custom;
					GameSession.CreateFeed = () =>
					{
						var feed = new LocalFeed(lvl, attacker ? MatchMode.Versus : MatchMode.Campaign, attacker ? PlayerSide.Attacker : PlayerSide.Defender, false)
						{
							DefenderBot = new DefenderBot(), AttackerBot = attacker ? new AttackerBot(5) : null,
						};
						feed.Match.Money += attacker ? 0 : 60;
						return feed;
					};
					await Goto(GameSession.GameScene, 0.3f);
					var screen = GetTree().CurrentScene as GameScreen;
					if (parts[0] == "win")
					{
						Engine.TimeScale = 4;
						while (screen.Feed.Current.MatchState == MatchState.Playing)
							await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						Engine.TimeScale = 1;
						await Wait(2.5f);
					}
					else
					{
						await Wait(wait);
						if (!attacker)
							screen.Build.Select(Catalog.Turrets[0]);
					}
					break;
				case "pause":
					(GetTree().CurrentScene as GameScreen)?.OpenMenu();
					await Wait(0.4f);
					break;
			}
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			var path = outDir.PathJoin(shot.Replace(":", "_") + ".png");
			GetViewport().GetTexture().GetImage().SavePng(path);
			GD.Print($"saved {path}");
		}
		GetTree().Quit();
	}

	async Task Goto(string path, float wait)
	{
		Transition.Go(path);
		await Wait(0.8f + wait);
	}

	async Task Wait(float seconds) => await ToSignal(GetTree().CreateTimer(seconds, true, false, true), SceneTreeTimer.SignalName.Timeout);
}
