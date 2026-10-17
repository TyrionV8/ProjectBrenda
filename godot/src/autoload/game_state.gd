extends Node
## Campaign progress: unlocked levels, bought upgrades and upgrade points.
## Saved as JSON in user://save.json.

signal changed

const SAVE_PATH := "user://save.json"
const START_POINTS := 5
## Points earned the first time each level is cleared.
const POINTS_PER_LEVEL := 3

var current_level := 0
var unlocked: Array[bool] = []
## turret id -> {upgrade id -> level}
var upgrades: Dictionary = {}
var points := START_POINTS


func _ready() -> void:
	reset()


func reset() -> void:
	current_level = 0
	unlocked.clear()
	unlocked.resize(DB.levels.size())
	unlocked.fill(false)
	if not unlocked.is_empty():
		unlocked[0] = true
	upgrades = {}
	points = START_POINTS
	changed.emit()


func has_save() -> bool:
	return FileAccess.file_exists(SAVE_PATH)


func save() -> void:
	var file := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file == null:
		push_error("Can't write save: %s" % error_string(FileAccess.get_open_error()))
		return
	file.store_string(JSON.stringify({
		"version": 1,
		"unlocked": unlocked,
		"upgrades": upgrades,
		"points": points,
	}, "\t"))


func load_save() -> bool:
	if not has_save():
		return false
	var data: Variant = JSON.parse_string(FileAccess.get_file_as_string(SAVE_PATH))
	if data is not Dictionary:
		return false
	reset()
	var saved_unlocked: Array = data.get("unlocked", [])
	for i in mini(saved_unlocked.size(), unlocked.size()):
		unlocked[i] = bool(saved_unlocked[i])
	var saved_upgrades: Dictionary = data.get("upgrades", {})
	for turret_id in saved_upgrades:
		var levels := {}
		for upgrade_id in saved_upgrades[turret_id]:
			levels[upgrade_id] = int(saved_upgrades[turret_id][upgrade_id])
		upgrades[turret_id] = levels
	points = int(data.get("points", START_POINTS))
	current_level = last_unlocked()
	changed.emit()
	return true


func last_unlocked() -> int:
	return maxi(unlocked.rfind(true), 0)


func is_unlocked(index: int) -> bool:
	return index >= 0 and index < unlocked.size() and unlocked[index]


func upgrade_level(turret_id: String, upgrade_id: String) -> int:
	return int(upgrades.get(turret_id, {}).get(upgrade_id, 0))


func can_buy_upgrade(turret: TurretDef, up: UpgradeDef) -> bool:
	return points > 0 and upgrade_level(turret.id, up.id) < up.max_level


func buy_upgrade(turret: TurretDef, up: UpgradeDef) -> bool:
	if not can_buy_upgrade(turret, up):
		return false
	var levels: Dictionary = upgrades.get_or_add(turret.id, {})
	levels[up.id] = int(levels.get(up.id, 0)) + 1
	points -= 1
	changed.emit()
	return true


func turret_stats(turret: TurretDef) -> Dictionary:
	return turret.compute_stats(upgrades.get(turret.id, {}))


## Marks the current level as cleared. Returns true if it was the last one.
func complete_current_level() -> bool:
	var next := current_level + 1
	if next >= unlocked.size():
		save()
		return true
	if not unlocked[next]:
		unlocked[next] = true
		points += POINTS_PER_LEVEL
	current_level = next
	save()
	changed.emit()
	return false
