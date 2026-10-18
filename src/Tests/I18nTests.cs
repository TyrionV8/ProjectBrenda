using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Tests;

public sealed class I18nTests : TestSuite
{
	static SignalAwaiter Frame() => SceneTests.Tree.ToSignal(SceneTests.Tree, SceneTree.SignalName.ProcessFrame);
	static System.Collections.Generic.IEnumerable<Node> Nodes(Node root)
	{
		yield return root;
		foreach (var child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
	}

	[Test]
	public void CatalogKeysAndPlaceholderContractsAreComplete()
	{
		using var en = GD.Load<Translation>("res://translations/strings.en.translation");
		using var pt = GD.Load<Translation>("res://translations/strings.pt_BR.translation");
		using var csv = FileAccess.Open("res://translations/strings.csv", FileAccess.ModeFlags.Read);
		var header = csv.GetCsvLine();
		Check(header.SequenceEqual(new[] { "keys", "en", "pt_BR" }), "CSV header");
		var sourceRows = new System.Collections.Generic.List<string[]>();
		while (csv.GetPosition() < csv.GetLength())
		{
			var row = csv.GetCsvLine();
			if (row.Length == 1 && row[0] == "") continue;
			CheckEq(row.Length, 3, "three CSV columns");
			sourceRows.Add(row);
		}
		var keys = sourceRows.Select(row => row[0]).ToArray();
		CheckEq(keys.Distinct().Count(), keys.Length, "no duplicate CSV keys");
		Check(keys.Length > 300, "the full interface catalog is imported");

		foreach (var key in keys)
		{
			var english = en.GetMessage(key).ToString();
			var portuguese = pt.GetMessage(key).ToString();
			Check(!string.IsNullOrWhiteSpace(english) && !string.IsNullOrWhiteSpace(portuguese), "both values: " + key);
			string Slots(string text) => string.Join(",", Regex.Matches(text, @"\{\d+\}").Select(m => m.Value).Order());
			CheckEq(Slots(english), Slots(portuguese), "compatible placeholders: " + key);
		}
		void Has(string key) => Check(keys.Contains(key), "data translation exists: " + key);
		foreach (var turret in Catalog.Turrets)
		{
			Has($"TURRET_{turret.Id.ToUpperInvariant()}_NAME");
			Has($"TURRET_{turret.Id.ToUpperInvariant()}_DESCRIPTION");
			foreach (var upgrade in turret.Upgrades)
			{
				Has($"UPGRADE_{turret.Id.ToUpperInvariant()}_{upgrade.Id.ToUpperInvariant()}_NAME");
				Has($"UPGRADE_{turret.Id.ToUpperInvariant()}_{upgrade.Id.ToUpperInvariant()}_DESCRIPTION");
			}
		}
		foreach (var enemy in Catalog.EnemyList)
		{
			Has($"ENEMY_{enemy.Id.ToUpperInvariant()}_NAME");
			Has($"ENEMY_{enemy.Id.ToUpperInvariant()}_DESCRIPTION");
		}
		for (var i = 1; i <= Catalog.Levels.Count; i++) { Has($"LEVEL_{i}_TITLE"); Has($"LEVEL_{i}_SUBTITLE"); }
		foreach (var id in TutorialTips.Texts.Keys) Has("TIP_" + id.ToUpperInvariant());
	}

	[Test]
	public void DefaultPreferencePersistenceAndDynamicMessages()
	{
		var settings = Settings.Instance;
		var previous = settings.Language;
		var saved = FileAccess.FileExists("user://settings.cfg") ? FileAccess.GetFileAsBytes("user://settings.cfg") : null;
		try
		{
			CheckEq(Settings.DefaultLanguage("pt"), "pt_BR", "Portuguese system default");
			CheckEq(Settings.DefaultLanguage("en"), "en", "English system default");
			CheckEq(Settings.DefaultLanguage("fr"), "en", "other system default");
			var volume = settings.Volumes["Master"]; var tips = settings.Tips; var name = settings.PlayerName;
			settings.SetLanguage("pt_BR");
			CheckEq(Loc.S("SELECT LEVEL"), "ESCOLHER FASE", "Portuguese real translation");
			CheckEq(Loc.S("WAVE 3 CLEARED"), "ONDA 3 CONCLUÍDA", "dynamic banner");
			CheckEq(Loc.S("Unlocks in 12s"), "Disponível em 12s", "protocol message presentation");
			CheckEq(Loc.S("Connecting to Green..."), "Conectando a Green...", "host named like a translated color remains intact");
			CheckEq(Loc.S("Saved Red"), "Salvo: Red", "filename remains intact");
			const string waiting = "Waiting for the attacker...\nAsk them to join  Red:7777  or  Blue:7777";
			CheckEq(Loc.S(waiting), "Aguardando atacante...\nPeça que entrem em  Red:7777  ou  Blue:7777", "host separator translated while addresses remain literal");
			CheckEq(Loc.S("Connecting to ws://127.0.0.1:7777..."), "Conectando a ws://127.0.0.1:7777...", "address retained");
			Check(Loc.S(Bestiary.EnemyAttributes(Catalog.Enemy("bug"))).Contains("Dano à CPU"), "dynamic multiline attributes");
			CheckEq(Loc.S("3. WI-FI ROUTER"), "3. ROTEADOR WI-FI", "numbered data title");
			var cfg = new ConfigFile(); cfg.Load("user://settings.cfg");
			CheckEq((string)cfg.GetValue("interface", "language"), "pt_BR", "preference saved");
			settings.SetLanguage("en", persist: false);
			typeof(Settings).GetMethod("LoadFile", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(settings, null);
			CheckEq(settings.Language, "pt_BR", "saved preference loaded");
			settings.SetLanguage("en", persist: false);
			CheckEq(Loc.S("SELECT LEVEL"), "SELECT LEVEL", "switch to English");
			CheckEq(Loc.S(waiting), waiting, "English host separator unchanged");
			CheckEq(settings.Volumes["Master"], volume, "volume preserved"); CheckEq(settings.Tips, tips, "tips preserved"); CheckEq(settings.PlayerName, name, "network name preserved");
		}
		finally
		{
			settings.SetLanguage(previous, persist: false);
			if (saved == null) DirAccess.RemoveAbsolute("user://settings.cfg");
			else { using var file = FileAccess.Open("user://settings.cfg", FileAccess.ModeFlags.Write); file.StoreBuffer(saved); }
		}
	}

	[Test]
	public async Task LanguageOptionUpdatesOpenPausedMatchBestiaryAndTooltipsInPlace()
	{
		using var isolated = GameState.UseTemporarySave();
		var settings = Settings.Instance; var previous = settings.Language;
		var saved = FileAccess.FileExists("user://settings.cfg") ? FileAccess.GetFileAsBytes("user://settings.cfg") : null;
		settings.SetLanguage("en", persist: false);
		GameState.Reset(); GameState.Balance = 321; GameState.SeeEnemy("bug");
		foreach (var turret in Catalog.Turrets) { GameState.UnlockedTurrets.Add(turret.Id); GameState.SeeTurret(turret.Id); }
		GameSession.Current = GameSession.Kind.Custom;
		var feed = new LocalFeed(Catalog.Levels[0], MatchMode.Campaign, PlayerSide.Defender, false);
		GameSession.CreateFeed = () => feed;
		var screen = GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
		SceneTests.Tree.Root.AddChild(screen); await Frame();
		try
		{
			screen.OpenMenu();
			var pause = screen.GetChildren().OfType<PauseMenu>().Single();
			pause.OpenBestiary(); await Frame();
			var bestiary = Nodes(pause).OfType<Bestiary>().Single(); bestiary.ShowTab(2);
			var option = Nodes(pause).OfType<OptionButton>().Single(n => n.Name == "LanguageOption");
			var time = feed.Match.Time; var money = feed.Match.Money; var originalScene = SceneTests.Tree.CurrentScene;
			option.Selected = 1; option.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
			await Frame();
			CheckEq(settings.Language, "pt_BR", "actual option applies preference");
			Check(ReferenceEquals(screen.Feed, feed) && ReferenceEquals(originalScene, SceneTests.Tree.CurrentScene), "same feed and scene retained");
			Check(SceneTests.Tree.Paused, "local pause retained"); CheckNear(feed.Match.Time, time, "paused time unchanged"); CheckEq(feed.Match.Money, money, "match money unchanged");
			CheckEq(GameState.Balance, 321, "campaign balance retained"); Check(GameState.SeenEnemies.Contains("bug"), "discovery retained");
			var labels = Nodes(bestiary).OfType<Label>().ToArray();
			Check(labels.Any(n => n.Text == "BESTIÁRIO"), "already open bestiary title updates");
			var entries = Nodes(bestiary).OfType<Button>().Select(n => n.Text).ToArray();
			foreach (var turret in Catalog.Turrets) Check(entries.Contains(Loc.S(turret.Name)), "Portuguese turret entry " + turret.Id);
			var slots = Nodes(screen.Hud).OfType<TurretSlot>().ToArray();
			CheckEq(slots.Length, 9, "all nine turret slots retained");
			Check(slots.Single(n => n.Def.Id == "technician").TooltipText.StartsWith("Robô técnico\n"), "existing tooltip updates");
			bestiary.ShowTab(0); await Frame();
			Check(Nodes(bestiary).OfType<Label>().Any(n => n.Text.Contains("Dano à CPU")), "selection details translated");
			option.Selected = 0; option.EmitSignal(OptionButton.SignalName.ItemSelected, 0L); await Frame();
			Check(Nodes(bestiary).OfType<Label>().Any(n => n.Text == "BESTIARY"), "same bestiary switches back");
			Check(ReferenceEquals(screen.Feed, feed) && SceneTests.Tree.Paused, "second switch keeps match and pause");
		}
		finally
		{
			SceneTests.Tree.Paused = false; screen.QueueFree(); await Frame(); settings.SetLanguage(previous, persist: false);
			if (saved == null) DirAccess.RemoveAbsolute("user://settings.cfg");
			else { using var file = FileAccess.Open("user://settings.cfg", FileAccess.ModeFlags.Write); file.StoreBuffer(saved); }
		}
	}
	[Test]
	public async Task OfficialNetworkCloneTranslatesWhileCustomTitlesRemainLiteral()
	{
		using var isolated = GameState.UseTemporarySave();
		var previous = Settings.Instance.Language;
		Settings.Instance.SetLanguage("pt_BR", persist: false);
		try
		{
			foreach (var custom in new[] { false, true })
			{
				var level = Catalog.Levels[2].Clone();
				if (custom) { level.Title = "Red"; level.Subtitle = "Blue"; }
				GameSession.Current = GameSession.Kind.Versus;
				GameSession.CreateFeed = () => new LocalFeed(level, MatchMode.Versus, PlayerSide.Attacker, false);
				var screen = GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
				SceneTests.Tree.Root.AddChild(screen); await Frame();
				try
				{
					var labels = Nodes(screen.Hud).OfType<Label>().Select(label => label.Text).ToArray();
					Check(labels.Contains(custom ? "RED" : "ROTEADOR WI-FI"), "initial title from deserialized official/custom board");
					Check(labels.Contains(custom ? "Blue" : "Pacotes interrompidos"), "initial subtitle from deserialized official/custom board");
				}
				finally { screen.QueueFree(); await Frame(); }
			}
		}
		finally { Settings.Instance.SetLanguage(previous, persist: false); }
	}

	[Test]
	public async Task PortugueseEditorKeepsAllTwentyFiveWaveTabsAndNativeControlsAccessible()
	{
		var previous = Settings.Instance.Language;
		Settings.Instance.SetLanguage("pt_BR", persist: false);
		HardwareDefence.Editor.EditorState.Load(Catalog.Levels[7].Clone(), string.Format(Catalog.LevelPath, 8));
		var editor = new HardwareDefence.Editor.LevelEditor();
		SceneTests.Tree.Root.AddChild(editor); await Frame();
		try
		{
			typeof(HardwareDefence.Editor.LevelEditor).GetMethod("SetMode", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { true });
			await Frame(); await Frame();
			foreach (var source in new[] { "MAP", "WAVES" })
			{
				var button = Nodes(editor).OfType<Button>().Single(node => node.HasMeta("i18n_source") && node.GetMeta("i18n_source").AsString() == source);
				var width = button.GetThemeFont("font").GetStringSize(button.Text, fontSize: button.GetThemeFontSize("font_size")).X;
				Check(width <= button.Size.X - 6 + 1, source + " stays a whole word at native font size");
			}
			var scroll = Nodes(editor).OfType<ScrollContainer>().Single(node => node.Name == "WaveTabScroll");
			var tabs = Nodes(scroll).OfType<Button>().ToArray();
			CheckEq(tabs.Length, 26, "25 wave tabs plus add control");
			Check(scroll.GetGlobalRect().End.X <= Grid.Origin.X + Grid.Size.X + 1, "header stays within board");
			var add = tabs.Last(); scroll.EnsureControlVisible(add); await Frame();
			Check(add.GetGlobalRect().End.X <= scroll.GetGlobalRect().End.X + 1, "last wave/add accessible by scrolling");
			foreach (var source in new[] { "Save", "TEST", "Exit" })
			{
				var button = Nodes(editor).OfType<Button>().Single(node => node.HasMeta("i18n_source") && node.GetMeta("i18n_source").AsString() == source);
				var rect = button.GetGlobalRect();
				Check(rect.End.X <= Grid.Origin.X + 1 && rect.End.Y <= 768, source + " fits native left column: " + rect);
			}
		}
		finally { editor.QueueFree(); await Frame(); Settings.Instance.SetLanguage(previous, persist: false); }
	}

}
