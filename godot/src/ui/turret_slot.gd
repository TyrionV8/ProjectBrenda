class_name TurretSlot
extends Button
## Inventory slot for one turret type: preview, name, cost and hotkey.

const SOCKET := preload("res://assets/art/turrets/socket.svg")

var def: TurretDef
var hotkey := 1
var selected := false:
	set(value):
		selected = value
		queue_redraw()
var affordable := true:
	set(value):
		if value != affordable:
			affordable = value
			queue_redraw()
var _t := 0.0


func _init(p_def: TurretDef, p_hotkey: int) -> void:
	def = p_def
	hotkey = p_hotkey
	flat = true
	focus_mode = Control.FOCUS_NONE
	custom_minimum_size = Vector2(186, 92)
	tooltip_text = "%s\n%s" % [def.display_name, def.description]
	mouse_entered.connect(func() -> void:
		Audio.ui("hover")
		queue_redraw())
	mouse_exited.connect(queue_redraw)


func _process(delta: float) -> void:
	if selected:
		_t += delta
		queue_redraw()


func _draw() -> void:
	var r := Rect2(Vector2.ZERO, size)
	var hovered := is_hovered()
	var bg := Color(0.05, 0.11, 0.18, 0.95) if not hovered else Color(0.08, 0.17, 0.26, 0.98)
	var border := Color(UiTheme.CYAN, 0.25)
	if selected:
		var pulse := 0.6 + 0.4 * sin(_t * 6.0)
		border = Color(def.color, pulse)
		draw_style_box(_box(Color(0, 0, 0, 0), Color(0, 0, 0, 0), Color(def.color, 0.35 * pulse), 14), r)
	elif hovered:
		border = Color(def.color, 0.7)
	draw_style_box(_box(bg, border, Color(0, 0, 0, 0.4), 6), r)
	# Recessed preview well.
	var well := Rect2(7, 7, 78, 78)
	draw_style_box(_box(Color(0.01, 0.03, 0.05, 1), Color(1, 1, 1, 0.06), Color(0, 0, 0, 0), 0), well)
	var mod := Color.WHITE if affordable else Color(0.5, 0.5, 0.55, 0.7)
	draw_texture_rect(SOCKET, well.grow(-6), false, mod)
	draw_texture_rect(def.icon, well.grow(-6), false, mod)
	var font_bold := UiTheme.PIXEL_BOLD
	var font := UiTheme.PIXEL_FONT
	var words := def.display_name.to_upper().split(" ", false, 1)
	for i in words.size():
		draw_string(font, Vector2(96, 24 + i * 15), words[i], HORIZONTAL_ALIGNMENT_LEFT, size.x - 100, 11, UiTheme.TEXT if affordable else UiTheme.DIM)
	var cost_color := UiTheme.GOLD if affordable else UiTheme.RED
	draw_string(font_bold, Vector2(96, 72), "$%d" % def.cost, HORIZONTAL_ALIGNMENT_LEFT, -1, 22, cost_color)
	draw_rect(Rect2(size.x - 26, size.y - 26, 18, 18), Color(1, 1, 1, 0.08))
	draw_string(font, Vector2(size.x - 22, size.y - 12), str(hotkey), HORIZONTAL_ALIGNMENT_LEFT, -1, 13, UiTheme.DIM)
	draw_rect(Rect2(96, 80, 40, 3), Color(def.color, 0.9 if affordable else 0.3))


static func _box(bg: Color, border: Color, shadow: Color, shadow_size: int) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = bg
	s.border_color = border
	s.set_border_width_all(2 if border.a > 0 else 0)
	s.set_corner_radius_all(8)
	s.shadow_color = shadow
	s.shadow_size = shadow_size
	s.anti_aliasing = true
	return s
