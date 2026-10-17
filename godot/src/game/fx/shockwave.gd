class_name Shockwave
extends Node2D
## Expanding ring that fades out.

var radius := 60.0
var color := Color.WHITE
var duration := 0.4
var _t := 0.0


func _ready() -> void:
	material = FxUtil.additive()


func _process(delta: float) -> void:
	_t += delta / duration
	if _t >= 1.0:
		queue_free()
		return
	queue_redraw()


func _draw() -> void:
	var k := ease(_t, 0.4)
	var r := lerpf(radius * 0.15, radius, k)
	var a := 1.0 - _t
	draw_arc(Vector2.ZERO, r, 0, TAU, 48, Color(color, a * 0.9), lerpf(10.0, 1.5, _t), true)
	draw_arc(Vector2.ZERO, r * 0.85, 0, TAU, 48, Color(color, a * 0.25), lerpf(18.0, 3.0, _t), true)
