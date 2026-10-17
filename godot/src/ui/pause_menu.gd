class_name PauseMenu
extends CanvasLayer
## Pause overlay with resume/restart/settings/main menu.

var game: Game
var _root: Control
var _menu: Control
var _settings: SettingsPanel


func _ready() -> void:
	layer = 20
	process_mode = Node.PROCESS_MODE_ALWAYS
	visible = false
	_root = Control.new()
	_root.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_root)
	var dim := ColorRect.new()
	dim.color = Color(0.0, 0.02, 0.05, 0.72)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	_root.add_child(dim)
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(380, 0)
	var box := UI.vbox(12)
	panel.add_child(box)
	box.add_child(UI.label("PAUSED", "TitleLabel", HORIZONTAL_ALIGNMENT_CENTER))
	box.add_child(UI.spacer(4))
	box.add_child(UI.button("Resume", close, "BigButton"))
	box.add_child(UI.button("Restart", func() -> void: game.restart()))
	box.add_child(UI.button("Settings", _open_settings))
	box.add_child(UI.button("Main Menu", func() -> void: Transition.change_scene("res://scenes/ui/main_menu.tscn"), "DangerButton"))
	_menu = UI.centered(panel)
	_root.add_child(_menu)
	_settings = SettingsPanel.new()
	_settings.closed.connect(func() -> void:
		_settings.get_parent().visible = false
		_menu.visible = true)
	var settings_center := UI.centered(_settings)
	settings_center.visible = false
	_root.add_child(settings_center)


func open() -> void:
	if visible:
		return
	visible = true
	_menu.visible = true
	_settings.get_parent().visible = false
	get_tree().paused = true
	Audio.ui("click")
	_root.modulate.a = 0.0
	create_tween().tween_property(_root, "modulate:a", 1.0, 0.15)


func _open_settings() -> void:
	_menu.visible = false
	_settings.get_parent().visible = true


func close() -> void:
	visible = false
	get_tree().paused = false


func _unhandled_input(event: InputEvent) -> void:
	if visible and event.is_action_pressed("pause"):
		if _settings.get_parent().visible:
			_settings.closed.emit()
		else:
			close()
		get_viewport().set_input_as_handled()
