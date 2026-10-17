shader_type canvas_item;
// Original vignette/scanlines/damage, plus sparse board-only intrusion bands.
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform float vignette = 0.45;
uniform float scanlines = 0.06;
uniform float aberration = 1.2;
uniform float damage : hint_range(0.0, 1.0) = 0.0;
uniform float intrusion : hint_range(0.0, 0.3) = 0.0;
uniform vec4 board_rect;
uniform vec2 logical_size = vec2(1366.0, 768.0);
uniform vec2 warning_gap;

float noise(float value) {
	return fract(sin(value * 12.9898) * 43758.5453);
}

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 centered = uv - 0.5;
	float dist = length(centered);
	vec2 shift = centered * (aberration + damage * 6.0) * SCREEN_PIXEL_SIZE.x * 2.0;
	float inside = step(board_rect.x, uv.x) * step(uv.x, board_rect.z)
		* step(board_rect.y, uv.y) * step(uv.y, board_rect.w);
	inside *= min(1.0, step(uv.y, warning_gap.x) + step(warning_gap.y, uv.y));
	float broken = 0.0;
	vec2 tapped = uv;
	// The extra work is inactive during ordinary play and on every other screen.
	if (intrusion > 0.0 && inside > 0.0) {
		float band = floor(uv.y * 256.0);
		float tick = floor(TIME * 13.0);
		broken = step(0.88, noise(band + tick * 71.0));
		// One to three art pixels, in integer 3-logical-pixel steps of the world.
		float pixels = floor(intrusion * 12.0) * sign(noise(tick + band * 3.0) - 0.5);
		tapped.x = clamp(uv.x + pixels * 3.0 / logical_size.x * broken, board_rect.x, board_rect.z);
	}
	vec3 col;
	col.r = texture(screen_tex, tapped + shift).r;
	col.g = texture(screen_tex, tapped).g;
	col.b = texture(screen_tex, tapped - shift).b;
	col = mix(col, col.bgr + vec3(0.03, 0.01, 0.04), intrusion * broken);
	float line = sin(FRAGCOORD.y * 3.14159) * 0.5 + 0.5;
	col *= 1.0 - scanlines * line;
	col *= 1.0 - vignette * smoothstep(0.35, 0.85, dist);
	col = mix(col, col * vec3(1.4, 0.45, 0.45), damage * smoothstep(0.2, 0.75, dist));
	COLOR = vec4(col, 1.0);
}
