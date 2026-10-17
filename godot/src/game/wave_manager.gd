class_name WaveManager
extends Node
## Runs a level's waves: a short countdown, then spawns each bug at its time.
## A wave ends when everything spawned is dead or escaped.

signal countdown_started(wave_index: int, seconds: float)
signal wave_started(wave_index: int)
signal spawn_requested(enemy_id: String)
signal level_cleared

enum State { IDLE, COUNTDOWN, SPAWNING, WAITING, DONE }

## Pause before each wave (the original used a 3 second timeline).
const COUNTDOWN := 3.0

var level: LevelData
var state := State.IDLE
var wave_index := 0
var countdown_left := 0.0
var _time := 0.0
var _next := 0
## Callable returning how many bugs are still alive.
var alive_count: Callable = func() -> int: return 0


func start(p_level: LevelData) -> void:
	level = p_level
	wave_index = 0
	if level.waves.is_empty():
		state = State.DONE
		level_cleared.emit()
		return
	_begin_countdown()


func total_waves() -> int:
	return level.waves.size() if level else 0


## Bugs of the current wave that haven't spawned yet, in order.
func upcoming() -> Array:
	if level == null or wave_index >= level.waves.size():
		return []
	var wave: Array = level.waves[wave_index]
	if state == State.COUNTDOWN:
		return wave
	return wave.slice(_next)


func _begin_countdown() -> void:
	state = State.COUNTDOWN
	countdown_left = COUNTDOWN
	countdown_started.emit(wave_index, COUNTDOWN)


func _process(delta: float) -> void:
	match state:
		State.COUNTDOWN:
			countdown_left -= delta
			if countdown_left <= 0.0:
				state = State.SPAWNING
				_time = 0.0
				_next = 0
				wave_started.emit(wave_index)
		State.SPAWNING:
			_time += delta
			var wave: Array = level.waves[wave_index]
			while _next < wave.size() and wave[_next].time <= _time:
				spawn_requested.emit(wave[_next].enemy)
				_next += 1
			if _next >= wave.size():
				state = State.WAITING
		State.WAITING:
			if alive_count.call() == 0:
				if wave_index >= level.waves.size() - 1:
					state = State.DONE
					level_cleared.emit()
				else:
					wave_index += 1
					_begin_countdown()
