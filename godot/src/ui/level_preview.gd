class_name LevelPreview
extends Control
## Miniature of a level: board, route, components and the CPU.

var level: LevelData
var locked := false


func _init(p_level: LevelData, p_locked: bool) -> void:
	level = p_level
	locked = p_locked
	custom_minimum_size = Vector2(252, 192)
	mouse_filter = Control.MOUSE_FILTER_IGNORE


func _draw() -> void:
	var k := size.x / Grid.SIZE.x
	var board := Rect2(Vector2.ZERO, Grid.SIZE * k)
	draw_rect(board, Color("123456") if not locked else Color("1a2230"))
	draw_rect(board, Color(UiTheme.CYAN, 0.3), false, 1.0)
	for c in level.components:
		var r := Grid.cell_rect(c.cell, c.size)
		draw_rect(Rect2(r.position * k, r.size * k).grow(-1), Color(0.05, 0.06, 0.08))
	var points := PackedVector2Array()
	for p in level.path_points():
		points.append(p * k)
	draw_polyline(points, Color("c8892e") if not locked else Color("4a4f58"), 26.0 * k + 2, true)
	draw_circle(Grid.cell_center(level.cpu_cell()) * k, 7, UiTheme.CYAN if not locked else UiTheme.DIM)
	draw_circle(level.entry_point() * k, 5, UiTheme.RED if not locked else UiTheme.DIM)
