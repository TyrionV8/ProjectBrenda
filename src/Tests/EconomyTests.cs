using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Tests;

public sealed class EconomyTests : TestSuite
{
	/// <summary>Keep the existing save byte-for-byte, even if an assertion throws.</summary>
	public sealed class SaveScope : IDisposable
	{
		readonly string _original = FileAccess.FileExists(GameState.SavePath) ? FileAccess.GetFileAsString(GameState.SavePath) : null;
		public void Dispose()
		{
			if (_original == null) DirAccess.RemoveAbsolute(GameState.SavePath);
			else
			{
				using var file = FileAccess.Open(GameState.SavePath, FileAccess.ModeFlags.Write);
				file.StoreString(_original);
			}
			GameState.Reset();
		}
	}

	static TurretDef LockedTurret(string id = "economy_test") => new()
	{
		Id = id, Name = "Test turret", Description = "Economy-only locked turret fixture.", UnlockPrice = 80,
		Icon = Catalog.Turrets[0].Icon,
		Upgrades = new() { new() { Id = "power", Label = "Power", Description = "Test", Stat = "damage", Price = 20, PriceStep = 10, MaxLevel = 2, Icon = Catalog.Turrets[0].Upgrades[0].Icon } },
	};

	[Test]
	public void BestRemainingCashCreditsOnlyImprovementIncludingLastLevel()
	{
		using var save = new SaveScope();
		GameState.Reset();
		GameState.CompleteCurrentLevel(2, 90);
		CheckEq(GameState.Balance, 90, "first victory credits remaining money");
		var turret = Catalog.Turrets[0];
		var purchase = turret.Upgrades[0];
		Check(GameState.Buy(turret, purchase), "spend part of first credit");
		var spent = purchase.Price;
		GameState.CurrentLevel = 0;
		GameState.CompleteCurrentLevel(1, 50);
		CheckEq(GameState.Balance, 90 - spent, "worse replay does not refill spent balance");
		GameState.CurrentLevel = 0;
		GameState.CompleteCurrentLevel(3, 120);
		CheckEq(GameState.Balance, 120 - spent, "better replay credits difference only");
		CheckEq(GameState.BestRemainingMoney[0], 120, "record updated");
		CheckEq(GameState.Stars[0], 3, "best stars independent of cash");
		GameState.CurrentLevel = Catalog.Levels.Count - 1;
		Check(GameState.CompleteCurrentLevel(1, 45), "last level finishes campaign");
		CheckEq(GameState.Balance, 165 - spent, "last level also credits money");
		GameState.Reset();
		Check(GameState.Load(), "read saved result");
		CheckEq(GameState.BestRemainingMoney[^1], 45, "last record persisted");
		CheckEq(GameState.Balance, 165 - spent, "last credit persisted");
		CheckEq(new Match(Catalog.Levels[0]).Money, Catalog.Levels[0].StartMoney, "campaign money remains start_money, independent of upgrade balance");
	}

	[Test]
	public void PurchasesRespectUnlockPricesBalanceMaximumAndPersistBeforeNotification()
	{
		using var save = new SaveScope();
		GameState.Reset();
		var turret = LockedTurret();
		var up = turret.Upgrades[0];
		Check(GameState.IsTurretAvailable(Catalog.Turrets[0]), "no unlock_price means available");
		Check(!GameState.IsTurretAvailable(turret), "priced turret starts locked");
		CheckEq(GameState.NextUpgradePrice(turret, up), 20, "first price");
		GameState.Balance = 79;
		GameState.Save();
		var original = FileAccess.GetFileAsString(GameState.SavePath);
		Check(!GameState.Unlock(turret), "insufficient unlock balance rejected");
		Check(!GameState.Buy(turret, up), "locked turret cannot upgrade even with upgrade money");
		CheckEq(FileAccess.GetFileAsString(GameState.SavePath), original, "rejected operations leave file untouched");
		CheckEq(GameState.Balance, 79, "rejected operations leave balance untouched");
		CheckEq(GameState.Upgrades.Count, 0, "rejected operations create no upgrade entries");
		GameState.Balance = 130;
		var notifications = 0;
		void Observe()
		{
			notifications++;
			using var doc = JsonDocument.Parse(FileAccess.GetFileAsString(GameState.SavePath));
			CheckEq(doc.RootElement.GetProperty("balance").GetInt32(), GameState.Balance, "notification sees persisted balance");
			Check(doc.RootElement.GetProperty("unlocked_turrets").EnumerateArray().Any(v => v.GetString() == turret.Id), "notification sees persisted unlock");
		}
		GameState.Changed += Observe;
		try
		{
			Check(GameState.Unlock(turret), "unlock with exact price component");
			CheckEq(GameState.Balance, 50, "unlock deducted once");
			Check(!GameState.Unlock(turret), "cannot buy unlock twice");
			Check(GameState.Buy(turret, up), "buy level one");
			CheckEq(GameState.NextUpgradePrice(turret, up), 30, "price increases by step");
			Check(GameState.Buy(turret, up), "buy level two using exact remaining balance");
			CheckEq(GameState.Balance, 0, "exact price deducted");
			GameState.Balance = 100;
			Check(!GameState.Buy(turret, up), "max level blocks purchase with ample balance");
			CheckEq(GameState.NextUpgradePrice(turret, up), 0, "max has no next price");
			CheckEq(notifications, 3, "only successful transactions notify");
		}
		finally { GameState.Changed -= Observe; }
		GameState.Reset();
		Check(GameState.Load(), "automatic purchases persist");
		Check(GameState.IsTurretAvailable(turret), "unlock survives restart");
		CheckEq(GameState.UpgradeLevel(turret.Id, up.Id), 2, "upgrades survive restart");
		CheckEq(GameState.Balance, 0, "last successful balance saved");
		var cheap = Catalog.Turrets[0].Upgrades.OrderBy(u => u.Price).First();
		GameState.Balance = cheap.Price - 1;
		original = FileAccess.GetFileAsString(GameState.SavePath);
		Check(!GameState.Buy(Catalog.Turrets[0], cheap), "insufficient available upgrade rejected");
		CheckEq(GameState.Balance, cheap.Price - 1, "insufficient upgrade leaves balance");
		CheckEq(FileAccess.GetFileAsString(GameState.SavePath), original, "insufficient upgrade leaves save");
	}

	[Test]
	public void RealVersionTwoFileMigratesOnceAndRoundTripsAsVersionThree()
	{
		using var save = new SaveScope();
		var path = $"user://economy-migration-{Guid.NewGuid():N}.json";
		try
		{
			using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
				file.StoreString("{\"version\":2,\"unlocked\":[true,true,false],\"stars\":[3,2,0],\"upgrades\":{\"blaster\":{\"damage\":4}},\"points\":7}");
			GameState.Reset();
			var events = 0;
			void Loaded() { events++; CheckEq(GameState.Balance, 140, "load event sees fully migrated balance"); CheckEq(GameState.UpgradeLevel("blaster", "damage"), 4, "load event sees upgrades"); }
			GameState.Changed += Loaded;
			try { Check(GameState.Load(path), "load physical v2 file"); }
			finally { GameState.Changed -= Loaded; }
			CheckEq(events, 1, "one fully loaded event");
			CheckEq(GameState.Balance, 7 * GameState.LegacyPointValue, "legacy points converted");
			CheckEq(GameState.Stars[1], 2, "stars preserved");
			Check(GameState.IsUnlocked(1) && !GameState.IsUnlocked(2), "level unlocks preserved");
			CheckEq(GameState.UpgradeLevel("blaster", "damage"), 4, "existing upgrades preserved");
			Check(GameState.BestRemainingMoney.All(value => value == 0), "legacy record list starts empty");
			Check(GameState.Save(path), "write v3 to temporary file");
			using (var doc = JsonDocument.Parse(FileAccess.GetFileAsString(path)))
				CheckEq(doc.RootElement.GetProperty("version").GetInt32(), 3, "save upgraded to version 3");
			GameState.Reset();
			Check(GameState.Load(path), "reload v3");
			CheckEq(GameState.Balance, 140, "v3 reload does not multiply again");
			GameState.BestRemainingMoney[1] = 75;
			GameState.UnlockedTurrets.Add("economy_test");
			GameState.Save(path);
			GameState.Reset();
			GameState.Load(path);
			CheckEq(GameState.BestRemainingMoney[1], 75, "v3 best cash round trip");
			Check(GameState.IsTurretAvailable(LockedTurret()), "v3 unlocked turrets round trip");
			CheckEq(GameState.UpgradeLevel("blaster", "damage"), 4, "v3 upgrades round trip");
		}
		finally { DirAccess.RemoveAbsolute(path); DirAccess.RemoveAbsolute(path + ".tmp"); }
	}

	[Test]
	public void CapturePersistenceContextNeverTouchesTheDefaultSaveAndRestoresItsRoute()
	{
		using var save = new SaveScope();
		GameState.Reset();
		GameState.Balance = 123;
		GameState.Save();
		var original = FileAccess.GetFileAsString(GameState.SavePath);
		try
		{
			using var captureSave = GameState.UseTemporarySave();
			Check(!GameState.HasSave(), "capture starts with a separate empty file");
			GameState.Reset();
			GameState.Balance = 420;
			Check(GameState.Buy(Catalog.Turrets[0], Catalog.Turrets[0].Upgrades[0]), "capture buys using disposable route");
			Check(GameState.HasSave(), "purchase wrote capture file");
			GameState.CompleteCurrentLevel(3, 70);
			CheckEq(FileAccess.GetFileAsString(GameState.SavePath), original, "capture purchase and victory leave default bytes untouched");
			GameState.Reset();
			Check(GameState.Load(), "load follows disposable route");
			CheckEq(GameState.Balance, 420 - Catalog.Turrets[0].Upgrades[0].Price + 70, "capture loads its own result");
			throw new InvalidOperationException("simulated capture failure");
		}
		catch (InvalidOperationException e) when (e.Message == "simulated capture failure") { }
		CheckEq(FileAccess.GetFileAsString(GameState.SavePath), original, "exception restores route without rewriting original");
		GameState.Reset();
		Check(GameState.Load(), "default route restored after capture failure");
		CheckEq(GameState.Balance, 123, "default campaign was preserved");
	}

	[Test]
	public void CatalogHasPositiveProgressivePrices()
	{
		GameState.Reset();
		foreach (var turret in Catalog.Turrets)
			foreach (var up in turret.Upgrades)
			{
				Check(up.Price > 0 && up.PriceStep > 0, $"{turret.Id}/{up.Id}: explicit positive prices");
				CheckEq(GameState.NextUpgradePrice(turret, up), up.Price, "base price");
				GameState.Upgrades[turret.Id] = new() { [up.Id] = 2 };
				CheckEq(GameState.NextUpgradePrice(turret, up), up.Price + 2 * up.PriceStep, "third level price");
				GameState.Upgrades.Clear();
			}
		GameState.Reset();
	}

	[Test]
	public async Task NineTurretGridIncludesLockedFixturesAndKeepsPreviewSize()
	{
		using var save = new SaveScope();
		GameState.Reset();
		var fixtures = (List<TurretDef>)Catalog.Turrets;
		var count = fixtures.Count;
		UpgradeScreen screen = null;
		try
		{
			for (var i = count; i < 9; i++) fixtures.Add(LockedTurret($"economy_test_{i}"));
			screen = GD.Load<PackedScene>("res://scenes/upgrade_screen.tscn").Instantiate<UpgradeScreen>();
			SceneTests.Tree.Root.AddChild(screen);
			await SceneTests.Tree.ToSignal(SceneTests.Tree, SceneTree.SignalName.ProcessFrame);
			var grids = screen.FindChildren("*", "GridContainer", true, false).Cast<GridContainer>().ToArray();
			Check(grids.Any(grid => grid.Columns == 3 && grid.GetChildCount() == 9), "scrollable grid contains all nine cards");
			foreach (var preview in screen.FindChildren("*", "Control", true, false).OfType<TurretPreview>())
				CheckEq(preview.CustomMinimumSize, new Vector2(110, 110), "preview unchanged");
			var unlocks = screen.FindChildren("*", "Button", true, false).Cast<Button>().Where(button => button.Text.StartsWith("Unlock")).ToArray();
			CheckEq(unlocks.Count(button => button.Visible), Catalog.Turrets.Count(t => t.UnlockPrice > 0), "all four real counter turrets show unlock button");
			Check(unlocks.Where(button => button.Visible).All(button => button.Disabled), "unlock disabled without balance");
		}
		finally
		{
			screen?.QueueFree();
			await SceneTests.Tree.ToSignal(SceneTests.Tree, SceneTree.SignalName.ProcessFrame);
			fixtures.RemoveRange(count, fixtures.Count - count);
		}
	}

	[Test]
	public async Task DefeatCustomAndVersusResultsNeverCreditCampaign()
	{
		using var save = new SaveScope();
		GameState.Reset();
		GameState.Balance = 17;
		GameState.Save();
		var original = FileAccess.GetFileAsString(GameState.SavePath);
		foreach (var (kind, mode, state) in new[]
		{
			(GameSession.Kind.Campaign, MatchMode.Campaign, MatchState.AttackerWon),
			(GameSession.Kind.Custom, MatchMode.Campaign, MatchState.DefenderWon),
			(GameSession.Kind.TestPlay, MatchMode.Campaign, MatchState.DefenderWon),
			(GameSession.Kind.Versus, MatchMode.Versus, MatchState.DefenderWon),
		})
		{
			GameSession.Current = kind;
			GameSession.CreateFeed = () =>
			{
				var feed = new LocalFeed(Catalog.Levels[0], mode, PlayerSide.Defender, false);
				feed.Match.Money = 200;
				feed.Match.State = state;
				return feed;
			};
			var screen = GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
			SceneTests.Tree.Root.AddChild(screen);
			try
			{
				await SceneTests.Tree.ToSignal(SceneTests.Tree.CreateTimer(1.7), SceneTreeTimer.SignalName.Timeout);
				CheckEq(GameState.Balance, 17, $"{kind}/{state}: balance unchanged");
				CheckEq(GameState.BestRemainingMoney[0], 0, $"{kind}/{state}: no record");
				CheckEq(FileAccess.GetFileAsString(GameState.SavePath), original, $"{kind}/{state}: file unchanged");
			}
			finally { screen.QueueFree(); await SceneTests.Tree.ToSignal(SceneTests.Tree, SceneTree.SignalName.ProcessFrame); }
		}
	}
}
