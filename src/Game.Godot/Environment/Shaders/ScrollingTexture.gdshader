shader_type canvas_item;
render_mode unshaded;
#include "res://Environment/Shaders/WorldNoise.gdshaderinc"
uniform sampler2D repeat_texture : source_color, repeat_enable, filter_linear;
uniform bool textured = true;
uniform bool band_fade = false;
void fragment() {
    vec2 uv = (world_point - motion_offset) / texture_scale;
    vec4 sample_color = textured ? texture(repeat_texture, uv) : vec4(tint.rgb, noise2(uv * 3.0) * 0.12);
    float y = clamp((world_point.y - region.y) / max(region.w, 1.0), 0.0, 1.0);
    float fade = band_fade ? sin(y * 3.141593) : 1.0;
    COLOR = vec4(sample_color.rgb * tint.rgb, sample_color.a * opacity * fade);
}
