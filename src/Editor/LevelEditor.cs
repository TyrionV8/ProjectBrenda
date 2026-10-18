using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Editor;

/// <summary>
/// In-game level editor. MAP mode edits the route and components on the real board; WAVES mode
/// edits the spawns on a timeline. Levels are saved as JSON (see data/levels).
/// </summary>
public partial class LevelEditor : Control
{
	const string Scene = "res://scenes/level_editor.tscn";

	MapCanvas _canvas;
	CanvasLayer _boardLayer;
	WaveTimeline _timeline;
	Control _mapPanel;
	Control _wavePanel;
	VBoxContainer _toolPanel;
	Label _file;
	Label _status;
	Label _toast;
	Button _mapTab;
	Button _waveTab;
	readonly Dictionary<MapTool, Button> _toolButtons = new();
	readonly List<ComponentButton> _palette = new();
	HBoxContainer _waveTabs;
	ScrollContainer _waveTabScroll;
	const float WaveHeaderHeight = 72;
	Control _waveHeader;
	bool _confirmLeave;
	Button _exit;
	Control _dialog;

	// Component properties (brush in Place, selection in Select).
	SpinBox _w;
	SpinBox _h;
	OptionButton _color;
	LineEdit _label;
	Label _labelHint;
	bool _syncing;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
		Audio.PlayMusic("title");

		_boardLayer = new CanvasLayer { Layer = -1 };
		AddChild(_boardLayer);
		_boardLayer.AddChild(new ColorRect { Color = new Color(0.0235f, 0.0431f, 0.0745f), Size = new Vector2(1366, 768), MouseFilter = MouseFilterEnum.Ignore });
		_canvas = new MapCanvas();
		_boardLayer.AddChild(_canvas);
		_canvas.SelectionChanged += _ => RefreshToolPanel();

		_timeline = new WaveTimeline { Position = Grid.Origin + new Vector2(0, WaveHeaderHeight), Size = new Vector2(Grid.Size.X, Grid.Size.Y - WaveHeaderHeight), Visible = false };
		AddChild(_timeline);
		BuildWaveHeader();
		BuildLeft();
		BuildRight();
		_toast = new Label { Position = new Vector2(Grid.Origin.X, 708), Size = new Vector2(Grid.Size.X, 30), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore, ThemeTypeVariation = "SmallHeader" };
		_toast.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
		_toast.AddThemeConstantOverride("outline_size", 6);
		AddChild(_toast);

		EditorState.Changed += OnChanged;
		SetTool(MapTool.Place);
		SetMode(false);
		OnChanged();
	}

	public override void _ExitTree() => EditorState.Changed -= OnChanged;

	void OnChanged()
	{
		if (EditorState.Path == null) Loc.Set(_file, EditorState.Dirty ? "unsaved *" : "unsaved");
		else Loc.Raw(_file, EditorState.DisplayName);
		var problems = EditorState.Level.Validate(Catalog.Enemies.Keys.ToList());
		Loc.Set(_status, problems.Count == 0 ? "LEVEL OK" : $"{problems.Count} PROBLEM{(problems.Count > 1 ? "S" : "")}");
		_status.Modulate = problems.Count == 0 ? UiTheme.Green : UiTheme.Red;
		Loc.Tooltip(_status, problems.Count == 0 ? "Ready to play." : string.Join("\n", problems));
		RefreshWaveTabs();
		RefreshToolPanel();
		_timeline.QueueRedraw();
	}

	// ------------------------------------------------------------ layout

	void BuildLeft()
	{
		var panel = new PanelContainer { ThemeTypeVariation = "SidePanel", Position = Vector2.Zero, Size = new Vector2(Grid.Origin.X, 768), ClipContents = true };
		AddChild(panel);
		panel.AddThemeStyleboxOverride("panel", UiTheme.Box(new Color(0.02f, 0.05f, 0.09f, 0.96f), new Color(UiTheme.Cyan, 0.25f), 0, 0, 8, 12));
		var box = Ui.VBox(4);
		panel.AddChild(box);
		box.AddChild(Ui.Label("EDITOR", "SmallHeader", HorizontalAlignment.Center));
		var modes = Ui.HBox(0);
		_mapTab = Tool("MAP", () => SetMode(false));
		_waveTab = Tool("WAVES", () => SetMode(true));
		modes.AddChild(_mapTab);
		modes.AddChild(_waveTab);
		box.AddChild(modes);
		// Fit both whole words in the native 128px column without changing font size.
		_mapTab.CustomMinimumSize = new Vector2(56, 0);
		_waveTab.CustomMinimumSize = new Vector2(68, 0);
		foreach (var button in new[] { _mapTab, _waveTab })
			foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
			{
				var style = (StyleBox)button.GetThemeStylebox(state).Duplicate();
				style.ContentMarginLeft = 3; style.ContentMarginRight = 3;
				button.AddThemeStyleboxOverride(state, style);
			}
		box.AddChild(Ui.Divider());
		box.AddChild(Ui.Label("TOOLS", "SmallHeader", HorizontalAlignment.Center));
		foreach (var (tool, name, key) in new[] { (MapTool.Route, "Route", "1"), (MapTool.Place, "Place", "2"), (MapTool.Select, "Select", "3"), (MapTool.Erase, "Erase", "4") })
		{
			var b = Tool($"{key} {name}", () => SetTool(tool));
			_toolButtons[tool] = b;
			box.AddChild(b);
		}
		var grid = new CheckBox { Text = "Grid", ButtonPressed = true };
		grid.AddThemeFontSizeOverride("font_size", UiTheme.PixelSmall);
		grid.Toggled += on => _canvas.ShowGrid = on;
		box.AddChild(grid);
		var undo = Ui.VBox(4);
		undo.AddChild(Tool("Undo", EditorState.UndoStep));
		undo.AddChild(Tool("Redo", EditorState.RedoStep));
		box.AddChild(undo);
		box.AddChild(Ui.Spacer(0, true));
		_status = Ui.Label("", "SmallHeader", HorizontalAlignment.Center);
		_status.MouseFilter = MouseFilterEnum.Stop;
		_status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		box.AddChild(_status);
		box.AddChild(Ui.Divider());
		box.AddChild(Tool("Options", OpenSettings));
		box.AddChild(Tool("New", NewLevel));
		box.AddChild(Tool("Open", OpenBrowser));
		box.AddChild(Tool("Save", () => Save(false)));
		box.AddChild(Tool("Save copy", () => Save(true)));
		var test = Ui.Button("TEST", TestPlay, "ToolButton");
		test.AddThemeColorOverride("font_color", UiTheme.Green);
		box.AddChild(test);
		_exit = Ui.Button("Exit", Leave, "ToolButton");
		box.AddChild(_exit);
	}

	void BuildRight()
	{
		var left = Grid.Origin.X + Grid.Size.X;
		var panel = new PanelContainer { ThemeTypeVariation = "SidePanel", Position = new Vector2(left, 0), Size = new Vector2(1366 - left, 768) };
		AddChild(panel);
		panel.AddThemeStyleboxOverride("panel", UiTheme.Box(new Color(0.02f, 0.05f, 0.09f, 0.96f), new Color(UiTheme.Cyan, 0.25f), 0, 0, 8, 12));
		// Text remains at its native pixel size; long help/property panels scroll.
		var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		panel.AddChild(scroll);
		var box = Ui.VBox(6);
		box.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(box);
		_file = Ui.Label("", "SmallHeader", HorizontalAlignment.Center);
		_file.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
		box.AddChild(_file);
		box.AddChild(Ui.Divider());
		_mapPanel = Ui.VBox(6);
		_mapPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
		box.AddChild(_mapPanel);
		_toolPanel = Ui.VBox(6);
		_mapPanel.AddChild(_toolPanel);
		_wavePanel = BuildWavePanel();
		box.AddChild(_wavePanel);
	}

	static Button Tool(string text, Action action)
	{
		var b = Ui.Button(text, action, "ToolButton");
		b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		b.ClipText = true;
		b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		return b;
	}

	public void ShowWaves(bool waves) => SetMode(waves);

	void SetMode(bool waves)
	{
		_boardLayer.Visible = !waves;
		_canvas.SetProcessUnhandledInput(!waves);
		_timeline.Visible = waves;
		_waveHeader.Visible = waves;
		_mapPanel.Visible = !waves;
		_wavePanel.Visible = waves;
		_mapTab.ButtonPressed = !waves;
		_waveTab.ButtonPressed = waves;
		_mapTab.ToggleMode = _waveTab.ToggleMode = true;
		_mapTab.ButtonPressed = !waves;
		_waveTab.ButtonPressed = waves;
		foreach (var b in _toolButtons.Values)
			b.Disabled = waves;
	}

	void SetTool(MapTool tool)
	{
		_canvas.Tool = tool;
		if (tool != MapTool.Select)
			_canvas.Select(-1);
		foreach (var (t, b) in _toolButtons)
		{
			b.ToggleMode = true;
			b.ButtonPressed = t == tool;
		}
		RefreshToolPanel();
		_canvas.Redraw();
	}

	// ------------------------------------------------------------ right panel per tool

	void RefreshToolPanel()
	{
		if (_toolPanel == null)
			return;
		foreach (var child in _toolPanel.GetChildren())
			child.QueueFree();
		_palette.Clear();
		switch (_canvas.Tool)
		{
			case MapTool.Route:
				_toolPanel.AddChild(Ui.Label("ROUTE", "SmallHeader", HorizontalAlignment.Center));
				_toolPanel.AddChild(Help("Click cells to draw the bug route. Start on an edge so bugs come from outside. The last point is the CPU.\n\nCorners are added for you. Click an earlier point to cut the route there. Right click removes the last point."));
				var points = EditorState.Level.Path.Count;
				_toolPanel.AddChild(Help($"{points} points, {EditorState.Level.PathCells().Count} cells"));
				_toolPanel.AddChild(Ui.Button("Clear route", () => EditorState.Modify(l => l.Path.Clear()), "DangerButton"));
				break;
			case MapTool.Place:
				_toolPanel.AddChild(Ui.Label("COMPONENTS", "SmallHeader", HorizontalAlignment.Center));
				var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 360), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
				var grid = new GridContainer { Columns = 2 };
				grid.AddThemeConstantOverride("h_separation", 4);
				grid.AddThemeConstantOverride("v_separation", 4);
				scroll.AddChild(grid);
				foreach (var type in ComponentTypes.All.Keys)
				{
					var b = new ComponentButton(type) { Active = type == _canvas.Brush.Type };
					b.Pressed += () => PickBrush(type);
					grid.AddChild(b);
					_palette.Add(b);
				}
				_toolPanel.AddChild(scroll);
				AddProperties(_canvas.Brush, false);
				_toolPanel.AddChild(Help("Click to place, right click to remove, R rotates."));
				break;
			case MapTool.Select:
				_toolPanel.AddChild(Ui.Label("SELECT", "SmallHeader", HorizontalAlignment.Center));
				if (_canvas.Selected < 0)
					_toolPanel.AddChild(Help("Click a component to edit it, drag to move it. Delete removes it, R rotates."));
				else
				{
					var c = EditorState.Level.Components[_canvas.Selected];
					var title = Ui.Label(c.Kind.Name.ToUpperInvariant(), "SmallHeader", HorizontalAlignment.Center);
					title.AddThemeColorOverride("font_color", UiTheme.Cyan);
					_toolPanel.AddChild(title);
					AddProperties(c, true);
					var index = _canvas.Selected;
					_toolPanel.AddChild(Ui.Button("Delete", () =>
					{
						EditorState.Modify(l => l.Components.RemoveAt(index));
						_canvas.Select(-1);
					}, "DangerButton"));
				}
				break;
			case MapTool.Erase:
				_toolPanel.AddChild(Ui.Label("ERASE", "SmallHeader", HorizontalAlignment.Center));
				_toolPanel.AddChild(Help("Click components to remove them."));
				break;
		}
		var stats = EditorState.Level;
		_toolPanel.AddChild(Ui.Divider());
		var free = 0;
		var blocked = stats.Path.Count > 0 ? stats.BlockedCells() : new HashSet<Vector2I>();
		for (var x = 0; x < Grid.Cols; x++)
			for (var y = 0; y < Grid.Rows; y++)
				if (!blocked.Contains(new Vector2I(x, y)))
					free++;
		_toolPanel.AddChild(Help($"{stats.Components.Count} components\n{free} free cells for turrets"));
	}

	static Label Help(string text)
	{
		var l = Ui.Label(text, "DimLabel");
		l.AddThemeFontSizeOverride("font_size", UiTheme.BodySize);
		l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		l.CustomMinimumSize = new Vector2(170, 0);
		return l;
	}

	void PickBrush(string type)
	{
		var kind = ComponentTypes.Get(type);
		_canvas.Brush = new ComponentData { Type = type, Size = new[] { kind.DefaultSize.X, kind.DefaultSize.Y }, Label = type == "label" ? "TEXT" : null };
		RefreshToolPanel();
		_canvas.Redraw();
	}

	/// <summary>Size, colour and label fields bound to a component (the brush or the selection).</summary>
	void AddProperties(ComponentData c, bool undoable)
	{
		void Apply(Action<ComponentData> change)
		{
			if (_syncing)
				return;
			if (undoable)
			{
				var index = _canvas.Selected;
				var copy = EditorState.Level.Components[index].Clone();
				change(copy);
				if (!MapCanvas.CanPlace(copy, index))
				{
					Audio.Play("error", "UI");
					RefreshToolPanel();
					return;
				}
				EditorState.Modify(l => l.Components[index] = copy);
			}
			else
			{
				change(c);
				_canvas.Redraw();
			}
		}

		_syncing = true;
		_w = Field(_toolPanel, "Width", 1, 12, 1, c.Size[0]);
		_h = Field(_toolPanel, "Height", 1, 12, 1, c.Size[1]);
		_w.ValueChanged += v => Apply(d => d.Size = new[] { (int)v, d.Size[1] });
		_h.ValueChanged += v => Apply(d => d.Size = new[] { d.Size[0], (int)v });
		_color = new OptionButton { FitToLongestItem = false, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		for (var i = 0; i < ComponentPresets.Colors.Length; i++)
		{
			Loc.AddItem(_color, ComponentPresets.Colors[i].Name);
			if (ComponentPresets.Colors[i].Hex == c.Color)
				_color.Selected = i;
		}
		_color.ItemSelected += i => Apply(d => d.Color = ComponentPresets.Colors[i].Hex);
		_toolPanel.AddChild(_color);
		_label = new LineEdit { Text = c.Label ?? "", PlaceholderText = "label" };
		_label.TextSubmitted += t => Apply(d => d.Label = t == "" ? null : t);
		_label.FocusExited += () => Apply(d => d.Label = _label.Text == "" ? null : _label.Text);
		_toolPanel.AddChild(_label);
		_labelHint = Help(ComponentPresets.LabelHints.GetValueOrDefault(c.Type, "optional marking"));
		_toolPanel.AddChild(_labelHint);
		_syncing = false;
	}

	// ------------------------------------------------------------ waves

	void BuildWaveHeader()
	{
		var header = new PanelContainer { ThemeTypeVariation = "SidePanel", Position = Grid.Origin, Name = "WaveHeader", Size = new Vector2(Grid.Size.X, WaveHeaderHeight), Visible = false };
		_waveHeader = header;
		AddChild(header);
		var row = Ui.HBox(6);
		header.AddChild(row);
		row.AddChild(Ui.Label("WAVE", "SmallHeader"));
		_waveTabScroll = new ScrollContainer { Name = "WaveTabScroll", SizeFlagsHorizontal = SizeFlags.ExpandFill,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
		row.AddChild(_waveTabScroll);
		_waveTabs = Ui.HBox(4);
		_waveTabScroll.AddChild(_waveTabs);
	}

	void RefreshWaveTabs()
	{
		if (_waveTabs == null)
			return;
		foreach (var child in _waveTabs.GetChildren())
			child.QueueFree();
		var waves = EditorState.Level.Waves;
		if (_timeline.Wave >= waves.Count)
			_timeline.Wave = Math.Max(waves.Count - 1, 0);
		for (var i = 0; i < waves.Count; i++)
		{
			var index = i;
			var b = Ui.Button($"{i + 1}", () =>
			{
				_timeline.Wave = index;
				RefreshWaveTabs();
				_timeline.QueueRedraw();
			}, "ToolButton");
			b.ToggleMode = true;
			b.ButtonPressed = i == _timeline.Wave;
			b.CustomMinimumSize = new Vector2(36, 0);
			_waveTabs.AddChild(b);
			if (b.ButtonPressed) Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(b) && b.IsInsideTree() && !b.IsQueuedForDeletion()) _waveTabScroll.EnsureControlVisible(b);
			}).CallDeferred();
		}
		_waveTabs.AddChild(Ui.Button("+ wave", () =>
		{
			EditorState.Modify(l => l.Waves.Add(new List<SpawnData>()));
			_timeline.Wave = EditorState.Level.Waves.Count - 1;
			RefreshWaveTabs();
		}, "ToolButton"));
	}

	Control BuildWavePanel()
	{
		var box = Ui.VBox(8);
		box.SizeFlagsVertical = SizeFlags.ExpandFill;
		box.AddChild(Ui.Label("WAVES", "SmallHeader", HorizontalAlignment.Center));
		box.AddChild(Help("Click a lane to add a bug at that time. Drag to move (or change type), right click to remove. Wheel scrolls, Ctrl+wheel zooms."));
		box.AddChild(Ui.Button("Duplicate wave", () => EditorState.Modify(l =>
		{
			var copy = l.Waves[_timeline.Wave].Select(s => new SpawnData { Enemy = s.Enemy, Time = s.Time }).ToList();
			l.Waves.Insert(_timeline.Wave + 1, copy);
			_timeline.Wave++;
		}), "ToolButton"));
		box.AddChild(Ui.Button("Clear wave", () => EditorState.Modify(l => l.Waves[_timeline.Wave].Clear()), "ToolButton"));
		box.AddChild(Ui.Button("Delete wave", () =>
		{
			if (EditorState.Level.Waves.Count > 1)
				EditorState.Modify(l => l.Waves.RemoveAt(_timeline.Wave));
		}, "DangerButton"));
		box.AddChild(Ui.Divider());
		box.AddChild(Ui.Label("FILL", "SmallHeader", HorizontalAlignment.Center));
		var type = new OptionButton { FitToLongestItem = false, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (var e in Catalog.WaveEnemies)
		{
			// The complete name remains in the tooltip; this caption fits the narrow editor column.
			Loc.AddItem(type, e.Name);
			type.SetItemTooltip(type.ItemCount - 1, e.Name);
		}
		box.AddChild(type);
		var count = Field(box, "Count", 1, 60, 1, 8);
		var every = Field(box, "Every (s)", 0.25, 20, 0.25, 1.5);
		var from = Field(box, "Start (s)", 0, 300, 0.25, 0);
		box.AddChild(Ui.Button("Add bugs", () => EditorState.Modify(l =>
		{
			var wave = l.Waves[_timeline.Wave];
			for (var i = 0; i < (int)count.Value; i++)
				wave.Add(new SpawnData { Enemy = Catalog.WaveEnemies[type.Selected].Id, Time = (float)(from.Value + i * every.Value) });
			wave.Sort((a, b) => a.Time.CompareTo(b.Time));
		}), "ToolButton"));
		box.AddChild(Ui.Spacer(0, true));
		var summary = Help("");
		box.AddChild(summary);
		EditorState.Changed += () =>
		{
			if (!IsInstanceValid(summary))
				return;
			var waves = EditorState.Level.Waves;
			var wave = _timeline.Wave < waves.Count ? waves[_timeline.Wave] : new List<SpawnData>();
			var hp = wave.Sum(s => Catalog.Enemy(s.Enemy)?.MaxHp ?? 0);
			var reward = wave.Sum(s => Catalog.Enemy(s.Enemy)?.Reward ?? 0);
			Loc.Set(summary, $"This wave: {wave.Count} bugs, {hp:0} HP, ${reward} rewards, {(wave.Count > 0 ? wave.Max(s => s.Time) : 0):0.0}s\nLevel: {waves.Count} waves, {waves.Sum(w => w.Count)} bugs");
		};
		return box;
	}

	static SpinBox Field(Container parent, string label, double min, double max, double step, double value)
	{
		var row = Ui.VBox(2);
		var l = Ui.Label(label, "DimLabel");
		l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(l);
		var spin = new SpinBox { MinValue = min, MaxValue = max, Step = step, Value = value, CustomMinimumSize = new Vector2(76, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddChild(spin);
		parent.AddChild(row);
		return spin;
	}

	// ------------------------------------------------------------ files and dialogs

	void Toast(string text, Color color)
	{
		Loc.Set(_toast, text);
		_toast.Modulate = color;
		var tween = CreateTween();
		tween.TweenInterval(2.2f);
		tween.TweenProperty(_toast, "modulate:a", 0f, 0.6f);
	}

	void Save(bool copy)
	{
		var path = EditorState.Save(copy);
		if (path == null)
			Toast("Could not save", UiTheme.Red);
		else
			Toast($"Saved {path}", UiTheme.Green);
	}

	void NewLevel()
	{
		EditorState.Load(EditorState.NewLevel(), null);
		_canvas.Select(-1);
		Toast("New level", UiTheme.Cyan);
	}

	void TestPlay()
	{
		var problems = EditorState.Level.Validate(Catalog.Enemies.Keys.ToList());
		if (problems.Count > 0)
		{
			Toast(problems[0], UiTheme.Red);
			Audio.Play("error", "UI");
			return;
		}
		var level = EditorState.Level.Clone();
		level.SortWaves();
		GameSession.StartCustom(level, GameSession.Kind.TestPlay, Scene);
	}

	void Leave()
	{
		if (EditorState.Dirty && !_confirmLeave)
		{
			_confirmLeave = true;
			Loc.Set(_exit, "Discard?");
			_exit.AddThemeColorOverride("font_color", UiTheme.Red);
			return;
		}
		EditorState.Dirty = false;
		Transition.Go(GameSession.MenuScene);
	}

	Control OpenDialog(Control content)
	{
		CloseDialog();
		_dialog = new Control();
		_dialog.SetAnchorsPreset(LayoutPreset.FullRect);
		_dialog.AddChild(Ui.Dim(0.7f));
		_dialog.AddChild(Ui.Centered(content));
		AddChild(_dialog);
		Ui.Reveal(content);
		return _dialog;
	}

	void CloseDialog()
	{
		_dialog?.QueueFree();
		_dialog = null;
	}

	void OpenSettings()
	{
		var level = EditorState.Level;
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(520, 0) };
		var box = Ui.VBox(10);
		panel.AddChild(box);
		box.AddChild(Ui.Label("LEVEL SETTINGS", "HeaderLabel", HorizontalAlignment.Center));
		var title = Row(box, "Title", new LineEdit { Text = level.Title });
		var subtitle = Row(box, "Subtitle", new LineEdit { Text = level.Subtitle });
		var core = Row(box, "Core label", new LineEdit { Text = level.CoreLabel });
		var theme = new OptionButton { FitToLongestItem = false, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (var name in View.BoardTheme.Names)
			Loc.AddItem(theme, name);
		theme.Selected = Math.Max(Array.IndexOf(View.BoardTheme.Names, level.Theme), 0);
		Row(box, "Board colour", theme);
		var money = Row(box, "Start money", new SpinBox { MinValue = 0, MaxValue = 999, Value = level.StartMoney });
		var music = new OptionButton { FitToLongestItem = false, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var tracks = new[] { "music_level_1", "music_level_2", "music_level_3", "title", "ending" };
		foreach (var t in tracks)
			Loc.AddItem(music, t);
		music.Selected = Math.Max(Array.IndexOf(tracks, level.Music), 0);
		Row(box, "Music", music);
		box.AddChild(Ui.Spacer(6));
		var row = Ui.HBox(12);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddChild(Ui.Button("Cancel", CloseDialog));
		row.AddChild(Ui.Button("Apply", () =>
		{
			EditorState.Modify(l =>
			{
				l.Title = title.Text.Trim() == "" ? "Untitled" : title.Text.Trim();
				l.Subtitle = subtitle.Text.Trim();
				l.CoreLabel = core.Text.Trim() == "" ? "CPU" : core.Text.Trim().ToUpperInvariant();
				l.Theme = View.BoardTheme.Names[theme.Selected];
				l.StartMoney = (int)money.Value;
				l.Music = tracks[music.Selected];
			});
			CloseDialog();
		}, "BigButton"));
		box.AddChild(row);
		OpenDialog(panel);
	}

	static T Row<T>(Container parent, string label, T field) where T : Control
	{
		var row = Ui.HBox(10);
		var l = Ui.Label(label, "DimLabel");
		l.CustomMinimumSize = new Vector2(150, 0);
		row.AddChild(l);
		field.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(field);
		parent.AddChild(row);
		return field;
	}

	void OpenBrowser()
	{
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(620, 0) };
		var box = Ui.VBox(8);
		panel.AddChild(box);
		box.AddChild(Ui.Label("OPEN LEVEL", "HeaderLabel", HorizontalAlignment.Center));
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 460), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		var list = Ui.VBox(4);
		list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(list);
		box.AddChild(scroll);
		for (var i = 0; i < Catalog.Levels.Count; i++)
		{
			var path = string.Format(Catalog.LevelPath, i + 1);
			var level = Catalog.Levels[i];
			list.AddChild(BrowserRow($"{i + 1}. {level.Title}", "built-in", () => Open(path), null));
		}
		foreach (var (path, level) in Catalog.CustomLevels())
			list.AddChild(BrowserRow(level.Title, path.GetFile(), () => Open(path), () =>
			{
				DirAccess.RemoveAbsolute(path);
				OpenBrowser();
			}));
		box.AddChild(Ui.Button("Cancel", CloseDialog));
		OpenDialog(panel);
	}

	static Control BrowserRow(string title, string where, Action open, Action delete)
	{
		var row = Ui.HBox(8);
		var b = Ui.Button(title, open, "ToolButton");
		if (delete != null) Loc.Raw(b, title);
		b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		b.Alignment = HorizontalAlignment.Left;
		row.AddChild(b);
		var location = Ui.Label(where, "DimLabel");
		if (delete != null) Loc.Raw(location, where);
		row.AddChild(location);
		if (delete != null)
			row.AddChild(Ui.Button("x", delete, "DangerButton"));
		return row;
	}

	void Open(string path)
	{
		var level = JsonIO.Load<LevelData>(path);
		if (level == null)
		{
			Toast($"Could not open {path}", UiTheme.Red);
			return;
		}
		CloseDialog();
		_canvas.Select(-1);
		EditorState.Load(level, path);
		Toast($"Opened {path}", UiTheme.Cyan);
	}

	public override void _UnhandledKeyInput(InputEvent e)
	{
		if (e is not InputEventKey { Pressed: true, Echo: false } key || GetViewport().GuiGetFocusOwner() is LineEdit)
			return;
		if (key.CtrlPressed && key.Keycode == Key.Z)
		{
			if (key.ShiftPressed)
				EditorState.RedoStep();
			else
				EditorState.UndoStep();
		}
		else if (key.CtrlPressed && key.Keycode == Key.Y)
			EditorState.RedoStep();
		else if (key.CtrlPressed && key.Keycode == Key.S)
			Save(false);
		else if (key.Keycode == Key.Tab)
			SetMode(!_timeline.Visible);
		else if (!_timeline.Visible && key.Keycode >= Key.Key1 && key.Keycode <= Key.Key4)
			SetTool((MapTool)(key.Keycode - Key.Key1));
		else if (key.Keycode == Key.Escape && _dialog != null)
			CloseDialog();
		else
			return;
		GetViewport().SetInputAsHandled();
	}
}
