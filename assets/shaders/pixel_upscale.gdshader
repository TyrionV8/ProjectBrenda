shader_type canvas_item;
// Upscales the low-resolution world with sharp pixels. Plain nearest sampling makes some
// art pixels a screen pixel wider than others when the window scale isn't a whole number;
// this keeps each pixel's interior solid and only blends the 1 screen pixel on its edges.
// Needs linear filtering on the texture (the container sets it).

void fragment() {
	vec2 size = vec2(textureSize(TEXTURE, 0));
	vec2 texel = UV * size;
	vec2 seam = floor(texel + 0.5);
	vec2 width = max(fwidth(texel), vec2(1e-5));
	texel = seam + clamp((texel - seam) / width, -0.5, 0.5);
	COLOR = texture(TEXTURE, texel / size);
}
