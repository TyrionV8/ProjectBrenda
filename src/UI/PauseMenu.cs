using HardwareDefence.Game;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Pause overlay (campaign pauses the game; online matches keep running).</summary>
public partial class PauseMenu : CanvasLayer
{
	readonly GameScreen _screen;
	Control _root;
	Control _menu;
	SettingsPanel _settings;
	Control _settingsCenter;

	public PauseMenu(GameScreen screen) => _screen = screen;

	public override void _Ready()
	{
		Layer = 20;
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_root);
		_root.AddChild(Ui.Dim(0.72f));
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(380, 0) };
		var box = Ui.VBox(12);
		panel.AddChild(box);
		var local = _screen.Feed.IsLocalOnly;
		box.AddChild(Ui.Label(local ? "PAUSED" : "MENU", "TitleLabel", HorizontalAlignment.Center));
		box.AddChild(Ui.Spacer(4));
		box.AddChild(Ui.Button("Resume", Close, "BigButton"));
		if (local)
			box.AddChild(Ui.Button("Restart", _screen.Restart));
		box.AddChild(Ui.Button("Settings", OpenSettings));
		box.AddChild(Ui.Button(local ? "Quit" : "Leave match", _screen.Leave, "DangerButton"));
		_menu = Ui.Centered(panel);
		_root.AddChild(_menu);
		_settings = new SettingsPanel();
		_settings.Closed += () =>
		{
			_settingsCenter.Visible = false;
			_menu.Visible = true;
		};
		_settingsCenter = Ui.Centered(_settings);
		_settingsCenter.Visible = false;
		_root.AddChild(_settingsCenter);
	}

	public void Open()
	{
		if (Visible)
			return;
		Visible = true;
		_menu.Visible = true;
		_settingsCenter.Visible = false;
		if (_screen.Feed.IsLocalOnly)
			GetTree().Paused = true;
		Autoload.Audio.Ui("click");
		_root.Modulate = new Color(1, 1, 1, 0);
		CreateTween().TweenProperty(_root, "modulate:a", 1f, 0.15f);
	}

	public void Close()
	{
		Visible = false;
		GetTree().Paused = false;
	}

	void OpenSettings()
	{
		_menu.Visible = false;
		_settingsCenter.Visible = true;
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (!Visible || !e.IsActionPressed("pause"))
			return;
		if (_settingsCenter.Visible)
			_settings.EmitClosed();
		else
			Close();
		GetViewport().SetInputAsHandled();
	}
}
