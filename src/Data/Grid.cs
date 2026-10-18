using Godot;

namespace HardwareDefence.Data;

/// <summary>Board geometry. Gameplay uses board-local pixels: (0, 0) is the corner of cell (0, 0).</summary>
public static class Grid
{
	public const int Cell = 48;
	public const int Cols = 21;
	public const int Rows = 16;
	/// <summary>Where the board sits on the 1366x768 screen (the left HUD column is 144 px wide).</summary>
	public static readonly Vector2 Origin = new(144, 0);
	public static readonly Vector2 Size = new(Cols * Cell, Rows * Cell);

	public static Vector2 CellCenter(Vector2I cell) => new Vector2(cell.X, cell.Y) * Cell + Vector2.One * (Cell * 0.5f);

	public static Rect2 CellRect(Vector2I cell, Vector2I size) => new(new Vector2(cell.X, cell.Y) * Cell, new Vector2(size.X, size.Y) * Cell);

	public static Vector2I ToCell(Vector2 local) => new(Mathf.FloorToInt(local.X / Cell), Mathf.FloorToInt(local.Y / Cell));

	public static bool InBounds(Vector2I cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Cols && cell.Y < Rows;
}
