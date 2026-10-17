class_name BugIcon
extends Control
## A small animated bug for lists (spawn queue, level previews).

var def: EnemyDef
var _body: BugBody


func _init(p_def: EnemyDef, icon_size := 30.0) -> void:
	def = p_def
	custom_minimum_size = Vector2(icon_size, icon_size)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_body = BugBody.new(def)
	_body.scale = Vector2.ONE * clampf(icon_size * 0.36 / def.radius, 0.2, 1.4)
	_body.rotation = -PI / 2
	_body.gait = randf() * TAU
	add_child(_body)
	resized.connect(func() -> void: _body.position = size / 2)


func _process(delta: float) -> void:
	_body.animate(delta * 6.0)
