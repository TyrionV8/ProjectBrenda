class_name UiTheme
extends RefCounted
## The game's UI theme, built in code and applied to the root window at startup.

const CYAN := Color("3ef0ff")
const GOLD := Color("ffd34d")
const RED := Color("ff4d5e")
const GREEN := Color("7dff6a")
const TEXT := Color("d8f6ff")
const DIM := Color("7f9bb0")
const PANEL := Color(0.03, 0.07, 0.12, 0.92)

const BODY_FONT := preload("res://assets/fonts/ShareTechMono-Regular.ttf")
const PIXEL_FONT := preload("res://assets/fonts/Silkscreen-Regular.ttf")
const PIXEL_BOLD := preload("res://assets/fonts/Silkscreen-Bold.ttf")


static func build() -> Theme:
	var t := Theme.new()
	t.default_font = BODY_FONT
	t.default_font_size = 20

	t.set_color("font_color", "Label", TEXT)

	_label_variation(t, "TitleLabel", PIXEL_BOLD, 64, CYAN)
	_label_variation(t, "HeaderLabel", PIXEL_BOLD, 28, TEXT)
	_label_variation(t, "SmallHeader", PIXEL_FONT, 14, DIM)
	_label_variation(t, "ValueLabel", PIXEL_BOLD, 34, CYAN)
	_label_variation(t, "MoneyLabel", PIXEL_BOLD, 34, GOLD)
	_label_variation(t, "DimLabel", BODY_FONT, 18, DIM)

	t.set_font("font", "Button", PIXEL_FONT)
	t.set_font_size("font_size", "Button", 20)
	t.set_color("font_color", "Button", TEXT)
	t.set_color("font_hover_color", "Button", Color.WHITE)
	t.set_color("font_pressed_color", "Button", Color("061018"))
	t.set_color("font_focus_color", "Button", Color.WHITE)
	t.set_color("font_disabled_color", "Button", Color(0.5, 0.58, 0.65, 0.6))
	t.set_stylebox("normal", "Button", _box(Color(0.04, 0.12, 0.2, 0.9), Color(CYAN, 0.45), 2, 6, 18, 10))
	t.set_stylebox("hover", "Button", _box(Color(0.07, 0.22, 0.33, 0.95), CYAN, 2, 6, 18, 10, Color(CYAN, 0.35), 10))
	t.set_stylebox("pressed", "Button", _box(CYAN, CYAN, 2, 6, 18, 10, Color(CYAN, 0.5), 14))
	t.set_stylebox("disabled", "Button", _box(Color(0.04, 0.08, 0.12, 0.7), Color(0.4, 0.5, 0.6, 0.25), 2, 6, 18, 10))
	t.set_stylebox("focus", "Button", _box(Color(0, 0, 0, 0), Color(1, 1, 1, 0.7), 2, 6, 18, 10))

	t.add_type("BigButton")
	t.set_type_variation("BigButton", "Button")
	t.set_font_size("font_size", "BigButton", 26)
	t.set_stylebox("normal", "BigButton", _box(Color(0.04, 0.12, 0.2, 0.9), Color(CYAN, 0.5), 2, 8, 28, 14))
	t.set_stylebox("hover", "BigButton", _box(Color(0.07, 0.22, 0.33, 0.95), CYAN, 2, 8, 28, 14, Color(CYAN, 0.4), 14))
	t.set_stylebox("pressed", "BigButton", _box(CYAN, CYAN, 2, 8, 28, 14, Color(CYAN, 0.5), 18))
	t.set_stylebox("disabled", "BigButton", _box(Color(0.04, 0.08, 0.12, 0.7), Color(0.4, 0.5, 0.6, 0.25), 2, 8, 28, 14))

	t.add_type("DangerButton")
	t.set_type_variation("DangerButton", "Button")
	t.set_stylebox("normal", "DangerButton", _box(Color(0.2, 0.04, 0.07, 0.9), Color(RED, 0.5), 2, 6, 18, 10))
	t.set_stylebox("hover", "DangerButton", _box(Color(0.33, 0.07, 0.1, 0.95), RED, 2, 6, 18, 10, Color(RED, 0.35), 10))
	t.set_stylebox("pressed", "DangerButton", _box(RED, RED, 2, 6, 18, 10))

	var glass := _box(PANEL, Color(CYAN, 0.35), 2, 10, 20, 18, Color(0, 0, 0, 0.55), 16)
	t.set_stylebox("panel", "PanelContainer", glass)
	t.set_stylebox("panel", "Panel", glass)
	t.add_type("SidePanel")
	t.set_type_variation("SidePanel", "PanelContainer")
	var side := _box(Color(0.02, 0.05, 0.09, 0.96), Color(CYAN, 0.25), 0, 0, 14, 16)
	side.border_width_left = 2
	side.border_width_right = 2
	t.set_stylebox("panel", "SidePanel", side)
	t.add_type("CardPanel")
	t.set_type_variation("CardPanel", "PanelContainer")
	t.set_stylebox("panel", "CardPanel", _box(Color(0.04, 0.1, 0.17, 0.92), Color(CYAN, 0.22), 2, 10, 14, 14))

	t.set_stylebox("slider", "HSlider", _box(Color(0.02, 0.06, 0.1, 1), Color(CYAN, 0.35), 1, 4, 0, 4))
	t.set_stylebox("grabber_area", "HSlider", _box(Color(CYAN, 0.75), Color(0, 0, 0, 0), 0, 4, 0, 4))
	t.set_stylebox("grabber_area_highlight", "HSlider", _box(CYAN, Color(0, 0, 0, 0), 0, 4, 0, 4))
	t.set_icon("grabber", "HSlider", _dot(22, CYAN))
	t.set_icon("grabber_highlight", "HSlider", _dot(22, Color.WHITE))

	t.set_font("font", "CheckBox", PIXEL_FONT)
	t.set_font_size("font_size", "CheckBox", 18)
	t.set_color("font_color", "CheckBox", TEXT)
	t.set_color("font_hover_color", "CheckBox", Color.WHITE)
	t.set_color("font_pressed_color", "CheckBox", CYAN)
	t.set_color("font_hover_pressed_color", "CheckBox", CYAN)
	t.set_icon("checked", "CheckBox", _check(true))
	t.set_icon("unchecked", "CheckBox", _check(false))
	return t


static func _label_variation(t: Theme, name: String, font: Font, size: int, color: Color) -> void:
	t.add_type(name)
	t.set_type_variation(name, "Label")
	t.set_font("font", name, font)
	t.set_font_size("font_size", name, size)
	t.set_color("font_color", name, color)


static func _box(bg: Color, border: Color, border_w: int, radius: int, pad_x: int, pad_y: int, shadow := Color(0, 0, 0, 0), shadow_size := 0) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = bg
	s.border_color = border
	s.set_border_width_all(border_w)
	s.set_corner_radius_all(radius)
	s.content_margin_left = pad_x
	s.content_margin_right = pad_x
	s.content_margin_top = pad_y
	s.content_margin_bottom = pad_y
	s.shadow_color = shadow
	s.shadow_size = shadow_size
	s.anti_aliasing = true
	return s


static func _dot(size: int, color: Color) -> ImageTexture:
	var img := Image.create(size, size, false, Image.FORMAT_RGBA8)
	var c := (size - 1) / 2.0
	for y in size:
		for x in size:
			var d := Vector2(x - c, y - c).length()
			var a := clampf(c - d + 0.5, 0.0, 1.0)
			var inner := clampf(c - 3.0 - d + 0.5, 0.0, 1.0)
			img.set_pixel(x, y, Color(color.lerp(Color("061018"), inner * 0.7), a))
	return ImageTexture.create_from_image(img)


static func _check(on: bool) -> ImageTexture:
	var size := 26
	var img := Image.create(size, size, false, Image.FORMAT_RGBA8)
	for y in size:
		for x in size:
			var edge := x < 2 or y < 2 or x >= size - 2 or y >= size - 2
			var inner := x >= 6 and y >= 6 and x < size - 6 and y < size - 6
			var c := Color(0, 0, 0, 0)
			if edge:
				c = Color(CYAN, 0.8)
			elif inner and on:
				c = CYAN
			elif not edge:
				c = Color(0.02, 0.06, 0.1, 0.9)
			img.set_pixel(x, y, c)
	return ImageTexture.create_from_image(img)
