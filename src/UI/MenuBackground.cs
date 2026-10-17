using System.Collections.Generic;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Living circuit board behind the menus: flowing route, blinking LEDs and bugs crawling by.</summary>
public partial class MenuBackground : CanvasLayer
{
	static readonly string[] BugTypes = { "bug", "bug", "spider", "termite", "beetle", "roach", "runner", "tank", "brute" };

	readonly List<(BugView View, float Progress, float Speed)> _bugs = new();
	PathLine _path;
	Node2D _bugLayer;
	float _spawn = 0.5f;

	public override void _Ready()
	{
		Layer = -1;
		var level = new LevelData
		{
			Theme = "blue", Title = "",
			Path = new List<int[]> { new[] { -1, 3 }, new[] { 7, 3 }, new[] { 7, 9 }, new[] { 15, 9 }, new[] { 15, 4 }, new[] { 22, 4 }, new[] { 22, 12 }, new[] { 12, 12 }, new[] { 12, 14 }, new[] { 29, 14 } },
			Components = new List<ComponentData>
			{
				new() { Type = "chip", Cell = new[] { 2, 6 }, Size = new[] { 2, 2 } },
				new() { Type = "capacitor", Cell = new[] { 10, 5 }, Size = new[] { 2, 2 } },
				new() { Type = "port", Cell = new[] { 25, 1 }, Size = new[] { 2, 2 }, Label = "rj45" },
				new() { Type = "resistor", Cell = new[] { 17, 7 }, Size = new[] { 2, 1 } },
				new() { Type = "bga", Cell = new[] { 24, 7 }, Size = new[] { 3, 3 }, Label = "HWD" },
				new() { Type = "led", Cell = new[] { 3, 11 }, Color = "3dff6a" },
				new() { Type = "led", Cell = new[] { 4, 11 }, Color = "ff3344" },
				new() { Type = "led", Cell = new[] { 5, 11 }, Color = "3dff6a" },
				new() { Type = "led", Cell = new[] { 18, 1 }, Color = "ffb43d" },
				new() { Type = "ram_slot", Cell = new[] { 9, 1 }, Size = new[] { 8, 1 } },
				new() { Type = "inductor", Cell = new[] { 18, 6 } },
				new() { Type = "inductor", Cell = new[] { 19, 6 } },
				new() { Type = "smd", Cell = new[] { 13, 7 } },
				new() { Type = "header", Cell = new[] { 0, 14 }, Size = new[] { 4, 1 } },
				new() { Type = "crystal", Cell = new[] { 9, 10 }, Size = new[] { 2, 1 } },
			},
		};
		var world = new PixelWorld();
		AddChild(world);
		world.Root.AddChild(new CanvasModulate { Color = new Color(0.55f, 0.6f, 0.72f) });
		var board = new BoardView();
		world.Root.AddChild(board);
		board.Setup(level, 29, 16, 7);
		var trace = new PathTraceView();
		board.AddChild(trace);
		trace.Setup(level.PathPoints(), level.EntryPoint());
		trace.SetIntensity(0.8f);
		_path = new PathLine(level.PathPoints());
		_bugLayer = new Node2D();
		board.AddChild(_bugLayer);
		for (var i = 0; i < 4; i++)
			Spawn((float)GD.RandRange(0, _path.Length * 0.9));
	}

	public override void _Process(double delta)
	{
		var dt = (float)delta;
		_spawn -= dt;
		if (_spawn <= 0)
		{
			_spawn = (float)GD.RandRange(1.2, 3.0);
			Spawn(0);
		}
		for (var i = _bugs.Count - 1; i >= 0; i--)
		{
			var (view, progress, speed) = _bugs[i];
			progress += speed * dt;
			if (progress >= _path.Length)
			{
				view.QueueFree();
				_bugs.RemoveAt(i);
				continue;
			}
			_bugs[i] = (view, progress, speed);
			var pos = _path.Sample(progress);
			view.Apply(new BugSnap { X = pos.X, Y = pos.Y, Angle = _path.AngleAt(progress), Hp = view.Def.MaxHp }, dt);
		}
	}

	void Spawn(float progress)
	{
		var def = Catalog.Enemy(BugTypes[GD.RandRange(0, BugTypes.Length - 1)]);
		var view = new BugView(def, 0);
		_bugLayer.AddChild(view);
		_bugs.Add((view, progress, def.Speed));
	}
}
