class_name Game
extends Node2D
## One level being played: owns money, the CPU, bugs, turrets and the win/lose flow.

signal money_changed(value: int)
signal enemy_count_changed

enum State { PLAYING, WON, LOST }

const SPEEDS: Array[float] = [1.0, 2.0, 4.0, 8.0]

var level: LevelData
var level_index := 0
var money := 0:
	set(value):
		money = value
		money_changed.emit(money)
var enemies: Array[Enemy] = []
var turrets: Dictionary = {} ## Vector2i -> Turret
var blocked: Dictionary = {}
var state := State.PLAYING
var speed_index := 0
var curve := Curve2D.new()

var _shake := 0.0

@onready var camera: Camera2D = $Camera
@onready var board: Board = $Board
@onready var trace: PathTrace = $Board/Trace
@onready var cpu: CpuCore = $Board/Cpu
@onready var turret_layer: Node2D = $Board/Turrets
@onready var enemy_layer: Node2D = $Board/Enemies
@onready var projectile_layer: Node2D = $Board/Projectiles
@onready var fx: Fx = $Board/Fx
@onready var build: BuildController = $Board/Build
@onready var waves: WaveManager = $WaveManager
@onready var hud: Hud = $Hud
@onready var pause_menu: PauseMenu = $PauseMenu


func _ready() -> void:
	level_index = clampi(GameState.current_level, 0, DB.levels.size() - 1)
	level = DB.levels[level_index]
	money = level.start_money
	board.setup(level, Grid.COLS, Grid.ROWS, level_index + 1)
	for p in level.path_points():
		curve.add_point(p)
	trace.setup(level.path_points(), level.entry_point())
	cpu.position = Grid.cell_center(level.cpu_cell())
	cpu.destroyed.connect(_on_cpu_destroyed)
	blocked = level.blocked_cells()
	fx.shake_requested.connect(shake)
	build.game = self
	waves.alive_count = func() -> int: return enemies.size()
	waves.spawn_requested.connect(spawn_enemy)
	waves.level_cleared.connect(_on_level_cleared)
	waves.wave_started.connect(func(_i: int) -> void: trace.set_intensity(1.0))
	waves.countdown_started.connect(func(_i: int, _s: float) -> void: trace.set_intensity(0.45))
	hud.setup(self)
	pause_menu.game = self
	Audio.play_music(level.music)
	waves.start(level)


func _process(delta: float) -> void:
	# Dead bugs were queue_free()d during the previous frame; drop them first,
	# before turrets and projectiles (children of the board) run this frame.
	var alive: Array[Enemy] = []
	for e in enemies:
		if is_instance_valid(e) and not e.dead:
			alive.append(e)
	if alive.size() != enemies.size():
		enemies = alive
		enemy_count_changed.emit()
	if _shake > 0.0:
		_shake = maxf(_shake - delta * 30.0, 0.0)
		camera.offset = Vector2(randf_range(-1, 1), randf_range(-1, 1)) * _shake
	else:
		camera.offset = Vector2.ZERO


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("pause") and state == State.PLAYING and not build.is_busy():
		pause_menu.open()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("toggle_grid"):
		board.show_grid = not board.show_grid
	elif event.is_action_pressed("toggle_speed"):
		cycle_speed()
	elif event is InputEventKey and event.pressed and not event.echo:
		var k: int = event.keycode - KEY_1
		if k >= 0 and k < DB.turrets.size():
			build.select(DB.turrets[k])


func spawn_enemy(enemy_id: String) -> void:
	var def := DB.enemy(enemy_id)
	if def == null:
		push_error("Unknown enemy '%s'" % enemy_id)
		return
	var enemy := Enemy.new()
	enemy.setup(def, curve)
	enemy.killed.connect(_on_enemy_killed)
	enemy.escaped.connect(_on_enemy_escaped)
	enemy_layer.add_child(enemy)
	enemies.append(enemy)
	enemy_count_changed.emit()


func add_projectile(p: Projectile) -> void:
	p.game = self
	projectile_layer.add_child(p)


func can_build(cell: Vector2i) -> bool:
	return Grid.in_bounds(cell) and not blocked.has(cell) and not turrets.has(cell)


func build_turret(def: TurretDef, cell: Vector2i, direction := 0.0) -> bool:
	if state != State.PLAYING or not can_build(cell) or money < def.cost:
		return false
	var turret: Turret = def.scene.instantiate()
	turret.setup(def, cell, self, GameState.turret_stats(def), direction)
	turret_layer.add_child(turret)
	turrets[cell] = turret
	money -= def.cost
	Audio.play("place")
	return true


func turret_at(cell: Vector2i) -> Turret:
	return turrets.get(cell)


func cycle_speed() -> void:
	speed_index = (speed_index + 1) % SPEEDS.size()
	Engine.time_scale = SPEEDS[speed_index]
	hud.update_speed(SPEEDS[speed_index])


func reset_speed() -> void:
	speed_index = 0
	Engine.time_scale = 1.0
	hud.update_speed(1.0)


func shake(amount: float) -> void:
	_shake = maxf(_shake, amount)


func restart() -> void:
	Transition.change_scene("res://scenes/game/game.tscn")


func _on_enemy_killed(enemy: Enemy) -> void:
	money += enemy.def.reward
	fx.bug_death(enemy.position, enemy.def)
	Audio.play("death", &"SFX", -4.0, 0.15)


func _on_enemy_escaped(_enemy: Enemy) -> void:
	cpu.hit(fx)
	PostFx.pulse_damage()


func _on_cpu_destroyed() -> void:
	if state != State.PLAYING:
		return
	state = State.LOST
	build.cancel()
	waves.set_process(false)
	reset_speed()
	Audio.play("lose", &"SFX")
	await get_tree().create_timer(1.2).timeout
	hud.show_result(false, false)


func _on_level_cleared() -> void:
	if state != State.PLAYING:
		return
	state = State.WON
	build.cancel()
	reset_speed()
	Audio.play("win", &"SFX")
	var points_before := GameState.points
	var finished := GameState.complete_current_level()
	await get_tree().create_timer(0.8).timeout
	hud.show_result(true, finished, GameState.points - points_before)
