class_name SettingsPanel
extends PanelContainer
## Volume sliders and display options. Emits `closed` when the player goes back.

signal closed


func _ready() -> void:
	custom_minimum_size = Vector2(520, 0)
	var box := UI.vbox(14)
	add_child(box)
	box.add_child(UI.label("SETTINGS", "HeaderLabel", HORIZONTAL_ALIGNMENT_CENTER))
	box.add_child(UI.spacer(6))
	for entry in [["Master", "Master volume"], ["Music", "Music"], ["SFX", "Effects"], ["UI", "Interface sounds"]]:
		box.add_child(_slider(entry[0], entry[1]))
	box.add_child(UI.spacer(4))
	if not OS.has_feature("mobile") and not OS.has_feature("web"):
		var full := CheckBox.new()
		full.text = "Full screen"
		full.button_pressed = Settings.fullscreen
		full.toggled.connect(func(on: bool) -> void:
			Audio.ui("click")
			Settings.set_fullscreen(on))
		box.add_child(full)
	var fx := CheckBox.new()
	fx.text = "Screen effects (scanlines, vignette)"
	fx.button_pressed = Settings.screen_fx
	fx.toggled.connect(func(on: bool) -> void:
		Audio.ui("click")
		Settings.set_screen_fx(on))
	box.add_child(fx)
	box.add_child(UI.spacer(8))
	var back := UI.button("Back", func() -> void: closed.emit())
	back.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	back.custom_minimum_size.x = 200
	box.add_child(back)


func _slider(bus: String, text: String) -> Control:
	var row := UI.vbox(4)
	var top := UI.hbox()
	var name_label := UI.label(text.to_upper(), "SmallHeader")
	name_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top.add_child(name_label)
	var value_label := UI.label("", "SmallHeader")
	top.add_child(value_label)
	row.add_child(top)
	var slider := HSlider.new()
	slider.min_value = 0.0
	slider.max_value = 1.0
	slider.step = 0.01
	slider.value = Settings.volumes[bus]
	slider.custom_minimum_size.y = 26
	value_label.text = "%d%%" % roundi(slider.value * 100)
	slider.value_changed.connect(func(v: float) -> void:
		value_label.text = "%d%%" % roundi(v * 100)
		Settings.set_volume(bus, v))
	slider.drag_ended.connect(func(_changed: bool) -> void:
		Audio.play("place", StringName(bus) if bus != "Master" else &"SFX"))
	row.add_child(slider)
	return row
