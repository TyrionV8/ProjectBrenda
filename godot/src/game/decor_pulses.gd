class_name DecorPulses
extends Node2D
## Tiny glowing "electrons" running along the board's decorative traces.

const COUNT := 14
const SPEED := 70.0

var lines: Array[PackedVector2Array] = []
var _pulses: Array[Dictionary] = []


func _ready() -> void:
	material = FxUtil.additive()
	for i in COUNT:
		_pulses.append(_new_pulse(randf()))


func _new_pulse(start_ratio := 0.0) -> Dictionary:
	var line: PackedVector2Array = lines.pick_random() if not lines.is_empty() else PackedVector2Array()
	var length := 0.0
	for i in range(1, line.size()):
		length += line[i - 1].distance_to(line[i])
	return {"line": line, "length": length, "d": length * start_ratio, "speed": SPEED * randf_range(0.6, 1.4)}


func _process(delta: float) -> void:
	for i in _pulses.size():
		var p: Dictionary = _pulses[i]
		p.d += p.speed * delta
		if p.d >= p.length:
			_pulses[i] = _new_pulse()
	queue_redraw()


func _draw() -> void:
	for p in _pulses:
		var line: PackedVector2Array = p.line
		if line.size() < 2:
			continue
		var pos := _point_at(line, p.d)
		var fade := clampf(minf(p.d, p.length - p.d) / 30.0, 0.0, 1.0)
		draw_texture_rect(FxUtil.GLOW_TEXTURE, Rect2(pos - Vector2(9, 9), Vector2(18, 18)), false, Color(0.3, 0.8, 1.0, 0.7 * fade))
		draw_circle(pos, 1.6, Color(0.85, 0.97, 1.0, fade))


static func _point_at(line: PackedVector2Array, d: float) -> Vector2:
	for i in range(1, line.size()):
		var seg := line[i - 1].distance_to(line[i])
		if d <= seg:
			return line[i - 1].lerp(line[i], d / maxf(seg, 0.001))
		d -= seg
	return line[-1]
