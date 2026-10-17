class_name GlitchTitle
extends Control
## Big title with a neon glow, RGB split and the occasional glitch burst.

@export var text := "BRENDA"
@export var font_size := 120
@export var color := UiTheme.CYAN

var _t := 0.0
var _burst := 0.0
var _font: Font = UiTheme.PIXEL_BOLD


func _ready() -> void:
	var size_needed := _font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size)
	custom_minimum_size = size_needed + Vector2(20, 10)
	mouse_filter = Control.MOUSE_FILTER_IGNORE


func _process(delta: float) -> void:
	_t += delta
	_burst = maxf(_burst - delta * 4.0, 0.0)
	if randf() < delta * 0.35:
		_burst = 1.0
	queue_redraw()


func _draw() -> void:
	var base := Vector2(10, font_size * 0.85)
	var jitter := Vector2(randf_range(-6, 6) * _burst, 0)
	# Soft glow: the text stamped a few times with low alpha.
	for i in 6:
		var off := Vector2.from_angle(i * TAU / 6.0) * 4.0
		draw_string(_font, base + off, text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, Color(color, 0.08))
	var split := 2.0 + 10.0 * _burst
	draw_string(_font, base + jitter + Vector2(-split, 0), text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, Color(1, 0.2, 0.4, 0.55))
	draw_string(_font, base + jitter + Vector2(split, 0), text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, Color(0.2, 0.6, 1, 0.55))
	draw_string(_font, base + jitter, text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, color.lerp(Color.WHITE, 0.35 + 0.1 * sin(_t * 3.0)))
	if _burst > 0.3:
		var rng := RandomNumberGenerator.new()
		rng.seed = int(_t * 30.0)
		for i in 4:
			var y := rng.randf_range(0, size.y)
			draw_rect(Rect2(rng.randf_range(0, size.x * 0.6), y, rng.randf_range(40, 200), rng.randf_range(2, 6)), Color(color, 0.35 * _burst))
