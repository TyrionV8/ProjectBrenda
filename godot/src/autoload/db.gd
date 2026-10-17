extends Node
## Read-only game data: turret and enemy definitions and the levels.

const TURRET_PATHS: Array[String] = [
	"res://data/turrets/blaster.tres",
	"res://data/turrets/mortar.tres",
	"res://data/turrets/burst.tres",
	"res://data/turrets/laser.tres",
	"res://data/turrets/freezer.tres",
]
const ENEMY_PATHS: Array[String] = [
	"res://data/enemies/bug.tres",
	"res://data/enemies/spider.tres",
	"res://data/enemies/beetle.tres",
	"res://data/enemies/tank.tres",
	"res://data/enemies/runner.tres",
	"res://data/enemies/brute.tres",
	"res://data/enemies/boss.tres",
]
const LEVEL_PATH := "res://data/levels/level_%d.json"

var turrets: Array[TurretDef] = []
var enemies: Dictionary = {} ## id -> EnemyDef
var levels: Array[LevelData] = []


func _ready() -> void:
	for path in TURRET_PATHS:
		turrets.append(load(path))
	for path in ENEMY_PATHS:
		var def: EnemyDef = load(path)
		enemies[def.id] = def
	var i := 1
	while FileAccess.file_exists(LEVEL_PATH % i):
		levels.append(LevelData.load_file(LEVEL_PATH % i))
		i += 1


func turret(id: String) -> TurretDef:
	for def in turrets:
		if def.id == id:
			return def
	return null


func enemy(id: String) -> EnemyDef:
	return enemies.get(id)
