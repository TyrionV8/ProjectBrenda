using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace Brenda.Data;

/// <summary>One upgradable stat of a turret. Each bought level adds <see cref="PerLevel"/> to <see cref="Stat"/>.</summary>
public sealed class UpgradeDef
{
	public string Id { get; set; }
	public string Label { get; set; }
	public string Description { get; set; }
	public string Stat { get; set; }
	public float PerLevel { get; set; }
	public int MaxLevel { get; set; } = 15;
	public string Icon { get; set; }
}

/// <summary>A turret type from data/turrets.json. Stats are linear: base + per_level * upgrade level.</summary>
public sealed class TurretDef
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	/// <summary>Which simulation behaviour drives it: blaster, mortar, burst, laser or freezer.</summary>
	public string Behavior { get; set; }
	public int Cost { get; set; }
	[JsonPropertyName("color")] public string ColorHex { get; set; } = "ffffff";
	public string Icon { get; set; }
	public bool NeedsDirection { get; set; }
	public Dictionary<string, float> Stats { get; set; } = new();
	public Dictionary<string, float[]> Limits { get; set; } = new();
	public List<UpgradeDef> Upgrades { get; set; } = new();

	[JsonIgnore] public Color Color => new(ColorHex);

	public Dictionary<string, float> ComputeStats(IReadOnlyDictionary<string, int> levels)
	{
		var stats = new Dictionary<string, float>(Stats);
		foreach (var up in Upgrades)
		{
			var level = levels != null && levels.TryGetValue(up.Id, out var l) ? l : 0;
			stats[up.Stat] = stats.GetValueOrDefault(up.Stat) + up.PerLevel * level;
		}
		foreach (var (stat, limit) in Limits)
			if (stats.ContainsKey(stat))
				stats[stat] = Mathf.Clamp(stats[stat], limit[0], limit[1]);
		return stats;
	}

	public UpgradeDef FindUpgrade(string id) => Upgrades.Find(u => u.Id == id);
}
