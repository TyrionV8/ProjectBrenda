using System;
using System.Linq;
using System.Threading.Tasks;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Tests;

public sealed class BestiaryTests : TestSuite
{
	static SignalAwaiter Frame() => SceneTests.Tree.ToSignal(SceneTests.Tree, SceneTree.SignalName.ProcessFrame);

	[Test]
	public void DiscoveriesRoundTripOnceAndOldSavesKeepEconomy()
	{
		using var isolated = GameState.UseTemporarySave();
		GameState.Reset();
		var changes = 0;
		void Changed() => changes++;
		GameState.Changed += Changed;
		try
		{
			Check(GameState.SeeEnemy("bug"), "first encounter recorded");
			Check(!GameState.SeeEnemy("bug"), "repeat encounter ignored");
			Check(!GameState.SeeEnemy("unknown_test"), "invalid discovery rejected");
			Check(GameState.SeeTurret("blaster"), "construction recorded");
			Check(GameState.MarkTipShown(TutorialTips.Wave), "first display recorded");
			Check(!GameState.MarkTipShown(TutorialTips.Wave), "repeat display ignored");
			CheckEq(changes, 3, "one save notification per new discovery/display");
		}
		finally { GameState.Changed -= Changed; }
		GameState.Reset();
		Check(GameState.Load(), "reload active isolated file");
		Check(GameState.SeenEnemies.SetEquals(new[] { "bug" }), "enemy persisted");
		Check(GameState.SeenTurrets.Contains("blaster"), "turret persisted");
		Check(GameState.ShownTips.Contains(TutorialTips.Wave), "tip persisted");
		const string legacy = "user://bestiary-legacy-test.json";
		try
		{
			JsonIO.Save(legacy, new { version = 2, points = 4, stars = new[] { 2 }, upgrades = new { blaster = new { damage = 3 } } });
			Check(GameState.Load(legacy), "v2 defaults load");
			CheckEq(GameState.Balance, 80, "point migration unchanged");
			CheckEq(GameState.UpgradeLevel("blaster", "damage"), 3, "upgrades preserved");
			CheckEq(GameState.Stars[0], 2, "stars preserved");
			CheckEq(GameState.SeenEnemies.Count, 0, "missing enemies default empty");
			CheckEq(GameState.ShownTips.Count, 0, "missing tips default empty");
			Check(GameState.HasSeenTurret("blaster"), "previous purchase is a discovery");
			JsonIO.Save(legacy, new { version = 3, balance = 123, unlocked_turrets = new[] { "firewall" }, best_remaining_money = new[] { 70 } });
			Check(GameState.Load(legacy), "earlier v3 loads without discovery fields");
			Check(GameState.HasSeenTurret("firewall"), "existing unlock discovered");
			CheckEq(GameState.Balance, 123, "balance unaffected");
			CheckEq(GameState.BestRemainingMoney[0], 70, "record unaffected");
		}
		finally { DirAccess.RemoveAbsolute(legacy); }
	}

	[Test]
	public async Task ColdStartVersusDiscoversWithoutOverwritingCampaign()
	{
		using var isolated = GameState.UseTemporarySave();
		GameState.Reset();
		GameState.Balance = 321;
		GameState.Upgrades["blaster"] = new() { ["damage"] = 2 };
		GameState.UnlockedTurrets.Add("firewall");
		GameState.BestRemainingMoney[0] = 77;
		GameState.Stars[0] = 3;
		GameState.Save();
		// Reproduce a fresh process: initial static defaults, with a pre-existing campaign file.
		GameState.Reset();
		var loaded = typeof(GameState).GetField("_loadedForDiscoveries", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
		loaded.SetValue(null, false);
		GameSession.Current = GameSession.Kind.Versus;
		LocalFeed feed = null;
		GameSession.CreateFeed = () => feed = new LocalFeed(Catalog.Levels[0], MatchMode.Versus, PlayerSide.Defender, false);
		var screen = GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
		SceneTests.Tree.Root.AddChild(screen);
		try
		{
			await Frame();
			feed.Match.SpawnBug("bug", 0);
			feed.Match.Money = 200;
			var cell = Enumerable.Range(0, Grid.Cols).SelectMany(x => Enumerable.Range(0, Grid.Rows).Select(y => new Vector2I(x, y))).First(feed.Match.CanBuild);
			feed.Match.Build(Catalog.Turret("blaster"), cell);
			await Frame(); await Frame();
			Check(GameState.SeenEnemies.Contains("bug") && GameState.SeenTurrets.Contains("blaster"), "real view discovers entities after cold startup");
			GameState.Reset();
			Check(GameState.Load(), "reload automatic discovery save");
			CheckEq(GameState.Balance, 321, "existing balance survives");
			CheckEq(GameState.UpgradeLevel("blaster", "damage"), 2, "existing upgrades survive");
			Check(GameState.UnlockedTurrets.Contains("firewall"), "existing unlock survives");
			CheckEq(GameState.BestRemainingMoney[0], 77, "cash record survives");
			CheckEq(GameState.Stars[0], 3, "stars survive");
		}
		finally { screen.QueueFree(); await Frame(); }
	}

	[Test]
	public async Task TipsConsumeOnlyOnDisplayAndRespectOption()
	{
		using var isolated = GameState.UseTemporarySave();
		GameState.Reset();
		var previous = Settings.Instance.Tips;
		var tips = new TutorialTips { ProcessMode = Node.ProcessModeEnum.Disabled };
		SceneTests.Tree.Root.AddChild(tips);
		try
		{
			Settings.Instance.Tips = false;
			tips.Enqueue(TutorialTips.Wave);
			tips._Process(10);
			Check(!tips.Visible && !GameState.ShownTips.Contains(TutorialTips.Wave), "off does not display or consume");
			Settings.Instance.Tips = true;
			tips.Enqueue(TutorialTips.Wave);
			tips.Enqueue(TutorialTips.Wave);
			Check(!GameState.ShownTips.Contains(TutorialTips.Wave), "queue does not consume");
			tips._Process(10);
			Check(tips.Visible && tips.CurrentId == TutorialTips.Wave, "first tip displayed");
			Check(GameState.ShownTips.Contains(TutorialTips.Wave), "display consumed");
			tips._Process(10);
			Check(!tips.Visible, "auto dismissed");
			tips.Enqueue(TutorialTips.Wave);
			tips._Process(10);
			Check(!tips.Visible, "displayed tip never repeated");
			tips.Enqueue(TutorialTips.Detour);
			tips._Process(1);
			Check(tips.Visible, "next contextual tip displayed");
			Settings.Instance.Tips = false;
			tips._Process(0);
			Check(!tips.Visible, "switching off dismisses active tip");
		}
		finally { Settings.Instance.Tips = previous; tips.QueueFree(); await Frame(); }
	}

	sealed class OnlineLikeFeed : LocalFeed
	{
		public OnlineLikeFeed() : base(Catalog.Levels[0], MatchMode.Versus, PlayerSide.Defender, false) { }
		public override bool IsLocalOnly => false;
	}

	[Test]
	public async Task PauseBestiaryReturnsToSameMatchAndOnlineKeepsRunning()
	{
		using var isolated = GameState.UseTemporarySave();
		GameState.Reset();
		foreach (var online in new[] { false, true })
		{
			GameState.ShownTips.Clear();
			GameSession.Current = online ? GameSession.Kind.Versus : GameSession.Kind.Custom;
			LocalFeed feed = online ? new OnlineLikeFeed() : new LocalFeed(Catalog.Levels[0], MatchMode.Campaign, PlayerSide.Defender, false);
			GameSession.CreateFeed = () => feed;
			var screen = GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
			SceneTests.Tree.Root.AddChild(screen);
			await Frame();
			try
			{
				screen.OpenMenu();
				var pause = screen.GetChildren().OfType<PauseMenu>().Single();
				var tips = screen.Hud.GetChildren().OfType<TutorialTips>().Single();
				tips.Enqueue(TutorialTips.Wave);
				var before = feed.Match.Time;
				pause.OpenBestiary();
				for (var i = 0; i < 5; i++) await Frame();
				tips._Process(20);
				Check(!GameState.ShownTips.Contains(TutorialTips.Wave), "covered tip is not consumed");
				CheckEq(SceneTests.Tree.Paused, !online, "correct pause mode");
				Check(ReferenceEquals(screen.Feed, feed), "same feed remains");
				var bestiary = pause.FindChildren("*", "Control", true, false).OfType<Bestiary>().Single();
				bestiary.ShowTab(1);
				bestiary.ShowTab(2);
				bestiary.Back();
				await Frame();
				Check(pause.Visible && SceneTests.Tree.Paused == !online, "back restores menu without resuming campaign");
				Check(!pause.FindChildren("*", "Control", true, false).OfType<Bestiary>().Any(), "overlay freed on back");
				if (online) Check(feed.Match.Time > before, "online match progresses under bestiary");
				else CheckNear(feed.Match.Time, before, "solo match stays frozen");
				pause.Close();
				await Frame();
				tips._Process(4);
				Check(tips.Visible && GameState.ShownTips.Contains(TutorialTips.Wave), "tip becomes legible after resume");
				screen.OpenMenu();
				tips._Process(20);
				Check(tips.Visible, "covered active tip keeps its remaining time");
				pause.Close();
				Check(!SceneTests.Tree.Paused, "resume releases pause");
				Check(ReferenceEquals(screen.Feed, feed), "resume keeps same match");
			}
			finally { SceneTests.Tree.Paused = false; screen.QueueFree(); await Frame(); }
		}
	}

	[Test]
	public void BestiaryUsesCatalogStatsAndEveryBossHasResponse()
	{
		foreach (var enemy in Catalog.EnemyList)
		{
			Check(Bestiary.EnemyAttributes(enemy).Contains($"damage: {enemy.CpuDamage}"), "CPU damage read from data");
			if (enemy.Boss) Check(TutorialTips.Texts.ContainsKey("boss_" + enemy.Ability), $"{enemy.Id} response exists");
		}
		foreach (var turret in Catalog.Turrets)
			Check(Bestiary.TurretAttributes(turret).Contains($"${turret.Cost}"), "construction cost read from data");
	}
}
