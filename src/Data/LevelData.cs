using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Godot;

namespace HardwareDefence.Data;

public sealed class ComponentData
{
	public string Type { get; set; } = "chip";
	public int[] Cell { get; set; } = { 0, 0 };
	public int[] Size { get; set; } = { 1, 1 };
	/// <summary>Optional colour (LEDs, chip markings) as a hex string.</summary>
	public string Color { get; set; }
	/// <summary>Optional silkscreen/marking text.</summary>
	public string Label { get; set; }

	[JsonIgnore] public Vector2I CellV => new(Cell[0], Cell[1]);
	[JsonIgnore] public Vector2I SizeV => new(Size[0], Size[1]);
	[JsonIgnore] public ComponentType Kind => ComponentTypes.Get(Type);

	public IEnumerable<Vector2I> Cells()
	{
		for (var x = 0; x < Size[0]; x++)
			for (var y = 0; y < Size[1]; y++)
				yield return new Vector2I(Cell[0] + x, Cell[1] + y);
	}

	public ComponentData Clone() => new() { Type = Type, Cell = (int[])Cell.Clone(), Size = (int[])Size.Clone(), Color = Color, Label = Label };
}

public sealed class SpawnData
{
	public string Enemy { get; set; } = "bug";
	public float Time { get; set; }
}

/// <summary>
/// A level as stored in data/levels/*.json: the bug route (waypoint cells), the board
/// components, starting money and the waves.
/// </summary>
public sealed class LevelData
{
	/// <summary>Cells around the CPU (in each direction) that can't be built on.</summary>
	public const int CpuFootprint = 1;

	public string Title { get; set; } = "New level";
	public string Subtitle { get; set; } = "";
	/// <summary>Solder mask colour: blue, green, red, black, purple or white.</summary>
	public string Theme { get; set; } = "blue";
	public string CoreLabel { get; set; } = "CPU";
	public int StartMoney { get; set; } = 30;
	public string Music { get; set; } = "music_level_1";
	public List<int[]> Path { get; set; } = new();
	public List<ComponentData> Components { get; set; } = new();
	public List<List<SpawnData>> Waves { get; set; } = new();

	[JsonIgnore] public List<Vector2I> Waypoints => Path.Select(p => new Vector2I(p[0], p[1])).ToList();
	[JsonIgnore] public Vector2I CpuCell => Waypoints[^1];

	/// <summary>Route in board pixels, through the centre of each waypoint cell.</summary>
	public Vector2[] PathPoints() => Waypoints.Select(Grid.CellCenter).ToArray();

	/// <summary>Where bugs enter: the first waypoint clamped to the board edge.</summary>
	public Vector2 EntryPoint()
	{
		var p = Grid.CellCenter(Waypoints[0]);
		return new Vector2(Mathf.Clamp(p.X, 0, Grid.Size.X), Mathf.Clamp(p.Y, 0, Grid.Size.Y));
	}

	/// <summary>Every board cell the route passes through.</summary>
	public List<Vector2I> PathCells()
	{
		var cells = new List<Vector2I>();
		var points = Waypoints;
		for (var i = 0; i < points.Count - 1; i++)
		{
			var a = points[i];
			var b = points[i + 1];
			var steps = Mathf.Max(Mathf.Abs(b.X - a.X), Mathf.Abs(b.Y - a.Y));
			for (var s = 0; s <= steps; s++)
			{
				var t = (float)s / Mathf.Max(steps, 1);
				var cell = new Vector2I(Mathf.RoundToInt(Mathf.Lerp(a.X, b.X, t)), Mathf.RoundToInt(Mathf.Lerp(a.Y, b.Y, t)));
				if (Grid.InBounds(cell) && !cells.Contains(cell))
					cells.Add(cell);
			}
		}
		return cells;
	}

	public IEnumerable<Vector2I> CpuCells()
	{
		for (var x = -CpuFootprint; x <= CpuFootprint; x++)
			for (var y = -CpuFootprint; y <= CpuFootprint; y++)
				yield return CpuCell + new Vector2I(x, y);
	}

	/// <summary>Cells where turrets can't go: route, blocking components and the CPU footprint.</summary>
	public HashSet<Vector2I> BlockedCells()
	{
		var blocked = new HashSet<Vector2I>(PathCells());
		foreach (var c in Components.Where(c => c.Kind.Blocks))
			foreach (var cell in c.Cells())
				blocked.Add(cell);
		foreach (var cell in CpuCells())
			blocked.Add(cell);
		return blocked;
	}

	public int EnemyCount(int wave) => wave < Waves.Count ? Waves[wave].Count : 0;

	/// <summary>Problems that make the level unplayable or look broken (empty when fine).</summary>
	public List<string> Validate(ICollection<string> enemyIds = null)
	{
		var problems = new List<string>();
		var points = Waypoints;
		if (points.Count < 2)
		{
			problems.Add("The route needs at least two points.");
			return problems;
		}
		for (var i = 0; i < points.Count - 1; i++)
		{
			var d = points[i + 1] - points[i];
			if (d.X != 0 && d.Y != 0)
				problems.Add($"Route segment {i + 1} is not horizontal or vertical.");
			if (d == Vector2I.Zero)
				problems.Add($"Route point {i + 2} repeats the previous one.");
		}
		if (!Grid.InBounds(CpuCell))
			problems.Add("The CPU (last route point) must be inside the board.");
		var start = points[0];
		if (!Grid.InBounds(start) && !Grid.InBounds(start + Vector2I.Right) && !Grid.InBounds(start + Vector2I.Left)
			&& !Grid.InBounds(start + Vector2I.Up) && !Grid.InBounds(start + Vector2I.Down))
			problems.Add("The route must start inside the board or just outside an edge.");
		var route = new HashSet<Vector2I>(PathCells());
		var cpu = new HashSet<Vector2I>(CpuCells());
		var used = new Dictionary<Vector2I, int>();
		for (var i = 0; i < Components.Count; i++)
		{
			var c = Components[i];
			if (!c.Kind.Blocks)
				continue;
			foreach (var cell in c.Cells())
			{
				if (!Grid.InBounds(cell))
					problems.Add($"{c.Kind.Name} #{i + 1} is outside the board.");
				else if (route.Contains(cell))
					problems.Add($"{c.Kind.Name} #{i + 1} is on the route.");
				else if (cpu.Contains(cell))
					problems.Add($"{c.Kind.Name} #{i + 1} overlaps the CPU.");
				else if (used.TryGetValue(cell, out var other))
					problems.Add($"{c.Kind.Name} #{i + 1} overlaps component #{other + 1}.");
				used[cell] = i;
			}
		}
		if (Waves.Count == 0 || Waves.All(w => w.Count == 0))
			problems.Add("There are no bugs in the waves.");
		if (enemyIds != null)
			foreach (var s in Waves.SelectMany(w => w))
				if (!enemyIds.Contains(s.Enemy))
				{
					problems.Add($"Unknown bug type '{s.Enemy}'.");
					break;
				}
		return problems.Distinct().ToList();
	}

	public void SortWaves()
	{
		foreach (var wave in Waves)
			wave.Sort((a, b) => a.Time.CompareTo(b.Time));
	}

	public LevelData Clone() => JsonIO.Read<LevelData>(JsonIO.Write(this));
}
