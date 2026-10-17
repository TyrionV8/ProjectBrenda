class_name SfxSynth
extends RefCounted
## Small procedural sound effects (the original project only had a few samples).

const RATE := 22050


static func build_all() -> Dictionary:
	return {
		"death": _death(),
		"explode": _explode(),
		"cpu_hit": _cpu_hit(),
		"place": _arpeggio([660.0, 990.0], 0.055, "square", 0.35),
		"error": _arpeggio([220.0, 0.0, 220.0], 0.07, "square", 0.3),
		"wave": _arpeggio([440.0, 554.4, 659.3, 880.0], 0.075, "triangle", 0.5),
		"upgrade": _arpeggio([523.3, 659.3, 784.0, 1046.5], 0.06, "square", 0.3),
		"win": _arpeggio([523.3, 659.3, 784.0, 1046.5, 784.0, 1046.5, 1318.5], 0.09, "triangle", 0.5),
		"lose": _arpeggio([392.0, 311.1, 261.6, 196.0, 130.8], 0.14, "square", 0.35),
		"pew": _sweep(1500.0, 500.0, 0.08, "square", 0.22),
		"freeze": _freeze(),
	}


static func _death() -> AudioStreamWAV:
	var n := int(RATE * 0.16)
	var data := PackedFloat32Array()
	data.resize(n)
	var phase := 0.0
	for i in n:
		var t := float(i) / n
		phase += lerpf(900.0, 160.0, t) / RATE
		var square := 1.0 if fmod(phase, 1.0) < 0.5 else -1.0
		data[i] = (square * 0.55 + randf_range(-1, 1) * 0.45 * (1.0 - t)) * pow(1.0 - t, 1.5) * 0.45
	return _to_wav(data)


static func _explode() -> AudioStreamWAV:
	var n := int(RATE * 0.55)
	var data := PackedFloat32Array()
	data.resize(n)
	var low := 0.0
	var phase := 0.0
	for i in n:
		var t := float(i) / n
		low = lerpf(low, randf_range(-1, 1), lerpf(0.5, 0.05, t))
		phase += lerpf(90.0, 35.0, t) / RATE
		data[i] = (low * 0.8 + sin(phase * TAU) * 0.6 * (1.0 - t)) * pow(1.0 - t, 2.0) * 0.8
	return _to_wav(data)


static func _cpu_hit() -> AudioStreamWAV:
	var n := int(RATE * 0.35)
	var data := PackedFloat32Array()
	data.resize(n)
	var phase := 0.0
	for i in n:
		var t := float(i) / n
		phase += (110.0 + sin(t * 60.0) * 12.0) / RATE
		var saw := fmod(phase, 1.0) * 2.0 - 1.0
		data[i] = (saw * 0.6 + randf_range(-1, 1) * 0.4 * pow(1.0 - t, 4.0)) * (1.0 - t) * 0.6
	return _to_wav(data)


static func _freeze() -> AudioStreamWAV:
	var n := int(RATE * 0.3)
	var data := PackedFloat32Array()
	data.resize(n)
	var freqs := [1760.0, 2217.0, 2637.0, 3136.0]
	for i in n:
		var t := float(i) / n
		var s := 0.0
		for k in freqs.size():
			s += sin(TAU * freqs[k] * i / RATE + k) * (0.5 + 0.5 * sin(t * 40.0 + k * 1.7))
		data[i] = s / freqs.size() * pow(1.0 - t, 2.0) * 0.4
	return _to_wav(data)


static func _sweep(from_hz: float, to_hz: float, length: float, wave: String, volume: float) -> AudioStreamWAV:
	var n := int(RATE * length)
	var data := PackedFloat32Array()
	data.resize(n)
	var phase := 0.0
	for i in n:
		var t := float(i) / n
		phase += lerpf(from_hz, to_hz, t) / RATE
		data[i] = _osc(wave, phase) * (1.0 - t) * volume
	return _to_wav(data)


static func _arpeggio(notes: Array, note_length: float, wave: String, volume: float) -> AudioStreamWAV:
	var per_note := int(RATE * note_length)
	var data := PackedFloat32Array()
	data.resize(per_note * notes.size())
	for k in notes.size():
		var phase := 0.0
		for i in per_note:
			var t := float(i) / per_note
			phase += notes[k] / RATE
			var env := minf(t * 20.0, 1.0) * (1.0 - t * 0.7)
			data[k * per_note + i] = 0.0 if notes[k] <= 0.0 else _osc(wave, phase) * env * volume
	return _to_wav(data)


static func _osc(wave: String, phase: float) -> float:
	var p := fmod(phase, 1.0)
	match wave:
		"square":
			return 1.0 if p < 0.5 else -1.0
		"triangle":
			return 4.0 * absf(p - 0.5) - 1.0
		_:
			return sin(p * TAU)


static func _to_wav(data: PackedFloat32Array) -> AudioStreamWAV:
	var bytes := PackedByteArray()
	bytes.resize(data.size() * 2)
	for i in data.size():
		bytes.encode_s16(i * 2, int(clampf(data[i], -1.0, 1.0) * 32767.0))
	var wav := AudioStreamWAV.new()
	wav.format = AudioStreamWAV.FORMAT_16_BITS
	wav.mix_rate = RATE
	wav.stereo = false
	wav.data = bytes
	return wav
