class_name MenuBackground
extends CanvasLayer
## Living circuit board behind the menus: flowing route, blinking LEDs and bugs crawling by.

const BUGS: Array[String] = ["bug", "bug", "spider", "beetle", "runner", "tank", "brute"]

var _level: LevelData
var _curve := Curve2D.new()
var _board: Board
var _bugs: Node2D
var _spawn_timer := 0.5


func _ready() -> void:
	layer = -1
	_level = LevelData.from_dict({
		"path": [[-1, 3], [7, 3], [7, 9], [15, 9], [15, 4], [22, 4], [22, 12], [12, 12], [12, 14], [29, 14]],
		"components": [
			{"type": "chip", "cell": [2, 6], "size": [2, 2]},
			{"type": "capacitor", "cell": [10, 5], "size": [2, 2]},
			{"type": "connector", "cell": [25, 1], "size": [2, 2]},
			{"type": "resistor", "cell": [17, 7], "size": [2, 1]},
			{"type": "chip", "cell": [24, 8], "size": [2, 2]},
			{"type": "led", "cell": [3, 11], "color": "3dff6a"},
			{"type": "led", "cell": [4, 11], "color": "ff3344"},
			{"type": "led", "cell": [5, 11], "color": "3dff6a"},
			{"type": "led", "cell": [18, 1], "color": "ffb43d"},
			{"type": "resistor", "cell": [4, 14], "size": [2, 1]},
		],
	})
	var modulate_node := CanvasModulate.new()
	modulate_node.color = Color(0.55, 0.6, 0.72)
	add_child(modulate_node)
	_board = Board.new()
	add_child(_board)
	_board.setup(_level, 29, 16, 7)
	var trace := PathTrace.new()
	_board.add_child(trace)
	trace.setup(_level.path_points(), _level.entry_point())
	trace.set_intensity(0.8)
	for p in _level.path_points():
		_curve.add_point(p)
	_bugs = Node2D.new()
	_board.add_child(_bugs)
	for i in 4:
		_spawn(randf_range(0.0, _curve.get_baked_length() * 0.9))


func _process(delta: float) -> void:
	_spawn_timer -= delta
	if _spawn_timer <= 0.0:
		_spawn_timer = randf_range(1.2, 3.0)
		_spawn(0.0)


func _spawn(progress: float) -> void:
	var def := DB.enemy(BUGS.pick_random())
	if def == null:
		return
	var enemy := Enemy.new()
	enemy.setup(def, _curve)
	enemy.progress = progress
	_bugs.add_child(enemy)
