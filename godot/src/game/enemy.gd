class_name Enemy
extends Node2D
## A bug walking the route. Position is the distance travelled along the curve.

signal killed(enemy: Enemy)
signal escaped(enemy: Enemy)

var def: EnemyDef
var hp := 1.0
var progress := 0.0
var dead := false
var radius: float:
	get:
		return def.radius

var _curve: Curve2D
var _length := 0.0
var _slow_factor := 1.0
var _slow_time := 0.0
var _flash := 0.0
var _body: BugBody
var _bar_visible := false


func setup(p_def: EnemyDef, curve: Curve2D) -> void:
	def = p_def
	hp = def.max_hp
	_curve = curve
	_length = curve.get_baked_length()


func _ready() -> void:
	material = FxUtil.unshaded()
	_body = BugBody.new(def)
	add_child(_body)
	position = _curve.sample_baked(0.0)
	_body.rotation = (_curve.sample_baked(8.0) - position).angle()
	scale = Vector2.ONE * 0.2
	create_tween().tween_property(self, "scale", Vector2.ONE, 0.25).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)


func _process(delta: float) -> void:
	if dead:
		return
	if _slow_time > 0.0:
		_slow_time -= delta
		if _slow_time <= 0.0:
			_slow_factor = 1.0
			_body.set_frozen(0.0)
	var step := def.speed * _slow_factor * delta
	progress += step
	if progress >= _length:
		dead = true
		escaped.emit(self)
		queue_free()
		return
	position = _curve.sample_baked(progress)
	var ahead := _curve.sample_baked(minf(progress + 6.0, _length))
	if ahead != position:
		_body.rotation = lerp_angle(_body.rotation, (ahead - position).angle(), 1.0 - exp(-14.0 * delta))
	_body.animate(step * 0.45 / maxf(def.radius / 12.0, 0.6))
	if _flash > 0.0:
		_flash = maxf(_flash - delta * 8.0, 0.0)
		_body.set_flash(_flash)


func take_damage(amount: float) -> void:
	if dead:
		return
	hp -= amount
	_flash = 0.8
	if not _bar_visible:
		_bar_visible = true
	queue_redraw()
	if hp <= 0.0:
		dead = true
		killed.emit(self)
		queue_free()


func apply_slow(factor: float, duration: float) -> void:
	if dead:
		return
	_slow_factor = minf(_slow_factor, factor)
	_slow_time = maxf(_slow_time, duration)
	_body.set_frozen(1.0)


func is_slowed() -> bool:
	return _slow_time > 0.0


## Segmented health bar like the original prototype: one square per HP for weak bugs.
func _draw() -> void:
	if not _bar_visible:
		return
	var width := maxf(def.radius * 2.2, 26.0)
	var y := -def.radius - 12.0
	var segments := int(def.max_hp) if def.max_hp <= 12 else 10
	var gap := 2.0
	var seg_w := (width - gap * (segments - 1)) / segments
	var filled := hp / def.max_hp * segments
	draw_rect(Rect2(-width / 2 - 2, y - 2, width + 4, 8), Color(0, 0, 0, 0.6))
	for i in segments:
		var x := -width / 2 + i * (seg_w + gap)
		var fill := clampf(filled - i, 0.0, 1.0)
		draw_rect(Rect2(x, y, seg_w, 4), Color(1, 1, 1, 0.12))
		if fill > 0.0:
			var color := Color("ff4d5e").lerp(Color("7dff6a"), hp / def.max_hp)
			draw_rect(Rect2(x, y, seg_w * fill, 4), color)
