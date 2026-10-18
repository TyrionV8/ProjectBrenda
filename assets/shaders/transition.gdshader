shader_type canvas_item;
// Fade to black with horizontal "signal lost" bands.

uniform float progress : hint_range(0.0, 1.0) = 0.0;

float hash(float n) { return fract(sin(n) * 43758.5453); }

void fragment() {
	float band = floor(UV.y * 48.0);
	float offset = hash(band + floor(TIME * 24.0)) * 0.35;
	float a = smoothstep(offset, offset + 0.65, progress * 1.35);
	vec3 tint = mix(vec3(0.0, 0.9, 1.0), vec3(0.02, 0.04, 0.07), smoothstep(0.0, 0.6, a));
	COLOR = vec4(tint * 0.15 + vec3(0.02, 0.04, 0.07), a);
}
