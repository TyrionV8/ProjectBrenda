class_name Board
extends Node2D
## Draws the printed circuit board: solder mask, decorative traces, silkscreen
## and the components that block building. Drawn once and cached by the renderer.

const MASK_CENTER := Color("173e66")
const MASK_EDGE := Color("0a1d33")
const DECOR_TRACE := Color("1f4f7d")
const SILK := Color(0.85, 0.92, 1.0, 0.55)
const GOLD := Color("d9a441")
const GOLD_DARK := Color("7a5212")

var level: LevelData
var cols := Grid.COLS
var rows := Grid.ROWS
var seed_value := 1
var show_grid := false:
	set(value):
		show_grid = value
		queue_redraw()

var _gradient: GradientTexture2D
var _decor: Array[PackedVector2Array] = []
var _vias: PackedVector2Array = []
var _font: Font


func setup(p_level: LevelData, p_cols := Grid.COLS, p_rows := Grid.ROWS, p_seed := 1) -> void:
	level = p_level
	cols = p_cols
	rows = p_rows
	seed_value = p_seed
	_font = load("res://assets/fonts/Silkscreen-Regular.ttf")
	_gradient = GradientTexture2D.new()
	_gradient.fill = GradientTexture2D.FILL_RADIAL
	_gradient.fill_from = Vector2(0.5, 0.45)
	_gradient.fill_to = Vector2(1.1, 1.0)
	var g := Gradient.new()
	g.colors = PackedColorArray([MASK_CENTER, MASK_EDGE])
	_gradient.gradient = g
	_build_decor()
	_spawn_leds()
	var pulses := DecorPulses.new()
	pulses.lines = _decor
	add_child(pulses)
	move_child(pulses, 0)
	queue_redraw()


func board_size() -> Vector2:
	return Vector2(cols, rows) * Grid.CELL


func _build_decor() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	_decor.clear()
	_vias.clear()
	var step := Grid.CELL * 0.5
	var size := board_size()
	for i in 34:
		var p := Vector2(rng.randi_range(1, int(size.x / step) - 1), rng.randi_range(1, int(size.y / step) - 1)) * step
		var line := PackedVector2Array([p])
		var dir: Vector2 = [Vector2.RIGHT, Vector2.LEFT, Vector2.UP, Vector2.DOWN][rng.randi() % 4]
		for s in rng.randi_range(2, 5):
			var length := rng.randi_range(2, 7) * step
			p += dir * length
			p = p.clamp(Vector2.ONE * step, size - Vector2.ONE * step)
			line.append(p)
			# Turn 45 degrees for a short run, then continue orthogonally (classic PCB routing).
			var diag := (dir + dir.orthogonal() * (1 if rng.randf() < 0.5 else -1)).normalized()
			p += diag * step * rng.randi_range(1, 2) * sqrt(2.0)
			p = p.clamp(Vector2.ONE * step, size - Vector2.ONE * step)
			line.append(p)
			dir = dir.orthogonal() * (1 if rng.randf() < 0.5 else -1) if rng.randf() < 0.4 else dir
		_decor.append(line)
		_vias.append(line[0])
		_vias.append(line[-1])


func _spawn_leds() -> void:
	for child in get_children():
		if child is Led or child is DecorPulses:
			child.queue_free()
	if level == null:
		return
	for c in level.components:
		if c.type == "led":
			var led := Led.new()
			led.color = c.color
			led.position = Grid.cell_center(c.cell)
			add_child(led)
			move_child(led, 0)


func _draw() -> void:
	var size := board_size()
	# Drop shadow and board edge.
	draw_rect(Rect2(Vector2(6, 8), size), Color(0, 0, 0, 0.45))
	draw_texture_rect(_gradient, Rect2(Vector2.ZERO, size), false)
	# Fibre-glass weave: very faint diagonal hatching.
	for x in range(-int(size.y), int(size.x), 12):
		draw_line(Vector2(x, 0), Vector2(x + size.y, size.y), Color(1, 1, 1, 0.012), 1.0)
	# Decorative traces and vias.
	for line in _decor:
		draw_polyline(line, Color(0, 0, 0, 0.25), 7.0, true)
		draw_polyline(line, DECOR_TRACE, 4.0, true)
		draw_polyline(line, DECOR_TRACE.lightened(0.15), 1.5, true)
	for via in _vias:
		draw_circle(via, 6.0, DECOR_TRACE.lightened(0.1))
		draw_circle(via, 4.5, Color("b8893a"))
		draw_circle(via, 2.2, Color("05101c"))
	# Mounting holes in the corners, like the original PCB sprite.
	for corner in [Vector2(36, 36), Vector2(size.x - 36, 36), Vector2(36, size.y - 36), size - Vector2(36, 36)]:
		draw_circle(corner, 15.0, GOLD_DARK)
		draw_circle(corner, 12.0, GOLD)
		draw_circle(corner, 8.0, Color("030608"))
	# Board outline silkscreen.
	draw_rect(Rect2(Vector2(10, 10), size - Vector2(20, 20)), SILK * Color(1, 1, 1, 0.35), false, 1.5)
	if _font:
		draw_string(_font, Vector2(60, size.y - 16), "PROJECT BRENDA  REV 0.4", HORIZONTAL_ALIGNMENT_LEFT, -1, 12, SILK * Color(1, 1, 1, 0.6))
	if level:
		for i in level.components.size():
			_draw_component(level.components[i], i)
	if show_grid:
		for x in range(1, cols):
			draw_line(Vector2(x * Grid.CELL, 0), Vector2(x * Grid.CELL, size.y), Color(0, 0, 0, 0.45), 1.0)
		for y in range(1, rows):
			draw_line(Vector2(0, y * Grid.CELL), Vector2(size.x, y * Grid.CELL), Color(0, 0, 0, 0.45), 1.0)


func _draw_component(c: Dictionary, index: int) -> void:
	var rect := Grid.cell_rect(c.cell, c.size)
	var inner := rect.grow(-6)
	match c.type:
		"chip":
			_draw_chip(inner, "U%d" % (index + 1))
		"connector":
			_draw_connector(inner, "J%d" % (index + 1))
		"capacitor":
			_draw_capacitor(rect, "C%d" % (index + 1))
		"resistor":
			_draw_resistor(rect, "R%d" % (index + 1))
		"led":
			_draw_led_pad(rect, "D%d" % (index + 1))


func _silk(text: String, pos: Vector2) -> void:
	if _font:
		draw_string(_font, pos, text, HORIZONTAL_ALIGNMENT_LEFT, -1, 10, SILK)


func _draw_chip(r: Rect2, label: String) -> void:
	var pins := int(r.size.x / 12)
	for i in pins:
		var x := r.position.x + 8 + i * (r.size.x - 16) / maxf(pins - 1, 1)
		draw_rect(Rect2(x - 3, r.position.y - 5, 6, 8), Color("aeb7c2"))
		draw_rect(Rect2(x - 3, r.end.y - 3, 6, 8), Color("aeb7c2"))
	draw_rect(Rect2(r.position + Vector2(3, 4), r.size), Color(0, 0, 0, 0.5))
	draw_rect(r, Color("14171c"))
	draw_rect(Rect2(r.position, Vector2(r.size.x, 5)), Color(1, 1, 1, 0.07))
	draw_rect(r, Color("2c323b"), false, 1.5)
	draw_circle(r.position + Vector2(9, 11), 3.0, Color("2c323b"))
	if _font:
		draw_string(_font, r.get_center() + Vector2(-r.size.x * 0.35, 4), "BRD-%s" % label.substr(1), HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color(1, 1, 1, 0.35))
	_silk(label, r.position + Vector2(0, -8))


func _draw_connector(r: Rect2, label: String) -> void:
	draw_rect(Rect2(r.position + Vector2(3, 4), r.size), Color(0, 0, 0, 0.5))
	draw_rect(r, Color("8d96a3"))
	draw_rect(Rect2(r.position, Vector2(r.size.x, 6)), Color("c5ccd6"))
	draw_rect(Rect2(r.position.x, r.end.y - 5, r.size.x, 5), Color("5e6672"))
	var slots := 2
	for i in slots:
		var y := r.position.y + (i + 0.5) * r.size.y / slots
		draw_rect(Rect2(r.position.x + 10, y - 8, r.size.x - 20, 16), Color("2a2f37"))
		draw_rect(Rect2(r.position.x + 12, y - 6, r.size.x - 24, 4), Color("1a1d22"))
	draw_rect(r, Color("3c434d"), false, 1.5)
	_silk(label, r.position + Vector2(0, -8))


func _draw_capacitor(r: Rect2, label: String) -> void:
	var center := r.get_center()
	var radius := minf(r.size.x, r.size.y) * 0.42
	draw_circle(center + Vector2(3, 4), radius, Color(0, 0, 0, 0.5))
	draw_circle(center, radius + 3, SILK * Color(1, 1, 1, 0.5))
	draw_circle(center, radius, Color("1e3f7a"))
	draw_arc(center, radius - 4, PI * 0.65, PI * 1.35, 24, Color("8fb3ea"), 7.0)
	draw_circle(center, radius * 0.72, Color("a9b4c2"))
	draw_circle(center + Vector2(-4, -5), radius * 0.5, Color("d5dde7"))
	for a in [0.0, PI / 2, PI / 4]:
		var d := Vector2.from_angle(a) * radius * 0.6
		draw_line(center - d, center + d, Color("6b7684"), 2.5)
	_silk(label, r.position + Vector2(0, -2))


func _draw_resistor(r: Rect2, label: String) -> void:
	var y := r.get_center().y
	for x in [r.position.x + 6, r.end.x - 6]:
		draw_circle(Vector2(x, y), 7, GOLD_DARK)
		draw_circle(Vector2(x, y), 5, GOLD)
	draw_line(Vector2(r.position.x + 6, y), Vector2(r.end.x - 6, y), Color("aeb7c2"), 3.0)
	var body := Rect2(r.position.x + 20, y - 10, r.size.x - 40, 20)
	draw_rect(Rect2(body.position + Vector2(2, 3), body.size), Color(0, 0, 0, 0.45))
	draw_rect(body, Color("d8c39a"))
	draw_rect(Rect2(body.position, Vector2(body.size.x, 5)), Color(1, 1, 1, 0.25))
	var bands := [Color("8a4b13"), Color("111111"), Color("d12f2f"), Color("d9a441")]
	for i in bands.size():
		draw_rect(Rect2(body.position.x + 8 + i * 9, body.position.y, 5, body.size.y), bands[i])
	_silk(label, r.position + Vector2(4, y - r.position.y - 14))


func _draw_led_pad(r: Rect2, label: String) -> void:
	var c := r.get_center()
	draw_rect(Rect2(c - Vector2(14, 6), Vector2(8, 12)), GOLD)
	draw_rect(Rect2(c + Vector2(6, -6), Vector2(8, 12)), GOLD)
	draw_rect(Rect2(c - Vector2(11, 11), Vector2(22, 22)), SILK * Color(1, 1, 1, 0.5), false, 1.0)
	_silk(label, r.position + Vector2(2, 9))
