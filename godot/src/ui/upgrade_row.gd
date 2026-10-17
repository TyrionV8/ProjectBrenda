class_name UpgradeRow
extends Button
## One upgrade in the upgrade screen: icon, name, level pips and a "+" to buy.

signal focused(row: UpgradeRow)

var turret: TurretDef
var upgrade: UpgradeDef
var _flash := 0.0


func _init(p_turret: TurretDef, p_upgrade: UpgradeDef) -> void:
	turret = p_turret
	upgrade = p_upgrade
	flat = true
	focus_mode = Control.FOCUS_NONE
	custom_minimum_size = Vector2(0, 52)
	mouse_entered.connect(func() -> void:
		Audio.ui("hover")
		focused.emit(self)
		queue_redraw())
	mouse_exited.connect(queue_redraw)
	GameState.changed.connect(queue_redraw)


func bought() -> void:
	_flash = 1.0
	set_process(true)


func _process(delta: float) -> void:
	_flash = maxf(_flash - delta * 3.0, 0.0)
	queue_redraw()
	if _flash == 0.0:
		set_process(false)


func _draw() -> void:
	var level := GameState.upgrade_level(turret.id, upgrade.id)
	var maxed := level >= upgrade.max_level
	var can_buy := GameState.can_buy_upgrade(turret, upgrade)
	var hovered := is_hovered()
	var bg := Color(0.05, 0.11, 0.18, 0.9).lerp(Color(turret.color, 0.35), _flash)
	if hovered:
		bg = bg.lightened(0.08)
	draw_rect(Rect2(Vector2.ZERO, size), bg)
	draw_rect(Rect2(Vector2.ZERO, size), Color(turret.color, 0.55 if hovered else 0.18), false, 1.5)
	draw_texture_rect(upgrade.icon, Rect2(8, 10, 32, 32), false, turret.color if level > 0 else Color(turret.color, 0.5))
	var font := UiTheme.PIXEL_FONT
	draw_string(font, Vector2(48, 22), upgrade.label.to_upper(), HORIZONTAL_ALIGNMENT_LEFT, size.x - 90, 12, UiTheme.TEXT)
	# Progress bar made of small blocks.
	var bar := Rect2(48, 31, size.x - 96, 9)
	var blocks := mini(upgrade.max_level, 25)
	var bw := bar.size.x / blocks
	var filled := float(level) / upgrade.max_level * blocks
	for i in blocks:
		var r := Rect2(bar.position.x + i * bw, bar.position.y, bw - 1.5, bar.size.y)
		draw_rect(r, turret.color if i < filled else Color(1, 1, 1, 0.08))
	var label := "MAX" if maxed else "%d/%d" % [level, upgrade.max_level]
	draw_string(font, Vector2(48, 50), label, HORIZONTAL_ALIGNMENT_LEFT, -1, 10, UiTheme.GOLD if maxed else UiTheme.DIM)
	# Buy button.
	var plus := Rect2(size.x - 40, 10, 32, 32)
	var plus_color := turret.color if can_buy else Color(0.4, 0.45, 0.5, 0.4)
	draw_rect(plus, Color(plus_color, 0.15))
	draw_rect(plus, plus_color, false, 2.0)
	if not maxed:
		draw_line(plus.get_center() - Vector2(8, 0), plus.get_center() + Vector2(8, 0), plus_color, 3.0)
		draw_line(plus.get_center() - Vector2(0, 8), plus.get_center() + Vector2(0, 8), plus_color, 3.0)
	else:
		draw_string(font, plus.position + Vector2(5, 21), "OK", HORIZONTAL_ALIGNMENT_LEFT, -1, 10, plus_color)
