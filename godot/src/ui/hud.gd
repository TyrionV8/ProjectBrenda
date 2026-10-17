class_name Hud
extends CanvasLayer
## In-game interface: level/wave info and spawn queue on the left, money and the
## turret inventory on the right, plus wave banners and the win/lose panels.

const CHIP_ICON := preload("res://assets/art/icons/chip.svg")

var game: Game
var _level_value: Label
var _wave_value: Label
var _status: Label
var _queue: VBoxContainer
var _queue_key := ""
var _money: Label
var _money_icon: TextureRect
var _slots: Array[TurretSlot] = []
var _speed_button: Button
var _cancel_button: Button
var _hint: Label
var _banner: Label
var _banner_sub: Label
var _cpu_bar: Control
var _overlay: Control


func setup(p_game: Game) -> void:
	game = p_game
	_build_left()
	_build_right()
	_build_board_overlay()
	game.money_changed.connect(_on_money_changed)
	game.build.selection_changed.connect(_on_selection_changed)
	game.waves.wave_started.connect(_on_wave_started)
	game.waves.countdown_started.connect(_on_countdown_started)
	_on_money_changed(game.money)
	_level_value.text = str(game.level_index + 1)
	_show_banner("LEVEL %d" % (game.level_index + 1), "Defend the CPU")


func _process(_delta: float) -> void:
	if game == null:
		return
	var waves := game.waves
	_wave_value.text = "%d/%d" % [mini(waves.wave_index + 1, waves.total_waves()), waves.total_waves()]
	match waves.state:
		WaveManager.State.COUNTDOWN:
			_status.text = "NEXT WAVE\n%.1f" % maxf(waves.countdown_left, 0.0)
			_status.modulate = UiTheme.CYAN
		WaveManager.State.SPAWNING, WaveManager.State.WAITING:
			_status.text = "BUGS LEFT\n%d" % (game.enemies.size() + waves.upcoming().size())
			_status.modulate = UiTheme.RED.lerp(Color.WHITE, 0.3)
		_:
			_status.text = ""
	var upcoming := waves.upcoming()
	var key := "%d:%d:%d" % [waves.wave_index, upcoming.size(), waves.state]
	if key != _queue_key:
		_queue_key = key
		_refresh_queue(upcoming)
	_cpu_bar.queue_redraw()


func update_speed(value: float) -> void:
	_speed_button.text = "%dx" % int(value)


func flash_money() -> void:
	_money.modulate = UiTheme.RED
	var tween := create_tween()
	tween.tween_property(_money, "modulate", Color.WHITE, 0.5)
	_money.pivot_offset = _money.size / 2
	_money.scale = Vector2.ONE * 1.25
	create_tween().tween_property(_money, "scale", Vector2.ONE, 0.25).set_trans(Tween.TRANS_BACK)


## Win/lose panel. `finished` means the last level was cleared.
func show_result(won: bool, finished: bool, points_gained := 0) -> void:
	_overlay = Control.new()
	_overlay.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(_overlay)
	var dim := ColorRect.new()
	dim.color = Color(0.0, 0.02, 0.05, 0.0)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	_overlay.add_child(dim)
	create_tween().tween_property(dim, "color:a", 0.6, 0.4)
	var panel := PanelContainer.new()
	panel.custom_minimum_size = Vector2(560, 0)
	var box := UI.vbox(16)
	panel.add_child(box)
	var title := UI.label("", "TitleLabel", HORIZONTAL_ALIGNMENT_CENTER)
	title.add_theme_font_size_override("font_size", 44)
	var sub := UI.label("", "", HORIZONTAL_ALIGNMENT_CENTER)
	sub.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	box.add_child(title)
	box.add_child(sub)
	var buttons := UI.hbox(16)
	buttons.alignment = BoxContainer.ALIGNMENT_CENTER
	if won:
		title.text = "ALL CIRCUITS SECURED" if finished else "CIRCUIT SECURED"
		title.add_theme_color_override("font_color", UiTheme.GREEN)
		sub.text = "Every bug was squashed."
		if points_gained > 0:
			sub.text += "\n+%d upgrade points" % points_gained
		if finished:
			buttons.add_child(UI.button("Continue", func() -> void: Transition.change_scene("res://scenes/ui/end_screen.tscn"), "BigButton"))
		else:
			buttons.add_child(UI.button("Upgrades", func() -> void: Transition.change_scene("res://scenes/ui/upgrade_screen.tscn"), "BigButton"))
	else:
		title.text = "SYSTEM FAILURE"
		title.add_theme_color_override("font_color", UiTheme.RED)
		sub.text = "The bugs reached the CPU."
		buttons.add_child(UI.button("Restart", game.restart, "BigButton"))
		buttons.add_child(UI.button("Main Menu", func() -> void: Transition.change_scene("res://scenes/ui/main_menu.tscn"), "BigButton"))
	box.add_child(UI.spacer(6))
	box.add_child(buttons)
	_overlay.add_child(UI.centered(panel))
	UI.reveal(panel, 0.1, Vector2(0, 40))


func _build_left() -> void:
	var panel := PanelContainer.new()
	panel.theme_type_variation = "SidePanel"
	panel.position = Vector2.ZERO
	panel.size = Vector2(Grid.ORIGIN.x, 768)
	add_child(panel)
	var box := UI.vbox(4)
	panel.add_child(box)
	box.add_child(UI.label("LEVEL", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER))
	_level_value = UI.label("1", "ValueLabel", HORIZONTAL_ALIGNMENT_CENTER)
	box.add_child(_level_value)
	box.add_child(UI.spacer(6))
	box.add_child(UI.label("WAVE", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER))
	_wave_value = UI.label("1/1", "ValueLabel", HORIZONTAL_ALIGNMENT_CENTER)
	_wave_value.add_theme_font_size_override("font_size", 28)
	box.add_child(_wave_value)
	box.add_child(UI.spacer(6))
	_status = UI.label("", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER)
	_status.add_theme_color_override("font_color", Color.WHITE)
	_status.custom_minimum_size.y = 44
	box.add_child(_status)
	box.add_child(_divider())
	box.add_child(UI.label("INCOMING", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER))
	_queue = UI.vbox(2)
	box.add_child(_queue)
	box.add_child(UI.spacer(0, true))
	box.add_child(_divider())
	box.add_child(UI.label("CPU", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER))
	_cpu_bar = Control.new()
	_cpu_bar.custom_minimum_size = Vector2(0, 18)
	_cpu_bar.draw.connect(_draw_cpu_bar)
	box.add_child(_cpu_bar)


func _build_right() -> void:
	var panel := PanelContainer.new()
	panel.theme_type_variation = "SidePanel"
	var left := Grid.ORIGIN.x + Grid.SIZE.x
	panel.position = Vector2(left, 0)
	panel.size = Vector2(1366 - left, 768)
	add_child(panel)
	var box := UI.vbox(8)
	panel.add_child(box)
	box.add_child(UI.label("CASH", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER))
	var money_row := UI.hbox(8)
	money_row.alignment = BoxContainer.ALIGNMENT_CENTER
	_money_icon = TextureRect.new()
	_money_icon.texture = CHIP_ICON
	_money_icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_money_icon.custom_minimum_size = Vector2(30, 30)
	_money_icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_money_icon.modulate = UiTheme.GOLD
	money_row.add_child(_money_icon)
	_money = UI.label("0", "MoneyLabel")
	money_row.add_child(_money)
	box.add_child(money_row)
	box.add_child(_divider())
	for i in DB.turrets.size():
		var slot := TurretSlot.new(DB.turrets[i], i + 1)
		var def := DB.turrets[i]
		slot.pressed.connect(func() -> void:
			if game.build.selected == def:
				game.build.cancel()
			else:
				game.build.select(def))
		box.add_child(slot)
		_slots.append(slot)
	box.add_child(UI.spacer(0, true))
	var row := UI.hbox(8)
	_speed_button = UI.button("1x", game.cycle_speed)
	_speed_button.tooltip_text = "Game speed (F)"
	_speed_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(_speed_button)
	var pause := UI.button("II", func() -> void: game.pause_menu.open())
	pause.tooltip_text = "Pause (Esc)"
	pause.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(pause)
	box.add_child(row)


func _build_board_overlay() -> void:
	var center_x := Grid.ORIGIN.x + Grid.SIZE.x / 2
	_cancel_button = UI.button("CANCEL", func() -> void: game.build.cancel(), "DangerButton")
	_cancel_button.custom_minimum_size = Vector2(200, 52)
	_cancel_button.position = Vector2(center_x - 100, 768 - 70)
	_cancel_button.visible = false
	add_child(_cancel_button)
	_hint = UI.label("", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER)
	_hint.add_theme_color_override("font_color", UiTheme.TEXT)
	_hint.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.9))
	_hint.add_theme_constant_override("outline_size", 6)
	_hint.position = Vector2(Grid.ORIGIN.x, 16)
	_hint.size = Vector2(Grid.SIZE.x, 24)
	_hint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_hint.visible = false
	add_child(_hint)
	_banner = UI.label("", "TitleLabel", HORIZONTAL_ALIGNMENT_CENTER)
	_banner.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.85))
	_banner.add_theme_constant_override("outline_size", 12)
	_banner.position = Vector2(Grid.ORIGIN.x, 250)
	_banner.size = Vector2(Grid.SIZE.x, 80)
	_banner.pivot_offset = _banner.size / 2
	_banner.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_banner.modulate.a = 0.0
	add_child(_banner)
	_banner_sub = UI.label("", "HeaderLabel", HORIZONTAL_ALIGNMENT_CENTER)
	_banner_sub.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.85))
	_banner_sub.add_theme_constant_override("outline_size", 8)
	_banner_sub.position = Vector2(Grid.ORIGIN.x, 330)
	_banner_sub.size = Vector2(Grid.SIZE.x, 40)
	_banner_sub.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_banner_sub.modulate.a = 0.0
	add_child(_banner_sub)


func _show_banner(text: String, sub := "") -> void:
	_banner.text = text
	_banner_sub.text = sub
	_banner.scale = Vector2(1.4, 1.4)
	var tween := create_tween().set_parallel()
	tween.tween_property(_banner, "modulate:a", 1.0, 0.25)
	tween.tween_property(_banner, "scale", Vector2.ONE, 0.35).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(_banner_sub, "modulate:a", 1.0, 0.3).set_delay(0.15)
	tween.chain().tween_interval(1.1)
	tween.chain().tween_property(_banner, "modulate:a", 0.0, 0.4)
	tween.parallel().tween_property(_banner_sub, "modulate:a", 0.0, 0.4)


func _divider() -> Control:
	var line := ColorRect.new()
	line.color = Color(UiTheme.CYAN, 0.18)
	line.custom_minimum_size = Vector2(0, 2)
	var wrap := UI.vbox(0)
	wrap.add_child(UI.spacer(6))
	wrap.add_child(line)
	wrap.add_child(UI.spacer(6))
	return wrap


func _refresh_queue(upcoming: Array) -> void:
	for child in _queue.get_children():
		child.queue_free()
	# Group consecutive bugs of the same type: "bug x5".
	var groups: Array = []
	for entry in upcoming:
		if not groups.is_empty() and groups[-1][0] == entry.enemy:
			groups[-1][1] += 1
		else:
			groups.append([entry.enemy, 1])
	for i in mini(groups.size(), 7):
		var def := DB.enemy(groups[i][0])
		if def == null:
			continue
		var row := UI.hbox(6)
		row.add_child(BugIcon.new(def, 30.0))
		var text := UI.label("x%d" % groups[i][1], "SmallHeader")
		text.add_theme_color_override("font_color", Color.WHITE if i == 0 else UiTheme.DIM)
		text.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		row.add_child(text)
		_queue.add_child(row)
	if groups.size() > 7:
		_queue.add_child(UI.label("+%d more" % (groups.size() - 7), "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER))
	if groups.is_empty():
		_queue.add_child(UI.label("-", "SmallHeader", HORIZONTAL_ALIGNMENT_CENTER))


func _draw_cpu_bar() -> void:
	var hp := game.cpu.hp
	var n := CpuCore.MAX_HP
	var w := (_cpu_bar.size.x - (n - 1) * 3) / n
	var color := CpuCore.CRITICAL.lerp(CpuCore.HEALTHY, float(hp) / n)
	for i in n:
		var r := Rect2(i * (w + 3), 2, w, 14)
		_cpu_bar.draw_rect(r, color if i < hp else Color(1, 1, 1, 0.08))


func _on_money_changed(value: int) -> void:
	_money.text = str(value)
	for slot in _slots:
		slot.affordable = value >= slot.def.cost


func _on_selection_changed(def: TurretDef) -> void:
	for slot in _slots:
		slot.selected = slot.def == def
	_cancel_button.visible = def != null
	_hint.visible = def != null
	if def:
		_hint.text = "%s  -  click a free cell to build  -  right click to cancel" % def.display_name.to_upper()


func _on_countdown_started(index: int, _seconds: float) -> void:
	if index > 0:
		_show_banner("WAVE %d CLEARED" % index, "Next wave incoming")


func _on_wave_started(index: int) -> void:
	_show_banner("WAVE %d" % (index + 1), "%d bugs" % game.level.enemy_count(index))
	Audio.play("wave", &"SFX", -2.0)
