using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HardwareDefence.Autoload;
using HardwareDefence.Data;
using HardwareDefence.Game;
using HardwareDefence.Sim;
using HardwareDefence.View;
using Godot;

namespace HardwareDefence.Tests;

/// <summary>Arrival identity, lifetime and pixel/HUD boundaries, independently of ability timing.</summary>
public sealed class BossFxTests : TestSuite
{
	static BugSnap Boss(string type = "boss", int id = 81) => new()
	{
		Id = id, Type = type, Hp = Catalog.Enemy(type).MaxHp, X = 504, Y = 360,
	};
	static Snapshot SnapshotOf(params BugSnap[] bugs) => new() { State = "playing", Bugs = bugs.ToList() };
	static IEnumerable<Node> Descendants(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			yield return child;
			foreach (var descendant in Descendants(child)) yield return descendant;
		}
	}

	[Test]
	public async Task RealWaveKeepsIntrusionBannerAndStartsIntroWithoutNewHooks()
	{
		var originalFeed = GameSession.CreateFeed;
		var originalKind = GameSession.Current;
		GameScreen screen = null;
		try
		{
			var level = Catalog.Levels[1].Clone();
			level.Waves = new() { new() { new() { Enemy = "keylogger", Time = 0 } } };
			LocalFeed feed = null;
			GameSession.Current = GameSession.Kind.Custom;
			GameSession.CreateFeed = () => feed = new LocalFeed(level, MatchMode.Campaign, PlayerSide.Defender, false);
			screen = GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
			SceneTests.Tree.Root.AddChild(screen);
			for (var i = 0; i < 200 && !feed.Match.Bugs.Any(); i++) screen._Process(Match.StepTime);
			Check(feed.Match.Bugs.Any(b => b.Def.Id == "keylogger"), "boss spawned through a real wave");
			var view = Descendants(screen).OfType<BossFx>().Single();
			CheckEq(view.Introductions.ArrivalCount, 1, "existing GameScreen sync automatically starts the introduction");
			CheckEq(view.Introductions.Featured?.Tier, 2, "real screen uses the catalog tier");
			Check(Descendants(screen.Hud).OfType<Label>().Any(label => label.Text == HardwareDefence.UI.Loc.S("INTRUSION DETECTED")),
				"original BugSpawned warning remains alongside the new introduction");
			screen._Process(0);
			CheckEq(view.Introductions.ArrivalCount, 1, "another real-screen sync does not restart it");
			// Let the existing warning's delayed colour reset complete before freeing
			// its labels, and exercise ordinary process frames as well as manual steps.
			await SceneTests.Tree.ToSignal(SceneTests.Tree.CreateTimer(2.35), SceneTreeTimer.SignalName.Timeout);
			Check(view.Introductions.Featured == null, "ordinary game frames close the short introduction");
		}
		finally
		{
			screen?.Free();
			GameSession.CreateFeed = originalFeed;
			GameSession.Current = originalKind;
		}
	}

	[Test]
	public void EightArrivalsFollowCatalogAndGrowWithoutCoveringHud()
	{
		BossIntro previous = null;
		for (var level = 0; level < 8; level++)
		{
			var type = Catalog.Levels[level].Waves.SelectMany(w => w).Select(s => s.Enemy)
				.First(id => Catalog.Enemy(id)?.Boss == true);
			CheckEq(BossIntroSequence.TierFor(type), level + 1, $"{type}: built-in level determines intensity");
			var current = new BossIntro(level + 1, Catalog.Enemy(type), level + 1, true);
			var frame = current.Frame(Grid.Size);
			Check(new Rect2(Vector2.Zero, Grid.Size).Encloses(frame.Grow(6)), $"{type}: panel/shadow stay inside board");
			Check(frame.Position.Y >= 440 && frame.End.Y <= 684, $"{type}: clear of warning and build controls");
			Check(frame.Size.X * frame.Size.Y < Grid.Size.X * Grid.Size.Y * .16f, $"{type}: most of the game stays visible");
			foreach (var coordinate in new[] { frame.Position.X, frame.Position.Y, frame.Size.X, frame.Size.Y })
				CheckNear(coordinate % PixelWorld.Scale, 0, $"{type}: whole art pixels");
			Check(current.Duration < 5, $"{type}: introduction closes before six-second capture");
			if (previous != null)
			{
				Check(current.Duration > previous.Duration, "duration grows");
				Check(current.ArtSize.X > previous.ArtSize.X && current.ArtSize.Y > previous.ArtSize.Y, "both panel dimensions grow");
				Check(current.Particles > previous.Particles && current.Shake > previous.Shake && current.Glitch > previous.Glitch, "arrival intensity grows");
			}
			previous = current;
		}
	}

	[Test]
	public void RepeatedSnapshotsNeverRestartAndNewIdsArriveOnce()
	{
		var sequence = new BossIntroSequence();
		var snapshot = SnapshotOf(Boss("trojan", 10));
		sequence.Sync(snapshot, .016f);
		CheckEq(sequence.Arrivals.Count, 1, "first snapshot arrives");
		for (var i = 0; i < 100; i++) sequence.Sync(snapshot, .1f);
		CheckEq(sequence.ArrivalCount, 1, "continuous sync is one arrival");
		Check(sequence.Featured == null, "expired panel stays closed while boss lives");
		CheckEq(sequence.Live.Count(), 1, "presence can continue after the panel expires");
		sequence.Sync(SnapshotOf(), .1f);
		CheckEq(sequence.Live.Count(), 0, "removal clears presence");
		sequence.Sync(snapshot, .1f);
		Check(sequence.Featured == null && sequence.Arrivals.Count == 0, "same id cannot reopen after a gap");
		var next = Boss("trojan", 11);
		sequence.Sync(SnapshotOf(next, next.Copy()), .016f);
		CheckEq(sequence.Arrivals.Count, 1, "duplicate entries do not duplicate a new id");
		CheckEq(sequence.ArrivalCount, 2, "a genuinely new id is announced once");
		CheckEq(sequence.Featured?.Id, 11, "new id owns its introduction");
	}

	[Test]
	public void SimultaneousBossesShareOnePanelAndMinionsNeverAnnounce()
	{
		var sequence = new BossIntroSequence();
		sequence.Sync(SnapshotOf(Boss("trojan", 1), Boss("boss", 2), Boss("packet", 3), Boss("worm_copy", 4)), .016f);
		CheckEq(sequence.Arrivals.Count, 2, "only real bosses arrive");
		CheckEq(sequence.Live.Count(), 2, "only real bosses maintain presence");
		CheckEq(sequence.Featured?.Tier, 8, "strongest arrival gets the single panel");
		sequence.Sync(SnapshotOf(Boss("trojan", 1)), .3f);
		CheckEq(sequence.Featured?.Tier, 1, "removing the stronger boss releases the panel");
	}

	[Test]
	public void DeathAndMatchEndRemovePresentationImmediately()
	{
		var sequence = new BossIntroSequence();
		var boss = Boss();
		sequence.Sync(SnapshotOf(boss), .016f);
		boss.Hp = 0;
		sequence.Sync(SnapshotOf(boss), .016f);
		Check(sequence.Featured == null && !sequence.Live.Any(), "zero-health boss never keeps a panel/presence");
		sequence.Sync(SnapshotOf(Boss("worm", 82)), .016f);
		var ended = SnapshotOf(Boss("worm", 82));
		ended.State = "defender_won";
		sequence.Sync(ended, .016f);
		Check(sequence.Featured == null && !sequence.Live.Any() && sequence.Arrivals.Count == 0, "result screen clears arrivals even if a final snapshot retains a boss");
	}

	[Test]
	public void BossFxCancelsGlitchAndLateBlueScreenAfterRemoval()
	{
		var root = new Node2D();
		var fx = new Fx();
		var view = new BossFx { Fx = fx };
		root.AddChild(fx);
		root.AddChild(view);
		SceneTests.Tree.Root.AddChild(root);
		try
		{
			var arrivalCanvas = view.GetChildren().OfType<Node2D>().Single(n => n.Name == "ArrivalVignettes");
			Check(arrivalCanvas.ZIndex + view.ZIndex > 0 && arrivalCanvas.ZIndex + view.ZIndex < 2,
				"arrival windows draw above the board and below creatures/projectiles");
			var snapshot = SnapshotOf(Boss());
			view.Sync(snapshot, .016f);
			view.Sync(snapshot, .5f);
			Check(view.Introductions.Featured?.GlitchStrength > 0, "active arrival drives a glitch beat");
			var panic = new SimEvent(EventKind.BossAbility, 81, new Vector2(504, 360), 1.4f, "panic");
			CheckEq(view.OnAbility(panic), "crash", "old panic sound/ability still respond");
			CheckNear(view.BlueScreenRemaining, 1.1f, "old blue-screen duration preserved");
			view.Sync(SnapshotOf(), .016f);
			Check(view.Introductions.Featured == null && !view.Introductions.Live.Any(), "death removes intro and presence in BossFx");
			CheckNear(PostFx.Instance.BossGlitchStrength, 0, "death clears post-processing immediately");
			CheckNear(view.BlueScreenRemaining, 0, "removed boss no longer covers the board");
			Check(view.OnAbility(panic) == null && view.BlueScreenRemaining == 0, "late panic cannot revive the blue screen");
			view.Sync(snapshot, .016f);
			Check(view.Introductions.Featured == null, "repeated old id does not reopen the introduction");
		}
		finally { root.Free(); }
	}

	[Test]
	public void ScreenFxPreferenceTimeoutAndViewExitClearNewGlitch()
	{
		var original = Settings.Instance.ScreenFx;
		try
		{
			Settings.Instance.SetScreenFx(true);
			PostFx.SetBossGlitch(.2f);
			CheckNear(PostFx.Instance.BossGlitchStrength, .2f, "enabled intrusion");
			Settings.Instance.SetScreenFx(false);
			CheckNear(PostFx.Instance.BossGlitchStrength, 0, "disabling effects clears immediately");
			PostFx.SetBossGlitch(.3f);
			CheckNear(PostFx.Instance.BossGlitchStrength, 0, "disabled effects cannot restart");
			Settings.Instance.SetScreenFx(true);
			PostFx.SetBossGlitch(.2f);
			PostFx.Instance._Process(.11);
			CheckNear(PostFx.Instance.BossGlitchStrength, 0, "missed refresh times out rather than leaking into another screen");
			var view = new BossFx();
			SceneTests.Tree.Root.AddChild(view);
			PostFx.SetBossGlitch(.2f);
			view.Free();
			CheckNear(PostFx.Instance.BossGlitchStrength, 0, "leaving the view clears immediately");
		}
		finally { Settings.Instance.SetScreenFx(original); PostFx.SetBossGlitch(0); }
	}

	[Test]
	public void OriginalRevealPayloadAndAbilityEffectsRemainAvailable()
	{
		var root = new Node2D();
		var fx = new Fx();
		var view = new BossFx { Fx = fx };
		root.AddChild(fx);
		root.AddChild(view);
		SceneTests.Tree.Root.AddChild(root);
		try
		{
			var reveals = 0;
			view.Revealed += () => reveals++;
			view.Sync(SnapshotOf(Boss("trojan", 1)), .016f);
			foreach (var (ability, sound) in new (string, string)[]
			{
				("steal", "steal"), ("intercept", null), ("flood", "pew"), ("cloak", "glitch"),
				("uncloak", null), ("encrypt", "lock"), ("reveal", "glitch"), ("payload", "glitch"), ("fork", "glitch"),
			})
				CheckEq(view.OnAbility(new SimEvent(EventKind.BossAbility, 1, new Vector2(504, 360), 80, ability)), sound, $"{ability}: original sound contract");
			CheckEq(reveals, 1, "Trojan reveal still notifies the existing intrusion banner");
			Check(fx.GetChildCount() > 10, "legacy ability particles/text/light effects still spawn");
		}
		finally { root.Free(); }
	}
}
