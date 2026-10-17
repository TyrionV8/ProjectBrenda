class_name FxUtil
extends RefCounted
## Shared textures/materials for glows and lights.

const LIGHT_TEXTURE: Texture2D = preload("res://assets/fx/light_soft.png")
const GLOW_TEXTURE: Texture2D = preload("res://assets/fx/glow.png")
const SPARK_TEXTURE: Texture2D = preload("res://assets/fx/spark.png")
const PIXEL_TEXTURE: Texture2D = preload("res://assets/fx/pixel.png")

static var _additive: CanvasItemMaterial
static var _unshaded: CanvasItemMaterial


## Additive, unaffected by scene lighting: makes things look emissive.
static func additive() -> CanvasItemMaterial:
	if _additive == null:
		_additive = CanvasItemMaterial.new()
		_additive.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
		_additive.light_mode = CanvasItemMaterial.LIGHT_MODE_UNSHADED
	return _additive


## Normal blending but ignores lights/CanvasModulate (for UI-like world text).
static func unshaded() -> CanvasItemMaterial:
	if _unshaded == null:
		_unshaded = CanvasItemMaterial.new()
		_unshaded.light_mode = CanvasItemMaterial.LIGHT_MODE_UNSHADED
	return _unshaded


static func glow_sprite(color: Color, scale := 1.0) -> Sprite2D:
	var sprite := Sprite2D.new()
	sprite.texture = GLOW_TEXTURE
	sprite.material = additive()
	sprite.modulate = color
	sprite.scale = Vector2.ONE * scale
	return sprite


static func light(color: Color, energy := 1.0, size := 1.0) -> PointLight2D:
	var l := PointLight2D.new()
	l.texture = LIGHT_TEXTURE
	l.color = color
	l.energy = energy
	l.texture_scale = size
	return l
