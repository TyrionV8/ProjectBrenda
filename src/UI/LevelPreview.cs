using HardwareDefence.Data;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Miniature of a level: board, route, components and the CPU.</summary>
public partial class LevelPreview : Control
{
	readonly LevelData _level;
	readonly bool _locked;

	public LevelPreview(LevelData level, bool locked, Vector2 size)
	{
		_level = level;
		_locked = locked;
		CustomMinimumSize = size;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		// Fit both dimensions: shrinking the selection cards must not paint over their captions.
		var k = Mathf.Min(Size.X / Grid.Size.X, Size.Y / Grid.Size.Y);
		DrawSetTransform(((Size - Grid.Size * k) / 2).Floor());
		var theme = BoardTheme.Get(_level.Theme);
		var board = new Rect2(Vector2.Zero, Grid.Size * k);
		DrawRect(board, _locked ? new Color("1a2230") : theme.Center);
		DrawRect(board, new Color(UiTheme.Cyan, 0.3f), false, 1);
		foreach (var c in _level.Components)
		{
			if (!c.Kind.Blocks)
				continue;
			var r = Grid.CellRect(c.CellV, c.SizeV);
			var color = _locked ? new Color(0.1f, 0.12f, 0.15f) : ComponentArt.MainColor(c).Darkened(0.2f);
			DrawRect(new Rect2(r.Position * k, r.Size * k).Grow(-1), color);
		}
		var points = _level.PathPoints();
		for (var i = 0; i < points.Length; i++)
			points[i] *= k;
		if (points.Length > 1)
			DrawPolyline(points, _locked ? new Color("4a4f58") : new Color("e3a345"), 45 * k + 1, true);
		if (_level.Path.Count == 0)
			return;
		DrawCircle(Grid.CellCenter(_level.CpuCell) * k, 7, _locked ? UiTheme.Dim : UiTheme.Cyan);
		DrawCircle(_level.EntryPoint() * k, 5, _locked ? UiTheme.Dim : UiTheme.Red);
	}
}
