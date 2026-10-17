using HardwareDefence.Autoload;
using HardwareDefence.Game;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Title screen.</summary>
public partial class MainMenu : Control
{
	Button _newGame;
	bool _confirmReset;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(Shade());
		Audio.PlayMusic("title");
		var hasSave = GameState.HasSave();

		var column = Ui.VBox(10);
		column.Position = new Vector2(110, 50);
		column.CustomMinimumSize = new Vector2(520, 0);
		AddChild(column);
		column.AddChild(new GlitchTitle());
		column.AddChild(Ui.Label("Stop the bugs before they reach the CPU.", "DimLabel"));
		column.AddChild(Ui.Spacer(14));

		var buttons = Ui.VBox(10);
		buttons.CustomMinimumSize = new Vector2(340, 0);
		buttons.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		column.AddChild(buttons);
		if (hasSave)
			buttons.AddChild(Ui.Button("Continue", Continue, "BigButton"));
		_newGame = Ui.Button("New Game", NewGame, "BigButton");
		buttons.AddChild(_newGame);
		var select = Ui.Button("Select Level", () =>
		{
			GameState.Load();
			Transition.Go("res://scenes/level_select.tscn");
		}, "BigButton");
		buttons.AddChild(select);
		buttons.AddChild(Ui.Button("Versus", () => Transition.Go("res://scenes/versus_menu.tscn"), "BigButton"));
		buttons.AddChild(Ui.Button("Level Editor", () => Transition.Go("res://scenes/level_editor.tscn"), "BigButton"));
		var row = Ui.HBox(10);
		var settings = Ui.Button("Settings", OpenSettings, "BigButton");
		settings.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(settings);
		if (!OS.HasFeature("web") && !OS.HasFeature("mobile"))
		{
			var exit = Ui.Button("Exit", () => GetTree().Quit(), "BigButton");
			exit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			row.AddChild(exit);
		}
		buttons.AddChild(row);

		var footer = Ui.VBox(2);
		footer.Position = new Vector2(1366 - 330, 768 - 56);
		footer.CustomMinimumSize = new Vector2(300, 0);
		footer.AddChild(Ui.Label("prototype - godot .net", "DimLabel", HorizontalAlignment.Right));
		AddChild(footer);

		var children = buttons.GetChildren();
		for (var i = 0; i < children.Count; i++)
			Ui.Reveal((CanvasItem)children[i], 0.15f + i * 0.06f, new Vector2(-30, 0));
	}

	static Control Shade()
	{
		var tex = new GradientTexture2D
		{
			FillFrom = Vector2.Zero, FillTo = new Vector2(1, 0),
			Gradient = new Gradient
			{
				Offsets = new[] { 0f, 0.45f, 0.8f },
				Colors = new[] { new Color(0.01f, 0.03f, 0.06f, 0.92f), new Color(0.01f, 0.03f, 0.06f, 0.65f), new Color(0.01f, 0.03f, 0.06f, 0) },
			},
		};
		var rect = new TextureRect { Texture = tex, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = MouseFilterEnum.Ignore };
		rect.SetAnchorsPreset(LayoutPreset.FullRect);
		return rect;
	}

	void Continue()
	{
		if (GameState.Load())
			Transition.Go("res://scenes/upgrade_screen.tscn");
	}

	void NewGame()
	{
		if (GameState.HasSave() && !_confirmReset)
		{
			_confirmReset = true;
			_newGame.Text = "Erase progress?";
			_newGame.ThemeTypeVariation = "DangerButton";
			return;
		}
		GameState.Reset();
		GameState.Save();
		GameSession.StartCampaign(0);
	}

	void OpenSettings()
	{
		var dim = Ui.Dim();
		AddChild(dim);
		var panel = new SettingsPanel();
		var center = Ui.Centered(panel);
		AddChild(center);
		panel.Closed += () =>
		{
			dim.QueueFree();
			center.QueueFree();
		};
		Ui.Reveal(panel);
	}
}
