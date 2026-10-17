using System.Collections.Generic;
using Godot;

namespace HardwareDefence.Data;

/// <summary>Gameplay properties of each kind of board component.</summary>
public sealed record ComponentType(
	string Id,
	string Name,
	Vector2I DefaultSize,
	float HpPerCell,
	bool Blocks = true,
	bool Chewable = true);

public static class ComponentTypes
{
	public static readonly Dictionary<string, ComponentType> All = new()
	{
		["chip"] = new("chip", "IC chip", new(2, 2), 12),
		["bga"] = new("bga", "BGA package", new(3, 3), 16),
		["header"] = new("header", "Pin header", new(1, 4), 6),
		["connector"] = new("connector", "Connector", new(2, 2), 10),
		["capacitor"] = new("capacitor", "Electrolytic capacitor", new(1, 1), 8),
		["smd"] = new("smd", "SMD passives", new(1, 1), 4),
		["resistor"] = new("resistor", "Resistor", new(2, 1), 5),
		["led"] = new("led", "LED", new(1, 1), 3),
		["crystal"] = new("crystal", "Crystal", new(2, 1), 6),
		["inductor"] = new("inductor", "Choke", new(1, 1), 10),
		["mosfet"] = new("mosfet", "MOSFET", new(1, 1), 8),
		["ram_slot"] = new("ram_slot", "RAM slot", new(8, 1), 8),
		["pcie_slot"] = new("pcie_slot", "PCIe slot", new(10, 1), 8),
		["sata"] = new("sata", "SATA port", new(1, 1), 6),
		["port"] = new("port", "I/O port", new(2, 2), 14),
		["battery"] = new("battery", "Coin battery", new(2, 2), 10),
		["heatsink"] = new("heatsink", "Heatsink", new(3, 3), 20),
		["shield"] = new("shield", "RF shield", new(3, 2), 14),
		["switch"] = new("switch", "Key switch", new(1, 1), 6),
		["jumper"] = new("jumper", "Jumper", new(1, 1), 4),
		["fingers"] = new("fingers", "Edge connector", new(8, 1), 20),
		["hole"] = new("hole", "Mounting hole", new(1, 1), 0, Blocks: true, Chewable: false),
		["label"] = new("label", "Silkscreen text", new(3, 1), 0, Blocks: false, Chewable: false),
	};

	public static ComponentType Get(string id) => All.TryGetValue(id, out var t) ? t : All["chip"];
}
