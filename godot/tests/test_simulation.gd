extends TestCase
## Plays real levels headless (run with --fixed-fps 60 so it's deterministic-ish and fast).

const GAME_SCENE := preload("res://scenes/game/game.tscn")
const TIME_LIMIT := 400.0


func test_level_1_lost_without_turrets() -> void:
	var result := await _play(0, false)
	check_eq(result.state, Game.State.LOST, "state")


func test_bot_clears_level_1() -> String:
	var result := await _play(0, true)
	check_eq(result.state, Game.State.WON, "state")
	return "(cpu %d/10, %.0fs)" % [result.cpu, result.time]


func test_bot_clears_level_2() -> String:
	var result := await _play(1, true)
	check_eq(result.state, Game.State.WON, "state")
	return "(cpu %d/10, %.0fs)" % [result.cpu, result.time]


func test_bot_clears_level_3() -> String:
	var result := await _play(2, true)
	check_eq(result.state, Game.State.WON, "state")
	return "(cpu %d/10, %.0fs)" % [result.cpu, result.time]


func test_every_turret_damages_bugs() -> void:
	for def in DB.turrets:
		GameState.current_level = 0
		var game: Game = GAME_SCENE.instantiate()
		get_tree().root.add_child(game)
		await get_tree().process_frame
		game.waves.set_process(false)
		# Bug walks down the route; turret next to the first straight segment.
		var cell := Vector2i(4, 3)
		game.money = 100
		check(game.build_turret(def, cell, -PI / 2), "%s built" % def.id)
		game.spawn_enemy("tank")
		var tank: Enemy = game.enemies[0]
		tank.def = tank.def.duplicate()
		tank.def.max_hp = 1000.0
		tank.hp = 1000.0
		tank.progress = 120.0
		var slowed := false
		for i in 60 * 12:
			await get_tree().process_frame
			slowed = slowed or tank.is_slowed()
		if def.id == "freezer":
			check(slowed, "freezer slowed the bug")
		else:
			check(tank.hp < 1000.0, "%s did no damage" % def.id)
		game.queue_free()
		await get_tree().process_frame


func _play(level: int, with_bot: bool) -> Dictionary:
	GameState.current_level = level
	var game: Game = GAME_SCENE.instantiate()
	get_tree().root.add_child(game)
	await get_tree().process_frame
	if with_bot:
		var bot := AutoPlayer.new()
		bot.game = game
		game.add_child(bot)
	Engine.time_scale = 2.0
	var time := 0.0
	while game.state == Game.State.PLAYING and time < TIME_LIMIT:
		await get_tree().process_frame
		time += get_process_delta_time()
	Engine.time_scale = 1.0
	var result := {"state": game.state, "cpu": game.cpu.hp, "time": time}
	game.queue_free()
	await get_tree().process_frame
	return result
