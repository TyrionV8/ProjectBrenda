class_name LevelData
extends RefCounted
## A level as stored in data/levels/*.json: the bug route, decorative components
## that block building, starting money and the waves.

const CPU_FOOTPRINT := 1 ## Cells around the CPU (in each direction) that can't be built on.

var title := ""
var start_money := 0
var music := ""
var waypoints: Array[Vector2i] = []
## Each entry: {"type": String, "cell": Vector2i, "size": Vector2i, "color": Color}
var components: Array[Dictionary] = []
## waves[i] is an Array of {"enemy": String, "time": float}, sorted by time.
var waves: Array = []


static func load_file(path: String) -> LevelData:
	var text := FileAccess.get_file_as_string(path)
	var parsed: Variant = JSON.parse_string(text)
	if parsed is not Dictionary:
		push_error("Invalid level file: %s" % path)
		return null
	return from_dict(parsed)


static func from_dict(d: Dictionary) -> LevelData:
	var level := LevelData.new()
	level.title = d.get("title", "")
	level.start_money = int(d.get("start_money", 0))
	level.music = d.get("music", "")
	for p in d.get("path", []):
		level.waypoints.append(Vector2i(int(p[0]), int(p[1])))
	for c in d.get("components", []):
		var size: Array = c.get("size", [1, 1])
		level.components.append({
			"type": String(c.get("type", "chip")),
			"cell": Vector2i(int(c.cell[0]), int(c.cell[1])),
			"size": Vector2i(int(size[0]), int(size[1])),
			"color": Color(c.get("color", "ff3344")),
		})
	for w in d.get("waves", []):
		var wave: Array = []
		for s in w:
			wave.append({"enemy": String(s.enemy), "time": float(s.time)})
		wave.sort_custom(func(a, b): return a.time < b.time)
		level.waves.append(wave)
	return level


func to_dict() -> Dictionary:
	var comps := []
	for c in components:
		comps.append({
			"type": c.type,
			"cell": [c.cell.x, c.cell.y],
			"size": [c.size.x, c.size.y],
			"color": c.color.to_html(false),
		})
	return {
		"title": title,
		"start_money": start_money,
		"music": music,
		"path": waypoints.map(func(p: Vector2i): return [p.x, p.y]),
		"components": comps,
		"waves": waves,
	}


## Route in board-local pixels, through the centre of each waypoint cell.
func path_points() -> PackedVector2Array:
	var points := PackedVector2Array()
	for cell in waypoints:
		points.append(Grid.cell_center(cell))
	return points


func cpu_cell() -> Vector2i:
	return waypoints[-1]


## Where bugs enter the board: the first waypoint clamped to the board edge.
func entry_point() -> Vector2:
	var p := Grid.cell_center(waypoints[0])
	return Vector2(clampf(p.x, 0.0, Grid.SIZE.x), clampf(p.y, 0.0, Grid.SIZE.y))


## Every board cell the route passes through.
func path_cells() -> Array[Vector2i]:
	var cells: Array[Vector2i] = []
	for i in range(waypoints.size() - 1):
		var a := waypoints[i]
		var b := waypoints[i + 1]
		var steps := maxi(absi(b.x - a.x), absi(b.y - a.y))
		for s in range(steps + 1):
			var t := float(s) / maxf(steps, 1)
			var cell := Vector2i(roundi(lerpf(a.x, b.x, t)), roundi(lerpf(a.y, b.y, t)))
			if Grid.in_bounds(cell) and not cells.has(cell):
				cells.append(cell)
	return cells


## Cells where turrets can't go: route, components and the CPU footprint.
func blocked_cells() -> Dictionary:
	var blocked := {}
	for cell in path_cells():
		blocked[cell] = true
	for c in components:
		for x in c.size.x:
			for y in c.size.y:
				blocked[c.cell + Vector2i(x, y)] = true
	var cpu := cpu_cell()
	for x in range(-CPU_FOOTPRINT, CPU_FOOTPRINT + 1):
		for y in range(-CPU_FOOTPRINT, CPU_FOOTPRINT + 1):
			blocked[cpu + Vector2i(x, y)] = true
	return blocked


func enemy_count(wave_index: int) -> int:
	return waves[wave_index].size() if wave_index < waves.size() else 0
