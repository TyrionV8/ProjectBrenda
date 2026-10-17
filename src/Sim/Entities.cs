using System.Collections.Generic;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Sim;

public enum DetourState
{
	OnRoute,
	Going,
	Chewing,
	Returning,
}

public sealed class Bug
{
	public int Id;
	public EnemyDef Def;
	public float Hp;
	/// <summary>Distance along the route in pixels; frozen while off route.</summary>
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
	public DetourState Detour;
	public int DetourTarget = -1;
	public Vector2 DetourExit;
	public Vector2 DetourDestination;
	public float DetourChewLeft;
	public float DetourCooldown;
	/// <summary>One decision per component and bug, including unsuccessful rolls.</summary>
	public readonly HashSet<int> DetourConsidered = new();
	public bool OffRoute => Detour != DetourState.OnRoute;
	/// <summary>Boss abilities: time until the next use (or until the rootkit changes state).</summary>
	public float AbilityTimer;
	/// <summary>Rootkit: hidden, so turrets can't aim at or damage it.</summary>
	public bool Cloaked;
	public bool Exposed;
	/// <summary>Trojan: its disguise is off.</summary>
	public bool Revealed;
	/// <summary>Worm: copies made so far.</summary>
	public int Forks;

	public float Radius => Def.Radius;
	public bool Slowed => SlowTime > 0;
	public bool Gone => Dead || Escaped;
	/// <summary>Turrets can aim at it and hurt it.</summary>
	public bool Targetable => !Gone && (!Cloaked || Exposed);

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
	/// <summary>Seconds left encrypted (ransomware) or crashed (kernel panic): it can't fire.</summary>
	public float Locked;
	public float LockImmunity;

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
	/// <summary>Already checked by a man-in-the-middle (each shot gets one chance to be intercepted).</summary>
	public bool Screened;
	/// <summary>Has originated inside, or crossed, an active firewall range.</summary>
	public bool Protected;
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

/// <summary>A component on the board. Bugs leave the route to chew on it.</summary>
public sealed class BoardComponent
{
	public int Index;
	public ComponentData Data;
	public Rect2 Rect;
	public float MaxHp;
	public float Hp;
	public bool Destroyed;
	/// <summary>Rebuild progress, kept invisible until the destroyed component is fully repaired.</summary>
	public float RepairProgress;

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
