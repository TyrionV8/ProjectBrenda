using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using HardwareDefence.UI;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.Game;

/// <summary>
/// Plays one match. Everything on screen is rebuilt from the feed's snapshot each frame, and
/// simulation events become effects and sounds — the same code path for local and online play.
/// </summary>
public partial class GameScreen : Node2D
{
	static readonly float[] Speeds = { 1, 2, 4, 8 };

	public IMatchFeed Feed { get; private set; }
	public BuildController Build { get; private set; }
	public Hud Hud { get; private set; }
	public bool Playing => _state == MatchState.Playing && Feed.Current != null && string.IsNullOrEmpty(Feed.Status);

	PixelWorld _world;
	Camera2D _camera;
	BoardView _board;
	PathTraceView _trace;
	CpuView _cpu;
	Node2D _turretLayer;
	Node2D _bugLayer;
	Node2D _projectileLayer;
	Fx _fx;
	PauseMenu _pause;
	HashSet<Vector2I> _blocked;
	readonly Dictionary<int, BugView> _bugs = new();
	readonly Dictionary<int, TurretView> _turrets = new();
	readonly Dictionary<Vector2I, TurretView> _turretsByCell = new();
	readonly Dictionary<int, ProjectileView> _projectiles = new();
	MatchState _state = MatchState.Playing;
	int _speedIndex;
	float _shake;
	float _sparkTimer;
	bool _ended;

	public override void _Ready()
	{
		Feed = GameSession.CreateFeed?.Invoke() ?? new LocalFeed(Catalog.Levels[0], MatchMode.Campaign, PlayerSide.Defender, true);
		var level = Feed.Level;
		_blocked = level.BlockedCells();

		// The board is drawn in a low-resolution world so all the art shares one pixel grid.
		_world = new PixelWorld();
		AddChild(_world);
		_camera = _world.Camera;
		var world = _world.Root;
		var backdrop = new ColorRect { Color = new Color(0.0235f, 0.0431f, 0.0745f), Position = new Vector2(-40, -40), Size = new Vector2(1446, 848), MouseFilter = Control.MouseFilterEnum.Ignore };
		world.AddChild(backdrop);
		world.AddChild(new CanvasModulate { Color = new Color(0.5f, 0.55f, 0.68f) });

		_board = new BoardView { Position = Grid.Origin };
		world.AddChild(_board);
		_board.Setup(level, Grid.Cols, Grid.Rows, level.Title.GetHashCode() & 0xffff);
		_trace = new PathTraceView();
		_board.AddChild(_trace);
		_trace.Setup(level.PathPoints(), level.EntryPoint());
		_cpu = new CpuView { Position = Grid.CellCenter(level.CpuCell), Label = level.CoreLabel ?? "CPU" };
		_board.AddChild(_cpu);
		_turretLayer = new Node2D();
		_bugLayer = new Node2D();
		_projectileLayer = new Node2D();
		_fx = new Fx();
		_board.AddChild(_turretLayer);
		_board.AddChild(_bugLayer);
		_board.AddChild(_projectileLayer);
		_board.AddChild(_fx);
		_fx.ShakeRequested += amount => _shake = Mathf.Max(_shake, amount);
		Build = new BuildController { Screen = this };
		_board.AddChild(Build);

		Hud = new Hud(this);
		AddChild(Hud);
		_pause = new PauseMenu(this);
		AddChild(_pause);
		Build.SelectionChanged += Hud.OnSelectionChanged;
		Build.Denied += Hud.FlashMoney;

		Audio.PlayMusic(string.IsNullOrEmpty(level.Music) ? "music_level_1" : level.Music);
		var subtitle = Feed.Mode == MatchMode.Versus ? (Feed.LocalSide == PlayerSide.Defender ? "Defend the CPU" : "Break into the CPU") : "Defend the CPU";
		Hud.ShowBanner(level.Title.ToUpperInvariant(), string.IsNullOrEmpty(level.Subtitle) ? subtitle : level.Subtitle);
	}

	public override void _ExitTree()
	{
		Feed?.Close();
		Engine.TimeScale = 1;
	}

	public bool CanBuild(Vector2I cell) => Feed.Current != null && Feed.Current.CanBuild(Feed.Level, _blocked, cell);

	public TurretView TurretViewAt(Vector2I cell) => _turretsByCell.GetValueOrDefault(cell);

	/// <summary>The pointer in board coordinates (the world viewport doesn't get input events itself).</summary>
	public Vector2 BoardMouse() => GetGlobalMousePosition() - Grid.Origin;

	public override void _Process(double delta)
	{
		var dt = (float)delta;
		Feed.Process(dt);
		var snap = Feed.Current;
		var status = Feed.Status;
		if (!string.IsNullOrEmpty(status) && !_ended)
			Hud.ShowWaiting(status);
		else
			Hud.HideWaiting();
		if (snap == null)
			return;
		SyncBugs(snap, dt);
		SyncTurrets(snap);
		SyncProjectiles(snap, dt);
		SyncComponents(snap);
		_cpu.Hp = snap.CpuHp;
		foreach (var e in Feed.DrainEvents())
			HandleEvent(e);
		LaserSparks(snap, dt);
		Hud.Apply(snap);
		_state = snap.MatchState;
		if (_state != MatchState.Playing && !_ended)
			EndMatch(snap);
		if (_shake > 0)
		{
			_shake = Mathf.Max(_shake - dt * 30, 0);
			_camera.Offset = new Vector2((float)GD.RandRange(-1, 1), (float)GD.RandRange(-1, 1)) * _shake;
		}
		else
			_camera.Offset = Vector2.Zero;
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (Feed.LocalSide == PlayerSide.Defender && e is InputEventMouse && Build.HandlePointer(e))
			GetViewport().SetInputAsHandled();
		else if (e.IsActionPressed("pause"))
		{
			if (Build.Busy)
				Build.Back();
			else if (!_ended)
				OpenMenu();
			GetViewport().SetInputAsHandled();
		}
		else if (e.IsActionPressed("toggle_grid"))
			_board.ShowGrid = !_board.ShowGrid;
		else if (e.IsActionPressed("toggle_speed"))
			CycleSpeed();
		else if (e is InputEventKey { Pressed: true, Echo: false } key)
		{
			var k = (int)key.Keycode - (int)Key.Key1;
			if (Feed.LocalSide == PlayerSide.Defender && k >= 0 && k < Catalog.Turrets.Count)
				Build.Select(Catalog.Turrets[k]);
			else if (Feed.LocalSide == PlayerSide.Attacker && k >= 0 && k < Catalog.EnemyList.Count)
				SendBug(Catalog.EnemyList[k]);
		}
	}

	public void SendBug(EnemyDef def)
	{
		if (!Playing)
			return;
		var reason = Feed.CannotSend(def);
		if (reason != null)
		{
			Audio.Play("error", "UI");
			if (reason == "Not enough energy")
				Hud.FlashMoney();
			return;
		}
		Feed.SendBug(def);
		Hud.BugSent(def);
		Audio.Play("send", "UI");
	}

	public void CycleSpeed()
	{
		if (!Feed.IsLocalOnly || _ended)
			return;
		_speedIndex = (_speedIndex + 1) % Speeds.Length;
		Engine.TimeScale = Speeds[_speedIndex];
		Hud.UpdateSpeed(Speeds[_speedIndex]);
	}

	void ResetSpeed()
	{
		_speedIndex = 0;
		Engine.TimeScale = 1;
		Hud.UpdateSpeed(1);
	}

	public void OpenMenu() => _pause.Open();

	public void Restart() => Transition.Go(GameSession.GameScene);

	public void Leave()
	{
		Feed.Close();
		Transition.Go(GameSession.Current == GameSession.Kind.Campaign ? GameSession.MenuScene : GameSession.ReturnScene);
	}

	// ------------------------------------------------------------ syncing views with the snapshot

	void SyncBugs(Snapshot snap, float dt)
	{
		var seen = new HashSet<int>();
		foreach (var b in snap.Bugs)
		{
			seen.Add(b.Id);
			if (!_bugs.TryGetValue(b.Id, out var view))
			{
				var def = Catalog.Enemy(b.Type);
				if (def == null)
					continue;
				view = new BugView(def, b.Id);
				_bugLayer.AddChild(view);
				_bugs[b.Id] = view;
			}
			view.Apply(b, dt);
		}
		foreach (var id in _bugs.Keys.Where(id => !seen.Contains(id)).ToList())
			RemoveBug(id);
	}

	void RemoveBug(int id)
	{
		if (!_bugs.Remove(id, out var view))
			return;
		view.QueueFree();
	}

	void SyncTurrets(Snapshot snap)
	{
		foreach (var t in snap.Turrets)
		{
			if (!_turrets.TryGetValue(t.Id, out var view))
			{
				var def = Catalog.Turret(t.Type);
				if (def == null)
					continue;
				var cell = new Vector2I(t.Cell[0], t.Cell[1]);
				view = new TurretView(def, t.Id, cell, t.Direction, Feed.StatsFor(def));
				_turretLayer.AddChild(view);
				_turrets[t.Id] = view;
				_turretsByCell[cell] = view;
				_fx.Sparks(view.Position, def.Color, 16, 140, 0.4f, 0.5f);
				_fx.GlowFlash(view.Position, def.Color, 1.4f, 0.3f);
			}
			view.Apply(t);
		}
	}

	void SyncProjectiles(Snapshot snap, float dt)
	{
		var seen = new HashSet<int>();
		foreach (var p in snap.Projectiles)
		{
			seen.Add(p.Id);
			if (!_projectiles.TryGetValue(p.Id, out var view))
			{
				var color = Catalog.Turret(p.Source)?.Color ?? Colors.White;
				view = new ProjectileView(p.Id, p.Kind, color) { Position = new Vector2(p.X, p.Y) };
				_projectileLayer.AddChild(view);
				_projectiles[p.Id] = view;
			}
			view.Apply(p, dt);
		}
		foreach (var id in _projectiles.Keys.Where(id => !seen.Contains(id)).ToList())
		{
			_projectiles[id].QueueFree();
			_projectiles.Remove(id);
		}
	}

	void SyncComponents(Snapshot snap)
	{
		var count = Mathf.Min(snap.Components.Count, _board.Components.Count);
		for (var i = 0; i < count; i++)
		{
			var c = _board.Components[i];
			var kind = c.Data.Kind;
			c.SetHp(snap.Components[i], kind.HpPerCell * c.Data.Size[0] * c.Data.Size[1]);
		}
	}

	void LaserSparks(Snapshot snap, float dt)
	{
		_sparkTimer -= dt;
		var emit = _sparkTimer <= 0;
		if (emit)
			_sparkTimer = 0.07f;
		foreach (var view in _turrets.Values)
		{
			if (view.Def.Behavior != "laser")
				continue;
			view.BeamHits.Clear();
			var dir = Vector2.FromAngle(view.Direction);
			var from = view.Position + dir * 24;
			var to = view.Position + dir * (24 + view.Stats.GetValueOrDefault("beam_length"));
			foreach (var b in snap.Bugs)
			{
				var pos = new Vector2(b.X, b.Y);
				var def = Catalog.Enemy(b.Type);
				if (def == null || Geometry2D.GetClosestPointToSegment(pos, from, to).DistanceTo(pos) > def.Radius + Match.BeamWidth * 0.5f)
					continue;
				view.BeamHits.Add(pos);
				if (emit)
					_fx.Sparks(pos, view.Def.Color, 4, 120, 0.25f, 0.35f);
			}
		}
	}

	// ------------------------------------------------------------ events → effects and sounds

	void HandleEvent(SimEvent e)
	{
		switch (e.Kind)
		{
			case EventKind.BugDied:
				var def = Catalog.Enemy(e.Type);
				if (def != null)
					_fx.BugDeath(e.Pos, def);
				Audio.Play("death", "SFX", -4, 0.15f);
				RemoveBug(e.Id);
				break;
			case EventKind.BugEscaped:
				RemoveBug(e.Id);
				break;
			case EventKind.CpuHit:
				_fx.Sparks(_cpu.Position, CpuView.Critical, 18, 220, 0.5f, 0.6f);
				_fx.LightFlash(_cpu.Position, CpuView.Critical, 2.5f, 1.6f, 0.4f);
				_fx.Shake(7);
				PostFx.PulseDamage();
				Audio.Play("cpu_hit");
				if (e.Value <= 0)
				{
					_fx.Explosion(_cpu.Position, 120);
					_fx.Debris(_cpu.Position, new Color("2c323b"), 30, 220);
				}
				break;
			case EventKind.TurretBuilt:
				Audio.Play("place");
				break;
			case EventKind.TurretFired:
				if (_turrets.TryGetValue(e.Id, out var turret))
				{
					turret.Fired();
					_fx.GlowFlash(e.Pos, turret.Def.Color, turret.Def.Behavior == "burst" ? 1 : 0.5f, 0.12f);
				}
				switch (e.Type)
				{
					case "blaster": Audio.Play("shoot", "SFX", -4); break;
					case "mortar": Audio.Play("bomb", "SFX", -2); break;
					case "burst": Audio.Play("pew", "SFX", -6); break;
				}
				break;
			case EventKind.BulletHit:
				_fx.Sparks(e.Pos, Catalog.Turret(e.Type)?.Color ?? Colors.White, 5, 90, 0.2f, 0.3f);
				break;
			case EventKind.Explosion:
				_fx.Explosion(e.Pos, e.Value);
				Audio.Play("explode", "SFX", -3);
				break;
			case EventKind.OrbPopped:
				var ice = Catalog.Turret(e.Type)?.Color ?? Colors.White;
				_fx.Sparks(e.Pos, ice, 12, 120, 0.4f, 0.45f);
				_fx.GlowFlash(e.Pos, ice, 1, 0.25f);
				Audio.Play("freeze", "SFX", -6);
				break;
			case EventKind.ComponentDestroyed:
				if (e.Id < _board.Components.Count)
				{
					var c = _board.Components[e.Id];
					_fx.ComponentDestroyed(c.Rect, ComponentArt.MainColor(c.Data));
				}
				Audio.Play("explode", "SFX", -9);
				break;
			case EventKind.Chew:
				if (e.Id < _board.Components.Count)
					_fx.Debris(e.Pos, ComponentArt.MainColor(_board.Components[e.Id].Data), 3, 50);
				Audio.Play("crunch", "SFX", -14, 0.2f);
				break;
			case EventKind.WaveCountdown:
				Hud.ShowBanner($"WAVE {e.Id} CLEARED", "Next wave incoming");
				_trace.SetIntensity(0.45f);
				break;
			case EventKind.WaveStarted:
				Hud.ShowBanner($"WAVE {e.Id + 1}", $"{Feed.Level.EnemyCount(e.Id)} bugs");
				Audio.Play("wave", "SFX", -2);
				_trace.SetIntensity(1);
				break;
		}
	}

	async void EndMatch(Snapshot snap)
	{
		_ended = true;
		Build.Cancel();
		ResetSpeed();
		var defenderWon = _state == MatchState.DefenderWon;
		var localWon = defenderWon == (Feed.LocalSide == PlayerSide.Defender);
		Audio.Play(localWon ? "win" : "lose");
		await ToSignal(GetTree().CreateTimer(defenderWon ? 0.8 : 1.4), SceneTreeTimer.SignalName.Timeout);
		if (Feed.Mode == MatchMode.Versus)
		{
			var title = localWon ? "YOU WIN" : "YOU LOSE";
			var message = defenderWon ? "The CPU survived the attack." : "The bugs broke into the CPU.";
			if (Feed is Net.HostFeed { OpponentLeft: true })
				message = "Your opponent left the match.";
			message += $"\nBoard integrity: {Mathf.RoundToInt(snap.Integrity * 100)}%";
			Hud.ShowResult(title, localWon ? UiTheme.Green : UiTheme.Red, message, 0, ("Main Menu", () => Leave()));
			return;
		}
		if (!defenderWon)
		{
			Hud.ShowResult("SYSTEM FAILURE", UiTheme.Red, "The bugs reached the CPU.", 0, ("Restart", Restart), ("Quit", Leave));
			return;
		}
		var integrity = $"Board integrity: {Mathf.RoundToInt(snap.Integrity * 100)}%";
		switch (GameSession.Current)
		{
			case GameSession.Kind.Campaign:
				var before = GameState.Points;
				var finished = GameState.CompleteCurrentLevel(snap.Stars);
				var gained = GameState.Points - before;
				var message = integrity + (gained > 0 ? $"\n+{gained} upgrade points" : "");
				if (finished)
					Hud.ShowResult("ALL CIRCUITS SECURED", UiTheme.Green, message, snap.Stars, ("Continue", () => Transition.Go("res://scenes/end_screen.tscn")));
				else
					Hud.ShowResult("CIRCUIT SECURED", UiTheme.Green, message, snap.Stars, ("Upgrades", () => Transition.Go("res://scenes/upgrade_screen.tscn")));
				break;
			default:
				Hud.ShowResult("CIRCUIT SECURED", UiTheme.Green, integrity, snap.Stars, ("Back", () => Transition.Go(GameSession.ReturnScene)));
				break;
		}
	}
}
