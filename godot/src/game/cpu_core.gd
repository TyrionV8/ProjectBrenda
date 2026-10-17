class_name CpuCore
extends Node2D
## The processor the player defends. Shows its integrity as a ring of segments.

signal destroyed

const MAX_HP := 10
const HEALTHY := Color(0.25, 0.95, 1.0)
const CRITICAL := Color(1.0, 0.25, 0.3)

var hp := MAX_HP
var _sprite: Sprite2D
var _core: Sprite2D
var _halo: Sprite2D
var _light: PointLight2D
var _ring: Node2D
var _t := 0.0
var _hit := 0.0
var _font: Font = preload("res://assets/fonts/Silkscreen-Bold.ttf")


func _ready() -> void:
	_halo = FxUtil.glow_sprite(HEALTHY, 2.2)
	_halo.modulate.a = 0.25
	add_child(_halo)
	_sprite = Sprite2D.new()
	_sprite.texture = preload("res://assets/art/board/cpu.svg")
	_sprite.scale = Vector2.ONE * 0.5
	add_child(_sprite)
	_core = FxUtil.glow_sprite(HEALTHY, 0.55)
	add_child(_core)
	_light = FxUtil.light(HEALTHY, 1.1, 1.3)
	add_child(_light)
	_ring = Node2D.new()
	_ring.material = FxUtil.unshaded()
	_ring.draw.connect(_draw_ring)
	add_child(_ring)


func hit(fx: Fx) -> void:
	if hp <= 0:
		return
	hp -= 1
	_hit = 1.0
	Audio.play("cpu_hit")
	fx.sparks(position, CRITICAL, 18, 220.0, 0.5, 0.6)
	fx.light_flash(position, CRITICAL, 2.5, 1.6, 0.4)
	fx.shake_requested.emit(7.0)
	if hp <= 0:
		fx.explosion(position, 120.0)
		fx.debris(position, Color("2c323b"), 30, 220.0)
		_sprite.modulate = Color(0.35, 0.3, 0.3)
		destroyed.emit()


func _process(delta: float) -> void:
	_t += delta
	_hit = maxf(_hit - delta * 2.5, 0.0)
	var health := float(hp) / MAX_HP
	var color := CRITICAL.lerp(HEALTHY, health)
	var flicker := 1.0
	if health <= 0.3 and hp > 0 and fmod(_t, 0.9) < 0.12:
		flicker = 0.3
	var beat := 0.8 + 0.2 * sin(_t * (3.0 + (1.0 - health) * 6.0))
	if hp <= 0:
		color = Color(0.2, 0.05, 0.05)
		beat = 0.2
	_core.modulate = color.lerp(CRITICAL, _hit) * Color(1, 1, 1, beat * flicker)
	_halo.modulate = Color(color.lerp(CRITICAL, _hit), 0.22 * beat * flicker + _hit * 0.5)
	_light.color = color.lerp(CRITICAL, _hit)
	_light.energy = (0.9 + _hit * 1.5) * beat * flicker
	_sprite.position = Vector2(randf_range(-2, 2), randf_range(-2, 2)) * _hit
	_ring.queue_redraw()


func _draw_ring() -> void:
	var health := float(hp) / MAX_HP
	var color := CRITICAL.lerp(HEALTHY, health)
	var radius := 62.0
	var gap := 0.06
	var seg := TAU / MAX_HP
	for i in MAX_HP:
		var start := -PI / 2 + i * seg + gap
		var lit := i < hp
		var c := Color(color, 0.95) if lit else Color(0.3, 0.35, 0.4, 0.35)
		_ring.draw_arc(Vector2.ZERO, radius, start, start + seg - gap * 2, 8, c, 5.0, true)
	var text := str(hp)
	var size := _font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, 22)
	var pos := Vector2(-size.x / 2, 8)
	_ring.draw_string_outline(_font, pos, text, HORIZONTAL_ALIGNMENT_LEFT, -1, 22, 5, Color(0, 0, 0, 0.85))
	_ring.draw_string(_font, pos, text, HORIZONTAL_ALIGNMENT_LEFT, -1, 22, color.lerp(Color.WHITE, 0.4))
