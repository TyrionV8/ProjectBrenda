class_name Turret
extends Node2D
## Base for all turrets: stats, targeting helpers, spawn animation and range display.

var def: TurretDef
var stats: Dictionary = {}
var cell := Vector2i.ZERO
var game: Game
## Aim chosen by the player when placing (used by the laser).
var direction := 0.0
var show_range := false:
	set(value):
		show_range = value
		queue_redraw()

var cooldown_left := 0.0
var _status: Sprite2D
var _idle_t := randf() * TAU

@onready var head: Node2D = $Head
@onready var light: PointLight2D = $Light


func setup(p_def: TurretDef, p_cell: Vector2i, p_game: Game, p_stats: Dictionary, p_direction := 0.0) -> void:
	def = p_def
	cell = p_cell
	game = p_game
	stats = p_stats
	direction = p_direction
	position = Grid.cell_center(cell)


func _ready() -> void:
	light.color = def.color
	scale = Vector2(0.3, 0.3)
	var tween := create_tween()
	tween.tween_property(self, "scale", Vector2.ONE, 0.35).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	var energy := light.energy
	light.energy = energy * 4.0
	create_tween().tween_property(light, "energy", energy, 0.6)
	if game:
		game.fx.sparks(position, def.color, 16, 140.0, 0.4, 0.5)
		game.fx.glow_flash(position, def.color, 1.4, 0.3)
	cooldown_left = stat("cooldown") * 0.5
	# Little status LED on the socket corner, blinking in the turret's colour.
	_status = FxUtil.glow_sprite(def.color, 0.12)
	_status.position = Vector2(15, 15)
	add_child(_status)


func _physics_process(delta: float) -> void:
	_idle_t += delta
	_status.modulate.a = 0.35 + 0.65 * absf(sin(_idle_t * 2.2))


## Slow sweep while there is nothing to shoot.
func idle_scan(delta: float) -> void:
	head.rotation = lerp_angle(head.rotation, sin(_idle_t * 0.7) * 1.2 + cell.x, 1.0 - exp(-2.0 * delta))


func stat(name: String) -> float:
	return float(stats.get(name, 0.0))


func attack_range() -> float:
	return stat("range")


func enemies_in_range(range_px: float, from := position) -> Array[Enemy]:
	var result: Array[Enemy] = []
	for e in game.enemies:
		if not e.dead and from.distance_to(e.position) <= range_px + e.radius * 0.5:
			result.append(e)
	return result


## The bug that is furthest along the route (closest to the CPU) within range.
func furthest_in_range(range_px: float) -> Enemy:
	var best: Enemy = null
	for e in enemies_in_range(range_px):
		if best == null or e.progress > best.progress:
			best = e
	return best


func nearest_enemy(from := position, max_range := INF) -> Enemy:
	var best: Enemy = null
	var best_d := max_range
	for e in game.enemies:
		if e.dead:
			continue
		var d := from.distance_to(e.position)
		if d < best_d:
			best = e
			best_d = d
	return best


func aim_towards(target: Vector2, delta: float, speed := 14.0) -> void:
	var wanted := (target - position).angle()
	head.rotation = lerp_angle(head.rotation, wanted, 1.0 - exp(-speed * delta))


func muzzle(distance := 26.0) -> Vector2:
	return position + Vector2.from_angle(head.rotation) * distance


func recoil(amount := 5.0) -> void:
	var sprite := head.get_child(0) as Node2D
	sprite.position = Vector2(-amount, 0)
	create_tween().tween_property(sprite, "position", Vector2.ZERO, 0.18).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	light.energy = 2.0
	create_tween().tween_property(light, "energy", 1.0, 0.25)


func _draw() -> void:
	if not show_range:
		return
	var r := attack_range()
	if r <= 0.0:
		return
	draw_circle(Vector2.ZERO, r, Color(def.color, 0.07))
	draw_arc(Vector2.ZERO, r, 0, TAU, 64, Color(def.color, 0.55), 2.0, true)
