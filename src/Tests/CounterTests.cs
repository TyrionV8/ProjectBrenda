using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;
using HardwareDefence.UI;
using Godot;

namespace HardwareDefence.Tests;

public sealed class CounterTests : TestSuite
{
	static Match Arena() => new(new LevelData
	{
		Path = new() { new[] { 0, 2 }, new[] { 18, 2 } },
		Components = new() { new() { Type = "led", Cell = new[] { 5, 5 }, Size = new[] { 1, 1 } } },
	}, MatchMode.Versus) { Money = 10000 };
	static Turret Build(Match m, string type, int x = 4, int y = 4)
	{
		m.Build(Catalog.Turret(type), new Vector2I(x,y));
		return m.Turrets.Last();
	}
	static void Run(Match m, float seconds)
	{
		for (var i = 0; i < Mathf.CeilToInt(seconds / Match.StepTime); i++) m.Step(Match.StepTime);
	}

	[Test]
	public void ScannerExposesOnlyWhileInsideAndRevealsTrojanOnce()
	{
		var m = Arena();
		Build(m, "blaster", 4, 3); // Weapon is deliberately before scanner.
		var scanner = Build(m, "antivirus");
		var rootkit = m.SpawnBug("rootkit", 190);
		rootkit.Cloaked = true; rootkit.AbilityTimer = 99;
		Run(m, .05f);
		Check(rootkit.Cloaked && rootkit.Exposed && rootkit.Targetable, "scan exposes without ending the cloak cycle");
		var hp = rootkit.Hp;
		m.DamageBug(rootkit, 1);
		Check(rootkit.Hp < hp, "exposed rootkit takes real damage");
		Check(Snapshot.From(m).Bugs.First(b => b.Id == rootkit.Id).Exposed, "snapshot reports scanner exposure");
		rootkit.Progress = 650;
		Run(m, .05f);
		Check(rootkit.Cloaked && !rootkit.Exposed && !rootkit.Targetable, "leaving scanner range hides rootkit again");
		hp = rootkit.Hp; m.DamageBug(rootkit, 1);
		CheckNear(rootkit.Hp, hp, "hidden rootkit ignores damage again");
		rootkit.Progress = 190;
		scanner.Locked = 1;
		Run(m, .05f);
		Check(!rootkit.Exposed, "locked scanner cannot expose");
		scanner.Locked = 0;
		var trojan = m.SpawnBug("trojan", 190);
		Run(m, .1f);
		Check(trojan.Revealed && trojan.Hp == trojan.Def.MaxHp, "scan reveals a healthy Trojan");
		CheckEq(m.Events.Count(e => e.Kind == EventKind.BossAbility && e.Type == "reveal" && e.Id == trojan.Id), 1, "one ordinary reveal event");
	}

	[Test]
	public void FirewallProtectsTheFiringOriginAndPacketsAreRemovedOnCooldown()
	{
		var m = Arena();
		var gun = Build(m, "blaster", 4, 3);
		gun.Stats["range"] = 400;
		var originShield = Build(m, "firewall",4,4);
		originShield.Stats["range"] = 50; // Turret origin inside; forward muzzle is outside.
		var middle = m.SpawnBug("mitm", 260);
		middle.Pos = gun.Pos + new Vector2(100,0);
		gun.Cooldown = 0;
		Run(m, .1f);
		Check(m.Projectiles.Any(p => p.Source == "blaster" && p.Protected), "real weapon shot inherits protection at the turret origin");
		var packet = m.SpawnBug("packet", 190);
		var firewall = m.Turrets.Last(); firewall.Stats["range"] = 155; firewall.Cooldown = 0;
		m.Step(Match.StepTime);
		Check(packet.Dead, "firewall destroys a packet in range");
		var second = m.SpawnBug("packet", 190);
		m.Step(Match.StepTime);
		Check(!second.Dead, "firewall waits for reload before deleting another packet");
		Check(m.Events.Any(e => e.Kind == EventKind.TurretAbility && e.Type == "block"), "shield effect event emitted");
	}

	[Test]
	public void FastShotsAndBombsCrossingFirewallRemainProtectedAfterLeaving()
	{
		foreach (var kind in new[] { ProjectileKind.Bullet, ProjectileKind.Bomb })
		{
			var m = Arena(); var firewall = Build(m, "firewall");
			firewall.Stats["range"] = 40;
			var boss = m.SpawnBug("mitm", 400);
			var shot = new Projectile
			{
				Id = 0, Kind = kind, Source = "blaster", Pos = firewall.Pos + new Vector2(-100,0),
				Start = firewall.Pos + new Vector2(-100,0), Target = firewall.Pos + new Vector2(220,0),
				Vel = new Vector2(19200,0), MaxDistance = 1000, Duration = Match.StepTime,
			};
			// MITM at the end of a segment with neither endpoint inside the small firewall.
			boss.Pos = shot.Target;
			m.Projectiles.Add(shot);
			m.Step(Match.StepTime);
			Check(shot.Protected, $"{kind}: whole path crossing grants protection");
			Check(!m.Events.Any(e => e.Kind == EventKind.BossAbility && e.Type == "intercept"), $"{kind}: interception prevented outside shield");
		}
		var plain = Arena();
		var mitm = plain.SpawnBug("mitm", 400);
		var unprotected = new Projectile { Id = 0, Kind = ProjectileKind.Bullet, Pos = mitm.Pos, MaxDistance = 1000 };
		plain.Projectiles.Add(unprotected); plain.Step(Match.StepTime);
		Check(plain.Events.Any(e => e.Type == "intercept"), "same deterministic shot is intercepted without a firewall");
	}

	[Test]
	public void LockedFirewallCannotProtectOrDeletePackets()
	{
		var m = Arena(); var firewall = Build(m, "firewall"); firewall.Locked = 2;
		var packet = m.SpawnBug("packet",190);
		var shot = new Projectile { Kind = ProjectileKind.Bullet, Pos = firewall.Pos, MaxDistance = 1000 };
		m.Projectiles.Add(shot); Run(m,.1f);
		Check(!packet.Dead && !shot.Protected, "locked shield has no active functions");
	}

	[Test]
	public void BackupRecoveryIsOrderIndependentAndGrantsExpiringImmunity()
	{
		foreach (var backupFirst in new[] { false, true })
		{
			var m = Arena();
			if (backupFirst) Build(m,"backup");
			var target = Build(m,"blaster",4,3);
			if (!backupFirst) Build(m,"backup");
			target.Locked = 2;
			Run(m,.55f);
			Check(target.Locked <= 0 && target.LockImmunity > 0, "backup restores four times faster and grants immunity");
			var boss = m.SpawnBug("ransomware",190); boss.AbilityTimer = 0;
			m.Step(Match.StepTime);
			Check(target.Locked <= 0, "freshly restored turret ignores encryption");
			Run(m,1.6f); boss.AbilityTimer = 0; m.Step(Match.StepTime);
			Check(target.Locked > 0, "encryption works after immunity expires");
		}
		var plain = Arena(); var gun = Build(plain,"blaster"); gun.Locked = 2; Run(plain,.55f);
		Check(gun.Locked > 1, "ordinary recovery retains the original timer");
	}


	[Test]
	public void BackupRebootsAfterRealGlobalPanicAndRestoresNeighboursWithTemporaryImmunity()
	{
		float plainDuration = 0;
		foreach (var supported in new[] { false, true })
		{
			var m = Arena();
			var gun = Build(m,"blaster",4,3);
			var backup = supported ? Build(m,"backup") : null;
			var boss = m.SpawnBug("boss",190); boss.AbilityTimer=0;
			m.Step(Match.StepTime);
			Check(gun.Locked>0 && (!supported || backup.Locked>0),"real Panic initially locks every turret including backup");
			var elapsed=0f;
			while(gun.Locked>0 && elapsed<3) {m.Step(Match.StepTime);elapsed+=Match.StepTime;}
			if(!supported) {plainDuration=elapsed;continue;}
			Check(elapsed < plainDuration * .6f,$"backup shortens real global Panic: {elapsed:0.00}s versus {plainDuration:0.00}s");
			Check(backup.Locked<=0 && gun.LockImmunity>0,"backup boots first and restores the weapon with immunity");
			Check(m.Events.Any(e=>e.Kind==EventKind.TurretAbility && e.Type=="restore"),"actual Panic recovery emits restoration");
			boss.AbilityTimer=0;m.Step(Match.StepTime);
			Check(gun.Locked<=0,"restored weapon resists another global Panic during its immunity");
			Run(m,1.6f);boss.AbilityTimer=0;m.Step(Match.StepTime);
			Check(gun.Locked>0 && backup.Locked>0,"both can be crashed again after finite immunity expires");
		}
	}

	[Test]
	public void AllSupportTurretsCanBeEncryptedAndPanicLocked()
	{
		foreach (var enemy in new[] { "ransomware", "boss" })
		{
			var m = Arena();
			foreach (var (id,index) in new[] { "antivirus","firewall","backup","technician" }.Select((id,index)=>(id,index)))
				Build(m,id,3+index,3);
			var b = m.SpawnBug(enemy,190); b.AbilityTimer = 0;
			if (enemy == "ransomware") b.Pos = Grid.CellCenter(new Vector2I(4,3));
			// Set the route point as well so normal movement keeps the attack close.
			if (enemy == "ransomware") foreach (var t in m.Turrets) t.Pos = new Vector2(216,160);
			m.Step(Match.StepTime);
			Check(m.Turrets.All(t => t.Locked > 0), $"{enemy}: all support is locked including backup itself");
			Check(Snapshot.From(m).Turrets.All(t=>t.Locked), "network snapshot includes support locks");
		}
	}

	[Test]
	public void TechnicianRepairsLedAndRebuildsOnlyWhenComplete()
	{
		var m = Arena(); var technician = Build(m,"technician");
		var led = m.Components[0]; led.Hp = 1;
		Run(m,.5f);
		Check(led.Hp > 1 && led.Hp < led.MaxHp, "damaged LED repairs slowly");
		technician.Locked = 1;
		var hp = led.Hp; Run(m,.5f);
		CheckNear(led.Hp,hp,"locked technician does not repair");
		technician.Locked = 0; led.Hp = 0; led.Destroyed = true;
		CheckNear(m.LightLevel,0,"destroyed LED extinguishes lighting");
		Run(m,2);
		Check(led.Destroyed && led.RepairProgress > 0, "partial rebuild keeps the component destroyed");
		CheckNear(Snapshot.From(m).Components[0],-1,"snapshot stays destroyed while rebuilding");
		Run(m,4);
		Check(!led.Destroyed && led.Hp == led.MaxHp,"complete rebuild revives at full health");
		CheckNear(Snapshot.From(m).Components[0],led.MaxHp,"snapshot goes from -1 to full health");
		CheckNear(m.LightLevel,1,"revived LED restores light");
		Check(m.Events.Any(e=>e.Kind==EventKind.TurretAbility && e.Type=="repair" && e.Value==0),"repair event names component index");
	}

	[Test]
	public void FourDefinitionsHaveUnlocksAndRealProgressiveUpgrades()
	{
		var counters=Catalog.Turrets.Where(t=>t.UnlockPrice>0).ToArray();
		CheckEq(counters.Length,4,"four purchasable counter turrets");
		foreach(var t in counters)
		{
			Check(t.Cost>15 && t.Upgrades.Count>=1 && t.Upgrades.Count<=3,$"{t.Id}: support cost and upgrade count");
			Check(t.Upgrades.All(u=>u.Price==5 && u.PriceStep==10),$"{t.Id}: auxiliary upgrade price model");
			var functionalStats=t.Id switch
			{
				"antivirus" => new[]{"range"},
				"firewall" => new[]{"range","cooldown"},
				"backup" => new[]{"range","recovery_rate","immunity"},
				_ => new[]{"range","repair_rate"},
			};
			Check(t.Upgrades.All(u=>functionalStats.Contains(u.Stat)),$"{t.Id}: every paid upgrade changes gameplay");
		}
	}


	[Test]
	public void DefenderChoosesTheCounterForTheCurrentWaveAfterBuildingWeapons()
	{
		foreach (var (boss, counter) in new[] { ("trojan","antivirus"),("rootkit","antivirus"),("mitm","firewall"),("ddos","firewall"),("ransomware","backup"),("boss","backup") })
		{
			var level = Catalog.Levels[0].Clone();
			level.Waves = new() { new() { new() { Enemy = boss, Time = 100 } } };
			var m = new Match(level) { Money = 10000 };
			var bot = new DefenderBot();
			for (var i = 0; i < 7; i++) bot.Step(m,1);
			CheckEq(m.Turrets.Count,7,$"{boss}: seven weapons built first");
			Check(m.Turrets.All(t=>t.Def.UnlockPrice==0),$"{boss}: initial defence remains offensive");
			m.Money = 10;
			bot.Step(m,1);
			CheckEq(m.Turrets.Count,7,$"{boss}: reserves reward budget instead of buying an eighth weapon");
			for (var reward = 0; reward < 5; reward++) { m.Money += 5; bot.Step(m,1); }
			CheckEq(m.Turrets.Last().Def.Id,counter,$"{boss}: uses counter for upcoming boss in current wave");
			bot.Step(m,1);
			CheckEq(m.Turrets.Count(t=>t.Def.Id==counter),1,$"{boss}: support does not replace the rest of the defence");
		}
	}


	[Test]
	public async Task LocalInventoryAndHotkeysFollowTheSameSaveInEveryMode()
	{
		using var save = GameState.UseTemporarySave();
		GameState.Reset(); GameState.Balance=1000;
		// Unlock only one counter so its visible key differs from its catalog index.
		Check(GameState.Unlock(Catalog.Turret("technician")),"buy real technician unlock");
		GameState.Reset(); GameState.Load();
		foreach(var kind in new[]{GameSession.Kind.Campaign,GameSession.Kind.Custom,GameSession.Kind.Versus})
		{
			GameSession.Current=kind;
			GameSession.CreateFeed=()=>new LocalFeed(Catalog.Levels[0],kind==GameSession.Kind.Versus?MatchMode.Versus:MatchMode.Campaign,PlayerSide.Defender,false);
			var screen=GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
			SceneTests.Tree.Root.AddChild(screen);
			await SceneTests.Tree.ToSignal(SceneTests.Tree,SceneTree.SignalName.ProcessFrame);
			try
			{
				CheckEq(screen.AvailableTurrets.Count,6,$"{kind}: five originals plus purchased counter");
				var slots=screen.Hud.FindChildren("*","Button",true,false).OfType<TurretSlot>().ToArray();
				CheckEq(slots.Length,6,$"{kind}: HUD filters locked counters");
				screen._UnhandledInput(new InputEventKey{Pressed=true,Keycode=Key.Key6});
				CheckEq(screen.Build.Selected?.Id,"technician",$"{kind}: key six selects sixth visible slot");
				screen.Build.Select(Catalog.Turret("antivirus"));
				CheckEq(screen.Build.Selected?.Id,"technician",$"{kind}: direct selection cannot bypass unlock");
				var sim=((LocalFeed)screen.Feed).Match; sim.Money=10000;
				Check(sim.Build(Catalog.Turret("antivirus"),DefenderBot.BestCell(sim,Catalog.Turret("antivirus"))),$"{kind}: simulation still accepts a locked definition");
			}
			finally { screen.QueueFree(); await SceneTests.Tree.ToSignal(SceneTests.Tree,SceneTree.SignalName.ProcessFrame); }
		}
		GameState.Reset();
	}

	[Test]
	public async Task AllNineSlotsAndBossBarFitAndNinthShortcutWorks()
	{
		using var save=GameState.UseTemporarySave(); GameState.Reset(); GameState.Balance=1000;
		foreach(var t in Catalog.Turrets.Where(t=>t.UnlockPrice>0)) GameState.Unlock(t);
		GameSession.Current=GameSession.Kind.Custom;
		GameSession.CreateFeed=()=>{var f=new LocalFeed(Catalog.Levels[0],MatchMode.Campaign,PlayerSide.Defender,false);f.Match.SpawnBug("trojan",200);return f;};
		var screen=GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
		SceneTests.Tree.Root.AddChild(screen);
		for(var i=0;i<3;i++)await SceneTests.Tree.ToSignal(SceneTests.Tree,SceneTree.SignalName.ProcessFrame);
		try
		{
			var slots=screen.Hud.FindChildren("*","Button",true,false).OfType<TurretSlot>().ToArray();
			CheckEq(slots.Length,9,"nine visible slots");
			foreach(var slot in slots) Check(slot.GlobalPosition.Y>=0 && slot.GlobalPosition.Y+slot.Size.Y<=768,$"{slot.Def.Id}: actual slot stays in viewport");
			var bossBar=screen.Hud.FindChild("BossBar",true,false) as Control;
			var footer=screen.Hud.FindChild("HudFooter",true,false) as Control;
			Check(bossBar != null && bossBar.Visible,"real boss bar is displayed");
			foreach(var control in new[]{bossBar,footer}.Concat(footer.GetChildren().OfType<Button>()))
			{
				var rect=control.GetGlobalRect();
				Check(rect.Size.X>0 && rect.Size.Y>0 && rect.Position.X>=0 && rect.Position.Y>=0 && rect.End.X<=1366 && rect.End.Y<=768,$"{control.Name}: actual boss bar/footer fits 1366 by 768");
			}
			Check(slots.Last().GetGlobalRect().End.Y<=bossBar.GetGlobalRect().Position.Y,"last turret does not overlap boss bar");
			Check(bossBar.GetGlobalRect().End.Y<=footer.GetGlobalRect().Position.Y,"boss bar does not overlap footer");
			screen._UnhandledInput(new InputEventKey{Pressed=true,Keycode=Key.Key9});
			CheckEq(screen.Build.Selected?.Id,"technician","ninth shortcut selects ninth visible turret");
		}
		finally {screen.QueueFree();await SceneTests.Tree.ToSignal(SceneTests.Tree,SceneTree.SignalName.ProcessFrame);GameState.Reset();}
	}
}
