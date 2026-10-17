namespace Brenda.Sim;

/// <summary>
/// Versus mode: the defender (host) builds turrets, the attacker spends energy to send
/// bugs. The defender wins if the CPU survives until the timer runs out.
/// </summary>
public sealed class VersusRules
{
	public float Duration { get; set; } = 300;
	public float StartEnergy { get; set; } = 40;
	/// <summary>Energy per second at the start of the match.</summary>
	public float EnergyRate { get; set; } = 4;
	/// <summary>The rate grows by this much every second.</summary>
	public float EnergyRateGrowth { get; set; } = 0.02f;
	public float EnergyCap { get; set; } = 400;
	/// <summary>Energy the attacker earns per HP of each component its bugs destroy.</summary>
	public float ComponentEnergy { get; set; } = 0.5f;
	/// <summary>Extra money for the defender at the start (on top of the level's).</summary>
	public int DefenderBonusMoney { get; set; } = 20;
	/// <summary>Defender money per second, on top of kill rewards.</summary>
	public float DefenderIncome { get; set; } = 0.6f;
	/// <summary>Minimum seconds between two bugs sent by the attacker.</summary>
	public float SendInterval { get; set; } = 0.2f;
}
