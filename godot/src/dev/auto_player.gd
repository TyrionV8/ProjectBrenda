class_name AutoPlayer
extends Node
## A simple bot for tests and screenshots: whenever it can afford a turret,
## it builds the next one from `plan` on the free cell that covers the most route.

var game: Game
var plan: Array[String] = ["blaster", "burst", "blaster", "freezer", "mortar", "burst"]
var interval := 0.4
var _next := 0
var _timer := 0.0


func _process(delta: float) -> void:
	if game == null or game.state != Game.State.PLAYING:
		return
	_timer -= delta
	if _timer > 0.0:
		return
	_timer = interval
	var def := DB.turret(plan[_next % plan.size()])
	if game.money < def.cost:
		return
	var cell := best_cell(def)
	if cell.x >= 0 and game.build_turret(def, cell, best_direction(def, cell)):
		_next += 1


func best_cell(def: TurretDef) -> Vector2i:
	var stats := GameState.turret_stats(def)
	var reach := float(stats.get("range", 0.0))
	if def.needs_direction:
		reach = float(stats.get("beam_length", 48.0)) + 24.0
	var points := game.curve.get_baked_points()
	var best := Vector2i(-1, -1)
	var best_score := 0
	for x in Grid.COLS:
		for y in Grid.ROWS:
			var cell := Vector2i(x, y)
			if not game.can_build(cell):
				continue
			var center := Grid.cell_center(cell)
			var score := 0
			for i in range(0, points.size(), 4):
				if center.distance_to(points[i]) <= reach:
					score += 1
			if score > best_score:
				best_score = score
				best = cell
	return best


func best_direction(def: TurretDef, cell: Vector2i) -> float:
	if not def.needs_direction:
		return 0.0
	var center := Grid.cell_center(cell)
	var best := 0.0
	var best_score := -1
	for d in 4:
		var dir := Vector2.from_angle(d * PI / 2)
		var score := 0
		for step in range(1, 4):
			if game.blocked.has(Grid.to_cell(center + dir * Grid.CELL * step)):
				score += 1
		if score > best_score:
			best_score = score
			best = d * PI / 2
	return best
