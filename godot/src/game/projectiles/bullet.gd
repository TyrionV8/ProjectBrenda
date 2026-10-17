class_name Bullet
extends Projectile
## Straight shot that damages the first bug it touches.

var velocity := Vector2.ZERO
var damage := 1.0
var size := 4.0
var max_distance := 400.0
var _travelled := 0.0


func _ready() -> void:
	material = FxUtil.additive()


func _process(delta: float) -> void:
	var from := position
	var step := velocity * delta
	position += step
	_travelled += step.length()
	# Test the whole segment travelled this frame so fast shots can't skip over a bug.
	for e in game.enemies:
		if e.dead:
			continue
		var hit := Geometry2D.get_closest_point_to_segment(e.position, from, position)
		if hit.distance_to(e.position) <= e.radius + size:
			e.take_damage(damage)
			game.fx.sparks(hit, color, 5, 90.0, 0.2, 0.3)
			queue_free()
			return
	if _travelled >= max_distance or out_of_board():
		queue_free()
		return
	queue_redraw()


func _draw() -> void:
	var back := -velocity.normalized() * minf(_travelled, 16.0)
	draw_line(back, Vector2.ZERO, Color(color, 0.35), size * 1.6, true)
	draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(-Vector2.ONE * size * 4, Vector2.ONE * size * 8), false, Color(color, 0.9))
	draw_circle(Vector2.ZERO, size * 0.7, Color(1, 1, 1, 0.95))
