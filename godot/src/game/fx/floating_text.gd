class_name FloatingText
extends Node2D
## "+3"-style text that pops, rises and fades.

var text := ""
var color := Color.WHITE
var font: Font
var font_size := 16


func _ready() -> void:
	z_index = 20
	material = FxUtil.unshaded()
	scale = Vector2.ONE * 0.4
	var tween := create_tween()
	tween.tween_property(self, "scale", Vector2.ONE, 0.18).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.parallel().tween_property(self, "position:y", position.y - 34, 0.8).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	tween.tween_property(self, "modulate:a", 0.0, 0.25)
	tween.tween_callback(queue_free)


func _draw() -> void:
	var width := font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size).x
	var pos := Vector2(-width / 2.0, font_size * 0.35)
	draw_string_outline(font, pos, text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, 4, Color(0, 0, 0, 0.8))
	draw_string(font, pos, text, HORIZONTAL_ALIGNMENT_LEFT, -1, font_size, color)
