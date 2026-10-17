extends Node
## Player settings (volumes, fullscreen, screen effects), saved in user://settings.cfg.

signal changed

const PATH := "user://settings.cfg"
const BUSES: Array[String] = ["Master", "Music", "SFX", "UI"]

var volumes := {"Master": 0.8, "Music": 0.7, "SFX": 0.8, "UI": 0.8}
var fullscreen := false
var screen_fx := true


func _ready() -> void:
	_load()
	for bus in BUSES:
		_apply_volume(bus)
	if DisplayServer.get_name() != "headless":
		if fullscreen:
			DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		else:
			_fit_window()


func set_volume(bus: String, value: float) -> void:
	volumes[bus] = clampf(value, 0.0, 1.0)
	_apply_volume(bus)
	_save()
	changed.emit()


func set_fullscreen(on: bool) -> void:
	fullscreen = on
	if on:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		_fit_window()
	_save()
	changed.emit()


func set_screen_fx(on: bool) -> void:
	screen_fx = on
	_save()
	changed.emit()


func _apply_volume(bus: String) -> void:
	var index := AudioServer.get_bus_index(bus)
	if index < 0:
		return
	var value: float = volumes[bus]
	AudioServer.set_bus_mute(index, value <= 0.001)
	AudioServer.set_bus_volume_db(index, linear_to_db(maxf(value, 0.001)))


## Opens the window at 4/5 of the screen width keeping 16:9, centred (as the original did).
func _fit_window() -> void:
	if OS.has_feature("mobile"):
		return
	var screen := DisplayServer.screen_get_usable_rect()
	var width := int(screen.size.x * 0.8)
	var size := Vector2i(width, width * 9 / 16)
	DisplayServer.window_set_size(size)
	DisplayServer.window_set_position(screen.position + (screen.size - size) / 2)


func _load() -> void:
	var cfg := ConfigFile.new()
	if cfg.load(PATH) != OK:
		return
	for bus in BUSES:
		volumes[bus] = float(cfg.get_value("audio", bus, volumes[bus]))
	fullscreen = bool(cfg.get_value("display", "fullscreen", fullscreen))
	screen_fx = bool(cfg.get_value("display", "screen_fx", screen_fx))


func _save() -> void:
	var cfg := ConfigFile.new()
	for bus in BUSES:
		cfg.set_value("audio", bus, volumes[bus])
	cfg.set_value("display", "fullscreen", fullscreen)
	cfg.set_value("display", "screen_fx", screen_fx)
	cfg.save(PATH)
