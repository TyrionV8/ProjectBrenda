extends Turret
## Turret 03: fires a ring of pellets in every direction, alternating the angle.

var _alternate := false
var _spin := 0.6


func _process(delta: float) -> void:
	cooldown_left -= delta
	_spin = lerpf(_spin, 0.6, delta * 2.0)
	head.rotation += _spin * delta
	if cooldown_left > 0.0 or enemies_in_range(attack_range()).is_empty():
		return
	cooldown_left = stat("cooldown")
	var shots := maxi(int(stat("shots")), 1)
	var offset := (PI / shots) if _alternate else 0.0
	_alternate = not _alternate
	for i in shots:
		var angle := TAU * i / shots + offset
		var bullet := Bullet.new()
		bullet.position = position + Vector2.from_angle(angle) * 20.0
		bullet.velocity = Vector2.from_angle(angle) * stat("projectile_speed")
		bullet.damage = stat("damage")
		bullet.color = def.color
		bullet.size = 3.0
		bullet.max_distance = attack_range()
		game.add_projectile(bullet)
	_spin = 9.0
	game.fx.glow_flash(position, def.color, 1.0, 0.15)
	light.energy = 2.0
	create_tween().tween_property(light, "energy", 1.0, 0.25)
	Audio.play("pew", &"SFX", -6.0)
