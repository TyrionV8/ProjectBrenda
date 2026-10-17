using System.Collections.Generic;
using HardwareDefence.Data;
using HardwareDefence.Sim;
using Godot;

namespace HardwareDefence.Game;

/// <summary>
/// Where the game screen gets the match from: a local simulation (campaign, versus vs AI,
/// or the host of an online match) or a remote host (online attacker).
/// </summary>
public interface IMatchFeed
{
	LevelData Level { get; }
	MatchMode Mode { get; }
	PlayerSide LocalSide { get; }
	/// <summary>The state to draw this frame (null while waiting for the match to start).</summary>
	Snapshot Current { get; }
	/// <summary>Short status line shown while waiting (e.g. "Waiting for the attacker...").</summary>
	string Status { get; }
	/// <summary>Campaign/solo matches can pause and change speed; online ones can't.</summary>
	bool IsLocalOnly { get; }

	void Process(float delta);
	List<SimEvent> DrainEvents();
	void Build(TurretDef def, Vector2I cell, float direction);
	void SendBug(EnemyDef def);
	/// <summary>Why the local attacker can't send this bug now (null if it can).</summary>
	string CannotSend(EnemyDef def);
	Dictionary<string, float> StatsFor(TurretDef def);
	void Close();
}
