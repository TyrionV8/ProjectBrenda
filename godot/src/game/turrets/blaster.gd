extends Turret
## Turret 01: single shots at the bug closest to the CPU.


func _process(delta: float) -> void:
	cooldown_left -= delta
	var target := furthest_in_range(attack_range())
	if target == null:
		idle_scan(delta)
		return
	aim_towards(target.position, delta)
	if cooldown_left <= 0.0:
		cooldown_left = stat("cooldown")
		head.rotation = (target.position - position).angle()
		var bullet := Bullet.new()
		bullet.position = muzzle(30.0)
		bullet.velocity = Vector2.from_angle(head.rotation) * stat("projectile_speed")
		bullet.damage = stat("damage")
		bullet.color = def.color
		bullet.max_distance = attack_range() * 2.5
		game.add_projectile(bullet)
		game.fx.glow_flash(bullet.position, def.color, 0.5, 0.1)
		recoil()
		Audio.play("shoot", &"SFX", -4.0)
