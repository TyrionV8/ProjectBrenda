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

	[JsonIgnore] public Color Shell => new(ShellColor);
	[JsonIgnore] public Color Accent => new(AccentColor);
	[JsonIgnore] public Color Eye => new(EyeColor);
	[JsonIgnore] public Color Leg => new(LegColor);
}
