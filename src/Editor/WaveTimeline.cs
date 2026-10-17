using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Editor;

/// <summary>
/// One wave as a timeline with a lane per bug type: click to add a bug at that time, drag to
/// move it (also between lanes), right click to remove. Wheel scrolls, Ctrl+wheel zooms.
/// </summary>
public partial class WaveTimeline : Control
{
	const float LabelWidth = 170;
	const float HeaderHeight = 30;
	const float Snap = 0.25f;

	public int Wave;
	float _pps = 14;
	float _scroll;
	(int Wave, SpawnData Spawn)? _drag;
	Vector2 _mouse;

	static IReadOnlyList<EnemyDef> Types => Catalog.EnemyList;
	float LaneHeight => (Size.Y - HeaderHeight) / Types.Count;
	List<SpawnData> Spawns => Wave < EditorState.Level.Waves.Count ? EditorState.Level.Waves[Wave] : new List<SpawnData>();

	public override void _Ready()
	{
		ClipContents = true;
		MouseFilter = MouseFilterEnum.Stop;
		for (var i = 0; i < Types.Count; i++)
		{
			var icon = new BugIcon(Types[i], 40);
			AddChild(icon);
		}
		Resized += Layout;
		Layout();
	}

	void Layout()
	{
		var icons = GetChildren().OfType<BugIcon>().ToList();
		for (var i = 0; i < icons.Count; i++)
			icons[i].Position = new Vector2(8, HeaderHeight + i * LaneHeight + (LaneHeight - 40) / 2);
		QueueRedraw();
	}

	float TimeAt(float x) => Mathf.Max(0, Mathf.Snapped((x - LabelWidth) / _pps + _scroll, Snap));
	float XAt(float t) => LabelWidth + (t - _scroll) * _pps;
	int LaneAt(float y) => Mathf.Clamp((int)((y - HeaderHeight) / LaneHeight), 0, Types.Count - 1);
	float LaneY(int lane) => HeaderHeight + (lane + 0.5f) * LaneHeight;
	int LaneOf(string enemy) => Types.ToList().FindIndex(t => t.Id == enemy);

	SpawnData SpawnAt(Vector2 p)
	{
		SpawnData best = null;
		var bestD = 12f;
		foreach (var s in Spawns)
		{
			var d = new Vector2(XAt(s.Time), LaneY(LaneOf(s.Enemy))).DistanceTo(p);
			if (d < bestD)
			{
				best = s;
				bestD = d;
			}
		}
		return best;
	}

	public override void _GuiInput(InputEvent e)
	{
		if (e is InputEventMouseMotion motion)
		{
			_mouse = motion.Position;
			if (_drag is { } drag && drag.Wave == Wave)
			{
				drag.Spawn.Time = TimeAt(_mouse.X);
				drag.Spawn.Enemy = Types[LaneAt(_mouse.Y)].Id;
			}
			QueueRedraw();
			return;
		}
		if (e is not InputEventMouseButton mb)
			return;
		if (mb.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown && mb.Pressed)
		{
			var dir = mb.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
			if (mb.CtrlPressed)
				_pps = Mathf.Clamp(_pps * (dir > 0 ? 1.15f : 1 / 1.15f), 4, 80);
			else
				_scroll = Mathf.Max(0, _scroll - dir * 40 / _pps);
			QueueRedraw();
			AcceptEvent();
			return;
		}
		if (mb.Position.X < LabelWidth || mb.Position.Y < HeaderHeight)
			return;
		if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
		{
			if (Wave >= EditorState.Level.Waves.Count)
				return;
			var hit = SpawnAt(mb.Position);
			EditorState.Checkpoint();
			if (hit == null)
			{
				hit = new SpawnData { Enemy = Types[LaneAt(mb.Position.Y)].Id, Time = TimeAt(mb.Position.X) };
				Spawns.Add(hit);
				Autoload.Audio.Ui("click");
			}
			_drag = (Wave, hit);
		}
		else if (mb.ButtonIndex == MouseButton.Left && !mb.Pressed && _drag != null)
		{
			_drag = null;
			Spawns.Sort((a, b) => a.Time.CompareTo(b.Time));
			EditorState.Touch();
		}
		else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
		{
			var hit = SpawnAt(mb.Position);
			if (hit != null)
				EditorState.Modify(_ => Spawns.Remove(hit));
		}
		QueueRedraw();
		AcceptEvent();
	}

	public override void _Draw()
	{
		var font = UiTheme.PixelFont;
		DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.02f, 0.05f, 0.09f, 0.97f));
		// Lanes.
		for (var i = 0; i < Types.Count; i++)
		{
			var y = HeaderHeight + i * LaneHeight;
			DrawRect(new Rect2(0, y, Size.X, LaneHeight), i % 2 == 0 ? new Color(1, 1, 1, 0.025f) : new Color(0, 0, 0, 0));
			DrawLine(new Vector2(0, y), new Vector2(Size.X, y), new Color(UiTheme.Cyan, 0.12f));
			var count = Spawns.Count(s => s.Enemy == Types[i].Id);
			DrawString(font, new Vector2(54, y + LaneHeight / 2 - 2), Types[i].Name.ToUpperInvariant(), HorizontalAlignment.Left, LabelWidth - 58, 11, UiTheme.TextColor);
			DrawString(font, new Vector2(54, y + LaneHeight / 2 + 14), $"x{count}  hp {Types[i].MaxHp}", HorizontalAlignment.Left, LabelWidth - 58, 10, count > 0 ? UiTheme.Gold : UiTheme.Dim);
		}
		DrawLine(new Vector2(LabelWidth, 0), new Vector2(LabelWidth, Size.Y), new Color(UiTheme.Cyan, 0.4f), 2);
		// Time ruler.
		var visible = (Size.X - LabelWidth) / _pps;
		var step = _pps < 8 ? 10 : 5;
		for (var t = Mathf.Ceil(_scroll / step) * step; t <= _scroll + visible; t += step)
		{
			var x = XAt(t);
			DrawLine(new Vector2(x, HeaderHeight - 6), new Vector2(x, Size.Y), new Color(1, 1, 1, t % 30 == 0 ? 0.18f : 0.07f));
			DrawString(font, new Vector2(x + 3, HeaderHeight - 10), $"{t:0}s", HorizontalAlignment.Left, -1, 11, UiTheme.Dim);
		}
		// Spawns.
		foreach (var s in Spawns)
		{
			var lane = LaneOf(s.Enemy);
			if (lane < 0)
				continue;
			var def = Types[lane];
			var p = new Vector2(XAt(s.Time), LaneY(lane));
			if (p.X < LabelWidth - 10)
				continue;
			var r = Mathf.Clamp(def.Radius * 0.55f, 6, 13);
			DrawCircle(p + new Vector2(1, 2), r + 2, new Color(0, 0, 0, 0.5f));
			DrawCircle(p, r + 2, def.Accent);
			DrawCircle(p, r, def.Shell);
			DrawCircle(p + new Vector2(r * 0.45f, -r * 0.3f), 1.8f, def.Eye);
			DrawCircle(p + new Vector2(r * 0.45f, r * 0.3f), 1.8f, def.Eye);
		}
		// Hover guide.
		if (_mouse.X > LabelWidth && _mouse.Y > HeaderHeight)
		{
			var t = TimeAt(_mouse.X);
			var x = XAt(t);
			DrawLine(new Vector2(x, HeaderHeight), new Vector2(x, Size.Y), new Color(UiTheme.Gold, 0.5f), 1);
			var lane = LaneAt(_mouse.Y);
			DrawRect(new Rect2(LabelWidth, HeaderHeight + lane * LaneHeight, Size.X - LabelWidth, LaneHeight), new Color(UiTheme.Gold, 0.05f));
			DrawString(font, new Vector2(x + 4, HeaderHeight + 12), $"{t:0.00}s", HorizontalAlignment.Left, -1, 11, UiTheme.Gold);
		}
	}
}
