shader_type canvas_item;
// Screen post-processing: soft vignette, faint scanlines and a hint of chromatic aberration.

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform float vignette = 0.45;
uniform float scanlines = 0.06;
uniform float aberration = 1.2;
uniform float damage : hint_range(0.0, 1.0) = 0.0;

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 centered = uv - 0.5;
	float dist = length(centered);
	vec2 shift = centered * (aberration + damage * 6.0) * SCREEN_PIXEL_SIZE.x * 2.0;
	vec3 col;
	col.r = texture(screen_tex, uv + shift).r;
	col.g = texture(screen_tex, uv).g;
	col.b = texture(screen_tex, uv - shift).b;
	float line = sin(FRAGCOORD.y * 3.14159) * 0.5 + 0.5;
	col *= 1.0 - scanlines * line;
	col *= 1.0 - vignette * smoothstep(0.35, 0.85, dist);
	col = mix(col, col * vec3(1.4, 0.45, 0.45), damage * smoothstep(0.2, 0.75, dist));
	COLOR = vec4(col, 1.0);
}
