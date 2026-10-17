class_name Projectile
extends Node2D
## Base for things turrets shoot. `game` is set by Game.add_projectile().

var game: Game
var color := Color.WHITE


func out_of_board(margin := 64.0) -> bool:
	return not Rect2(Vector2.ONE * -margin, Grid.SIZE + Vector2.ONE * margin * 2).has_point(position)
