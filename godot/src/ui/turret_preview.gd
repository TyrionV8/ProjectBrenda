class_name TurretPreview
extends Control
## A turret drawn large with its head slowly scanning; used on the upgrade screen.

const SOCKET := preload("res://assets/art/turrets/socket.svg")

var def: TurretDef
var _t := randf() * TAU


func _init(p_def: TurretDef) -> void:
	def = p_def
	custom_minimum_size = Vector2(110, 110)
	mouse_filter = Control.MOUSE_FILTER_IGNORE


func _process(delta: float) -> void:
	_t += delta
	queue_redraw()


func _draw() -> void:
	var c := size / 2
	draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(c - Vector2(70, 70), Vector2(140, 140)), false, Color(def.color, 0.25 + 0.08 * sin(_t * 2.0)))
	draw_texture_rect(SOCKET, Rect2(c - Vector2(44, 44), Vector2(88, 88)), false)
	draw_set_transform(c, sin(_t * 0.8) * 0.6 if not def.needs_direction else 0.0, Vector2.ONE)
	draw_texture_rect(def.icon, Rect2(-Vector2(44, 44), Vector2(88, 88)), false)
	draw_set_transform_matrix(Transform2D.IDENTITY)
