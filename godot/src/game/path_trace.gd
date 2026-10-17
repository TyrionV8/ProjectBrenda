class_name PathTrace
extends Node2D
## The bug route drawn as a wide copper trace with data pulses flowing to the CPU,
## solder pads on the corners and a glitching breach where bugs come in.

const COPPER := Color("c8892e")
const COPPER_EDGE := Color("5c3b10")

var _points := PackedVector2Array()
var _entry := Vector2.ZERO
var _breach_angle := 0.0
var _flow: Line2D
var _breach_light: PointLight2D
var _breach_glow: Sprite2D
var _t := 0.0
var _breach: Node2D


func setup(points: PackedVector2Array, entry: Vector2) -> void:
	_points = points
	_entry = entry
	var first_dir := (points[1] - points[0]).normalized() if points.size() > 1 else Vector2.RIGHT
	_breach_angle = first_dir.angle()

	_add_line(points, 36.0, Color(0, 0, 0, 0.4), Vector2(4, 5))
	_add_line(points, 32.0, COPPER_EDGE)
	var copper := _add_line(points, 26.0, Color.WHITE)
	copper.texture = _bevel_texture()
	copper.texture_mode = Line2D.LINE_TEXTURE_TILE
	var groove := _add_line(points, 8.0, Color(0.25, 0.15, 0.05, 0.55))
	groove.z_index = 0

	_flow = _add_line(points, 22.0, Color.WHITE)
	_flow.texture = _bevel_texture()
	_flow.texture_mode = Line2D.LINE_TEXTURE_STRETCH
	var mat := ShaderMaterial.new()
	mat.shader = load("res://assets/shaders/trace_flow.gdshader")
	mat.set_shader_parameter("line_length", _length(points))
	_flow.material = mat

	_breach = Node2D.new()
	_breach.material = FxUtil.unshaded()
	_breach.draw.connect(_draw_breach)
	add_child(_breach)
	_breach_glow = FxUtil.glow_sprite(Color(1.0, 0.15, 0.35), 1.6)
	_breach_glow.position = entry
	add_child(_breach_glow)
	_breach_light = FxUtil.light(Color(1.0, 0.2, 0.35), 1.2, 0.8)
	_breach_light.position = entry
	add_child(_breach_light)
	var embers := CPUParticles2D.new()
	embers.position = entry
	embers.amount = 18
	embers.lifetime = 1.1
	embers.texture = FxUtil.PIXEL_TEXTURE
	embers.material = FxUtil.additive()
	embers.emission_shape = CPUParticles2D.EMISSION_SHAPE_SPHERE
	embers.emission_sphere_radius = 14.0
	embers.direction = first_dir
	embers.spread = 50.0
	embers.gravity = Vector2.ZERO
	embers.initial_velocity_min = 10.0
	embers.initial_velocity_max = 35.0
	embers.scale_amount_min = 0.2
	embers.scale_amount_max = 0.5
	var g := Gradient.new()
	g.colors = PackedColorArray([Color(1, 0.3, 0.5, 1), Color(1, 0.1, 0.3, 0)])
	embers.color_ramp = g
	add_child(embers)
	queue_redraw()


## Make pulses race faster (used when a wave is running).
func set_intensity(value: float) -> void:
	var mat := _flow.material as ShaderMaterial
	mat.set_shader_parameter("intensity", value)
	mat.set_shader_parameter("speed", 70.0 + 80.0 * value)


func _process(delta: float) -> void:
	_t += delta
	var pulse := 0.75 + 0.25 * sin(_t * 5.0) + (0.3 if fmod(_t, 2.3) < 0.08 else 0.0)
	_breach_glow.modulate.a = pulse
	_breach_light.energy = 1.0 * pulse
	_breach.queue_redraw()


func _draw() -> void:
	# Solder pads at the corners.
	for i in range(1, _points.size() - 1):
		var p := _points[i]
		draw_circle(p + Vector2(3, 4), 22, Color(0, 0, 0, 0.35))
		draw_circle(p, 21, COPPER_EDGE)
		draw_circle(p, 18, Color("d9a441"))
		draw_circle(p, 12, Color("8f6220"))
		draw_circle(p, 5, Color("2a1a08"))


func _draw_breach() -> void:
	# The breach: a torn, glitching opening in the board edge.
	var t := floorf(_t * 14.0)
	var rng := RandomNumberGenerator.new()
	rng.seed = int(t)
	var side := Vector2.from_angle(_breach_angle).orthogonal()
	for i in 7:
		var off := side * rng.randf_range(-24, 24) + Vector2.from_angle(_breach_angle) * rng.randf_range(-6, 10)
		var size := Vector2(rng.randf_range(4, 14), rng.randf_range(2, 6))
		_breach.draw_rect(Rect2(_entry + off - size / 2, size), Color(1, 0.2, 0.4, rng.randf_range(0.3, 0.9)))
	_breach.draw_circle(_entry, 17, Color(0.05, 0.0, 0.02, 0.85))
	_breach.draw_arc(_entry, 19, 0, TAU, 32, Color(1, 0.25, 0.45, 0.9), 2.5, true)


func _add_line(points: PackedVector2Array, width: float, color: Color, offset := Vector2.ZERO) -> Line2D:
	var line := Line2D.new()
	line.points = points
	line.position = offset
	line.width = width
	line.default_color = color
	line.joint_mode = Line2D.LINE_JOINT_ROUND
	line.begin_cap_mode = Line2D.LINE_CAP_ROUND
	line.end_cap_mode = Line2D.LINE_CAP_ROUND
	line.antialiased = true
	add_child(line)
	return line


static func _length(points: PackedVector2Array) -> float:
	var total := 0.0
	for i in range(1, points.size()):
		total += points[i - 1].distance_to(points[i])
	return total


## Vertical gradient across the trace width so it reads as a bevelled metal strip.
static func _bevel_texture() -> Texture2D:
	var tex := GradientTexture2D.new()
	tex.width = 4
	tex.height = 32
	tex.fill_from = Vector2(0, 0)
	tex.fill_to = Vector2(0, 1)
	var g := Gradient.new()
	g.offsets = PackedFloat32Array([0.0, 0.18, 0.45, 0.82, 1.0])
	g.colors = PackedColorArray([Color("8a5a1a"), Color("ffd98a"), Color("e3a345"), Color("b97a28"), Color("5c3b10")])
	tex.gradient = g
	return tex
