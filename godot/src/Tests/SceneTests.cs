using System.Threading.Tasks;
using Brenda.Data;
using Brenda.Game;
using Brenda.Sim;
using Godot;

namespace Brenda.Tests;

/// <summary>Runs the real game screen for a while (headless) to catch view errors.</summary>
public sealed class SceneTests : TestSuite
{
	public static SceneTree Tree;

	// Await on the tree itself: an awaiter owned by a node that gets freed never completes.
	static SignalAwaiter NextFrame() => Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);

	[Test]
	public async Task GameScreenRunsWithBot()
	{
		foreach (var mode in new[] { MatchMode.Campaign, MatchMode.Versus })
		{
			GameSession.Current = mode == MatchMode.Versus ? GameSession.Kind.Versus : GameSession.Kind.Custom;
			LocalFeed feed = null;
			GameSession.CreateFeed = () =>
			{
				feed = new LocalFeed(Catalog.Levels[0], mode, PlayerSide.Defender, false) { DefenderBot = new DefenderBot() };
				if (mode == MatchMode.Versus)
					feed.AttackerBot = new AttackerBot(3);
				return feed;
			};
			var scene = GD.Load<PackedScene>(GameSession.GameScene).Instantiate<GameScreen>();
			Tree.Root.AddChild(scene);
			Engine.TimeScale = 8;
			for (var i = 0; i < 400; i++)
				await NextFrame();
			Engine.TimeScale = 1;
			Check(feed.Match.Turrets.Count > 0, $"{mode}: bot built turrets");
			Check(feed.Match.Time > 5, $"{mode}: match advanced");
			Note += $"{mode}: {feed.Match.Time:0}s sim, {feed.Match.Turrets.Count} turrets. ";
			scene.QueueFree();
			await NextFrame();
		}
	}
}
