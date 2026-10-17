extends Turret
## Turret 05: keeps up to four frost orbs around it; each homes in on a bug and slows it.

const SLOTS: Array[Vector2] = [Vector2(24, 0), Vector2(0, -24), Vector2(-24, 0), Vector2(0, 24)]

var _orbs: Array = [null, null, null, null]
var _next_slot := 0

@onready var _fan: Node2D = $Head/Fan


func _process(delta: float) -> void:
	_fan.rotation += delta * 7.0
	cooldown_left -= delta
	if cooldown_left > 0.0:
		return
	for i in SLOTS.size():
		_next_slot = (_next_slot + 1) % SLOTS.size()
		var orb: Variant = _orbs[_next_slot]
		if not is_instance_valid(orb) or orb.launched:
			_spawn_orb(_next_slot)
			cooldown_left = stat("cooldown")
			return
	# Every slot is busy: check again next frame, like the original.
	cooldown_left = 0.0


func _spawn_orb(slot: int) -> void:
	var orb := FreezeOrb.new()
	orb.position = position + SLOTS[slot]
	orb.slow_factor = stat("slow_factor")
	orb.slow_duration = stat("slow_duration")
	orb.speed = stat("projectile_speed")
	orb.search_range = attack_range()
	orb.color = def.color
	orb.game = game
	_orbs[slot] = orb
	game.add_projectile(orb)
	game.fx.glow_flash(orb.position, def.color, 0.6, 0.2)
