class_name BuildController
extends Node2D
## Placing turrets: a ghost follows the pointer, snapped to the grid.
## Click/tap to build; the laser needs a second click to choose its direction.
## Right click, Esc or the HUD cancel button stop placing.

signal selection_changed(def: TurretDef)

const SOCKET := preload("res://assets/art/turrets/socket.svg")

var game: Game
var selected: TurretDef
var aiming := false
var _cell := Vector2i(-100, -100)
var _aim_cell := Vector2i.ZERO
var _aim_dir := 0.0
var _hover_turret: Turret
var _pointer_inside := false
var _deny_flash := 0.0


func _ready() -> void:
	material = FxUtil.unshaded()
	z_index = 10


func select(def: TurretDef) -> void:
	if game.state != Game.State.PLAYING:
		return
	selected = def
	aiming = false
	Audio.ui("click")
	selection_changed.emit(def)
	_update_pointer(get_local_mouse_position())
	queue_redraw()


func cancel() -> void:
	if selected == null:
		return
	selected = null
	aiming = false
	selection_changed.emit(null)
	queue_redraw()


## True while placing; Esc then cancels instead of pausing.
func is_busy() -> bool:
	return selected != null


func _unhandled_input(event: InputEvent) -> void:
	if game == null or game.state != Game.State.PLAYING:
		return
	if event is InputEventMouseMotion:
		_update_pointer(get_local_mouse_position())
	elif event is InputEventMouseButton and event.pressed:
		_update_pointer(get_local_mouse_position())
		if event.button_index == MOUSE_BUTTON_LEFT:
			_on_primary()
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			_on_secondary()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("pause") and selected != null:
		_on_secondary()
		get_viewport().set_input_as_handled()


func _process(delta: float) -> void:
	if _deny_flash > 0.0:
		_deny_flash = maxf(_deny_flash - delta * 3.0, 0.0)
		queue_redraw()


func _on_primary() -> void:
	if selected == null:
		return
	if aiming:
		if game.build_turret(selected, _aim_cell, _aim_dir):
			aiming = false
		else:
			_deny()
		queue_redraw()
		return
	if not _pointer_inside:
		return
	if not game.can_build(_cell) or game.money < selected.cost:
		_deny()
		return
	if selected.needs_direction:
		aiming = true
		_aim_cell = _cell
		_aim_dir = 0.0
		Audio.ui("click")
		queue_redraw()
	else:
		game.build_turret(selected, _cell)


func _on_secondary() -> void:
	if aiming:
		aiming = false
		queue_redraw()
	else:
		cancel()


func _deny() -> void:
	_deny_flash = 1.0
	Audio.play("error", &"UI")
	game.hud.flash_money()


func _update_pointer(local: Vector2) -> void:
	_pointer_inside = Rect2(Vector2.ZERO, Grid.SIZE).has_point(local)
	if aiming:
		var dir := local - Grid.cell_center(_aim_cell)
		if dir.length() > 6.0:
			_aim_dir = snappedf(dir.angle(), PI / 2)
		queue_redraw()
		return
	var cell := Grid.to_cell(local)
	if cell != _cell:
		_cell = cell
		queue_redraw()
	var hovered := game.turret_at(cell) if _pointer_inside and selected == null else null
	if hovered != _hover_turret:
		if is_instance_valid(_hover_turret):
			_hover_turret.show_range = false
		_hover_turret = hovered
		if _hover_turret:
			_hover_turret.show_range = true


func _draw() -> void:
	if selected == null:
		return
	var cell := _aim_cell if aiming else _cell
	if not aiming and not _pointer_inside:
		return
	var center := Grid.cell_center(cell)
	var ok := aiming or (game.can_build(cell) and game.money >= selected.cost)
	var color := Color(0.3, 1.0, 0.85) if ok else Color(1.0, 0.3, 0.35)
	color = color.lerp(Color(1, 0.2, 0.2), _deny_flash)
	var rect := Grid.cell_rect(cell).grow(-2)
	draw_rect(rect, Color(color, 0.18))
	draw_rect(rect, Color(color, 0.9), false, 2.0)
	var stats := GameState.turret_stats(selected)
	var r := float(stats.get("range", 0.0))
	if r > 0.0:
		draw_circle(center, r, Color(color, 0.06))
		draw_arc(center, r, 0, TAU, 64, Color(color, 0.5), 1.5, true)
	draw_set_transform(center, 0.0, Vector2.ONE * 0.5)
	draw_texture(SOCKET, -SOCKET.get_size() / 2, Color(1, 1, 1, 0.55))
	draw_set_transform(center, _aim_dir if aiming else 0.0, Vector2.ONE * 0.5)
	draw_texture(selected.icon, -selected.icon.get_size() / 2, Color(1, 1, 1, 0.7))
	draw_set_transform_matrix(Transform2D.IDENTITY)
	if selected.needs_direction:
		var length := 24.0 + float(stats.get("beam_length", 48.0))
		for d in 4:
			var a := d * PI / 2
			var active := aiming and is_equal_approx(wrapf(a - _aim_dir, -PI, PI), 0.0)
			var tip := center + Vector2.from_angle(a) * (length if active else 30.0)
			draw_line(center + Vector2.from_angle(a) * 24.0, tip, Color(selected.color, 0.9 if active else 0.3), 4.0 if active else 2.0, true)
		if not aiming:
			_label(center + Vector2(-60, -34), "click to aim")
		else:
			_label(center + Vector2(-60, -34), "click to confirm")


func _label(pos: Vector2, text: String) -> void:
	var font := ThemeDB.fallback_font
	draw_string_outline(font, pos, text, HORIZONTAL_ALIGNMENT_CENTER, 120, 14, 4, Color(0, 0, 0, 0.8))
	draw_string(font, pos, text, HORIZONTAL_ALIGNMENT_CENTER, 120, 14, Color.WHITE)
