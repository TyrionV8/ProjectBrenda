using Godot;

namespace HardwareDefence.Sim;

public enum EventKind
{
	BugSpawned,
	BugDied,
	BugEscaped,
	TurretBuilt,
	TurretFired,
	BulletHit,
	Explosion,
	OrbPopped,
	CpuHit,
	ComponentDestroyed,
	Chew,
	WaveCountdown,
	WaveStarted,
	MatchEnded,
	/// <summary>A boss used its ability. Id = bug, Type = what happened (see Match.StepAbility).</summary>
	BossAbility,
	/// <summary>Counter turret ability. Id = turret, Pos = target, Value = support range (scan/block/restore) or component index (repair).</summary>
	TurretAbility,
}

/// <summary>
/// Something that happened during a simulation step. Views use them for effects and
/// sounds; the host forwards them to remote players.
/// </summary>
/// <param name="Id">Bug, turret, projectile or component index depending on the kind.</param>
/// <param name="Type">Bug or turret type id when relevant.</param>
public readonly record struct SimEvent(EventKind Kind, int Id = 0, Vector2 Pos = default, float Value = 0, string Type = null);
