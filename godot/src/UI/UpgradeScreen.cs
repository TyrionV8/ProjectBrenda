using System.Collections.Generic;
using Brenda.Autoload;
using Brenda.Data;
using Brenda.Game;
using Godot;

namespace Brenda.UI;

/// <summary>Spend upgrade points between levels, then start the next level.</summary>
public partial class UpgradeScreen : Control
{
	Label _points;
	Label _details;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(new MenuBackground());
		AddChild(Ui.Dim(0.78f));
		Audio.PlayMusic("title");
		var level = Catalog.Levels[GameState.CurrentLevel];

		var root = Ui.VBox(18);
		root.Position = new Vector2(40, 28);
		root.Size = new Vector2(1366 - 80, 768 - 56);
		AddChild(root);

		var header = Ui.HBox(16);
		var title = Ui.Label("UPGRADES", "TitleLabel");
		title.AddThemeFontSizeOverride("font_size", 52);
		header.AddChild(title);
		var next = Ui.Label($"next: {GameState.CurrentLevel + 1}. {level.Title.ToUpperInvariant()}", "HeaderLabel");
		next.AddThemeColorOverride("font_color", UiTheme.Dim);
		next.AddThemeFontSizeOverride("font_size", 22);
		next.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		next.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(next);
		var pointsBox = new PanelContainer { ThemeTypeVariation = "CardPanel" };
		var pointsRow = Ui.HBox(10);
		pointsBox.AddChild(pointsRow);
		pointsRow.AddChild(new TextureRect
		{
			Texture = GD.Load<Texture2D>("res://assets/art/icons/chip.svg"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			CustomMinimumSize = new Vector2(36, 36), Modulate = UiTheme.Gold,
		});
		var col = Ui.VBox(0);
		col.AddChild(Ui.Label("POINTS", "SmallHeader"));
		_points = Ui.Label("0", "MoneyLabel");
		col.AddChild(_points);
		pointsRow.AddChild(col);
		header.AddChild(pointsBox);
		root.AddChild(header);

		var cards = Ui.HBox(14);
		cards.SizeFlagsVertical = SizeFlags.ExpandFill;
		root.AddChild(cards);
		var list = new List<Control>();
		foreach (var def in Catalog.Turrets)
		{
			var card = Card(def);
			card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			cards.AddChild(card);
			list.Add(card);
		}

		var footer = Ui.HBox(16);
		footer.AddChild(Ui.Button("Main Menu", () => Transition.Go(GameSession.MenuScene), "BigButton"));
		_details = Ui.Label("Hover an upgrade to see what it does. Click + to buy it.", "DimLabel");
		_details.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_details.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_details.VerticalAlignment = VerticalAlignment.Center;
		footer.AddChild(_details);
		footer.AddChild(Ui.Button($"Start Level {GameState.CurrentLevel + 1}", () =>
		{
			GameState.Save();
			GameSession.StartCampaign(GameState.CurrentLevel);
		}, "BigButton"));
		root.AddChild(footer);

		GameState.Changed += Refresh;
		Refresh();
		for (var i = 0; i < list.Count; i++)
			Ui.Reveal(list[i], 0.05f * i);
	}

	public override void _ExitTree() => GameState.Changed -= Refresh;

	void Refresh() => _points.Text = GameState.Points.ToString();

	Control Card(TurretDef def)
	{
		var panel = new PanelContainer { ThemeTypeVariation = "CardPanel" };
		var box = Ui.VBox(8);
		panel.AddChild(box);
		box.AddChild(new TurretPreview(def) { SizeFlagsHorizontal = SizeFlags.ShrinkCenter });
		var name = Ui.Label(def.Name.ToUpperInvariant(), "SmallHeader", HorizontalAlignment.Center);
		name.AddThemeColorOverride("font_color", def.Color);
		name.AddThemeFontSizeOverride("font_size", 15);
		box.AddChild(name);
		var desc = Ui.Label(def.Description, "DimLabel", HorizontalAlignment.Center);
		desc.AddThemeFontSizeOverride("font_size", 14);
		desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		desc.CustomMinimumSize = new Vector2(200, 54);
		box.AddChild(desc);
		foreach (var up in def.Upgrades)
		{
			var row = new UpgradeRow(def, up);
			row.Pressed += () => Buy(row);
			row.Focused += ShowDetails;
			box.AddChild(row);
		}
		return panel;
	}

	void Buy(UpgradeRow row)
	{
		if (GameState.Buy(row.Turret, row.Upgrade))
		{
			Audio.Ui("upgrade");
			row.Bought();
			GameState.Save();
		}
		else
			Audio.Play("error", "UI");
		ShowDetails(row);
	}

	void ShowDetails(UpgradeRow row)
	{
		var stats = GameState.TurretStats(row.Turret);
		var level = GameState.UpgradeLevel(row.Turret.Id, row.Upgrade.Id);
		var current = stats.GetValueOrDefault(row.Upgrade.Stat);
		var text = $"{row.Turret.Name} - {row.Upgrade.Label}: {row.Upgrade.Description}  Now: {Format(row.Upgrade.Stat, current)}";
		if (level < row.Upgrade.MaxLevel)
		{
			var limits = row.Turret.Limits.GetValueOrDefault(row.Upgrade.Stat) ?? new[] { float.MinValue, float.MaxValue };
			text += $"  ->  {Format(row.Upgrade.Stat, Mathf.Clamp(current + row.Upgrade.PerLevel, limits[0], limits[1]))}";
		}
		_details.Text = text;
		_details.AddThemeColorOverride("font_color", UiTheme.TextColor);
	}

	static string Format(string stat, float value) => stat switch
	{
		"cooldown" => $"{value:0.00}s between shots",
		"range" or "blast_radius" or "beam_length" => $"{Mathf.RoundToInt(value)} px",
		"projectile_speed" => $"{Mathf.RoundToInt(value)} px/s",
		"dps" => $"{value:0.0} damage/s",
		"slow_factor" => $"{Mathf.RoundToInt(value * 100)}% speed",
		"shots" => $"{(int)value} pellets",
		_ => $"{value:0.0}",
	};
}
