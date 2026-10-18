using System.Collections.Generic;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Net;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.UI;

/// <summary>
/// Versus lobby: play against the computer, host a match (you defend, the other player attacks)
/// or join one (you attack). Online play uses WebSocket; see docs/PROTOCOL.md.
/// </summary>
public partial class VersusMenu : Control
{
	readonly List<LevelData> _levels = new();
	OptionButton _level;
	SpinBox _port;
	LineEdit _address;
	LineEdit _name;
	Label _status;
	RemoteFeed _joining;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(Ui.Dim(0.72f));
		Audio.PlayMusic("title");
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(760, 0) };
		var box = Ui.VBox(14);
		panel.AddChild(box);
		box.AddChild(Ui.Label("VERSUS", "TitleLabel", HorizontalAlignment.Center));
		var intro = Ui.Label("One player defends the CPU and hosts the match. The other spends energy to send bugs. The defender wins if the CPU survives for 5 minutes.", "DimLabel", HorizontalAlignment.Center);
		intro.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		box.AddChild(intro);

		_level = new OptionButton();
		foreach (var level in Catalog.Levels)
		{
			_levels.Add(level);
			Loc.AddItem(_level, level.Title);
		}
		foreach (var (path, level) in Catalog.CustomLevels())
			if (level.Validate(new List<string>(Catalog.Enemies.Keys)).Count == 0)
			{
				_levels.Add(level);
				Loc.AddItem(_level, $"{level.Title} (custom)");
			}
		box.AddChild(Row("Board", _level));
		box.AddChild(Ui.Divider());

		box.AddChild(Section("Against the computer", "You defend, a bot attacks.", Ui.Button("Play vs AI", () => GameSession.StartVersusBot(_levels[_level.Selected]), "BigButton")));

		_port = new SpinBox { MinValue = 1024, MaxValue = 65535, Value = Protocol.DefaultPort, CustomMinimumSize = new Vector2(120, 0) };
		var host = Ui.HBox(10);
		host.AddChild(Ui.Label("Port", "DimLabel"));
		host.AddChild(_port);
		host.AddChild(Ui.Button("Host game", Host, "BigButton"));
		var ips = Protocol.LocalAddresses();
		box.AddChild(Section("Host (you defend)", ips.Count > 0 ? $"Your address: {string.Join(", ", ips)}" : "Others join with your IP address.", host));

		_name = new LineEdit { Text = Settings.Instance.PlayerName, PlaceholderText = "your name", CustomMinimumSize = new Vector2(150, 0) };
		_address = new LineEdit { Text = Settings.Instance.LastAddress, PlaceholderText = "host:7777", CustomMinimumSize = new Vector2(200, 0) };
		var join = Ui.HBox(10);
		join.AddChild(_name);
		join.AddChild(_address);
		join.AddChild(Ui.Button("Join", Join, "BigButton"));
		box.AddChild(Section("Join (you attack)", "Enter the host's address.", join));

		_status = Ui.Label("", "SmallHeader", HorizontalAlignment.Center);
		_status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		box.AddChild(_status);
		var back = Ui.Button("Back", () =>
		{
			_joining?.Close();
			Transition.Go(GameSession.MenuScene);
		}, "BigButton");
		back.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		box.AddChild(back);
		AddChild(Ui.Centered(panel));
		Ui.Reveal(panel);
	}

	static Control Row(string label, Control field)
	{
		var row = Ui.HBox(12);
		var l = Ui.Label(label, "DimLabel");
		l.CustomMinimumSize = new Vector2(120, 0);
		row.AddChild(l);
		field.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(field);
		return row;
	}

	static Control Section(string title, string hint, Control action)
	{
		var row = Ui.HBox(16);
		var text = Ui.VBox(2);
		text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		text.AddChild(Ui.Label(title.ToUpperInvariant(), "SmallHeader"));
		var h = Ui.Label(hint, "DimLabel");
		h.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		h.AddThemeFontSizeOverride("font_size", UiTheme.BodySize);
		text.AddChild(h);
		row.AddChild(text);
		row.AddChild(action);
		return row;
	}

	void Host()
	{
		var level = _levels[_level.Selected];
		var port = (int)_port.Value;
		GameSession.StartOnline(() => new HostFeed(level, port, PlayerSide.Defender, PlayerSide.Attacker));
	}

	void Join()
	{
		_joining?.Close();
		Settings.Instance.SetNetwork(_name.Text.Trim() == "" ? "Player" : _name.Text.Trim(), _address.Text.Trim());
		_joining = new RemoteFeed(_address.Text.Trim(), PlayerSide.Attacker, Settings.Instance.PlayerName);
	}

	public override void _Process(double delta)
	{
		if (_joining == null)
			return;
		_joining.Process((float)delta);
		Loc.Set(_status, _joining.Status);
		_status.Modulate = _joining.LastError != null ? UiTheme.Red : UiTheme.Cyan;
		if (!_joining.Joined)
			return;
		var feed = _joining;
		_joining = null;
		GameSession.StartOnline(() => feed);
	}
}
