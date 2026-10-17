using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.Editor;

public enum MapTool
{
	Route,
	Place,
	Select,
	Erase,
}

/// <summary>The board in the editor: draws the level like the game does and handles the map tools.</summary>
public partial class MapCanvas : Node2D
{
	public MapTool Tool = MapTool.Place;
	/// <summary>Component the Place tool puts down (type, size, colour, label).</summary>
	public ComponentData Brush = new() { Type = "chip", Size = new[] { 2, 2 } };
	public int Selected = -1;
	public event Action<int> SelectionChanged;

	BoardView _board;
	PathTraceView _trace;
	CpuView _cpu;
	Node2D _ghost;
	Node2D _overlay;
	Vector2I _hover = new(-99, -99);
	bool _inside;
	bool _dragging;
	Vector2I _dragOffset;
	Vector2I _dragCell;

	static LevelData Level => EditorState.Level;

	public override void _Ready()
	{
		Position = Grid.Origin;
		_board = new BoardView { ShowGrid = true };
		AddChild(_board);
		_trace = new PathTraceView();
		AddChild(_trace);
		_cpu = new CpuView();
		AddChild(_cpu);
		_ghost = new Node2D { Modulate = new Color(1, 1, 1, 0.6f), ZIndex = 5 };
		_ghost.Draw += DrawGhost;
		AddChild(_ghost);
		_overlay = new Node2D { Material = FxUtil.Unshaded, ZIndex = 10 };
		_overlay.Draw += DrawOverlay;
		AddChild(_overlay);
		EditorState.Changed += Rebuild;
		Rebuild();
	}

	public override void _ExitTree() => EditorState.Changed -= Rebuild;

	public bool ShowGrid
	{
		get => _board.ShowGrid;
		set => _board.ShowGrid = value;
	}

	public void Rebuild()
	{
		if (Selected >= Level.Components.Count)
			Select(-1);
		_board.Setup(Level, Grid.Cols, Grid.Rows, Level.Title.GetHashCode() & 0xffff);
		if (Level.Path.Count >= 2)
			_trace.Setup(Level.PathPoints(), Level.EntryPoint());
		else
			_trace.Setup(Array.Empty<Vector2>(), Vector2.Zero);
		_cpu.Visible = Level.Path.Count >= 1 && Grid.InBounds(Level.CpuCell);
		if (_cpu.Visible)
			_cpu.Position = Grid.CellCenter(Level.CpuCell);
		_cpu.Label = Level.CoreLabel ?? "CPU";
		Redraw();
	}

	public void Redraw()
	{
		_ghost.QueueRedraw();
		_overlay.QueueRedraw();
	}

	public void Select(int index)
	{
		Selected = index;
		SelectionChanged?.Invoke(index);
		Redraw();
	}

	public void RotateBrush()
	{
		Brush.Size = new[] { Brush.Size[1], Brush.Size[0] };
		Redraw();
	}

	// ------------------------------------------------------------ input

	public override void _UnhandledInput(InputEvent e)
	{
		if (!IsVisibleInTree())
			return;
		if (e is InputEventMouseMotion)
		{
			UpdateHover();
			if (_dragging)
				Redraw();
		}
		else if (e is InputEventMouseButton mb)
		{
			UpdateHover();
			if (!_inside && !_dragging)
				return;
			if (mb.Pressed && mb.ButtonIndex == MouseButton.Left)
				Primary();
			else if (!mb.Pressed && mb.ButtonIndex == MouseButton.Left && _dragging)
				Drop();
			else if (mb.Pressed && mb.ButtonIndex == MouseButton.Right)
				Secondary();
			GetViewport().SetInputAsHandled();
		}
		else if (e is InputEventKey { Pressed: true, Echo: false } key)
		{
			if (key.Keycode == Key.R)
			{
				if (Tool == MapTool.Select && Selected >= 0)
					EditorState.Modify(l => l.Components[Selected].Size = new[] { l.Components[Selected].Size[1], l.Components[Selected].Size[0] });
				else
					RotateBrush();
			}
			else if (key.Keycode is Key.Delete or Key.Backspace && Selected >= 0)
				DeleteComponent(Selected);
		}
	}

	void UpdateHover()
	{
		var local = GetLocalMousePosition();
		_inside = new Rect2(Vector2.Zero, Grid.Size).HasPoint(local);
		var cell = Grid.ToCell(local);
		if (cell == _hover)
			return;
		_hover = cell;
		Redraw();
	}

	void Primary()
	{
		switch (Tool)
		{
			case MapTool.Route:
				AddRoutePoint(_hover);
				break;
			case MapTool.Place:
				var c = Brush.Clone();
				c.Cell = new[] { _hover.X, _hover.Y };
				if (CanPlace(c, -1))
					EditorState.Modify(l => l.Components.Add(c));
				else
					Autoload.Audio.Play("error", "UI");
				break;
			case MapTool.Select:
				var index = ComponentAt(_hover);
				Select(index);
				if (index >= 0)
				{
					_dragging = true;
					_dragOffset = _hover - Level.Components[index].CellV;
					_dragCell = Level.Components[index].CellV;
				}
				break;
			case MapTool.Erase:
				var hit = ComponentAt(_hover);
				if (hit >= 0)
					DeleteComponent(hit);
				break;
		}
	}

	void Secondary()
	{
		switch (Tool)
		{
			case MapTool.Route:
				if (Level.Path.Count > 0)
					EditorState.Modify(l => l.Path.RemoveAt(l.Path.Count - 1));
				break;
			case MapTool.Select:
				Select(-1);
				break;
			default:
				var hit = ComponentAt(_hover);
				if (hit >= 0)
					DeleteComponent(hit);
				break;
		}
	}

	void Drop()
	{
		_dragging = false;
		if (Selected < 0)
			return;
		var target = _hover - _dragOffset;
		var moved = Level.Components[Selected].Clone();
		moved.Cell = new[] { target.X, target.Y };
		if (target != _dragCell && CanPlace(moved, Selected))
		{
			var index = Selected;
			EditorState.Modify(l => l.Components[index].Cell = moved.Cell);
		}
		Redraw();
	}

	void DeleteComponent(int index)
	{
		EditorState.Modify(l => l.Components.RemoveAt(index));
		Select(-1);
	}

	/// <summary>Route clicks: first click on an edge starts outside the board; then straight segments
	/// (a corner is added automatically); clicking an earlier point cuts the route there.</summary>
	void AddRoutePoint(Vector2I cell)
	{
		if (!Grid.InBounds(cell))
			return;
		var points = Level.Waypoints;
		var existing = points.IndexOf(cell);
		if (existing >= 0 && existing < points.Count - 1)
		{
			EditorState.Modify(l => l.Path.RemoveRange(existing + 1, l.Path.Count - existing - 1));
			return;
		}
		if (points.Count == 0)
		{
			var start = cell;
			if (cell.X == 0)
				start = new Vector2I(-1, cell.Y);
			else if (cell.Y == 0)
				start = new Vector2I(cell.X, -1);
			else if (cell.X == Grid.Cols - 1)
				start = new Vector2I(Grid.Cols, cell.Y);
			else if (cell.Y == Grid.Rows - 1)
				start = new Vector2I(cell.X, Grid.Rows);
			EditorState.Modify(l =>
			{
				l.Path.Add(new[] { start.X, start.Y });
				if (start != cell)
					l.Path.Add(new[] { cell.X, cell.Y });
			});
			return;
		}
		var last = points[^1];
		if (cell == last)
			return;
		EditorState.Modify(l =>
		{
			if (cell.X != last.X && cell.Y != last.Y)
				l.Path.Add(new[] { cell.X, last.Y });
			l.Path.Add(new[] { cell.X, cell.Y });
		});
	}

	int ComponentAt(Vector2I cell)
	{
		for (var i = Level.Components.Count - 1; i >= 0; i--)
			if (Level.Components[i].Cells().Contains(cell))
				return i;
		return -1;
	}

	/// <summary>Inside the board and not on the route, the CPU or another blocking component.</summary>
	public static bool CanPlace(ComponentData c, int ignore)
	{
		var cells = c.Cells().ToList();
		if (cells.Any(cell => !Grid.InBounds(cell)))
			return false;
		if (!c.Kind.Blocks)
			return true;
		var blocked = new HashSet<Vector2I>(Level.PathCells());
		if (Level.Path.Count > 0)
			foreach (var cell in Level.CpuCells())
				blocked.Add(cell);
		for (var i = 0; i < Level.Components.Count; i++)
			if (i != ignore && Level.Components[i].Kind.Blocks)
				foreach (var cell in Level.Components[i].Cells())
					blocked.Add(cell);
		return cells.All(cell => !blocked.Contains(cell));
	}

	// ------------------------------------------------------------ drawing

	void DrawGhost()
	{
		ComponentData c = null;
		if (Tool == MapTool.Place && _inside)
		{
			c = Brush.Clone();
			c.Cell = new[] { _hover.X, _hover.Y };
		}
		else if (Tool == MapTool.Select && _dragging && Selected >= 0)
		{
			c = Level.Components[Selected].Clone();
			var target = _hover - _dragOffset;
			c.Cell = new[] { target.X, target.Y };
		}
		if (c == null)
			return;
		var origin = new Vector2(c.Cell[0], c.Cell[1]) * Grid.Cell;
		_ghost.DrawSetTransform(origin, 0, Vector2.One);
		ComponentArt.Draw(_ghost, c, new Vector2(c.Size[0], c.Size[1]) * Grid.Cell, BoardTheme.Get(Level.Theme), 1);
		_ghost.DrawSetTransformMatrix(Transform2D.Identity);
	}

	void DrawOverlay()
	{
		var font = HardwareDefence.UI.UiTheme.PixelBold;
		// Components that break the rules (on the route, overlapping...) are outlined in red.
		var route = new HashSet<Vector2I>(Level.PathCells());
		for (var i = 0; i < Level.Components.Count; i++)
		{
			var c = Level.Components[i];
			var rect = Grid.CellRect(c.CellV, c.SizeV);
			if (c.Kind.Blocks && c.Cells().Any(route.Contains))
				_overlay.DrawRect(rect.Grow(-1), new Color(1, 0.2f, 0.25f, 0.9f), false, 3);
			if (i == Selected)
			{
				_overlay.DrawRect(rect.Grow(2), new Color(HardwareDefence.UI.UiTheme.Cyan, 0.25f));
				_overlay.DrawRect(rect.Grow(2), HardwareDefence.UI.UiTheme.Cyan, false, 2);
			}
		}
		// Route points.
		var points = Level.Waypoints;
		for (var i = 0; i < points.Count; i++)
		{
			var p = Grid.CellCenter(points[i]);
			var last = i == points.Count - 1;
			_overlay.DrawCircle(p, 11, new Color(0, 0, 0, 0.7f));
			_overlay.DrawArc(p, 11, 0, Mathf.Tau, 24, last ? HardwareDefence.UI.UiTheme.Cyan : i == 0 ? HardwareDefence.UI.UiTheme.Red : HardwareDefence.UI.UiTheme.Gold, 2, true);
			var text = (i + 1).ToString();
			var w = font.GetStringSize(text, HorizontalAlignment.Left, -1, 11).X;
			_overlay.DrawString(font, p + new Vector2(-w / 2, 4), text, HorizontalAlignment.Left, -1, 11, Colors.White);
		}
		if (!_inside)
			return;
		var hover = Grid.CellRect(_hover, Vector2I.One);
		switch (Tool)
		{
			case MapTool.Route:
				_overlay.DrawRect(hover.Grow(-2), new Color(HardwareDefence.UI.UiTheme.Gold, 0.9f), false, 2);
				if (points.Count > 0 && _hover != points[^1])
				{
					var from = Grid.CellCenter(points[^1]);
					var to = Grid.CellCenter(_hover);
					var corner = new Vector2(to.X, from.Y);
					var dash = new Color(HardwareDefence.UI.UiTheme.Gold, 0.7f);
					_overlay.DrawDashedLine(from, corner, dash, 3, 8);
					_overlay.DrawDashedLine(corner, to, dash, 3, 8);
				}
				break;
			case MapTool.Place:
				var c = Brush.Clone();
				c.Cell = new[] { _hover.X, _hover.Y };
				var ok = CanPlace(c, -1);
				var r = Grid.CellRect(_hover, new Vector2I(c.Size[0], c.Size[1]));
				_overlay.DrawRect(r.Grow(-1), ok ? new Color(0.3f, 1, 0.85f, 0.9f) : new Color(1, 0.3f, 0.35f, 0.9f), false, 2);
				break;
			case MapTool.Select:
			case MapTool.Erase:
				var hit = ComponentAt(_hover);
				var hr = hit >= 0 ? Grid.CellRect(Level.Components[hit].CellV, Level.Components[hit].SizeV) : hover;
				_overlay.DrawRect(hr.Grow(1), Tool == MapTool.Erase ? new Color(1, 0.3f, 0.35f, 0.9f) : new Color(1, 1, 1, 0.6f), false, 2);
				break;
		}
	}
}
