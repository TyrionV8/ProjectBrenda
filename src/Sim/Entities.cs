using System.Collections.Generic;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Sim;

public sealed class Bug
{
	public int Id;
	public EnemyDef Def;
	public float Hp;
	/// <summary>Distance travelled along the route, in pixels.</summary>
	public float Progress;
	public Vector2 Pos;
	public float Angle;
	public float SlowFactor = 1;
	public float SlowTime;
	public bool Dead;
	public bool Escaped;
	/// <summary>Index of the component being chewed, or -1.</summary>
	public int ChewTarget = -1;
	public float ChewFxTimer;

	public float Radius => Def.Radius;
	public bool Slowed => SlowTime > 0;
	public bool Gone => Dead || Escaped;

	public void ApplySlow(float factor, float duration)
	{
		SlowFactor = Mathf.Min(SlowFactor, factor);
		SlowTime = Mathf.Max(SlowTime, duration);
	}
}

public sealed class Turret
{
	public static readonly Vector2[] OrbSlots = { new(24, 0), new(0, -24), new(-24, 0), new(0, 24) };

	public int Id;
	public TurretDef Def;
	public Vector2I Cell;
	public Vector2 Pos;
	/// <summary>Aim chosen by the player when placing (the laser).</summary>
	public float Direction;
	public Dictionary<string, float> Stats;
	public float Cooldown;
	public float HeadAngle;
	public float Spin = 0.6f;
	public bool Alternate;
	public readonly int[] Orbs = new int[4];
	public int NextSlot;

	public float Stat(string name) => Stats.GetValueOrDefault(name);
	public float Range => Def.Behavior == "laser" ? 0 : Stat("range");
	public Vector2 Muzzle(float distance) => Pos + Vector2.FromAngle(HeadAngle) * distance;
}

public enum ProjectileKind
{
	Bullet,
	Bomb,
	Orb,
}

public sealed class Projectile
{
	public int Id;
	public ProjectileKind Kind;
	/// <summary>Turret type that fired it (for colours).</summary>
	public string Source;
	public Vector2 Pos;
	public bool Done;

	// Bullet
	public Vector2 Vel;
	public float Damage;
	public float Size = 4;
	public float MaxDistance;
	public float Travelled;

	// Bomb
	public Vector2 Start;
	public Vector2 Target;
	public float T;
	public float Duration = 1;
	public float Radius;
	public float Height;

	// Frost orb
	public int OwnerId;
	public Vector2 Anchor;
	public int TargetId;
	public bool Launched;
	public float SlowFactor;
	public float SlowDuration;
	public float Speed;
	public float SearchRange;
	public float Phase;
}

/// <summary>A component on the board. Bugs chew on it while they walk past.</summary>
public sealed class BoardComponent
{
	public int Index;
	public ComponentData Data;
	public Rect2 Rect;
	public float MaxHp;
	public float Hp;
	public bool Destroyed;

	public bool Chewable => MaxHp > 0 && !Destroyed;
	public float Ratio => MaxHp <= 0 ? 1 : Hp / MaxHp;

	public float DistanceTo(Vector2 p)
	{
		var clamped = new Vector2(Mathf.Clamp(p.X, Rect.Position.X, Rect.End.X), Mathf.Clamp(p.Y, Rect.Position.Y, Rect.End.Y));
		return clamped.DistanceTo(p);
	}

	public Vector2 ClosestPoint(Vector2 p) =>
		new(Mathf.Clamp(p.X, Rect.Position.X, Rect.End.X), Mathf.Clamp(p.Y, Rect.Position.Y, Rect.End.Y));
}
