shader_type canvas_item;
render_mode blend_add, unshaded;
// Data pulses running along the bug route (used on an additive Line2D).

uniform vec4 pulse_color : source_color = vec4(0.25, 0.95, 1.0, 1.0);
uniform float line_length = 1000.0;
uniform float spacing = 140.0;
uniform float speed = 90.0;
uniform float pulse_size = 28.0;
uniform float intensity = 1.0;

void fragment() {
	float d = UV.x * line_length - TIME * speed;
	float k = fract(d / spacing) * spacing;
	float head = smoothstep(pulse_size, 0.0, k) * smoothstep(0.0, 3.0, k);
	float across = 1.0 - abs(UV.y - 0.5) * 2.0;
	across = pow(clamp(across, 0.0, 1.0), 1.5);
	float base = 0.10 * across;
	COLOR = vec4(pulse_color.rgb * (head * across * 1.2 + base) * intensity, 1.0);
}
