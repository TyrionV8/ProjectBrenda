class_name Fx
extends Node2D
## Spawns short-lived visual effects. Lives inside the board, so positions are board-local.

const FONT := preload("res://assets/fonts/Silkscreen-Bold.ttf")

signal shake_requested(amount: float)


func sparks(pos: Vector2, color: Color, amount := 14, speed := 160.0, lifetime := 0.45, size := 0.5) -> void:
	var p := _particles(pos, amount, lifetime)
	p.texture = FxUtil.SPARK_TEXTURE
	p.material = FxUtil.additive()
	p.initial_velocity_min = speed * 0.4
	p.initial_velocity_max = speed
	p.damping_min = speed * 1.2
	p.damping_max = speed * 2.0
	p.scale_amount_min = size * 0.5
	p.scale_amount_max = size
	p.color_ramp = _fade(color)
	p.emitting = true


func debris(pos: Vector2, color: Color, amount := 16, speed := 120.0) -> void:
	var p := _particles(pos, amount, 0.7)
	p.texture = FxUtil.PIXEL_TEXTURE
	p.initial_velocity_min = speed * 0.3
	p.initial_velocity_max = speed
	p.damping_min = speed
	p.damping_max = speed * 1.6
	p.angular_velocity_min = -360.0
	p.angular_velocity_max = 360.0
	p.scale_amount_min = 0.35
	p.scale_amount_max = 0.8
	p.color_ramp = _fade(color)
	p.emitting = true


func glow_flash(pos: Vector2, color: Color, size := 1.0, duration := 0.25) -> void:
	var sprite := FxUtil.glow_sprite(color, size * 0.4)
	sprite.position = pos
	add_child(sprite)
	var tween := sprite.create_tween().set_parallel()
	tween.tween_property(sprite, "scale", Vector2.ONE * size, duration).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	tween.tween_property(sprite, "modulate:a", 0.0, duration)
	tween.chain().tween_callback(sprite.queue_free)


func light_flash(pos: Vector2, color: Color, energy := 1.5, size := 1.0, duration := 0.3) -> void:
	var l := FxUtil.light(color, energy, size)
	l.position = pos
	add_child(l)
	var tween := l.create_tween()
	tween.tween_property(l, "energy", 0.0, duration).set_ease(Tween.EASE_OUT)
	tween.tween_callback(l.queue_free)


func shockwave(pos: Vector2, radius: float, color: Color, duration := 0.4) -> void:
	var ring := Shockwave.new()
	ring.position = pos
	ring.radius = radius
	ring.color = color
	ring.duration = duration
	add_child(ring)


func floating_text(pos: Vector2, text: String, color: Color, size := 16) -> void:
	var label := FloatingText.new()
	label.position = pos
	label.text = text
	label.color = color
	label.font = FONT
	label.font_size = size
	add_child(label)


func explosion(pos: Vector2, radius: float) -> void:
	var orange := Color(1.0, 0.6, 0.2)
	glow_flash(pos, Color(1.0, 0.75, 0.4), radius / 40.0, 0.35)
	glow_flash(pos, Color(1, 1, 0.9), radius / 110.0, 0.12)
	shockwave(pos, radius, orange, 0.45)
	sparks(pos, orange, 26, radius * 2.2, 0.55, 0.7)
	debris(pos, Color(0.35, 0.3, 0.28), 10, radius * 1.4)
	light_flash(pos, orange, 2.2, radius / 70.0, 0.45)
	shake_requested.emit(4.0)


func bug_death(pos: Vector2, def: EnemyDef) -> void:
	var big := def.radius / 12.0
	debris(pos, def.shell_color, int(10 + 6 * big), 90.0 * big)
	sparks(pos, def.eye_color.lerp(Color.WHITE, 0.3), int(6 + 4 * big), 110.0 * big, 0.35, 0.45)
	glow_flash(pos, def.shell_color.lerp(Color.WHITE, 0.4), 0.6 * big, 0.2)
	floating_text(pos + Vector2(0, -def.radius), "+%d" % def.reward, Color("ffd34d"))
	if def.style == EnemyDef.Style.BOSS:
		shockwave(pos, 90, def.eye_color, 0.6)
		light_flash(pos, def.eye_color, 2.0, 1.4, 0.6)
		shake_requested.emit(8.0)


func _particles(pos: Vector2, amount: int, lifetime: float) -> CPUParticles2D:
	var p := CPUParticles2D.new()
	p.position = pos
	p.one_shot = true
	p.explosiveness = 0.95
	p.amount = amount
	p.lifetime = lifetime
	p.direction = Vector2.RIGHT
	p.spread = 180.0
	p.gravity = Vector2.ZERO
	p.finished.connect(p.queue_free)
	add_child(p)
	return p


func _fade(color: Color) -> Gradient:
	var g := Gradient.new()
	g.colors = PackedColorArray([color, Color(color, 0.0)])
	return g
