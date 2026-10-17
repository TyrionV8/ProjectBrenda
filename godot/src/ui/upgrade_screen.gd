extends Control
## Spend upgrade points between levels, then start the next level.

var _points: Label
var _details: Label
var _cards: Array[Control] = []


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(MenuBackground.new())
	var dim := ColorRect.new()
	dim.color = Color(0.01, 0.03, 0.06, 0.78)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(dim)
	Audio.play_music("title")

	var root := UI.vbox(18)
	root.position = Vector2(40, 28)
	root.size = Vector2(1366 - 80, 768 - 56)
	add_child(root)

	var header := UI.hbox(16)
	var title := UI.label("UPGRADES", "TitleLabel")
	title.add_theme_font_size_override("font_size", 52)
	header.add_child(title)
	var next := UI.label("next: LEVEL %d" % (GameState.current_level + 1), "HeaderLabel")
	next.add_theme_color_override("font_color", UiTheme.DIM)
	next.size_flags_vertical = Control.SIZE_SHRINK_END
	next.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	header.add_child(next)
	var points_box := PanelContainer.new()
	points_box.theme_type_variation = "CardPanel"
	var points_row := UI.hbox(10)
	points_box.add_child(points_row)
	var chip := TextureRect.new()
	chip.texture = preload("res://assets/art/icons/chip.svg")
	chip.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	chip.custom_minimum_size = Vector2(36, 36)
	chip.modulate = UiTheme.GOLD
	points_row.add_child(chip)
	var points_col := UI.vbox(0)
	points_col.add_child(UI.label("POINTS", "SmallHeader"))
	_points = UI.label("0", "MoneyLabel")
	points_col.add_child(_points)
	points_row.add_child(points_col)
	header.add_child(points_box)
	root.add_child(header)

	var cards := UI.hbox(14)
	cards.size_flags_vertical = Control.SIZE_EXPAND_FILL
	root.add_child(cards)
	for i in DB.turrets.size():
		var card := _card(DB.turrets[i])
		card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		cards.add_child(card)
		_cards.append(card)

	var footer := UI.hbox(16)
	footer.add_child(UI.button("Main Menu", func() -> void: Transition.change_scene("res://scenes/ui/main_menu.tscn"), "BigButton"))
	_details = UI.label("Hover an upgrade to see what it does. Click + to buy it.", "DimLabel")
	_details.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_details.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_details.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	footer.add_child(_details)
	footer.add_child(UI.button("Start Level %d" % (GameState.current_level + 1), func() -> void:
		GameState.save()
		Transition.change_scene("res://scenes/game/game.tscn"), "BigButton"))
	root.add_child(footer)

	GameState.changed.connect(_refresh)
	_refresh()
	for i in _cards.size():
		UI.reveal(_cards[i], 0.05 * i)


func _refresh() -> void:
	_points.text = str(GameState.points)


func _card(def: TurretDef) -> Control:
	var panel := PanelContainer.new()
	panel.theme_type_variation = "CardPanel"
	var box := UI.vbox(8)
	panel.add_child(box)
	var preview := TurretPreview.new(def)
	preview.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	box.add_child(preview)
	var name_label := UI.label(def.display_name.to_upper(), "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER)
	name_label.add_theme_color_override("font_color", def.color)
	name_label.add_theme_font_size_override("font_size", 15)
	box.add_child(name_label)
	var desc := UI.label(def.description, "DimLabel", HORIZONTAL_ALIGNMENT_CENTER)
	desc.add_theme_font_size_override("font_size", 14)
	desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	desc.custom_minimum_size = Vector2(200, 54)
	box.add_child(desc)
	for up in def.upgrades:
		var row := UpgradeRow.new(def, up)
		row.pressed.connect(func() -> void: _buy(row))
		row.focused.connect(_show_details)
		box.add_child(row)
	return panel


func _buy(row: UpgradeRow) -> void:
	if GameState.buy_upgrade(row.turret, row.upgrade):
		Audio.ui("upgrade")
		row.bought()
		GameState.save()
	else:
		Audio.play("error", &"UI")
	_show_details(row)


func _show_details(row: UpgradeRow) -> void:
	var stats := GameState.turret_stats(row.turret)
	var level := GameState.upgrade_level(row.turret.id, row.upgrade.id)
	var current := float(stats.get(row.upgrade.stat, 0.0))
	var text := "%s - %s: %s  Now: %s" % [row.turret.display_name, row.upgrade.label, row.upgrade.description, _format(row.upgrade.stat, current)]
	if level < row.upgrade.max_level:
		var limits: Vector2 = row.turret.stat_limits.get(row.upgrade.stat, Vector2(-INF, INF))
		var next := clampf(current + row.upgrade.per_level, limits.x, limits.y)
		text += "  ->  %s" % _format(row.upgrade.stat, next)
	_details.text = text
	_details.add_theme_color_override("font_color", UiTheme.TEXT)


static func _format(stat: String, value: float) -> String:
	match stat:
		"cooldown":
			return "%.2fs between shots" % value
		"range", "blast_radius", "beam_length":
			return "%d px" % roundi(value)
		"projectile_speed":
			return "%d px/s" % roundi(value)
		"dps":
			return "%.1f damage/s" % value
		"slow_factor":
			return "%d%% speed" % roundi(value * 100.0)
		"shots":
			return "%d pellets" % int(value)
	return "%.1f" % value
