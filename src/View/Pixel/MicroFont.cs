using System.Collections.Generic;
using Godot;

namespace HardwareDefence.View;

/// <summary>
/// A 3x5 pixel font for text that lives on the board (chip markings, silkscreen, CPU health),
/// so in-world text uses the same pixel grid as the art. Lower case is drawn as upper case.
/// </summary>
public static class MicroFont
{
	public const int Height = 5;
	const int Spacing = 1;

	static readonly Dictionary<char, string[]> Glyphs = new()
	{
		['A'] = new[] { ".#.", "#.#", "###", "#.#", "#.#" },
		['B'] = new[] { "##.", "#.#", "##.", "#.#", "##." },
		['C'] = new[] { ".##", "#..", "#..", "#..", ".##" },
		['D'] = new[] { "##.", "#.#", "#.#", "#.#", "##." },
		['E'] = new[] { "###", "#..", "##.", "#..", "###" },
		['F'] = new[] { "###", "#..", "##.", "#..", "#.." },
		['G'] = new[] { ".##", "#..", "#.#", "#.#", ".##" },
		['H'] = new[] { "#.#", "#.#", "###", "#.#", "#.#" },
		['I'] = new[] { "###", ".#.", ".#.", ".#.", "###" },
		['J'] = new[] { "..#", "..#", "..#", "#.#", ".#." },
		['K'] = new[] { "#.#", "#.#", "##.", "#.#", "#.#" },
		['L'] = new[] { "#..", "#..", "#..", "#..", "###" },
		['M'] = new[] { "#.#", "###", "###", "#.#", "#.#" },
		['N'] = new[] { "##.", "#.#", "#.#", "#.#", "#.#" },
		['O'] = new[] { "###", "#.#", "#.#", "#.#", "###" },
		['P'] = new[] { "##.", "#.#", "##.", "#..", "#.." },
		['Q'] = new[] { ".#.", "#.#", "#.#", "##.", ".##" },
		['R'] = new[] { "##.", "#.#", "##.", "#.#", "#.#" },
		['S'] = new[] { ".##", "#..", ".#.", "..#", "##." },
		['T'] = new[] { "###", ".#.", ".#.", ".#.", ".#." },
		['U'] = new[] { "#.#", "#.#", "#.#", "#.#", "###" },
		['V'] = new[] { "#.#", "#.#", "#.#", "#.#", ".#." },
		['W'] = new[] { "#.#", "#.#", "###", "###", "#.#" },
		['X'] = new[] { "#.#", "#.#", ".#.", "#.#", "#.#" },
		['Y'] = new[] { "#.#", "#.#", ".#.", ".#.", ".#." },
		['Z'] = new[] { "###", "..#", ".#.", "#..", "###" },
		['0'] = new[] { ".#.", "#.#", "#.#", "#.#", ".#." },
		['1'] = new[] { ".#.", "##.", ".#.", ".#.", "###" },
		['2'] = new[] { "##.", "..#", ".#.", "#..", "###" },
		['3'] = new[] { "##.", "..#", ".#.", "..#", "##." },
		['4'] = new[] { "#.#", "#.#", "###", "..#", "..#" },
		['5'] = new[] { "###", "#..", "##.", "..#", "##." },
		['6'] = new[] { ".##", "#..", "###", "#.#", "###" },
		['7'] = new[] { "###", "..#", ".#.", ".#.", ".#." },
		['8'] = new[] { "###", "#.#", "###", "#.#", "###" },
		['9'] = new[] { "###", "#.#", "###", "..#", "##." },
		['.'] = new[] { ".", ".", ".", ".", "#" },
		[','] = new[] { ".", ".", ".", "#", "#" },
		[':'] = new[] { ".", "#", ".", "#", "." },
		['!'] = new[] { "#", "#", "#", ".", "#" },
		['\''] = new[] { "#", "#", ".", ".", "." },
		['-'] = new[] { "...", "...", "###", "...", "..." },
		['+'] = new[] { "...", ".#.", "###", ".#.", "..." },
		['='] = new[] { "...", "###", "...", "###", "..." },
		['/'] = new[] { "..#", "..#", ".#.", "#..", "#.." },
		['%'] = new[] { "#.#", "..#", ".#.", "#..", "#.#" },
		['?'] = new[] { "##.", "..#", ".#.", "...", ".#." },
		['$'] = new[] { ".##", "##.", ".#.", ".##", "##." },
		['#'] = new[] { "#.#", "###", "#.#", "###", "#.#" },
		['_'] = new[] { "...", "...", "...", "...", "###" },
		['*'] = new[] { "#.#", ".#.", "#.#", "...", "..." },
		['('] = new[] { ".#", "#.", "#.", "#.", ".#" },
		[')'] = new[] { "#.", ".#", ".#", ".#", "#." },
		['<'] = new[] { "..#", ".#.", "#..", ".#.", "..#" },
		['>'] = new[] { "#..", ".#.", "..#", ".#.", "#.." },
		[' '] = new[] { "..", "..", "..", "..", ".." },
	};

	static string[] Glyph(char ch) => Glyphs.TryGetValue(char.ToUpperInvariant(ch), out var g) ? g : Glyphs['?'];

	public static int Width(string text)
	{
		if (string.IsNullOrEmpty(text))
			return 0;
		var w = 0;
		foreach (var ch in text)
			w += Glyph(ch)[0].Length + Spacing;
		return w - Spacing;
	}

	/// <summary>Draws into a pixel canvas with the top-left corner at (x, y).</summary>
	public static void Draw(PixelCanvas canvas, int x, int y, string text, Color color)
	{
		foreach (var ch in text ?? "")
		{
			var g = Glyph(ch);
			for (var row = 0; row < Height; row++)
				for (var col = 0; col < g[row].Length; col++)
					if (g[row][col] == '#')
						canvas.Plot(x + col, y + row, color);
			x += g[0].Length + Spacing;
		}
	}

	/// <summary>
	/// Draws on a canvas item in world units, <paramref name="size"/> world units per font pixel
	/// (<see cref="PixelWorld.Scale"/> = one art pixel). <paramref name="pos"/> is the top-left corner.
	/// </summary>
	public static void Draw(CanvasItem ci, Vector2 pos, string text, Color color, float size = PixelWorld.Scale)
	{
		foreach (var ch in text ?? "")
		{
			var g = Glyph(ch);
			for (var row = 0; row < Height; row++)
				for (var col = 0; col < g[row].Length; col++)
					if (g[row][col] == '#')
						ci.DrawRect(new Rect2(pos + new Vector2(col, row) * size, Vector2.One * size), color);
			pos.X += (g[0].Length + Spacing) * size;
		}
	}

	/// <summary>Like <see cref="Draw(CanvasItem, Vector2, string, Color, float)"/> with a 1-pixel dark outline, centred on pos.</summary>
	public static void DrawOutlined(CanvasItem ci, Vector2 center, string text, Color color, Color outline, float size = PixelWorld.Scale)
	{
		var topLeft = center - new Vector2(Width(text), Height) * size / 2;
		topLeft = (topLeft / PixelWorld.Scale).Floor() * PixelWorld.Scale;
		foreach (var d in new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1), new Vector2(1, 1) })
			Draw(ci, topLeft + d * PixelWorld.Scale, text, outline, size);
		Draw(ci, topLeft, text, color, size);
	}
}
