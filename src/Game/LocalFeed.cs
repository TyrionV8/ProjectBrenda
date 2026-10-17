using System.Collections.Generic;
using System.Linq;
using Brenda.Autoload;
using Brenda.Data;
using Brenda.Sim;
using Godot;

namespace Brenda.Game;

/// <summary>Runs the simulation on this machine. Optional bots play the other side (or both).</summary>
public class LocalFeed : IMatchFeed
{
	public readonly Match Match;
	public DefenderBot DefenderBot;
	public AttackerBot AttackerBot;
	readonly bool _campaignUpgrades;

	public LocalFeed(LevelData level, MatchMode mode, PlayerSide localSide, bool campaignUpgrades)
	{
		_campaignUpgrades = campaignUpgrades;
		Match = new Match(level, mode, null, campaignUpgrades ? GameState.LevelsOf : null);
		LocalSide = localSide;
		Current = Snapshot.From(Match);
	}

	public LevelData Level => Match.Level;
	public MatchMode Mode => Match.Mode;
	public PlayerSide LocalSide { get; }
	public Snapshot Current { get; protected set; }
	public virtual string Status => "";
	public virtual bool IsLocalOnly => true;

	/// <summary>False while an online host waits for its opponent.</summary>
	protected virtual bool Running => true;

	public virtual void Process(float delta)
	{
		if (!Running)
			return;
		var before = Match.Time;
		Match.Advance(delta);
		var simulated = Match.Time - before;
		if (simulated > 0)
		{
			DefenderBot?.Step(Match, simulated);
			AttackerBot?.Step(Match);
		}
		Current = Snapshot.From(Match);
	}

	public virtual List<SimEvent> DrainEvents()
	{
		var events = Match.Events.ToList();
		Match.Events.Clear();
		return events;
	}

	public virtual void Build(TurretDef def, Vector2I cell, float direction) => Match.Build(def, cell, direction);

	public virtual void SendBug(EnemyDef def) => Match.SendBug(def);

	public string CannotSend(EnemyDef def) => Match.CannotSend(def);

	public Dictionary<string, float> StatsFor(TurretDef def) => _campaignUpgrades ? GameState.TurretStats(def) : def.ComputeStats(null);

	public virtual void Close()
	{
	}
}
