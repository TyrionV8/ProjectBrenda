extends CanvasLayer
## Full-screen post-processing (vignette, scanlines, aberration) and the red damage pulse.

var _rect: ColorRect
var _material: ShaderMaterial
var _damage := 0.0


func _ready() -> void:
	layer = 90
	process_mode = Node.PROCESS_MODE_ALWAYS
	_rect = ColorRect.new()
	_rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_material = ShaderMaterial.new()
	_material.shader = load("res://assets/shaders/post_fx.gdshader")
	_rect.material = _material
	add_child(_rect)
	Settings.changed.connect(_apply_settings)
	_apply_settings()


func pulse_damage(amount := 0.8) -> void:
	_damage = maxf(_damage, amount)
	_rect.visible = true


func _process(delta: float) -> void:
	if _damage > 0.0:
		_damage = maxf(_damage - delta * 1.6, 0.0)
		_material.set_shader_parameter("damage", _damage)
		if _damage == 0.0:
			_apply_settings()


func _apply_settings() -> void:
	_rect.visible = Settings.screen_fx or _damage > 0.0
	var on := 1.0 if Settings.screen_fx else 0.0
	_material.set_shader_parameter("vignette", 0.45 * on)
	_material.set_shader_parameter("scanlines", 0.05 * on)
	_material.set_shader_parameter("aberration", 1.0 * on)
