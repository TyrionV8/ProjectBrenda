using System.Collections.Generic;
using Brenda.Data;
using Godot;

namespace Brenda.View;

/// <summary>
/// The printed circuit board: solder mask, decorative traces and silkscreen (drawn once
/// and cached), plus a <see cref="ComponentView"/> for every component.
/// </summary>
public partial class BoardView : Node2D
{
	public LevelData Level;
	public BoardTheme Theme;
	public int Cols = Grid.Cols;
	public int Rows = Grid.Rows;
	public readonly List<ComponentView> Components = new();

	bool _showGrid;
	public bool ShowGrid
	{
		get => _showGrid;
		set
		{
			_showGrid = value;
			QueueRedraw();
		}
	}

	GradientTexture2D _gradient;
	readonly List<Vector2[]> _decor = new();
	readonly List<Vector2> _vias = new();
	Node2D _componentLayer;
	DecorPulses _pulses;

	public Vector2 BoardSize => new Vector2(Cols, Rows) * Grid.Cell;

	public void Setup(LevelData level, int cols = Grid.Cols, int rows = Grid.Rows, int seed = 1)
	{
		Level = level;
		Cols = cols;
		Rows = rows;
		Theme = BoardTheme.Get(level?.Theme ?? "blue");
		_gradient = new GradientTexture2D
		{
			Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.45f), FillTo = new Vector2(1.1f, 1f),
			Gradient = new Gradient { Colors = new[] { Theme.Center, Theme.Edge } },
		};
		BuildDecor(seed);
		_pulses?.QueueFree();
		_pulses = new DecorPulses { Lines = _decor };
		AddChild(_pulses);
		MoveChild(_pulses, 0);
		RebuildComponents();
		QueueRedraw();
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
	}

	public void RefreshBackground()
	{
		Theme = BoardTheme.Get(Level?.Theme ?? "blue");
		_gradient.Gradient = new Gradient { Colors = new[] { Theme.Center, Theme.Edge } };
		QueueRedraw();
	}

	void BuildDecor(int seed)
	{
		var rng = new RandomNumberGenerator { Seed = (ulong)seed };
		_decor.Clear();
		_vias.Clear();
		var step = Grid.Cell * 0.5f;
		var size = BoardSize;
		var min = Vector2.One * step;
		var max = size - Vector2.One * step;
		var count = (int)(34 * (Cols * Rows) / (float)(Grid.Cols * Grid.Rows));
		var dirs = new[] { Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down };
		for (var i = 0; i < count; i++)
		{
			var p = new Vector2(rng.RandiRange(1, (int)(size.X / step) - 1), rng.RandiRange(1, (int)(size.Y / step) - 1)) * step;
			var line = new List<Vector2> { p };
			var dir = dirs[rng.RandiRange(0, 3)];
			var segments = rng.RandiRange(2, 5);
			for (var s = 0; s < segments; s++)
			{
				p = (p + dir * rng.RandiRange(2, 7) * step).Clamp(min, max);
				line.Add(p);
				// A short 45 degree run, then continue orthogonally (classic PCB routing).
				var diag = (dir + dir.Orthogonal() * (rng.Randf() < 0.5f ? 1 : -1)).Normalized();
				p = (p + diag * step * rng.RandiRange(1, 2) * Mathf.Sqrt2).Clamp(min, max);
				line.Add(p);
				if (rng.Randf() < 0.4f)
					dir = dir.Orthogonal() * (rng.Randf() < 0.5f ? 1 : -1);
			}
			_decor.Add(line.ToArray());
			_vias.Add(line[0]);
			_vias.Add(line[^1]);
		}
	}

	public override void _Draw()
	{
		var size = BoardSize;
		DrawRect(new Rect2(new Vector2(6, 8), size), new Color(0, 0, 0, 0.45f));
		DrawTextureRect(_gradient, new Rect2(Vector2.Zero, size), false);
		// Fibre-glass weave: very faint diagonal hatching.
		for (var x = -(int)size.Y; x < size.X; x += 12)
			DrawLine(new Vector2(x, 0), new Vector2(x + size.Y, size.Y), new Color(1, 1, 1, 0.012f), 1);
		foreach (var line in _decor)
		{
			DrawPolyline(line, new Color(0, 0, 0, 0.25f), 7, true);
			DrawPolyline(line, Theme.Decor, 4, true);
			DrawPolyline(line, Theme.Decor.Lightened(0.15f), 1.5f, true);
		}
		foreach (var via in _vias)
		{
			DrawCircle(via, 6, Theme.Decor.Lightened(0.1f));
			DrawCircle(via, 4.5f, new Color("b8893a"));
			DrawCircle(via, 2.2f, new Color("05101c"));
		}
		foreach (var corner in new[] { new Vector2(30, 30), new Vector2(size.X - 30, 30), new Vector2(30, size.Y - 30), size - new Vector2(30, 30) })
		{
			DrawCircle(corner, 15, new Color("7a5212"));
			DrawCircle(corner, 12, new Color("d9a441"));
			DrawCircle(corner, 8, new Color("030608"));
		}
		DrawRect(new Rect2(new Vector2(10, 10), size - new Vector2(20, 20)), new Color(Theme.Silk, Theme.Silk.A * 0.35f), false, 1.5f);
		var title = Level?.Title?.ToUpperInvariant() ?? "";
		DrawString(ComponentArt.SilkFont, new Vector2(60, size.Y - 16), $"PROJECT BRENDA  {title}  REV 0.5", HorizontalAlignment.Left, -1, 12, new Color(Theme.Silk, Theme.Silk.A * 0.8f));
		if (!_showGrid)
			return;
		for (var x = 1; x < Cols; x++)
			DrawLine(new Vector2(x * Grid.Cell, 0), new Vector2(x * Grid.Cell, size.Y), new Color(0, 0, 0, 0.45f), 1);
		for (var y = 1; y < Rows; y++)
			DrawLine(new Vector2(0, y * Grid.Cell), new Vector2(size.X, y * Grid.Cell), new Color(0, 0, 0, 0.45f), 1);
	}
}
