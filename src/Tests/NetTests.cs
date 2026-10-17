using System.Linq;
using System.Threading.Tasks;
using Brenda.Data;
using Brenda.Net;
using Brenda.Sim;
using Godot;

namespace Brenda.Tests;

/// <summary>A host and a remote client talking over a real localhost WebSocket.</summary>
public sealed class NetTests : TestSuite
{
	static SignalAwaiter NextFrame() => SceneTests.Tree.ToSignal(SceneTests.Tree, SceneTree.SignalName.ProcessFrame);

	[Test]
	public async Task AttackerJoinsAndSendsBugs()
	{
		const int port = 47811;
		var host = new HostFeed(Catalog.Levels[0], port, PlayerSide.Defender, PlayerSide.Attacker) { DefenderBot = new DefenderBot() };
		Check(host.Host.Listening, host.Host.Error ?? "");
		Check(host.Status.Contains("Waiting"), "host waits for the attacker");
		var client = new RemoteFeed($"127.0.0.1:{port}", PlayerSide.Attacker, "Tester");
		for (var i = 0; i < 600 && client.Current == null; i++)
		{
			host.Process(1 / 60f);
			client.Process(1 / 60f);
			await NextFrame();
		}
		Check(client.Joined, $"client joined ({client.Status})");
		CheckEq(client.Level?.Title, Catalog.Levels[0].Title, "level sent in welcome");
		Check(client.Current != null, "client receives state");
		Check(host.Status == "", "host started");
		var spider = Catalog.Enemy("spider");
		for (var i = 0; i < 120 && client.CannotSend(spider) != null; i++)
		{
			host.Process(1 / 60f);
			client.Process(1 / 60f);
			await NextFrame();
		}
		client.SendBug(spider);
		var seen = false;
		for (var i = 0; i < 240 && !seen; i++)
		{
			host.Process(1 / 60f);
			client.Process(1 / 60f);
			seen = client.Current?.Bugs.Any(b => b.Type == "spider") ?? false;
			await NextFrame();
		}
		Check(host.Match.Bugs.Any(b => b.Def.Id == "spider") || seen, "host spawned the spider");
		Check(seen, "client sees the spider");
		// Defenders can't be impersonated by the attacker.
		client.Build(Catalog.Turret("blaster"), new Vector2I(4, 4), 0);
		for (var i = 0; i < 30; i++)
		{
			host.Process(1 / 60f);
			client.Process(1 / 60f);
			await NextFrame();
		}
		Check(host.Match.Turrets.All(t => t.Cell != new Vector2I(4, 4)), "attacker can't build");
		client.Close();
		for (var i = 0; i < 60 && host.Match.State == MatchState.Playing; i++)
		{
			host.Process(1 / 60f);
			client.Process(1 / 60f);
			await NextFrame();
		}
		CheckEq(host.Match.State, MatchState.DefenderWon, "attacker leaving forfeits");
		host.Close();
	}

	[Test]
	public async Task RejectsTakenRoleAndBadJson()
	{
		const int port = 47812;
		var host = new HostFeed(Catalog.Levels[1], port, PlayerSide.Defender, PlayerSide.Attacker);
		var first = new RemoteFeed($"127.0.0.1:{port}", PlayerSide.Attacker, "A");
		var second = new RemoteFeed($"127.0.0.1:{port}", PlayerSide.Attacker, "B");
		for (var i = 0; i < 400 && (!first.Joined || second.LastError == null); i++)
		{
			host.Process(1 / 60f);
			first.Process(1 / 60f);
			second.Process(1 / 60f);
			await NextFrame();
		}
		Check(first.Joined, "first attacker joined");
		Check(!second.Joined, "second attacker rejected");
		Check(second.LastError != null, "second attacker told why");
		var round = NetEvent.From(new SimEvent(EventKind.ComponentDestroyed, 3, new Vector2(10, 20), 5, "chip"));
		Check(round.TryToSim(out var back) && back.Kind == EventKind.ComponentDestroyed && back.Id == 3, "event round trip");
		Check(Protocol.Decode(System.Text.Encoding.UTF8.GetBytes("{nope")) == null, "bad JSON ignored");
		first.Close();
		second.Close();
		host.Close();
	}
}
