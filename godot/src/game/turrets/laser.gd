extends Turret
## Turret 04: a continuous beam in a fixed direction chosen when placing it.

const BEAM_WIDTH := 6.0

var _beam: Node2D
var _beam_light: PointLight2D
var _t := 0.0
var _spark_timer := 0.0
var _hitting: Array[Vector2] = []


func _ready() -> void:
	super()
	head.rotation = direction
	_beam = Node2D.new()
	_beam.material = FxUtil.additive()
	_beam.draw.connect(_draw_beam)
	add_child(_beam)
	_beam_light = FxUtil.light(def.color, 0.7, 0.35)
	add_child(_beam_light)
	_update_beam_light()


func attack_range() -> float:
	return 0.0


func beam_end_local() -> Vector2:
	return Vector2.from_angle(direction) * (24.0 + stat("beam_length"))


func _update_beam_light() -> void:
	var length := stat("beam_length")
	_beam_light.position = Vector2.from_angle(direction) * (24.0 + length * 0.5)
	_beam_light.rotation = direction
	_beam_light.scale = Vector2(maxf(length / 90.0, 0.5), 0.6)


func _process(delta: float) -> void:
	_t += delta
	_spark_timer -= delta
	var from := position + Vector2.from_angle(direction) * 24.0
	var to := position + beam_end_local()
	var dps := stat("dps")
	_hitting.clear()
	for e in game.enemies:
		if e.dead:
			continue
		var closest := Geometry2D.get_closest_point_to_segment(e.position, from, to)
		if closest.distance_to(e.position) <= e.radius + BEAM_WIDTH * 0.5:
			_hitting.append(e.position - position)
			e.take_damage(dps * delta)
	if not _hitting.is_empty() and _spark_timer <= 0.0:
		_spark_timer = 0.07
		for p in _hitting:
			game.fx.sparks(position + p, def.color, 4, 120.0, 0.25, 0.35)
	_beam.queue_redraw()


func _draw_beam() -> void:
	var from := Vector2.from_angle(direction) * 24.0
	var to := beam_end_local()
	var flicker := 0.85 + 0.15 * sin(_t * 40.0) + (0.1 if not _hitting.is_empty() else 0.0)
	_beam.draw_line(from, to, Color(def.color, 0.25 * flicker), BEAM_WIDTH * 3.0, true)
	_beam.draw_line(from, to, Color(def.color, 0.7 * flicker), BEAM_WIDTH, true)
	_beam.draw_line(from, to, Color(1, 0.9, 0.95, 0.9), 2.0, true)
	# Energy travelling along the beam.
	var length := from.distance_to(to)
	var dir := (to - from) / maxf(length, 1.0)
	var k := fmod(_t * 220.0, 40.0)
	while k < length:
		_beam.draw_circle(from + dir * k, 2.5, Color(1, 1, 1, 0.8))
		k += 40.0
	_beam.draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(to - Vector2(14, 14), Vector2(28, 28)), false, Color(def.color, 0.8))
	_beam.draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(from - Vector2(16, 16), Vector2(32, 32)), false, Color(def.color, flicker))
