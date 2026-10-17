using Godot;

namespace HardwareDefence.View;

/// <summary>Solder mask colours for each PCB theme.</summary>
public sealed record BoardTheme(Color Center, Color Edge, Color Decor, Color Silk)
{
	public static BoardTheme Get(string name) => name switch
	{
		"green" => new(new Color("1f5c38"), new Color("0b2616"), new Color("2f7d4c"), new Color(0.92f, 0.96f, 0.9f, 0.6f)),
		"red" => new(new Color("6b1d24"), new Color("2b0a0d"), new Color("8c2f37"), new Color(0.98f, 0.92f, 0.9f, 0.6f)),
		"black" => new(new Color("272b31"), new Color("0b0c0e"), new Color("3a4049"), new Color(0.9f, 0.92f, 0.95f, 0.5f)),
		"purple" => new(new Color("402566"), new Color("170c29"), new Color("5b3b8c"), new Color(0.95f, 0.9f, 1f, 0.6f)),
		"white" => new(new Color("c9d0d8"), new Color("8f99a5"), new Color("b4bdc8"), new Color(0.1f, 0.12f, 0.15f, 0.6f)),
		_ => new(new Color("173e66"), new Color("0a1d33"), new Color("1f4f7d"), new Color(0.85f, 0.92f, 1f, 0.55f)),
	};

	public static readonly string[] Names = { "blue", "green", "red", "black", "purple", "white" };
}
