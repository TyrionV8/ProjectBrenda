class_name EnemyDef
extends Resource
## Data for one bug type. Speeds are in pixels per second.

enum Style { BEETLE, SPIDER, SLIM, ARMORED, BOSS }

@export var id: String
@export var display_name: String
@export var max_hp := 2.0
@export var speed := 30.0
@export var reward := 1
## Body radius in pixels; also used for hit detection.
@export var radius := 11.0
@export var style := Style.BEETLE
@export var shell_color := Color("5bd94a")
@export var accent_color := Color("1f5f1a")
@export var eye_color := Color("ff3d3d")
@export var leg_color := Color("10220c")
