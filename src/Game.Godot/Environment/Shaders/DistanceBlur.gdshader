shader_type canvas_item;
render_mode unshaded;
uniform float blur_strength : hint_range(0.0, 4.0) = 1.0;
uniform float distance_fade : hint_range(0.0, 1.0) = 0.2;
uniform vec4 haze_tint : source_color = vec4(0.60, 0.70, 0.78, 0.1);
void fragment() {
    vec2 d = TEXTURE_PIXEL_SIZE * blur_strength;
    vec4 c = texture(TEXTURE, UV) * 0.4;
    c += (texture(TEXTURE, UV + vec2(d.x, 0)) + texture(TEXTURE, UV - vec2(d.x, 0)) + texture(TEXTURE, UV + vec2(0, d.y)) + texture(TEXTURE, UV - vec2(0, d.y))) * 0.15;
    float mask = (1.0 - smoothstep(0.0, 0.75, UV.y)) * distance_fade;
    c.rgb = mix(c.rgb, vec3(dot(c.rgb, vec3(0.299, 0.587, 0.114))), mask);
    c.rgb = mix(c.rgb, haze_tint.rgb, mask * haze_tint.a);
    COLOR = c * COLOR;
}
