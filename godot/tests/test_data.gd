extends TestCase
## Data loading, stats and save/progress logic.


func test_levels_load() -> void:
	check_eq(DB.levels.size(), 3, "level count")
	check_eq(DB.levels[0].waves.size(), 3, "level 1 waves")
	check_eq(DB.levels[0].start_money, 30, "level 1 money")
	for i in DB.levels.size():
		var level := DB.levels[i]
		for wave in level.waves:
			for spawn in wave:
				check(DB.enemy(spawn.enemy) != null, "level %d uses unknown enemy %s" % [i + 1, spawn.enemy])


func test_blocked_cells_cover_route_and_cpu() -> void:
	var level := DB.levels[0]
	var blocked := level.blocked_cells()
	for cell in level.path_cells():
		check(blocked.has(cell), "route cell %s not blocked" % cell)
	check(blocked.has(level.cpu_cell() + Vector2i(1, 1)), "cpu footprint")
	check(not blocked.has(Vector2i(10, 9)), "free cell blocked")


func test_components_dont_overlap_route() -> void:
	for i in DB.levels.size():
		var level := DB.levels[i]
		var route := level.path_cells()
		for c in level.components:
			for x in c.size.x:
				for y in c.size.y:
					check(not route.has(c.cell + Vector2i(x, y)), "level %d component %s on the route" % [i + 1, c.type])


func test_upgrades_change_stats() -> void:
	var blaster := DB.turret("blaster")
	var base := blaster.compute_stats({})
	check_near(base.damage, 1.0, "base damage")
	check_near(base.cooldown, 2.0, "base cooldown")
	var up := blaster.compute_stats({"damage": 3, "speed": 25, "range": 15})
	check_near(up.damage, 4.0, "upgraded damage")
	check_near(up.cooldown, 2.0 - 25.0 / 30.0, "upgraded cooldown")
	check_near(up.range, 125.0, "upgraded range")
	var freezer := DB.turret("freezer")
	check_near(freezer.compute_stats({"freeze": 50}).slow_factor, 0.1, "slow factor clamped")


func test_buying_upgrades_spends_points() -> void:
	var blaster := DB.turret("blaster")
	var damage := blaster.find_upgrade("damage")
	GameState.points = 2
	check(GameState.buy_upgrade(blaster, damage), "first buy")
	check(GameState.buy_upgrade(blaster, damage), "second buy")
	check(not GameState.buy_upgrade(blaster, damage), "no points left")
	check_eq(GameState.upgrade_level("blaster", "damage"), 2, "level")


func test_save_roundtrip() -> void:
	GameState.points = 3
	GameState.unlocked[1] = true
	GameState.buy_upgrade(DB.turret("mortar"), DB.turret("mortar").find_upgrade("bomb_area"))
	GameState.save()
	GameState.reset()
	check(GameState.load_save(), "load")
	check_eq(GameState.points, 2, "points")
	check(GameState.unlocked[1], "unlocked level 2")
	check_eq(GameState.upgrade_level("mortar", "bomb_area"), 1, "upgrade")
	check_eq(GameState.current_level, 1, "continues from last unlocked")
	DirAccess.remove_absolute(GameState.SAVE_PATH)


func test_completing_level_awards_points_once() -> void:
	GameState.current_level = 0
	var before := GameState.points
	check(not GameState.complete_current_level(), "not finished")
	check_eq(GameState.points, before + GameState.POINTS_PER_LEVEL, "points after first clear")
	GameState.current_level = 0
	GameState.complete_current_level()
	check_eq(GameState.points, before + GameState.POINTS_PER_LEVEL, "no points for replays")
	GameState.current_level = DB.levels.size() - 1
	check(GameState.complete_current_level(), "last level finishes the game")
	DirAccess.remove_absolute(GameState.SAVE_PATH)
