using System;
using System.Collections.Generic;
using System.Linq;
using HardwareDefence.Data;
using Godot;

namespace HardwareDefence.Sim;

public sealed class BugSnap
{
	public BugSnap Copy() => (BugSnap)MemberwiseClone();

	public int Id { get; set; }
	public string Type { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Angle { get; set; }
	public float Hp { get; set; }
	public bool Slowed { get; set; }
	public bool Chewing { get; set; }
	/// <summary>Leaving the route, chewing a component or returning to the saved exit.</summary>
	public bool OffRoute { get; set; }
	/// <summary>Distance along the route (pixels), frozen during a detour.</summary>
	public float Progress { get; set; }
	/// <summary>Rootkit boss hiding from the turrets.</summary>
	public bool Cloaked { get; set; }
	/// <summary>Trojan boss without its disguise.</summary>
	public bool Revealed { get; set; }
}

public sealed class TurretSnap
{
	public TurretSnap Copy() => (TurretSnap)MemberwiseClone();

	public int Id { get; set; }
	public string Type { get; set; }
	public int[] Cell { get; set; }
	public float Direction { get; set; }
	public float Head { get; set; }
	/// <summary>Encrypted or crashed by a boss: it can't fire.</summary>
	public bool Locked { get; set; }
}

public sealed class ProjectileSnap
{
	public ProjectileSnap Copy() => (ProjectileSnap)MemberwiseClone();

	public int Id { get; set; }
	/// <summary>bullet, bomb or orb.</summary>
	public string Kind { get; set; }
	/// <summary>Turret type that fired it.</summary>
	public string Source { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	/// <summary>Bomb arc height (0..1).</summary>
	public float H { get; set; }
	/// <summary>Travel direction in radians (bullets).</summary>
	public float A { get; set; }
}

/// <summary>
/// Everything needed to draw a moment of a match. The local view renders it every frame,
/// and the host sends it to remote players and AIs (see docs/PROTOCOL.md).
/// </summary>
public sealed class Snapshot
{
	public float Time { get; set; }
	public string Mode { get; set; }
	/// <summary>playing, defender_won or attacker_won.</summary>
	public string State { get; set; }
	public int Money { get; set; }
	public float Energy { get; set; }
	public float EnergyRate { get; set; }
	public int CpuHp { get; set; }
	public float TimeLeft { get; set; }
	public int Wave { get; set; }
	public int WaveCount { get; set; }
	/// <summary>countdown, spawning, waiting or done (campaign).</summary>
	public string WaveState { get; set; }
	public float Countdown { get; set; }
	public int Stars { get; set; }
	public float Integrity { get; set; }
	/// <summary>Bug types still to spawn in the current wave (campaign).</summary>
	public List<string> Upcoming { get; set; } = new();
	public List<BugSnap> Bugs { get; set; } = new();
	public List<TurretSnap> Turrets { get; set; } = new();
	public List<ProjectileSnap> Projectiles { get; set; } = new();
	/// <summary>Remaining HP of every component, by index (-1 = destroyed).</summary>
	public List<float> Components { get; set; } = new();

	public static string StateName(MatchState s) => s switch
	{
		MatchState.DefenderWon => "defender_won",
		MatchState.AttackerWon => "attacker_won",
		_ => "playing",
	};

	public static MatchState ParseState(string s) => s switch
	{
		"defender_won" => MatchState.DefenderWon,
		"attacker_won" => MatchState.AttackerWon,
		_ => MatchState.Playing,
	};

	public MatchState MatchState => ParseState(State);

	static float R(float v, int digits = 1) => MathF.Round(v, digits);

	public static Snapshot From(Match m)
	{
		var s = new Snapshot
		{
			Time = R(m.Time, 2), Mode = m.Mode == MatchMode.Versus ? "versus" : "campaign", State = StateName(m.State),
			Money = m.Money, Energy = R(m.Energy), EnergyRate = R(m.EnergyRate, 2), CpuHp = m.CpuHp, TimeLeft = R(m.TimeLeft),
			Wave = m.Waves.Index, WaveCount = m.Waves.Count, WaveState = m.Waves.State.ToString().ToLowerInvariant(),
			Countdown = R(m.Waves.Countdown, 2), Stars = m.Stars, Integrity = R(m.BoardIntegrity, 3),
		};
		s.Upcoming.AddRange(m.Waves.Upcoming().Select(u => u.Enemy));
		foreach (var b in m.Bugs)
			s.Bugs.Add(new BugSnap
			{
				Id = b.Id, Type = b.Def.Id, X = R(b.Pos.X), Y = R(b.Pos.Y), Angle = R(b.Angle, 3), Hp = R(b.Hp, 2),
				Slowed = b.Slowed, Chewing = b.Detour == DetourState.Chewing && b.ChewTarget >= 0, OffRoute = b.OffRoute, Progress = R(b.Progress), Cloaked = b.Cloaked, Revealed = b.Revealed,
			});
		foreach (var t in m.Turrets)
			s.Turrets.Add(new TurretSnap
			{
				Id = t.Id, Type = t.Def.Id, Cell = new[] { t.Cell.X, t.Cell.Y }, Direction = R(t.Direction, 3), Head = R(t.HeadAngle, 3), Locked = t.Locked > 0,
			});
		foreach (var p in m.Projectiles)
			s.Projectiles.Add(new ProjectileSnap
			{
				Id = p.Id, Kind = p.Kind.ToString().ToLowerInvariant(), Source = p.Source, X = R(p.Pos.X), Y = R(p.Pos.Y),
				H = R(p.Height, 2), A = R(p.Vel.Angle(), 3),
			});
		foreach (var c in m.Components)
			s.Components.Add(c.Destroyed ? -1 : R(c.Hp, 1));
		return s;
	}

	/// <summary>Blend two snapshots for smooth remote rendering (positions of matching ids).</summary>
	public static Snapshot Lerp(Snapshot a, Snapshot b, float t)
	{
		if (a == null)
			return b;
		var s = (Snapshot)b.MemberwiseClone();
		var oldBugs = a.Bugs.ToDictionary(x => x.Id);
		s.Bugs = b.Bugs.Select(nb =>
		{
			if (!oldBugs.TryGetValue(nb.Id, out var ob))
				return nb;
			var copy = nb.Copy();
			copy.X = Mathf.Lerp(ob.X, nb.X, t);
			copy.Y = Mathf.Lerp(ob.Y, nb.Y, t);
			copy.Angle = Mathf.LerpAngle(ob.Angle, nb.Angle, t);
			copy.Progress = Mathf.Lerp(ob.Progress, nb.Progress, t);
			return copy;
		}).ToList();
		var oldProj = a.Projectiles.ToDictionary(x => x.Id);
		s.Projectiles = b.Projectiles.Select(np =>
		{
			if (!oldProj.TryGetValue(np.Id, out var op))
				return np;
			var copy = np.Copy();
			copy.X = Mathf.Lerp(op.X, np.X, t);
			copy.Y = Mathf.Lerp(op.Y, np.Y, t);
			copy.H = Mathf.Lerp(op.H, np.H, t);
			return copy;
		}).ToList();
		var oldTurrets = a.Turrets.ToDictionary(x => x.Id);
		s.Turrets = b.Turrets.Select(nt =>
		{
			if (!oldTurrets.TryGetValue(nt.Id, out var ot))
				return nt;
			var copy = nt.Copy();
			copy.Head = Mathf.LerpAngle(ot.Head, nt.Head, t);
			return copy;
		}).ToList();
		return s;
	}

	/// <summary>Cells where the defender can build, from this snapshot.</summary>
	public bool CanBuild(LevelData level, HashSet<Vector2I> blocked, Vector2I cell) =>
		Grid.InBounds(cell) && !blocked.Contains(cell) && Turrets.All(t => t.Cell[0] != cell.X || t.Cell[1] != cell.Y);
}
