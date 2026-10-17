extends CanvasLayer
## Scene changes with a short "signal lost" fade.

var _rect: ColorRect
var _busy := false


func _ready() -> void:
	layer = 100
	process_mode = Node.PROCESS_MODE_ALWAYS
	_rect = ColorRect.new()
	_rect.color = Color(0.02, 0.04, 0.07, 0.0)
	_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var mat := ShaderMaterial.new()
	mat.shader = load("res://assets/shaders/transition.gdshader")
	_rect.material = mat
	add_child(_rect)


func change_scene(path: String) -> void:
	if _busy:
		return
	_busy = true
	_rect.mouse_filter = Control.MOUSE_FILTER_STOP
	var tween := create_tween()
	tween.tween_method(_set_progress, 0.0, 1.0, 0.28)
	await tween.finished
	get_tree().paused = false
	Engine.time_scale = 1.0
	get_tree().change_scene_to_file(path)
	await get_tree().process_frame
	await get_tree().process_frame
	tween = create_tween()
	tween.tween_method(_set_progress, 1.0, 0.0, 0.35)
	await tween.finished
	_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_busy = false


func _set_progress(value: float) -> void:
	_rect.color.a = value
	(_rect.material as ShaderMaterial).set_shader_parameter("progress", value)
