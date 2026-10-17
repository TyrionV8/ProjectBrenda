using System;
using System.Collections.Generic;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.UI;

/// <summary>
/// In-game interface. The left column is shared; the right column is the turret inventory
/// (defender) or the bug palette (attacker). Also shows wave banners and result panels.
/// </summary>
public partial class Hud : CanvasLayer
{
	readonly GameScreen _screen;
	InfoPanel _info;
	readonly List<TurretSlot> _turretSlots = new();
	readonly List<BugSlot> _bugSlots = new();
	Label _money;
	Label _energy;
	Button _speed;
	Button _cancel;
	Label _hint;
	Label _banner;
	Label _bannerSub;
	Control _waiting;
	Label _waitingText;
	Control _overlay;
	Control _bossBar;
	EnemyDef _boss;
	float _bossHp;
	bool _bossDisguised;

	public Hud(GameScreen screen) => _screen = screen;

	bool Attacker => _screen.Feed.LocalSide == PlayerSide.Attacker;

	public override void _Ready()
	{
		var versus = _screen.Feed.Mode == MatchMode.Versus;
		var levelNumber = GameSession.Current == GameSession.Kind.Campaign ? GameState.CurrentLevel + 1 : 0;
		_info = new InfoPanel(versus, levelNumber);
		AddChild(_info);
		if (Attacker)
			BuildAttackerPanel();
		else
			BuildDefenderPanel();
		BuildBoardOverlay();
	}

	public void Apply(Snapshot s)
	{
		_info.Apply(s);
		UpdateBossBar(s);
		if (Attacker)
		{
			_energy.Text = Mathf.FloorToInt(s.Energy).ToString();
			foreach (var slot in _bugSlots)
				slot.Blocked = _screen.Feed.CannotSend(slot.Def);
		}
		else
		{
			_money.Text = s.Money.ToString();
			foreach (var slot in _turretSlots)
				slot.Affordable = s.Money >= slot.Def.Cost;
		}
	}

	public void ShowWaiting(string text)
	{
		_waiting.Visible = true;
		_waitingText.Text = text;
	}

	public void HideWaiting() => _waiting.Visible = false;

	public void UpdateSpeed(float value)
	{
		if (_speed != null)
			_speed.Text = $"{(int)value}x";
	}

	public void FlashMoney()
	{
		var label = Attacker ? _energy : _money;
		label.Modulate = UiTheme.Red;
		CreateTween().TweenProperty(label, "modulate", Colors.White, 0.5f);
		label.PivotOffset = label.Size / 2;
		label.Scale = Vector2.One * 1.25f;
		CreateTween().TweenProperty(label, "scale", Vector2.One, 0.25f).SetTrans(Tween.TransitionType.Back);
	}

	public void BugSent(EnemyDef def) => _bugSlots.Find(s => s.Def == def)?.Sent();

	public void OnSelectionChanged(TurretDef def)
	{
		foreach (var slot in _turretSlots)
			slot.Selected = slot.Def == def;
		_cancel.Visible = def != null;
		_hint.Visible = def != null;
		if (def != null)
			_hint.Text = $"{def.Name.ToUpperInvariant()}  -  click a free cell to build  -  right click to cancel";
	}

	public void ShowBanner(string text, string sub = "")
	{
		_banner.Text = text;
		_bannerSub.Text = sub;
		_banner.Scale = new Vector2(1.4f, 1.4f);
		var tween = CreateTween().SetParallel();
		tween.TweenProperty(_banner, "modulate:a", 1f, 0.25f);
		tween.TweenProperty(_banner, "scale", Vector2.One, 0.35f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(_bannerSub, "modulate:a", 1f, 0.3f).SetDelay(0.15f);
		tween.Chain().TweenInterval(1.1f);
		tween.Chain().TweenProperty(_banner, "modulate:a", 0f, 0.4f);
		tween.Parallel().TweenProperty(_bannerSub, "modulate:a", 0f, 0.4f);
	}

	/// <summary>A boss just got onto the board.</summary>
	public void ShowBossWarning(EnemyDef def)
	{
		if (def.Ability == "trojan")
		{
			ShowBanner("NEW DOWNLOAD", "FREE_RAM.EXE  -  totally safe, double the RAM for free!");
			return;
		}
		ShowBanner("INTRUSION DETECTED", $"{def.Title ?? def.Name}  -  {def.Blurb}");
		_banner.AddThemeColorOverride("font_color", UiTheme.Red);
		GetTree().CreateTimer(2.2).Timeout += () => _banner.RemoveThemeColorOverride("font_color");
	}

	/// <summary>The first boss on the board, with its health, at the top of the board.</summary>
	void UpdateBossBar(Snapshot s)
	{
		EnemyDef boss = null;
		var hp = 0f;
		var disguised = false;
		foreach (var b in s.Bugs)
		{
			var def = Catalog.Enemy(b.Type);
			if (def is not { Boss: true })
				continue;
			boss = def;
			hp = b.Hp;
			disguised = def.Ability == "trojan" && !b.Revealed;
			break;
		}
		if (boss == _boss && Mathf.IsEqualApprox(hp, _bossHp) && disguised == _bossDisguised)
			return;
		_boss = boss;
		_bossHp = hp;
		_bossDisguised = disguised;
		_bossBar.Visible = boss != null;
		_bossBar.QueueRedraw();
	}

	Control BossBar()
	{
		var bar = new Control { CustomMinimumSize = new Vector2(0, 44), MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
		bar.Draw += DrawBossBar;
		return bar;
	}

	void DrawBossBar()
	{
		if (_boss == null)
			return;
		var size = _bossBar.Size;
		var name = _bossDisguised ? "FREE_RAM.EXE" : (_boss.Title ?? _boss.Name).ToUpperInvariant();
		var color = _bossDisguised ? UiTheme.Green : UiTheme.Red;
		var fontSize = size.X > 300 ? 16 : 11;
		_bossBar.DrawStringOutline(UiTheme.PixelBold, new Vector2(0, 14), name, HorizontalAlignment.Center, size.X, fontSize, 6, new Color(0, 0, 0, 0.9f));
		_bossBar.DrawString(UiTheme.PixelBold, new Vector2(0, 14), name, HorizontalAlignment.Center, size.X, fontSize, color);
		var bar = new Rect2(0, 22, size.X, 15);
		_bossBar.DrawRect(bar.Grow(3), new Color(0, 0, 0, 0.75f));
		var segments = size.X > 300 ? 30 : 15;
		var w = (bar.Size.X - (segments - 1) * 3) / segments;
		var filled = Mathf.Clamp(_bossHp / _boss.MaxHp, 0, 1) * segments;
		for (var i = 0; i < segments; i++)
		{
			var r = new Rect2(bar.Position.X + i * (w + 3), bar.Position.Y, w, bar.Size.Y);
			var on = _bossDisguised ? UiTheme.Green : UiTheme.Red.Lerp(new Color("ffb347"), (float)i / segments);
			_bossBar.DrawRect(r, i < filled ? on : new Color(1, 1, 1, 0.08f));
		}
	}

	/// <summary>Result panel with a title, a message, optional stars and buttons.</summary>
	public void ShowResult(string title, Color titleColor, string message, int stars, params (string Text, Action Action)[] buttons)
	{
		_overlay?.QueueFree();
		_overlay = new Control();
		_overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_overlay);
		var dim = Ui.Dim(0);
		_overlay.AddChild(dim);
		CreateTween().TweenProperty(dim, "color:a", 0.6f, 0.4f);
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(600, 0) };
		var box = Ui.VBox(16);
		panel.AddChild(box);
		var t = Ui.Label(title, "TitleLabel", HorizontalAlignment.Center);
		t.AddThemeFontSizeOverride("font_size", 44);
		t.AddThemeColorOverride("font_color", titleColor);
		box.AddChild(t);
		if (stars > 0)
			box.AddChild(new StarRow(stars, 46) { SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter });
		var m = Ui.Label(message, "", HorizontalAlignment.Center);
		m.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		box.AddChild(m);
		var row = Ui.HBox(16);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		foreach (var (text, action) in buttons)
			row.AddChild(Ui.Button(text, action, "BigButton"));
		box.AddChild(Ui.Spacer(6));
		box.AddChild(row);
		_overlay.AddChild(Ui.Centered(panel));
		Ui.Reveal(panel, 0.1f, new Vector2(0, 40));
	}

	void BuildDefenderPanel()
	{
		var panel = SidePanel();
		var box = Ui.VBox(8);
		panel.AddChild(box);
		box.AddChild(Ui.Label("CASH", "SmallHeader", HorizontalAlignment.Center));
		_money = Ui.Label("0", "MoneyLabel");
		box.AddChild(IconRow("res://assets/sprites/icons/chip.png", UiTheme.Gold, _money));
		box.AddChild(Ui.Divider());
		for (var i = 0; i < Catalog.Turrets.Count; i++)
		{
			var def = Catalog.Turrets[i];
			var slot = new TurretSlot(def, i + 1);
			slot.Pressed += () =>
			{
				if (_screen.Build.Selected == def)
					_screen.Build.Cancel();
				else
					_screen.Build.Select(def);
			};
			box.AddChild(slot);
			_turretSlots.Add(slot);
		}
		box.AddChild(Ui.Spacer(4));
		_bossBar = BossBar();
		box.AddChild(_bossBar);
		box.AddChild(Ui.Spacer(0, true));
		box.AddChild(BottomRow());
	}

	void BuildAttackerPanel()
	{
		var panel = SidePanel();
		var box = Ui.VBox(6);
		panel.AddChild(box);
		box.AddChild(Ui.Label("ENERGY", "SmallHeader", HorizontalAlignment.Center));
		_energy = Ui.Label("0", "MoneyLabel");
		_energy.AddThemeColorOverride("font_color", UiTheme.Magenta);
		box.AddChild(IconRow("res://assets/sprites/icons/damage.png", UiTheme.Magenta, _energy));
		box.AddChild(Ui.Divider());
		for (var i = 0; i < Catalog.VersusEnemies.Count; i++)
		{
			var def = Catalog.VersusEnemies[i];
			var slot = new BugSlot(def, i + 1);
			slot.Pressed += () => _screen.SendBug(def);
			box.AddChild(slot);
			_bugSlots.Add(slot);
		}
		box.AddChild(Ui.Spacer(0, true));
		box.AddChild(BottomRow());
	}

	Control BottomRow()
	{
		var row = Ui.HBox(8);
		if (_screen.Feed.IsLocalOnly)
		{
			_speed = Ui.Button("1x", _screen.CycleSpeed);
			_speed.TooltipText = "Game speed (F)";
			_speed.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			row.AddChild(_speed);
		}
		var menu = Ui.Button(_screen.Feed.IsLocalOnly ? "II" : "MENU", _screen.OpenMenu);
		menu.TooltipText = "Pause (Esc)";
		menu.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		row.AddChild(menu);
		return row;
	}

	PanelContainer SidePanel()
	{
		var left = Grid.Origin.X + Grid.Size.X;
		var panel = new PanelContainer { ThemeTypeVariation = "SidePanel", Position = new Vector2(left, 0), Size = new Vector2(1366 - left, 768) };
		AddChild(panel);
		return panel;
	}

	static Control IconRow(string icon, Color color, Label value)
	{
		var row = Ui.HBox(8);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddChild(new TextureRect
		{
			Texture = GD.Load<Texture2D>(icon), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, CustomMinimumSize = new Vector2(30, 30),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, Modulate = color, TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
		});
		row.AddChild(value);
		return row;
	}

	void BuildBoardOverlay()
	{
		var centerX = Grid.Origin.X + Grid.Size.X / 2;
		_cancel = Ui.Button("CANCEL", () => _screen.Build.Cancel(), "DangerButton");
		_cancel.CustomMinimumSize = new Vector2(200, 52);
		_cancel.Position = new Vector2(centerX - 100, 768 - 70);
		_cancel.Visible = false;
		AddChild(_cancel);
		_hint = OverlayLabel("SmallHeader", 16, 24);
		_hint.Visible = false;
		_banner = OverlayLabel("TitleLabel", 250, 80);
		_banner.PivotOffset = _banner.Size / 2;
		_banner.Modulate = new Color(1, 1, 1, 0);
		_bannerSub = OverlayLabel("HeaderLabel", 330, 60);
		_bannerSub.Modulate = new Color(1, 1, 1, 0);
		_bannerSub.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		if (_bossBar == null)
		{
			// The attacker's column is full of bugs: the bar goes on top of the board.
			_bossBar = BossBar();
			_bossBar.Position = new Vector2(Grid.Origin.X + Grid.Size.X / 2 - 210, 48);
			_bossBar.Size = new Vector2(420, 40);
			AddChild(_bossBar);
		}
		_waiting = new Control { Visible = false };
		_waiting.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_waiting.AddChild(Ui.Dim(0.75f));
		var panel = new PanelContainer();
		var box = Ui.VBox(16);
		panel.AddChild(box);
		_waitingText = Ui.Label("", "HeaderLabel", HorizontalAlignment.Center);
		_waitingText.AddThemeFontSizeOverride("font_size", 20);
		box.AddChild(_waitingText);
		var leave = Ui.Button("Leave", () => _screen.Leave(), "DangerButton");
		leave.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		box.AddChild(leave);
		_waiting.AddChild(Ui.Centered(panel));
		AddChild(_waiting);
	}

	Label OverlayLabel(string variation, float y, float height)
	{
		var label = Ui.Label("", variation, HorizontalAlignment.Center);
		label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
		label.AddThemeConstantOverride("outline_size", variation == "TitleLabel" ? 12 : 6);
		label.Position = new Vector2(Grid.Origin.X, y);
		label.Size = new Vector2(Grid.Size.X, height);
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(label);
		return label;
	}
}
