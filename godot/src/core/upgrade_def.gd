class_name UpgradeDef
extends Resource
## One upgradable stat of a turret. Each bought level adds `per_level` to `stat`.

@export var id: String
@export var label: String
@export_multiline var description: String
@export var stat: String
@export var per_level := 0.0
@export var max_level := 15
@export var icon: Texture2D
