using System.Collections.Generic;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// The printed circuit board: solder mask, decorative traces, vias and silkscreen, painted
/// once in art pixels, plus a <see cref="ComponentView"/> for every component.
/// </summary>
public partial class BoardView : Node2D
{
	const int Px = PixelWorld.Scale;
	/// <summary>Art pixels per board cell.</summary>
	public const int CellPx = Grid.Cell / Px;

	public LevelData Level;
	public BoardTheme Theme;
	public int Cols = Grid.Cols;
	public int Rows = Grid.Rows;
	public readonly List<ComponentView> Components = new();
	/// <summary>The LEDs along the route, in the order of LevelData.PathLeds (and of the simulation).</summary>
	public readonly List<PathLedView> Leds = new();

	bool _showGrid;
	public bool ShowGrid
	{
		get => _showGrid;
		set
		{
			_showGrid = value;
			_grid?.QueueRedraw();
		}
	}

	readonly List<Vector2I[]> _decor = new();
	Node2D _background;
	Node2D _grid;
	Node2D _componentLayer;
	Node2D _ledLayer;
	int _seed;

	public Vector2 BoardSize => new Vector2(Cols, Rows) * Grid.Cell;

	public void Setup(LevelData level, int cols = Grid.Cols, int rows = Grid.Rows, int seed = 1)
	{
		Level = level;
		Cols = cols;
		Rows = rows;
		_seed = seed;
		Theme = BoardTheme.Get(level?.Theme ?? "blue");
		BuildDecor(seed);
		if (_grid == null)
		{
			_grid = new Node2D();
			_grid.Draw += DrawGrid;
			AddChild(_grid);
		}
		PaintBackground();
		RebuildComponents();
		QueueRedraw();
	}

	void PaintBackground()
	{
		_background?.QueueFree();
		// In tiles, so that every light on the board can reach it.
		_background = Paint().ToTiles();
		AddChild(_background);
		MoveChild(_background, 0);
	}

	/// <summary>Recreate the component views (after the editor changes the level).</summary>
	public void RebuildComponents()
	{
		_componentLayer?.QueueFree();
		Components.Clear();
		_componentLayer = new Node2D();
		AddChild(_componentLayer);
		if (Level == null)
			return;
		for (var i = 0; i < Level.Components.Count; i++)
		{
			var view = new ComponentView(Level.Components[i], Theme, i);
			_componentLayer.AddChild(view);
			Components.Add(view);
		}
		RebuildLeds();
	}

	/// <summary>Recreate the route LEDs (after the route changes).</summary>
	public void RebuildLeds()
	{
		_ledLayer?.QueueFree();
		Leds.Clear();
		_ledLayer = new Node2D { ZIndex = 1 };
		AddChild(_ledLayer);
		if (Level == null || Level.Path.Count < 2)
			return;
		foreach (var pos in Level.PathLeds(Cols, Rows))
		{
			var led = new PathLedView(pos, Theme.Led);
			_ledLayer.AddChild(led);
			Leds.Add(led);
		}
	}

	public void RefreshBackground()
	{
		Theme = BoardTheme.Get(Level?.Theme ?? "blue");
		PaintBackground();
		RebuildLeds();
	}

	/// <summary>Random PCB routing in art pixels: straight runs with 45 degree jogs, ending on vias.</summary>
	void BuildDecor(int seed)
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)seed };
		_decor.Clear();
		const int step = CellPx / 2;
		var size = new Vector2I(Cols, Rows) * CellPx;
		var min = new Vector2I(step, step);
		var max = size - new Vector2I(step, step);
		var count = (int)(34 * (Cols * Rows) / (float)(Grid.Cols * Grid.Rows));
		var dirs = new[] { Vector2I.Right, Vector2I.Left, Vector2I.Up, Vector2I.Down };
		for (var i = 0; i < count; i++)
		{
			var p = new Vector2I(rng.RandiRange(1, size.X / step - 1), rng.RandiRange(1, size.Y / step - 1)) * step;
			var line = new List<Vector2I> { p };
			var dir = dirs[rng.RandiRange(0, 3)];
			var segments = rng.RandiRange(2, 5);
			for (var s = 0; s < segments; s++)
			{
				p = (p + dir * rng.RandiRange(2, 7) * step).Clamp(min, max);
				line.Add(p);
				var side = new Vector2I(-dir.Y, dir.X) * (rng.Randf() < 0.5f ? 1 : -1);
				p = (p + (dir + side) * step * rng.RandiRange(1, 2) / 2).Clamp(min, max);
				line.Add(p);
				if (rng.Randf() < 0.4f)
					dir = side;
			}
			_decor.Add(line.ToArray());
		}
	}

	PixelCanvas Paint()
	{
		var w = Cols * CellPx;
		var h = Rows * CellPx;
		var c = new PixelCanvas(w, h);
		var rng = new RandomNumberGenerator { Seed = (ulong)(_seed * 31 + 7) };

		// Solder mask: a radial gradient in a few flat bands, dithered where they meet.
		const int bands = 6;
		var centre = new Vector2(w * 0.5f, h * 0.45f);
		var reach = new Vector2(w, h).Length() * 0.62f;
		for (var y = 0; y < h; y++)
			for (var x = 0; x < w; x++)
			{
				var t = Mathf.Clamp(new Vector2(x, y).DistanceTo(centre) / reach, 0, 1) * (bands - 1);
				var band = Mathf.FloorToInt(t);
				if (t - band > PixelCanvas.Bayer(x, y))
					band++;
				var col = Theme.Center.Lerp(Theme.Edge, (float)band / (bands - 1));
				// Glass-fibre weave: a faint woven check.
				if (((x >> 1) + (y >> 1)) % 2 == 0 && (x + y) % 2 == 0)
					col = col.Lightened(0.03f);
				c.Set(x, y, col);
			}
		// Specks in the mask.
		for (var i = 0; i < w * h / 90; i++)
		{
			var x = rng.RandiRange(0, w - 1);
			var y = rng.RandiRange(0, h - 1);
			c.Tint(x, y, rng.Randf() < 0.5f ? new Color(1.08f, 1.08f, 1.08f) : new Color(0.92f, 0.92f, 0.92f));
		}

		// Traces: 2 px copper under the mask, a lit top edge and a shadow below.
		var trace = Theme.Decor;
		foreach (var line in _decor)
			c.Polyline(Offset(line, 1, 1), new Color(0, 0, 0, 0.22f), 2);
		foreach (var line in _decor)
		{
			c.Polyline(line, trace, 2);
			c.Polyline(line, trace.Lightened(0.18f));
		}
		foreach (var line in _decor)
		{
			Via(c, line[0], trace);
			Via(c, line[^1], trace);
		}

		// Mounting holes in the corners.
		foreach (var corner in new[] { new Vector2I(10, 10), new Vector2I(w - 10, 10), new Vector2I(10, h - 10), new Vector2I(w - 10, h - 10) })
		{
			c.Disc(corner.X, corner.Y, 5.5f, new Color("7a5212"));
			c.Disc(corner.X - 0.5f, corner.Y - 0.5f, 4.6f, new Color("d9a441"));
			c.Disc(corner.X, corner.Y, 3, new Color("030608"));
		}

		var silk = new Color(Theme.Silk, Theme.Silk.A * 0.4f);
		c.Frame(3, 3, w - 6, h - 6, silk);
		var title = Level?.Title?.ToUpperInvariant() ?? "";
		c.Text(20, h - 9, $"HARDWARE DEFENCE  {title}  REV 0.7", new Color(Theme.Silk, Theme.Silk.A * 0.85f));
		return c;
	}

	static Vector2I[] Offset(Vector2I[] line, int dx, int dy)
	{
		var copy = new Vector2I[line.Length];
		for (var i = 0; i < line.Length; i++)
			copy[i] = line[i] + new Vector2I(dx, dy);
		return copy;
	}

	/// <summary>A plated via: copper ring, gold barrel, dark hole.</summary>
	static void Via(PixelCanvas c, Vector2I p, Color trace)
	{
		var x = p.X + 1f;
		var y = p.Y + 1f;
		c.Disc(x + 0.5f, y + 0.5f, 2.6f, new Color(0, 0, 0, 0.25f));
		c.Disc(x, y, 2.6f, trace.Lightened(0.1f));
		c.Disc(x, y, 1.9f, new Color("b8893a"));
		c.Set(p.X, p.Y, new Color("ffe7a3"));
		c.Disc(x, y, 0.8f, new Color("05101c"));
	}

	void DrawGrid()
	{
		if (!_showGrid)
			return;
		var size = BoardSize;
		var line = new Color(0, 0, 0, 0.35f);
		for (var x = 1; x < Cols; x++)
			_grid.DrawRect(new Rect2(x * Grid.Cell, 0, Px, size.Y), line);
		for (var y = 1; y < Rows; y++)
			_grid.DrawRect(new Rect2(0, y * Grid.Cell, size.X, Px), line);
	}

	public override void _Draw()
	{
		// Board shadow on the backdrop.
		DrawRect(new Rect2(new Vector2(2, 3) * Px, BoardSize), new Color(0, 0, 0, 0.45f));
	}
}
