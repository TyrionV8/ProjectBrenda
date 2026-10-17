extends Node
## Music with cross-fades and a small pool of one-shot sound players.

const MUSIC := {
	"title": "res://assets/audio/music_title.ogg",
	"music_level_1": "res://assets/audio/music_level_1.ogg",
	"music_level_2": "res://assets/audio/music_level_2.ogg",
	"music_level_3": "res://assets/audio/music_level_3.ogg",
	"ending": "res://assets/audio/music_ending.ogg",
}
const SAMPLES := {
	"shoot": "res://assets/audio/sfx_shoot.ogg",
	"bomb": "res://assets/audio/sfx_bomb.ogg",
	"hover": "res://assets/audio/ui_hover.ogg",
	"click": "res://assets/audio/ui_click.ogg",
}
const POOL_SIZE := 12
## The same sound won't restart faster than this (keeps ten turrets from stacking up).
const MIN_REPEAT := 0.05

var _streams := {}
var _players: Array[AudioStreamPlayer] = []
var _next_player := 0
var _last_played := {}
var _music_players: Array[AudioStreamPlayer] = []
var _music_index := 0
var _music_id := ""


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	for id in SAMPLES:
		_streams[id] = load(SAMPLES[id])
	var synth := SfxSynth.build_all()
	for id in synth:
		_streams[id] = synth[id]
	for i in POOL_SIZE:
		var player := AudioStreamPlayer.new()
		add_child(player)
		_players.append(player)
	for i in 2:
		var player := AudioStreamPlayer.new()
		player.bus = &"Music"
		add_child(player)
		_music_players.append(player)


func play(id: String, bus := &"SFX", volume_db := 0.0, pitch_jitter := 0.06) -> void:
	var stream: AudioStream = _streams.get(id)
	if stream == null:
		push_warning("Unknown sound: %s" % id)
		return
	var now := Time.get_ticks_msec() / 1000.0
	if now - float(_last_played.get(id, -1.0)) < MIN_REPEAT:
		return
	_last_played[id] = now
	var player := _players[_next_player]
	_next_player = (_next_player + 1) % POOL_SIZE
	player.stream = stream
	player.bus = bus
	player.volume_db = volume_db
	player.pitch_scale = 1.0 + randf_range(-pitch_jitter, pitch_jitter)
	player.play()


func ui(id: String) -> void:
	play(id, &"UI", 0.0, 0.0)


func play_music(id: String, fade := 1.2) -> void:
	if id == _music_id or not MUSIC.has(id):
		return
	_music_id = id
	var old := _music_players[_music_index]
	_music_index = 1 - _music_index
	var new := _music_players[_music_index]
	var stream: AudioStream = load(MUSIC[id])
	if stream is AudioStreamOggVorbis:
		stream.loop = true
	new.stream = stream
	new.volume_db = -40.0
	new.play()
	var tween := create_tween().set_parallel()
	tween.tween_property(new, "volume_db", 0.0, fade)
	if old.playing:
		tween.tween_property(old, "volume_db", -40.0, fade)
		tween.chain().tween_callback(old.stop)
