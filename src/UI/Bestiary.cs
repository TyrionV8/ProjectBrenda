using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.UI;

/// <summary>Reusable full-screen compendium. In pause it stays in the current match's tree.</summary>
public partial class Bestiary : Control
{
	public event Action Closed;
	VBoxContainer _entries;
	VBoxContainer _detail;
	public override void _Ready()
	{
		Name = "Bestiary";
		SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(Ui.Dim(0.98f));
		var panel = new PanelContainer { CustomMinimumSize = new Vector2(1120, 660) };
		AddChild(Ui.Centered(panel));
		var box = Ui.VBox(12);
		panel.AddChild(box);
		var header = Ui.HBox();
		var title = Ui.Label("BESTIARY", "HeaderLabel");
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(title);
		header.AddChild(Ui.Button("Back", Back));
		box.AddChild(header);
		var tabs = Ui.HBox();
		foreach (var (text, index) in new[] { ("Bugs", 0), ("Bosses", 1), ("Turrets", 2) })
		{
			var button = Ui.Button(text, () => ShowTab(index));
			button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			tabs.AddChild(button);
		}
		box.AddChild(tabs);
		var body = Ui.HBox(24);
		body.SizeFlagsVertical = SizeFlags.ExpandFill;
		box.AddChild(body);
		var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(330, 480), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		body.AddChild(scroll);
		_entries = Ui.VBox(6);
		_entries.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		scroll.AddChild(_entries);
		_detail = Ui.VBox(12);
		_detail.CustomMinimumSize = new Vector2(680, 480);
		_detail.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		body.AddChild(_detail);
		box.AddChild(Ui.Label("Discover bugs in a match. Build or unlock turrets to reveal them.", "DimLabel"));
		ShowTab(0);
	}

	public void Back()
	{
		if (Closed != null) Closed.Invoke();
		else Transition.Go(Game.GameSession.MenuScene);
	}

	static void Clear(Node parent)
	{
		foreach (var child in parent.GetChildren()) { parent.RemoveChild(child); child.QueueFree(); }
	}

	public void ShowTab(int tab)
	{
		Clear(_entries);
		if (tab == 2)
		{
			foreach (var turret in Catalog.Turrets)
				_entries.AddChild(Ui.Button(GameState.HasSeenTurret(turret.Id) ? turret.Name : "???", () => ShowEntry(null, turret)));
			ShowEntry(null, Catalog.Turrets[0]);
		}
		else
		{
			var enemies = Catalog.EnemyList.Where(e => e.Boss == (tab == 1)).ToArray();
			foreach (var enemy in enemies)
				_entries.AddChild(Ui.Button(GameState.SeenEnemies.Contains(enemy.Id) ? enemy.Name : "???", () => ShowEntry(enemy, null)));
			ShowEntry(enemies[0], null);
		}
	}

	void ShowEntry(EnemyDef enemy, TurretDef turret)
	{
		Clear(_detail);
		var known = enemy != null ? GameState.SeenEnemies.Contains(enemy.Id) : GameState.HasSeenTurret(turret.Id);
		var name = known ? enemy?.Name ?? turret.Name : "???";
		var heading = Ui.Label(name, "HeaderLabel", HorizontalAlignment.Center);
		heading.AddThemeFontSizeOverride("font_size", UiTheme.PixelNormal);
		_detail.AddChild(heading);
		_detail.AddChild(new BestiaryPortrait(enemy, turret, known) { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		var description = known ? enemy?.Blurb ?? turret?.Description ?? "A board intruder. Stop it before it reaches the CPU." : "Unidentified. Encounter this entry to reveal its name and attributes.";
		var text = Ui.Label(description, "BodyLabel");
		text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_detail.AddChild(text);
		if (!known) return;
		var stats = enemy != null ? EnemyAttributes(enemy) : TurretAttributes(turret);
		var attributes = Ui.Label(stats, "BodyLabel");
		attributes.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_detail.AddChild(attributes);
	}

	static string N(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
	public static string EnemyAttributes(EnemyDef enemy) =>
		$"HP: {N(enemy.MaxHp)}   |   Speed: {N(enemy.Speed)} px/s\nCPU damage: {enemy.CpuDamage}\n" +
		(enemy.DetourChance > 0 ? $"Detour chance: {N(enemy.DetourChance * 100)}%   |   Reach: {N(enemy.DetourRange)} cells\nChewing: {N(enemy.DetourChewTime)} s   |   Cooldown: {N(enemy.DetourCooldown)} s" : "Detours: never leaves the route");
	public static string TurretAttributes(TurretDef turret) =>
		$"Build cost: ${turret.Cost}   |   Base range: {N(turret.Stats.GetValueOrDefault(turret.NeedsDirection ? "beam_length" : "range"))} px\n" +
		(turret.UnlockPrice > 0 ? $"Permanent unlock: ${turret.UnlockPrice}" : "Available from the start");
}
