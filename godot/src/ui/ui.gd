class_name UI
extends RefCounted
## Small helpers to build menus in code with consistent feedback (hover sound + pop).


static func button(text: String, on_pressed: Callable, variation := "") -> Button:
	var b := Button.new()
	b.text = text
	if variation != "":
		b.theme_type_variation = variation
	b.pressed.connect(func() -> void:
		Audio.ui("click")
		on_pressed.call())
	b.mouse_entered.connect(func() -> void:
		if not b.disabled:
			Audio.ui("hover")
			_pop(b, 1.04))
	b.mouse_exited.connect(func() -> void: _pop(b, 1.0))
	b.resized.connect(func() -> void: b.pivot_offset = b.size / 2)
	return b


static func label(text: String, variation := "", align := HORIZONTAL_ALIGNMENT_LEFT) -> Label:
	var l := Label.new()
	l.text = text
	l.horizontal_alignment = align
	if variation != "":
		l.theme_type_variation = variation
	return l


static func vbox(separation := 12) -> VBoxContainer:
	var b := VBoxContainer.new()
	b.add_theme_constant_override("separation", separation)
	return b


static func hbox(separation := 12) -> HBoxContainer:
	var b := HBoxContainer.new()
	b.add_theme_constant_override("separation", separation)
	return b


static func spacer(height := 0, expand := false) -> Control:
	var c := Control.new()
	c.custom_minimum_size.y = height
	if expand:
		c.size_flags_vertical = Control.SIZE_EXPAND_FILL
	c.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return c


static func centered(child: Control) -> CenterContainer:
	var c := CenterContainer.new()
	c.set_anchors_preset(Control.PRESET_FULL_RECT)
	c.add_child(child)
	return c


## Slide/fade a control in when a screen opens.
static func reveal(node: CanvasItem, delay := 0.0, offset := Vector2(0, 24)) -> void:
	node.modulate.a = 0.0
	if not node.is_inside_tree():
		await node.tree_entered
	await node.get_tree().process_frame
	if not is_instance_valid(node):
		return
	var tween := node.create_tween().set_parallel()
	tween.tween_property(node, "modulate:a", 1.0, 0.35).set_delay(delay)
	if node is Control:
		var control := node as Control
		var target := control.position
		control.position = target + offset
		tween.tween_property(control, "position", target, 0.45).set_delay(delay).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)


static func _pop(control: Control, to: float) -> void:
	control.create_tween().tween_property(control, "scale", Vector2.ONE * to, 0.12).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
