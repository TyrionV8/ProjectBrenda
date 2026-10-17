using System.Text.Json.Serialization;
using Godot;

namespace HardwareDefence.Data;

/// <summary>A bug type from data/enemies.json. Speeds are in pixels per second.</summary>
public sealed class EnemyDef
{
	public string Id { get; set; }
	public string Name { get; set; }
	public float MaxHp { get; set; } = 2;
	public float Speed { get; set; } = 30;
	public int Reward { get; set; } = 1;
	/// <summary>Body radius in pixels, also used for hit detection.</summary>
	public float Radius { get; set; } = 11;
	/// <summary>Kind of bug (informative; "boss" gets the boss effects). The art is assets/sprites/bugs/ID.png.</summary>
	public string Style { get; set; } = "beetle";
	public string ShellColor { get; set; } = "5bd94a";
	public string AccentColor { get; set; } = "1f5f1a";
	public string EyeColor { get; set; } = "ff3d3d";
	public string LegColor { get; set; } = "10220c";
	/// <summary>Damage per second dealt to components next to the route.</summary>
	public float ChewDps { get; set; } = 1;
	/// <summary>Versus mode: energy the attacker pays to send it.</summary>
	public int VersusCost { get; set; } = 10;
	/// <summary>Versus mode: seconds into the match before it can be sent.</summary>
	public float VersusUnlock { get; set; }
	/// <summary>CPU health lost when it gets through.</summary>
	public int CpuDamage { get; set; } = 1;

	/// <summary>A level boss: announced, shown with a health bar, and has an <see cref="Ability"/>.</summary>
	public bool Boss { get; set; }
	/// <summary>Only spawned by a boss (hidden from the editor and versus).</summary>
	public bool Minion { get; set; }
	/// <summary>Short name shown when the boss arrives, like a process name.</summary>
	public string Title { get; set; }
	/// <summary>One line about what it does (boss warning, tooltips).</summary>
	public string Blurb { get; set; }
	/// <summary>
	/// The hacker attack it performs: trojan, keylogger, mitm, ddos, rootkit, ransomware,
	/// worm or panic (see Match.StepAbility). Tuned with the four fields below.
	/// </summary>
	public string Ability { get; set; }
	/// <summary>Seconds between uses (rootkit: seconds visible).</summary>
	public float AbilityEvery { get; set; } = 5;
	/// <summary>How long the effect lasts (rootkit: seconds hidden).</summary>
	public float AbilityTime { get; set; } = 2;
	/// <summary>Reach of the effect in board pixels.</summary>
	public float AbilityRadius { get; set; } = 100;
	/// <summary>Bug type it releases (trojan payload, ddos packets, worm copies) and how many at a time.</summary>
	public string Spawn { get; set; }
	public int SpawnCount { get; set; } = 1;

	/// <summary>Can the attacker send it in versus mode.</summary>
	[JsonIgnore] public bool InVersus => VersusCost > 0 && !Minion;

	[JsonIgnore] public Color Shell => new(ShellColor);
	[JsonIgnore] public Color Accent => new(AccentColor);
	[JsonIgnore] public Color Eye => new(EyeColor);
	[JsonIgnore] public Color Leg => new(LegColor);
}
