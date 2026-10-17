class_name TurretDef
extends Resource
## Data for one turret type. Stats are linear: base value + per_level * upgrade level.

@export var id: String
@export var display_name: String
@export_multiline var description: String
@export var cost := 10
@export var color := Color.WHITE
@export var scene: PackedScene
@export var icon: Texture2D
## The laser is aimed by the player when it is placed.
@export var needs_direction := false
@export var base_stats: Dictionary = {}
## Lower/upper clamp per stat, e.g. {"cooldown": Vector2(0.1, 99)}.
@export var stat_limits: Dictionary = {}
@export var upgrades: Array[UpgradeDef] = []


func compute_stats(levels: Dictionary) -> Dictionary:
	var stats := base_stats.duplicate()
	for up in upgrades:
		stats[up.stat] = float(stats.get(up.stat, 0.0)) + up.per_level * int(levels.get(up.id, 0))
	for stat in stat_limits:
		var limits: Vector2 = stat_limits[stat]
		stats[stat] = clampf(stats[stat], limits.x, limits.y)
	return stats


func find_upgrade(upgrade_id: String) -> UpgradeDef:
	for up in upgrades:
		if up.id == upgrade_id:
			return up
	return null
