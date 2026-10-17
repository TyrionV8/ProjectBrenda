extends Node
## Developer entry points, only active with command-line user arguments:
##   godot --headless --path . --fixed-fps 60 -- --test         run tests/test_*.gd
##   godot --path . -- --capture=menu,game --out=/tmp/shots     save screenshots
##   godot --headless --path . -- --build-theme                 regenerate assets/ui/theme.tres from UiTheme

const TEST_DIR := "res://tests/"


func _ready() -> void:
	var args := OS.get_cmdline_user_args()
	if args.has("--build-theme"):
		DirAccess.make_dir_recursive_absolute("res://assets/ui")
		var err := ResourceSaver.save(UiTheme.build(), "res://assets/ui/theme.tres")
		print("theme saved: ", error_string(err))
		get_tree().quit(0 if err == OK else 1)
		return
	if args.has("--test"):
		_run_tests.call_deferred()
		return
	for arg in args:
		if arg.begins_with("--capture="):
			_capture.call_deferred(arg.trim_prefix("--capture=").split(","), _arg(args, "--out", "user://shots"))
			return


func _arg(args: PackedStringArray, key: String, default: String) -> String:
	for arg in args:
		if arg.begins_with(key + "="):
			return arg.trim_prefix(key + "=")
	return default


func _run_tests() -> void:
	var failures := 0
	var total := 0
	var files := DirAccess.get_files_at(TEST_DIR)
	for file in files:
		if not (file.begins_with("test_") and file.ends_with(".gd")):
			continue
		var suite: Object = load(TEST_DIR + file).new()
		if suite is Node:
			add_child(suite)
		for method in suite.get_method_list():
			var name: String = method.name
			if not name.begins_with("test_"):
				continue
			total += 1
			GameState.reset()
			var result: Variant = await suite.call(name)
			var errors: Array = suite.get("errors")
			if errors.is_empty():
				print("  PASS  %s.%s %s" % [file.get_basename(), name, "" if result == null else str(result)])
			else:
				failures += 1
				print("  FAIL  %s.%s" % [file.get_basename(), name])
				for e in errors:
					print("        - %s" % e)
				errors.clear()
		if suite is Node:
			suite.queue_free()
	print("%d/%d tests passed" % [total - failures, total])
	Engine.time_scale = 1.0
	get_tree().quit(1 if failures > 0 else 0)


func _capture(shots: PackedStringArray, out_dir: String) -> void:
	DirAccess.make_dir_recursive_absolute(out_dir)
	for shot in shots:
		var parts := shot.split(":")
		var what := parts[0]
		match what:
			"menu":
				await _goto("res://scenes/ui/main_menu.tscn", 2.5)
			"levels":
				GameState.unlocked.fill(true)
				await _goto("res://scenes/ui/level_select.tscn", 1.5)
			"upgrades":
				GameState.points = 9
				for i in 4:
					GameState.buy_upgrade(DB.turrets[i], DB.turrets[i].upgrades[0])
				await _goto("res://scenes/ui/upgrade_screen.tscn", 1.5)
			"end":
				await _goto("res://scenes/ui/end_screen.tscn", 2.0)
			"game":
				GameState.current_level = int(parts[1]) - 1 if parts.size() > 1 else 0
				await _goto("res://scenes/game/game.tscn", 0.5)
				var game: Game = get_tree().current_scene
				var bot := AutoPlayer.new()
				bot.game = game
				bot.plan = ["blaster", "burst", "laser", "mortar", "freezer", "blaster"]
				game.add_child(bot)
				game.money += 60
				await get_tree().create_timer(float(parts[2]) if parts.size() > 2 else 9.0).timeout
				var build := game.build
				build.select(DB.turrets[0])
				build._update_pointer(Grid.cell_center(Vector2i(10, 8)))
			"win":
				GameState.current_level = int(parts[1]) - 1 if parts.size() > 1 else 0
				await _goto("res://scenes/game/game.tscn", 0.2)
				var game: Game = get_tree().current_scene
				var bot := AutoPlayer.new()
				bot.game = game
				game.add_child(bot)
				game.cycle_speed()
				game.cycle_speed()
				while game.state == Game.State.PLAYING:
					await get_tree().process_frame
				Engine.time_scale = 1.0
				await get_tree().create_timer(2.0).timeout
			"pause":
				var game: Game = get_tree().current_scene
				game.pause_menu.open()
				await get_tree().create_timer(0.4).timeout
		await RenderingServer.frame_post_draw
		var path := out_dir.path_join("%s.png" % shot.replace(":", "_"))
		get_viewport().get_texture().get_image().save_png(path)
		print("saved ", path)
	get_tree().quit()


func _goto(path: String, wait: float) -> void:
	Transition.change_scene(path)
	await get_tree().create_timer(0.8 + wait).timeout
