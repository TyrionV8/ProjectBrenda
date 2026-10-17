class_name Bomb
extends Projectile
## Arcing capacitor bomb: flies to a point and explodes, hurting every bug in the blast.

var start := Vector2.ZERO
var target := Vector2.ZERO
var speed := 60.0
var damage := 4.0
var blast_radius := 100.0
var _t := 0.0
var _duration := 1.0
var _height := 0.0
var _glow: Node2D


func _ready() -> void:
	position = start
	_duration = maxf(start.distance_to(target) / maxf(speed, 1.0), 0.15)
	_glow = Node2D.new()
	_glow.material = FxUtil.additive()
	_glow.draw.connect(_draw_glow)
	add_child(_glow)


func _process(delta: float) -> void:
	_t += delta / _duration
	if _t >= 1.0:
		_explode()
		return
	position = start.lerp(target, _t)
	_height = sin(_t * PI)
	queue_redraw()
	_glow.queue_redraw()


func _explode() -> void:
	for e in game.enemies.duplicate():
		if not e.dead and target.distance_to(e.position) <= blast_radius + e.radius * 0.5:
			e.take_damage(damage)
	game.fx.explosion(target, blast_radius)
	Audio.play("explode", &"SFX", -3.0)
	queue_free()


func _draw() -> void:
	var lift := Vector2(0, -_height * 26.0)
	var scale_k := 1.0 + _height * 0.8
	draw_circle(Vector2(0, 3), 6.0 * (1.0 - _height * 0.4), Color(0, 0, 0, 0.35 * (1.0 - _height * 0.5)))
	draw_circle(lift, 7.0 * scale_k, Color("1f3f80"))
	draw_circle(lift, 5.0 * scale_k, Color("a9b4c2"))
	draw_circle(lift + Vector2(-1.5, -1.5) * scale_k, 2.0 * scale_k, Color.WHITE)


func _draw_glow() -> void:
	var lift := Vector2(0, -_height * 26.0)
	var blink := 0.6 + 0.4 * sin(_t * 40.0)
	_glow.draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(lift - Vector2(14, 14), Vector2(28, 28)), false, Color(color, 0.8 * blink))
