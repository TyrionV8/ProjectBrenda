class_name FreezeOrb
extends Projectile
## Frost orb: waits next to its turret, then homes in on a bug and slows it down.

var slow_factor := 0.7
var slow_duration := 3.0
var speed := 30.0
var search_range := 150.0
var launched := false
var _target: Variant = null
var _t := randf() * TAU
var _anchor := Vector2.ZERO


func _ready() -> void:
	material = FxUtil.additive()
	_anchor = position


func _process(delta: float) -> void:
	_t += delta
	if not is_instance_valid(_target) or _target.dead:
		_target = null
	if _target == null:
		_target = _find_target()
		if _target != null:
			launched = true
	if _target == null:
		if not launched:
			position = _anchor + Vector2(0, sin(_t * 3.0) * 2.0)
		queue_redraw()
		return
	var to_target: Vector2 = _target.position - position
	var dist: float = to_target.length()
	if dist <= _target.radius + 5.0:
		_target.apply_slow(slow_factor, slow_duration)
		game.fx.sparks(position, color, 12, 120.0, 0.4, 0.45)
		game.fx.glow_flash(position, color, 1.0, 0.25)
		Audio.play("freeze", &"SFX", -6.0)
		queue_free()
		return
	position += to_target / dist * minf(speed * delta, dist)
	queue_redraw()


func _find_target() -> Enemy:
	var best: Enemy = null
	var best_d := search_range
	for e in game.enemies:
		if e.dead:
			continue
		var d := position.distance_to(e.position)
		if d <= best_d:
			best = e
			best_d = d
	return best


func _draw() -> void:
	draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(-Vector2(16, 16), Vector2(32, 32)), false, Color(color, 0.85))
	draw_circle(Vector2.ZERO, 3.5, Color(0.85, 0.97, 1.0))
	for i in 3:
		var a := _t * 2.5 + TAU * i / 3.0
		draw_circle(Vector2.from_angle(a) * 8.0, 1.3, Color(0.8, 0.95, 1.0, 0.9))
