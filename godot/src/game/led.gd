class_name Led
extends Node2D
## A blinking SMD LED: coloured dome, additive glow and a small light.

var color := Color("ff3344")

var _glow: Sprite2D
var _light: PointLight2D
var _phase := randf() * TAU
var _speed := randf_range(0.6, 2.2)


func _ready() -> void:
	_glow = FxUtil.glow_sprite(color, 0.9)
	add_child(_glow)
	_light = PointLight2D.new()
	_light.texture = FxUtil.LIGHT_TEXTURE
	_light.color = color
	_light.texture_scale = 0.45
	add_child(_light)


func _process(delta: float) -> void:
	_phase += delta * _speed
	var on := 0.55 + 0.45 * sin(_phase) if fmod(_phase, 7.0) < 6.6 else 0.15
	_glow.modulate.a = on
	_light.energy = on * 0.9


func _draw() -> void:
	draw_circle(Vector2(1, 2), 9, Color(0, 0, 0, 0.45))
	draw_circle(Vector2.ZERO, 9, color.darkened(0.55))
	draw_circle(Vector2.ZERO, 7, color)
	draw_circle(Vector2(-2.5, -2.5), 3, color.lightened(0.7))
