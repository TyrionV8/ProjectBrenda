using System.Collections.Generic;

namespace Brenda.Editor;

/// <summary>Named colours offered by the editor for LEDs, capacitors, slots, switches...</summary>
public static class ComponentPresets
{
	public static readonly (string Name, string Hex)[] Colors =
	{
		("Default", null),
		("Red", "ff3344"),
		("Green", "3dff6a"),
		("Blue", "2f6fd6"),
		("Yellow", "ffd34d"),
		("Orange", "ff9a3c"),
		("Purple", "9b5cff"),
		("Cyan", "3ef0ff"),
		("White", "e9eef3"),
		("Black", "16191e"),
		("Grey", "6d7682"),
	};

	/// <summary>What the "label" field does for each type.</summary>
	public static readonly Dictionary<string, string> LabelHints = new()
	{
		["port"] = "usb, rj45, hdmi or audio",
		["label"] = "text printed on the board",
		["chip"] = "marking, e.g. U3",
		["bga"] = "marking, e.g. GPU",
		["crystal"] = "frequency, e.g. 25.000",
		["inductor"] = "marking, e.g. R47",
		["shield"] = "marking",
	};
}
