using System.Linq;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Level selection: unlocked levels lead to the upgrade screen, as in the original.</summary>
public partial class LevelSelect : Control
{
	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(Ui.Dim(0.7f));
		var root = Ui.VBox(18);
		root.Alignment = BoxContainer.AlignmentMode.Center;
		root.AddChild(Ui.Label("SELECT LEVEL", "TitleLabel", HorizontalAlignment.Center));
		var grid = new GridContainer { Columns = 4 };
		grid.AddThemeConstantOverride("h_separation", 16);
		grid.AddThemeConstantOverride("v_separation", 16);
		root.AddChild(grid);
		for (var i = 0; i < Catalog.Levels.Count; i++)
		{
			var card = Card(i);
			grid.AddChild(card);
			Ui.Reveal(card, 0.05f * i);
		}
		var row = Ui.HBox(16);
		row.Alignment = BoxContainer.AlignmentMode.Center;
		row.AddChild(Ui.Button("Back", () => Transition.Go(GameSession.MenuScene), "BigButton"));
		if (Catalog.CustomLevels().Count > 0)
			row.AddChild(Ui.Button("Custom levels", () => Transition.Go("res://scenes/custom_levels.tscn"), "BigButton"));
		root.AddChild(row);
		AddChild(Ui.Centered(root));
	}

	Control Card(int index)
	{
		var level = Catalog.Levels[index];
		var unlocked = GameState.IsUnlocked(index);
		var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };
		var box = Ui.VBox(6);
		panel.AddChild(box);
		var title = Ui.Label($"{index + 1}. {level.Title.ToUpperInvariant()}", "SmallHeader", HorizontalAlignment.Center);
		title.AddThemeColorOverride("font_color", unlocked ? UiTheme.TextColor : UiTheme.Dim);
		box.AddChild(title);
		box.AddChild(new LevelPreview(level, !unlocked, new Vector2(252, 144)));
		var stars = index < GameState.Stars.Count ? GameState.Stars[index] : 0;
		var info = Ui.HBox(8);
		info.Alignment = BoxContainer.AlignmentMode.Center;
		info.AddChild(new StarRow(stars, 18));
		info.AddChild(Ui.Label($"{level.Waves.Count} waves  {level.Waves.Sum(w => w.Count)} bugs", "DimLabel"));
		box.AddChild(info);
		var play = Ui.Button(unlocked ? "PLAY" : "LOCKED", () =>
		{
			GameState.CurrentLevel = index;
			Transition.Go("res://scenes/upgrade_screen.tscn");
		});
		play.Disabled = !unlocked;
		box.AddChild(play);
		return panel;
	}
}
