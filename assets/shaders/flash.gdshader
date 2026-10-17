shader_type canvas_item;
// Hit flash, freeze tint and the rootkit's "hidden process" dissolve for bug sprites.

uniform float flash : hint_range(0.0, 1.0) = 0.0;
uniform float frozen : hint_range(0.0, 1.0) = 0.0;
uniform float dissolve : hint_range(0.0, 1.0) = 0.0;

float hash(vec2 p) { return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453); }

void fragment() {
	vec4 c = COLOR;
	vec3 ice = vec3(0.55, 0.85, 1.0) * (0.6 + 0.4 * dot(c.rgb, vec3(0.33)));
	c.rgb = mix(c.rgb, ice, frozen * 0.65);
	c.rgb = mix(c.rgb, vec3(1.0), flash);
	if (dissolve > 0.0) {
		// Per screen pixel of the low-res world, reshuffled several times a second.
		float n = hash(floor(FRAGCOORD.xy) + floor(TIME * 12.0) * 7.31);
		if (n < dissolve) {
			discard;
		}
		c.rgb = mix(c.rgb, vec3(0.25, 1.0, 0.45), step(n, dissolve + 0.1) * dissolve);
	}
	COLOR = c;
}
