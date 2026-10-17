using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;
using HardwareDefence.Tests;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Autoload;

/// <summary>
/// Developer entry points, only active with command-line user arguments:
///   godot --headless --path . -- --test                    run all [Test] methods
///   godot --path . -- --capture=menu,game:1 --out=DIR      save screenshots
///   godot --headless --path . -- --build-theme             regenerate assets/ui/theme.tres
///   godot --headless --path . -- --balance                 bot vs bot versus report (data/versus.json)
///   godot --headless --path . -- --server [--level=N] [--port=7777] [--defender=remote|bot] [--attacker=remote|bot]
///                                                          dedicated versus host, e.g. for AI clients
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
		else if (args.Contains("--balance"))
		{
			BalanceReport();
			GetTree().Quit();
		}
		else if (args.Contains("--server"))
			RunServer(args);
		else if (args.Contains("--test"))
			Callable.From(() => { _ = RunTests(Arg(args, "--filter", "")); }).CallDeferred();
		else if (args.Any(a => a.StartsWith("--capture=")))
			Callable.From(() => { _ = CaptureOrQuit(Arg(args, "--capture", "").Split(','), Arg(args, "--out", "user://shots")); }).CallDeferred();
	}

	static string Arg(string[] args, string key, string fallback) =>
		args.FirstOrDefault(a => a.StartsWith(key + "="))?.Substring(key.Length + 1) ?? fallback;

	/// <summary>Plays every level 3 times with the defender and attacker bots using data/versus.json.</summary>
	static void BalanceReport()
	{
		var wins = 0;
		var games = 0;
		foreach (var level in Catalog.Levels)
		{
			var line = $"{level.Title,-18}";
			for (var seed = 1; seed <= 3; seed++)
			{
				var m = new Match(level, MatchMode.Versus);
				var defender = new DefenderBot();
				var attacker = new AttackerBot(seed);
				while (m.State == MatchState.Playing)
				{
					m.Step(Match.StepTime);
					m.Events.Clear();
					defender.Step(m, Match.StepTime);
					attacker.Step(m);
				}
				games++;
				var won = m.State == MatchState.DefenderWon;
				wins += won ? 1 : 0;
				line += won ? "  defender" : $"  attacker@{m.Time:0}s";
			}
			GD.Print(line);
		}
		GD.Print($"Defender won {wins}/{games} ({100 * wins / games}%). Aim for 40-60%.");
	}

	Net.HostFeed _server;
	int _serverReported = -1;
	double _serverEnd = -1;

	/// <summary>Headless versus host: remote roles wait for WebSocket clients, bot roles play themselves.</summary>
	void RunServer(string[] args)
	{
		var level = Catalog.Levels[Math.Clamp(int.Parse(Arg(args, "--level", "1")) - 1, 0, Catalog.Levels.Count - 1)];
		var port = int.Parse(Arg(args, "--port", Net.Protocol.DefaultPort.ToString()));
		var remote = new System.Collections.Generic.List<PlayerSide>();
		if (Arg(args, "--defender", "remote") == "remote")
			remote.Add(PlayerSide.Defender);
		if (Arg(args, "--attacker", "remote") == "remote")
			remote.Add(PlayerSide.Attacker);
		if (remote.Count == 0)
		{
			GD.PrintErr("Nothing to serve: at least one side must be remote (use --balance for bot vs bot).");
			GetTree().Quit(1);
			return;
		}
		_server = new Net.HostFeed(level, port, PlayerSide.Defender, remote.ToArray());
		if (!remote.Contains(PlayerSide.Defender))
			_server.DefenderBot = new DefenderBot();
		if (!remote.Contains(PlayerSide.Attacker))
			_server.AttackerBot = new AttackerBot((int)Time.GetTicksMsec());
		_server.Host.Joined += (role, name) => GD.Print($"{name} joined as {Net.Protocol.RoleName(role)}");
		_server.Host.Left += role => GD.Print($"the {Net.Protocol.RoleName(role)} left");
		GD.Print(_server.Host.Listening ? $"Serving '{level.Title}' on ws://0.0.0.0:{port} - waiting for {string.Join(", ", remote.Select(Net.Protocol.RoleName))}" : _server.Host.Error);
		if (!_server.Host.Listening)
			GetTree().Quit(1);
	}

	public override void _Process(double delta)
	{
		if (_server == null)
			return;
		_server.Process((float)delta);
		_server.DrainEvents();
		var m = _server.Match;
		var minute = (int)(m.Time / 30);
		if (minute != _serverReported && m.Time > 0)
		{
			_serverReported = minute;
			GD.Print($"t={m.Time:0}s cpu={m.CpuHp} energy={m.Energy:0} money={m.Money} bugs={m.Bugs.Count} turrets={m.Turrets.Count}");
		}
		if (m.State == MatchState.Playing)
			return;
		if (_serverEnd < 0)
		{
			GD.Print($"Match over: {Snapshot.StateName(m.State)} at {m.Time:0}s (cpu {m.CpuHp}, board {m.BoardIntegrity:P0})");
			_serverEnd = 0;
		}
		// Keep serving for a moment so clients receive the final state.
		_serverEnd += delta;
		if (_serverEnd < 1.5)
			return;
		_server.Close();
		_server = null;
		GetTree().Quit();
	}

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

	/// <summary>Errors in an awaited task would otherwise vanish and leave the game running.</summary>
	async Task CaptureOrQuit(string[] shots, string outDir)
	{
		try
		{
			await Capture(shots, outDir);
		}
		catch (Exception e)
		{
			GD.PrintErr($"capture failed: {e}");
			GetTree().Quit(1);
		}
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
				case "host":
					var hostLevel = Catalog.Levels[Math.Min(level, Catalog.Levels.Count - 1)];
					GameSession.StartOnline(() => new Net.HostFeed(hostLevel, 47900, PlayerSide.Defender, PlayerSide.Attacker));
					await Wait(2.5f);
					break;
				case "board":
					var boardLevel = Catalog.Levels[Math.Min(level, Catalog.Levels.Count - 1)];
					GameSession.Current = GameSession.Kind.Custom;
					GameSession.CreateFeed = () => new LocalFeed(boardLevel, MatchMode.Campaign, PlayerSide.Defender, false);
					await Goto(GameSession.GameScene, 2.5f);
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
					while (GetTree().CurrentScene is not GameScreen)
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					var screen = (GameScreen)GetTree().CurrentScene;
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
