extends Control
## Level selection: unlocked levels lead to the upgrade screen, as in the original.


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(MenuBackground.new())
	var dim := ColorRect.new()
	dim.color = Color(0.01, 0.03, 0.06, 0.7)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(dim)

	var root := UI.vbox(28)
	root.alignment = BoxContainer.ALIGNMENT_CENTER
	root.add_child(UI.label("SELECT LEVEL", "TitleLabel", HORIZONTAL_ALIGNMENT_CENTER))
	var row := UI.hbox(24)
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	root.add_child(row)
	for i in DB.levels.size():
		var card := _card(i)
		row.add_child(card)
		UI.reveal(card, 0.08 * i)
	var back := UI.button("Back", func() -> void: Transition.change_scene("res://scenes/ui/main_menu.tscn"), "BigButton")
	back.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	back.custom_minimum_size.x = 240
	root.add_child(back)
	add_child(UI.centered(root))


func _card(index: int) -> Control:
	var level := DB.levels[index]
	var unlocked := GameState.is_unlocked(index)
	var panel := PanelContainer.new()
	panel.theme_type_variation = "CardPanel"
	var box := UI.vbox(10)
	panel.add_child(box)
	var title := UI.label("LEVEL %d" % (index + 1), "HeaderLabel", HORIZONTAL_ALIGNMENT_CENTER)
	if not unlocked:
		title.add_theme_color_override("font_color", UiTheme.DIM)
	box.add_child(title)
	box.add_child(LevelPreview.new(level, not unlocked))
	var bugs := 0
	for w in level.waves:
		bugs += w.size()
	box.add_child(UI.label("%d waves  -  %d bugs" % [level.waves.size(), bugs], "DimLabel", HORIZONTAL_ALIGNMENT_CENTER))
	var play := UI.button("PLAY" if unlocked else "LOCKED", func() -> void:
		GameState.current_level = index
		Transition.change_scene("res://scenes/ui/upgrade_screen.tscn"))
	play.disabled = not unlocked
	box.add_child(play)
	return panel
