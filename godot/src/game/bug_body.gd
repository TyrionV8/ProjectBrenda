class_name BugBody
extends Node2D
## Procedural top-down bug, facing +X. Legs follow a tripod gait driven by `gait`.

const FLASH_SHADER := preload("res://assets/shaders/flash.gdshader")

var def: EnemyDef
var gait := 0.0
var _eyes: Node2D


func _init(p_def: EnemyDef) -> void:
	def = p_def


func _ready() -> void:
	var mat := ShaderMaterial.new()
	mat.shader = FLASH_SHADER
	material = mat
	_eyes = Node2D.new()
	_eyes.material = FxUtil.additive()
	_eyes.draw.connect(_draw_eyes)
	add_child(_eyes)


func set_flash(value: float) -> void:
	(material as ShaderMaterial).set_shader_parameter("flash", value)


func set_frozen(value: float) -> void:
	(material as ShaderMaterial).set_shader_parameter("frozen", value)


func animate(delta_gait: float) -> void:
	gait += delta_gait
	queue_redraw()
	_eyes.queue_redraw()


func _draw() -> void:
	var r := def.radius
	var shadow := Vector2(3, 5).rotated(-global_rotation)
	_ellipse(shadow, Vector2(r * 1.15, r * 0.85), Color(0, 0, 0, 0.35))
	# Dark rim so bugs read clearly on top of the bright copper.
	_ellipse(Vector2(-r * 0.15, 0), Vector2(r * 1.2, r * 0.95), Color(0, 0, 0, 0.45))
	match def.style:
		EnemyDef.Style.SPIDER:
			_draw_spider(r)
		EnemyDef.Style.SLIM:
			_draw_slim(r)
		EnemyDef.Style.ARMORED:
			_draw_armored(r)
		EnemyDef.Style.BOSS:
			_draw_boss(r)
		_:
			_draw_beetle(r)


func _draw_beetle(r: float) -> void:
	_legs(3, r * 0.55, r * 1.25, 2.0 + r * 0.08, 0.35)
	_antennae(r)
	_ellipse(Vector2(-r * 0.25, 0), Vector2(r * 0.95, r * 0.8), def.accent_color)
	_ellipse(Vector2(-r * 0.3, 0), Vector2(r * 0.85, r * 0.7), def.shell_color)
	draw_line(Vector2(r * 0.35, 0), Vector2(-r * 1.1, 0), def.accent_color, maxf(r * 0.08, 1.2))
	_ellipse(Vector2(-r * 0.45, -r * 0.3), Vector2(r * 0.35, r * 0.18), Color(1, 1, 1, 0.35))
	_ellipse(Vector2(r * 0.55, 0), Vector2(r * 0.4, r * 0.42), def.accent_color.darkened(0.3))
	_ellipse(Vector2(r * 0.85, 0), Vector2(r * 0.3, r * 0.3), def.leg_color)


func _draw_spider(r: float) -> void:
	_legs(4, r * 0.25, r * 1.7, 1.8 + r * 0.05, 0.5)
	_ellipse(Vector2(-r * 0.45, 0), Vector2(r * 0.85, r * 0.75), def.accent_color)
	_ellipse(Vector2(-r * 0.5, -r * 0.05), Vector2(r * 0.75, r * 0.65), def.shell_color)
	# Hourglass marking.
	var m := Vector2(-r * 0.5, 0)
	var mark := def.eye_color.darkened(0.2)
	draw_colored_polygon(PackedVector2Array([m + Vector2(-r * 0.3, -r * 0.18), m + Vector2(-r * 0.3, r * 0.18), m]), mark)
	draw_colored_polygon(PackedVector2Array([m + Vector2(r * 0.3, -r * 0.18), m, m + Vector2(r * 0.3, r * 0.18)]), mark)
	_ellipse(Vector2(-r * 0.7, -r * 0.3), Vector2(r * 0.25, r * 0.14), Color(1, 1, 1, 0.3))
	_ellipse(Vector2(r * 0.45, 0), Vector2(r * 0.45, r * 0.42), def.leg_color.lightened(0.15))


func _draw_slim(r: float) -> void:
	_legs(3, r * 0.6, r * 1.3, 1.6, 0.6, -0.5)
	_antennae(r * 1.3)
	_ellipse(Vector2(-r * 0.6, 0), Vector2(r * 1.1, r * 0.45), def.accent_color)
	_ellipse(Vector2(-r * 0.65, 0), Vector2(r * 1.0, r * 0.36), def.shell_color)
	for i in 3:
		var x := -r * (0.2 + i * 0.4)
		draw_line(Vector2(x, -r * 0.36), Vector2(x, r * 0.36), def.accent_color, 1.2)
	_ellipse(Vector2(r * 0.45, 0), Vector2(r * 0.35, r * 0.32), def.accent_color.darkened(0.2))
	_ellipse(Vector2(r * 0.75, 0), Vector2(r * 0.25, r * 0.25), def.leg_color)


func _draw_armored(r: float) -> void:
	_legs(3, r * 0.6, r * 1.15, 2.6 + r * 0.06, 0.25)
	_antennae(r * 0.8)
	_ellipse(Vector2(-r * 0.2, 0), Vector2(r * 1.0, r * 0.88), def.accent_color.darkened(0.3))
	for i in 4:
		var x := -r * 0.95 + i * r * 0.45
		var plate := Rect2(x, -r * 0.8, r * 0.42, r * 1.6)
		draw_rect(plate, def.shell_color.darkened(i * 0.06))
		draw_rect(Rect2(plate.position, Vector2(plate.size.x, r * 0.18)), Color(1, 1, 1, 0.25))
		draw_rect(plate, def.accent_color, false, 1.5)
	_ellipse(Vector2(r * 0.85, 0), Vector2(r * 0.35, r * 0.4), def.leg_color)


func _draw_boss(r: float) -> void:
	_legs(4, r * 0.5, r * 1.35, 4.0, 0.3)
	# Mandibles.
	var bite := sin(gait * 1.5) * 0.25
	for s in [-1.0, 1.0]:
		var base := Vector2(r * 0.85, s * r * 0.25)
		var tip := base + Vector2(r * 0.55, -s * r * (0.05 + bite))
		draw_polyline(PackedVector2Array([base, base + Vector2(r * 0.35, s * r * 0.15), tip]), def.leg_color, 5.0, true)
	_ellipse(Vector2(-r * 0.2, 0), Vector2(r * 1.05, r * 0.9), def.accent_color)
	_ellipse(Vector2(-r * 0.25, 0), Vector2(r * 0.95, r * 0.8), def.shell_color)
	# Spikes along the back.
	for i in 5:
		var x := -r * 0.9 + i * r * 0.38
		for s in [-1.0, 1.0]:
			var p := Vector2(x, s * r * 0.62)
			draw_colored_polygon(PackedVector2Array([p + Vector2(-5, 0), p + Vector2(5, 0), p + Vector2(0, s * 12)]), def.leg_color)
	draw_line(Vector2(r * 0.4, 0), Vector2(-r * 1.15, 0), def.accent_color, 3.0)
	_ellipse(Vector2(-r * 0.5, -r * 0.35), Vector2(r * 0.4, r * 0.15), Color(1, 1, 1, 0.25))
	_ellipse(Vector2(r * 0.7, 0), Vector2(r * 0.45, r * 0.5), def.leg_color.lightened(0.1))


func _draw_eyes() -> void:
	var r := def.radius
	var front := r * 0.9
	match def.style:
		EnemyDef.Style.SPIDER:
			front = r * 0.7
		EnemyDef.Style.SLIM:
			front = r * 0.85
		EnemyDef.Style.BOSS:
			front = r * 0.9
	var spread := r * 0.22
	var size := maxf(r * 0.12, 1.8)
	for s in [-1.0, 1.0]:
		var p := Vector2(front, s * spread)
		_eyes.draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(p - Vector2.ONE * size * 3, Vector2.ONE * size * 6), false, def.eye_color)
		_eyes.draw_circle(p, size * 0.6, def.eye_color.lerp(Color.WHITE, 0.6))
	if def.style == EnemyDef.Style.BOSS:
		var pulse := 0.6 + 0.4 * sin(gait * 0.8)
		_eyes.draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(Vector2(-r * 0.25, 0) - Vector2.ONE * r * 0.6, Vector2.ONE * r * 1.2), false, Color(def.eye_color, pulse))


## Paired legs from hip (on the body) through a knee to the foot, animated with a tripod gait.
func _legs(pairs: int, body_half: float, reach: float, width: float, swing: float, rake := 0.0) -> void:
	for i in pairs:
		var t := float(i) / maxf(pairs - 1, 1)
		var hip_x := lerpf(body_half, -body_half, t) * 0.8
		for side in [-1.0, 1.0]:
			var phase := gait + i * PI + (PI if side > 0 else 0.0)
			var step := sin(phase) * swing
			var lift := maxf(cos(phase), 0.0)
			var angle := lerpf(-0.9, 0.9, t) + rake + step
			var hip := Vector2(hip_x, side * def.radius * 0.45)
			var dir := Vector2(sin(angle), side * cos(angle))
			var knee := hip + dir * reach * 0.55 + Vector2(0, side * reach * 0.1 * lift)
			var foot := knee + Vector2(sin(angle) * 1.4, side * cos(angle) * 0.6).normalized() * reach * 0.6
			draw_polyline(PackedVector2Array([hip, knee, foot]), def.leg_color, width, true)


func _antennae(r: float) -> void:
	var wiggle := sin(gait * 0.7) * 0.15
	for s in [-1.0, 1.0]:
		var base := Vector2(def.radius * 0.95, s * def.radius * 0.2)
		var mid := base + Vector2(r * 0.45, s * r * (0.35 + wiggle))
		var tip := mid + Vector2(r * 0.45, s * r * (0.15 - wiggle))
		draw_polyline(PackedVector2Array([base, mid, tip]), def.leg_color, 1.3, true)


func _ellipse(center: Vector2, radii: Vector2, color: Color) -> void:
	var points := PackedVector2Array()
	for i in 20:
		var a := TAU * i / 20.0
		points.append(center + Vector2(cos(a) * radii.x, sin(a) * radii.y))
	draw_colored_polygon(points, color)
