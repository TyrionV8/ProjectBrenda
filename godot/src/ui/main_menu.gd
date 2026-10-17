extends Control
## Title screen.

const INSTAGRAM := "https://www.instagram.com/atlas_gamedev/"

var _new_game: Button
var _confirm_reset := false


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(MenuBackground.new())
	add_child(_shade())
	Audio.play_music("title")

	var column := UI.vbox(10)
	column.position = Vector2(110, 70)
	column.custom_minimum_size = Vector2(520, 0)
	add_child(column)
	var project := UI.label("PROJECT", "HeaderLabel")
	project.add_theme_color_override("font_color", UiTheme.DIM)
	project.add_theme_font_size_override("font_size", 34)
	column.add_child(project)
	column.add_child(GlitchTitle.new())
	var tagline := UI.label("Stop the bugs before they reach the CPU.", "DimLabel")
	column.add_child(tagline)
	column.add_child(UI.spacer(18))

	var buttons := UI.vbox(10)
	buttons.custom_minimum_size.x = 340
	buttons.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	column.add_child(buttons)
	_new_game = UI.button("New Game", _on_new_game, "BigButton")
	buttons.add_child(_new_game)
	var cont := UI.button("Continue", _on_continue, "BigButton")
	cont.disabled = not GameState.has_save()
	buttons.add_child(cont)
	var select := UI.button("Select Level", _on_select_level, "BigButton")
	select.disabled = not GameState.has_save()
	buttons.add_child(select)
	buttons.add_child(UI.button("Settings", _on_settings, "BigButton"))
	if not OS.has_feature("web") and not OS.has_feature("mobile"):
		buttons.add_child(UI.button("Exit", func() -> void: get_tree().quit(), "BigButton"))

	var footer := UI.vbox(2)
	footer.position = Vector2(1366 - 330, 768 - 96)
	footer.custom_minimum_size.x = 300
	var insta := UI.button("@atlas_gamedev", func() -> void: OS.shell_open(INSTAGRAM))
	insta.flat = true
	insta.alignment = HORIZONTAL_ALIGNMENT_RIGHT
	footer.add_child(insta)
	footer.add_child(UI.label("prototype - godot port", "DimLabel", HORIZONTAL_ALIGNMENT_RIGHT))
	add_child(footer)

	for i in buttons.get_child_count():
		UI.reveal(buttons.get_child(i), 0.15 + i * 0.06, Vector2(-30, 0))
	UI.reveal(column.get_child(0), 0.0, Vector2(0, -10))


func _shade() -> Control:
	var tex := GradientTexture2D.new()
	tex.fill_from = Vector2(0, 0)
	tex.fill_to = Vector2(1, 0)
	var g := Gradient.new()
	g.offsets = PackedFloat32Array([0.0, 0.45, 0.8])
	g.colors = PackedColorArray([Color(0.01, 0.03, 0.06, 0.92), Color(0.01, 0.03, 0.06, 0.65), Color(0.01, 0.03, 0.06, 0.0)])
	tex.gradient = g
	var rect := TextureRect.new()
	rect.texture = tex
	rect.set_anchors_preset(Control.PRESET_FULL_RECT)
	rect.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return rect


func _on_new_game() -> void:
	if GameState.has_save() and not _confirm_reset:
		_confirm_reset = true
		_new_game.text = "Erase progress?"
		_new_game.theme_type_variation = "DangerButton"
		return
	GameState.reset()
	GameState.save()
	Transition.change_scene("res://scenes/game/game.tscn")


func _on_continue() -> void:
	if GameState.load_save():
		Transition.change_scene("res://scenes/ui/upgrade_screen.tscn")


func _on_select_level() -> void:
	GameState.load_save()
	Transition.change_scene("res://scenes/ui/level_select.tscn")


func _on_settings() -> void:
	var dim := ColorRect.new()
	dim.color = Color(0, 0.02, 0.05, 0.7)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(dim)
	var panel := SettingsPanel.new()
	var center := UI.centered(panel)
	add_child(center)
	panel.closed.connect(func() -> void:
		dim.queue_free()
		center.queue_free())
	UI.reveal(panel, 0.0)
