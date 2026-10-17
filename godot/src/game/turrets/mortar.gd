extends Turret
## Turret 02: lobs a capacitor bomb at the nearest bug; it explodes in an area.


func _process(delta: float) -> void:
	cooldown_left -= delta
	var target := nearest_enemy()
	if target == null:
		idle_scan(delta)
		return
	aim_towards(target.position, delta, 6.0)
	if cooldown_left <= 0.0 and not enemies_in_range(attack_range()).is_empty():
		cooldown_left = stat("cooldown")
		var bomb := Bomb.new()
		bomb.start = muzzle(28.0)
		bomb.target = target.position
		bomb.speed = stat("projectile_speed")
		bomb.damage = stat("damage")
		bomb.blast_radius = stat("blast_radius")
		bomb.color = def.color
		game.add_projectile(bomb)
		game.fx.glow_flash(bomb.start, def.color, 0.8, 0.15)
		recoil(7.0)
		Audio.play("bomb", &"SFX", -2.0)
