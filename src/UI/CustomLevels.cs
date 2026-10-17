using Brenda.Autoload;
using Brenda.Editor;
using Brenda.Game;
using Godot;

namespace Brenda.UI;

/// <summary>Player-made levels (saved by the editor in user://levels): play, edit or delete them.</summary>
public partial class CustomLevels : Control
{
	VBoxContainer _list;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(Ui.Dim(0.75f));
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(820, 0) };
		var box = Ui.VBox(12);
		panel.AddChild(box);
		box.AddChild(Ui.Label("CUSTOM LEVELS", "TitleLabel", HorizontalAlignment.Center));
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 470), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		_list = Ui.VBox(8);
		_list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(_list);
		box.AddChild(scroll);
		var row = Ui.HBox(12);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddChild(Ui.Button("Back", () => Transition.Go(GameSession.MenuScene), "BigButton"));
		row.AddChild(Ui.Button("Level Editor", () => Transition.Go("res://scenes/level_editor.tscn"), "BigButton"));
		box.AddChild(row);
		AddChild(Ui.Centered(panel));
		Refresh();
	}

	void Refresh()
	{
		foreach (var child in _list.GetChildren())
			child.QueueFree();
		var levels = Data.Catalog.CustomLevels();
		if (levels.Count == 0)
			_list.AddChild(Ui.Label("No custom levels yet. Make one in the Level Editor!", "DimLabel", HorizontalAlignment.Center));
		foreach (var (path, level) in levels)
		{
			var card = new PanelContainer { ThemeTypeVariation = "CardPanel" };
			var row = Ui.HBox(12);
			card.AddChild(row);
			row.AddChild(new LevelPreview(level, false, new Vector2(126, 96)));
			var info = Ui.VBox(2);
			info.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			info.AddChild(Ui.Label(level.Title.ToUpperInvariant(), "SmallHeader"));
			info.AddChild(Ui.Label($"{level.Waves.Count} waves  -  {path.GetFile()}", "DimLabel"));
			row.AddChild(info);
			var problems = level.Validate(System.Linq.Enumerable.ToList(Data.Catalog.Enemies.Keys));
			var play = Ui.Button("Play", () => GameSession.StartCustom(level, GameSession.Kind.Custom, "res://scenes/custom_levels.tscn"));
			play.Disabled = problems.Count > 0;
			play.TooltipText = problems.Count > 0 ? string.Join("\n", problems) : "";
			row.AddChild(play);
			row.AddChild(Ui.Button("Versus", () => GameSession.StartVersusBot(level)));
			row.AddChild(Ui.Button("Edit", () =>
			{
				EditorState.Load(level, path);
				Transition.Go("res://scenes/level_editor.tscn");
			}));
			row.AddChild(Ui.Button("x", () =>
			{
				DirAccess.RemoveAbsolute(path);
				Refresh();
			}, "DangerButton"));
			_list.AddChild(card);
		}
	}
}
