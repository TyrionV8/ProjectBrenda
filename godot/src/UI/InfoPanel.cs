using System.Collections.Generic;
using System.Linq;
using Brenda.Data;
using Brenda.Sim;
using Brenda.View;
using Godot;

namespace Brenda.UI;

/// <summary>The left HUD column: level/wave and incoming bugs (campaign) or timer and energy (versus), board and CPU health.</summary>
public partial class InfoPanel : PanelContainer
{
	readonly bool _versus;
	readonly int _levelNumber;
	Label _top;
	Label _middle;
	Label _status;
	VBoxContainer _queue;
	string _queueKey = "";
	Control _energyBar;
	Label _integrity;
	Control _cpuBar;
	Snapshot _snap;

	public InfoPanel(bool versus, int levelNumber)
	{
		_versus = versus;
		_levelNumber = levelNumber;
		ThemeTypeVariation = "SidePanel";
		Position = Vector2.Zero;
		Size = new Vector2(Grid.Origin.X, 768);
	}

	public override void _Ready()
	{
		var box = UI.VBox(4);
		AddChild(box);
		box.AddChild(UI.Label(_versus ? "TIME LEFT" : "LEVEL", "SmallHeader", HorizontalAlignment.Center));
		_top = UI.Label(_versus ? "5:00" : (_levelNumber > 0 ? _levelNumber.ToString() : "-"), "ValueLabel", HorizontalAlignment.Center);
		if (_versus)
			_top.AddThemeFontSizeOverride("font_size", 28);
		box.AddChild(_top);
		box.AddChild(UI.Spacer(6));
		box.AddChild(UI.Label(_versus ? "ATTACKER" : "WAVE", "SmallHeader", HorizontalAlignment.Center));
		if (_versus)
		{
			_energyBar = new Control { CustomMinimumSize = new Vector2(0, 16) };
			_energyBar.Draw += DrawEnergy;
			box.AddChild(_energyBar);
		}
		_middle = UI.Label("1/1", "ValueLabel", HorizontalAlignment.Center);
		_middle.AddThemeFontSizeOverride("font_size", _versus ? 18 : 28);
		if (_versus)
			_middle.AddThemeColorOverride("font_color", UiTheme.Magenta);
		box.AddChild(_middle);
		box.AddChild(UI.Spacer(6));
		_status = UI.Label("", "SmallHeader", HorizontalAlignment.Center);
		_status.AddThemeColorOverride("font_color", Colors.White);
		_status.CustomMinimumSize = new Vector2(0, 44);
		box.AddChild(_status);
		box.AddChild(UI.Divider());
		box.AddChild(UI.Label(_versus ? "ON THE BOARD" : "INCOMING", "SmallHeader", HorizontalAlignment.Center));
		_queue = UI.VBox(2);
		box.AddChild(_queue);
		box.AddChild(UI.Spacer(0, true));
		box.AddChild(UI.Divider());
		box.AddChild(UI.Label("BOARD", "SmallHeader", HorizontalAlignment.Center));
		_integrity = UI.Label("100%", "SmallHeader", HorizontalAlignment.Center);
		_integrity.AddThemeColorOverride("font_color", Colors.White);
		box.AddChild(_integrity);
		box.AddChild(UI.Label("CPU", "SmallHeader", HorizontalAlignment.Center));
		_cpuBar = new Control { CustomMinimumSize = new Vector2(0, 18) };
		_cpuBar.Draw += DrawCpu;
		box.AddChild(_cpuBar);
	}

	public void Apply(Snapshot s)
	{
		_snap = s;
		if (_versus)
		{
			var t = Mathf.CeilToInt(s.TimeLeft);
			_top.Text = $"{t / 60}:{t % 60:00}";
			_top.AddThemeColorOverride("font_color", t <= 30 ? UiTheme.Gold : UiTheme.Cyan);
			_middle.Text = $"{Mathf.FloorToInt(s.Energy)} (+{s.EnergyRate:0.0}/s)";
			_status.Text = $"BUGS\n{s.Bugs.Count}";
			_energyBar.QueueRedraw();
		}
		else
		{
			_middle.Text = $"{Mathf.Min(s.Wave + 1, s.WaveCount)}/{s.WaveCount}";
			switch (s.WaveState)
			{
				case "countdown":
					_status.Text = $"NEXT WAVE\n{Mathf.Max(s.Countdown, 0):0.0}";
					_status.Modulate = UiTheme.Cyan;
					break;
				case "spawning":
				case "waiting":
					_status.Text = $"BUGS LEFT\n{s.Bugs.Count + s.Upcoming.Count}";
					_status.Modulate = UiTheme.Red.Lerp(Colors.White, 0.3f);
					break;
				default:
					_status.Text = "";
					break;
			}
		}
		_integrity.Text = $"{Mathf.RoundToInt(s.Integrity * 100)}%";
		_integrity.Modulate = UiTheme.Red.Lerp(UiTheme.Green, Mathf.Clamp((s.Integrity - 0.5f) * 2, 0, 1));
		var list = _versus ? s.Bugs.Select(b => b.Type).ToList() : s.Upcoming;
		var key = $"{s.Wave}:{s.WaveState}:{string.Join(",", _versus ? list.GroupBy(x => x).Select(g => g.Key + g.Count()) : list)}";
		if (key != _queueKey)
		{
			_queueKey = key;
			RefreshQueue(list, _versus);
		}
		_cpuBar.QueueRedraw();
	}

	void RefreshQueue(List<string> upcoming, bool group)
	{
		foreach (var child in _queue.GetChildren())
			child.QueueFree();
		var groups = new List<(string Id, int Count)>();
		if (group)
			groups = upcoming.GroupBy(x => x).Select(g => (g.Key, g.Count())).OrderByDescending(g => g.Item2).ToList();
		else
			foreach (var id in upcoming)
			{
				if (groups.Count > 0 && groups[^1].Id == id)
					groups[^1] = (id, groups[^1].Count + 1);
				else
					groups.Add((id, 1));
			}
		for (var i = 0; i < Mathf.Min(groups.Count, 7); i++)
		{
			var def = Catalog.Enemy(groups[i].Id);
			if (def == null)
				continue;
			var row = UI.HBox(6);
			row.AddChild(new BugIcon(def, 30));
			var text = UI.Label($"x{groups[i].Count}", "SmallHeader");
			text.AddThemeColorOverride("font_color", i == 0 ? Colors.White : UiTheme.Dim);
			text.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			row.AddChild(text);
			_queue.AddChild(row);
		}
		if (groups.Count > 7)
			_queue.AddChild(UI.Label($"+{groups.Count - 7} more", "SmallHeader", HorizontalAlignment.Center));
		if (groups.Count == 0)
			_queue.AddChild(UI.Label("-", "SmallHeader", HorizontalAlignment.Center));
	}

	void DrawEnergy()
	{
		if (_snap == null)
			return;
		var w = _energyBar.Size.X;
		_energyBar.DrawRect(new Rect2(0, 3, w, 10), new Color(1, 1, 1, 0.08f));
		_energyBar.DrawRect(new Rect2(0, 3, w * Mathf.Clamp(_snap.Energy / 400f, 0, 1), 10), UiTheme.Magenta);
	}

	void DrawCpu()
	{
		var hp = _snap?.CpuHp ?? Match.CpuMaxHp;
		const int n = Match.CpuMaxHp;
		var w = (_cpuBar.Size.X - (n - 1) * 3) / n;
		var color = CpuView.Critical.Lerp(CpuView.Healthy, (float)hp / n);
		for (var i = 0; i < n; i++)
			_cpuBar.DrawRect(new Rect2(i * (w + 3), 2, w, 14), i < hp ? color : new Color(1, 1, 1, 0.08f));
	}
}
