class_name Grid
extends RefCounted
## Board geometry. Everything inside the board uses board-local coordinates,
## where (0, 0) is the top-left corner of cell (0, 0).

const CELL := 48
const COLS := 21
const ROWS := 16
## Where the board sits on the 1366x768 screen (the left HUD column is 144 px wide).
const ORIGIN := Vector2(144, 0)
const SIZE := Vector2(COLS * CELL, ROWS * CELL)


static func cell_center(cell: Vector2i) -> Vector2:
	return Vector2(cell) * CELL + Vector2.ONE * (CELL * 0.5)


static func cell_rect(cell: Vector2i, size := Vector2i.ONE) -> Rect2:
	return Rect2(Vector2(cell) * CELL, Vector2(size) * CELL)


static func to_cell(local_pos: Vector2) -> Vector2i:
	return Vector2i(floori(local_pos.x / CELL), floori(local_pos.y / CELL))


static func in_bounds(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < COLS and cell.y < ROWS
