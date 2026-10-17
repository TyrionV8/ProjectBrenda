using Godot;

namespace Brenda.Sim;

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
}

/// <summary>
/// Something that happened during a simulation step. Views use them for effects and
/// sounds; the host forwards them to remote players.
/// </summary>
/// <param name="Id">Bug, turret, projectile or component index depending on the kind.</param>
/// <param name="Type">Bug or turret type id when relevant.</param>
public readonly record struct SimEvent(EventKind Kind, int Id = 0, Vector2 Pos = default, float Value = 0, string Type = null);
