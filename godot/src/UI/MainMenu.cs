using Brenda.Autoload;
using Brenda.Game;
using Godot;

namespace Brenda.UI;

/// <summary>Title screen.</summary>
public partial class MainMenu : Control
{
	const string Instagram = "https://www.instagram.com/atlas_gamedev/";

	Button _newGame;
	bool _confirmReset;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(Shade());
		Audio.PlayMusic("title");
		var hasSave = GameState.HasSave();

		var column = UI.VBox(10);
		column.Position = new Vector2(110, 50);
		column.CustomMinimumSize = new Vector2(520, 0);
		AddChild(column);
		var project = UI.Label("PROJECT", "HeaderLabel");
		project.AddThemeColorOverride("font_color", UiTheme.Dim);
		project.AddThemeFontSizeOverride("font_size", 34);
		column.AddChild(project);
		column.AddChild(new GlitchTitle());
		column.AddChild(UI.Label("Stop the bugs before they reach the CPU.", "DimLabel"));
		column.AddChild(UI.Spacer(14));

		var buttons = UI.VBox(10);
		buttons.CustomMinimumSize = new Vector2(340, 0);
		buttons.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		column.AddChild(buttons);
		if (hasSave)
			buttons.AddChild(UI.Button("Continue", Continue, "BigButton"));
		_newGame = UI.Button("New Game", NewGame, "BigButton");
		buttons.AddChild(_newGame);
		var select = UI.Button("Select Level", () =>
		{
			GameState.Load();
			Transition.Go("res://scenes/level_select.tscn");
		}, "BigButton");
		buttons.AddChild(select);
		buttons.AddChild(UI.Button("Versus", () => Transition.Go("res://scenes/versus_menu.tscn"), "BigButton"));
		buttons.AddChild(UI.Button("Level Editor", () => Transition.Go("res://scenes/level_editor.tscn"), "BigButton"));
		var row = UI.HBox(10);
		var settings = UI.Button("Settings", OpenSettings, "BigButton");
		settings.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(settings);
		if (!OS.HasFeature("web") && !OS.HasFeature("mobile"))
		{
			var exit = UI.Button("Exit", () => GetTree().Quit(), "BigButton");
			exit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			row.AddChild(exit);
		}
		buttons.AddChild(row);

		var footer = UI.VBox(2);
		footer.Position = new Vector2(1366 - 330, 768 - 96);
		footer.CustomMinimumSize = new Vector2(300, 0);
		var insta = UI.Button("@atlas_gamedev", () => OS.ShellOpen(Instagram));
		insta.Flat = true;
		insta.Alignment = HorizontalAlignment.Right;
		footer.AddChild(insta);
		footer.AddChild(UI.Label("prototype - godot .net", "DimLabel", HorizontalAlignment.Right));
		AddChild(footer);

		var children = buttons.GetChildren();
		for (var i = 0; i < children.Count; i++)
			UI.Reveal((CanvasItem)children[i], 0.15f + i * 0.06f, new Vector2(-30, 0));
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
		var dim = UI.Dim();
		AddChild(dim);
		var panel = new SettingsPanel();
		var center = UI.Centered(panel);
		AddChild(center);
		panel.Closed += () =>
		{
			dim.QueueFree();
			center.QueueFree();
		};
		UI.Reveal(panel);
	}
}
