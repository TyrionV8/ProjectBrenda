using System.Collections.Generic;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Spend campaign money between levels, then start the next level.</summary>
public partial class UpgradeScreen : Control
{
	Label _balance;
	readonly List<(TurretDef Def, TurretPreview Preview, Button Unlock)> _cards = new();
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
		title.AddThemeFontSizeOverride("font_size", UiTheme.PixelLarge);
		header.AddChild(title);
		var next = Ui.Label($"next: {GameState.CurrentLevel + 1}. {level.Title.ToUpperInvariant()}", "HeaderLabel");
		next.AddThemeColorOverride("font_color", UiTheme.Dim);
		next.AddThemeFontSizeOverride("font_size", UiTheme.PixelSmall);
		next.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		next.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(next);
		var pointsBox = new PanelContainer { ThemeTypeVariation = "CardPanel" };
		var pointsRow = Ui.HBox(10);
		pointsBox.AddChild(pointsRow);
		pointsRow.AddChild(new TextureRect
		{
			Texture = GD.Load<Texture2D>("res://assets/sprites/icons/chip.png"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			CustomMinimumSize = new Vector2(30, 30), Modulate = UiTheme.Gold, TextureFilter = TextureFilterEnum.Nearest,
			StretchMode = TextureRect.StretchModeEnum.Scale, SizeFlagsVertical = SizeFlags.ShrinkCenter,
		});
		var col = Ui.VBox(0);
		col.AddChild(Ui.Label("BALANCE", "SmallHeader"));
		_balance = Ui.Label("$0", "MoneyLabel");
		col.AddChild(_balance);
		pointsRow.AddChild(col);
		header.AddChild(pointsBox);
		root.AddChild(header);

		var cards = new GridContainer { Columns = 3 };
		cards.AddThemeConstantOverride("h_separation", 14);
		cards.AddThemeConstantOverride("v_separation", 14);
		cards.SizeFlagsVertical = SizeFlags.ExpandFill;
		var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
		cards.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(cards);
		root.AddChild(scroll);
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
		_details = Ui.Label("Spend your best remaining cash from each campaign level. Click + to buy.", "DimLabel");
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

	void Refresh()
	{
		_balance.Text = $"${GameState.Balance}";
		foreach (var (def, preview, unlock) in _cards)
		{
			var available = GameState.IsTurretAvailable(def);
			preview.Modulate = available ? Colors.White : new Color(0.28f, 0.32f, 0.36f);
			unlock.Visible = !available;
			unlock.Disabled = !GameState.CanUnlock(def);
		}
	}

	Control Card(TurretDef def)
	{
		var panel = new PanelContainer { ThemeTypeVariation = "CardPanel", CustomMinimumSize = new Vector2(400, 0) };
		var box = Ui.VBox(8);
		panel.AddChild(box);
		var intro = Ui.HBox(10);
		var preview = new TurretPreview(def) { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
		intro.AddChild(preview);
		var info = Ui.VBox(8);
		info.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		intro.AddChild(info);
		box.AddChild(intro);
		var name = Ui.Label(def.Name.ToUpperInvariant(), "SmallHeader", HorizontalAlignment.Center);
		name.AddThemeColorOverride("font_color", def.Color);
		name.AddThemeFontSizeOverride("font_size", UiTheme.PixelSmall);
		name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		info.AddChild(name);
		var desc = Ui.Label(def.Description, "DimLabel", HorizontalAlignment.Center);
		desc.AddThemeFontSizeOverride("font_size", UiTheme.BodySize);
		desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		desc.CustomMinimumSize = new Vector2(240, 100);
		info.AddChild(desc);
		var unlock = Ui.Button($"Unlock ${def.UnlockPrice}", () =>
		{
			if (GameState.Unlock(def)) Audio.Ui("upgrade");
			else Audio.Play("error", "UI");
		});
		unlock.TooltipText = $"Locked turret. Buy once for ${def.UnlockPrice}; upgrades are sold separately.";
		box.AddChild(unlock);
		_cards.Add((def, preview, unlock));
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
			text += $"  ->  {Format(row.Upgrade.Stat, Mathf.Clamp(current + row.Upgrade.PerLevel, limits[0], limits[1]))}  |  ${GameState.NextUpgradePrice(row.Turret, row.Upgrade)}";
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
