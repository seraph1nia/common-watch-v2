shader_type canvas_item;
render_mode unshaded;
#include "res://Environment/Shaders/WorldNoise.gdshaderinc"
uniform float height_fade = 0.0;
uniform bool cloud_shadow = false;
void fragment() {
    vec2 p = (world_point - velocity * visual_time) / texture_scale;
    float n = noise2(p) * 0.65 + noise2(p * 2.13 + vec2(18.7, 9.2)) * 0.35;
    float y = clamp((world_point.y - region.y) / max(region.w, 1.0), 0.0, 1.0);
    float height_mask = mix(0.35 + 0.65 * pow(sin(y * 3.141593), 2.0), 1.0 - smoothstep(0.0, 1.0, y), height_fade);
    float density = cloud_shadow ? smoothstep(0.3, 0.8, n) : smoothstep(0.15, 0.9, n);
    COLOR = vec4(tint.rgb, density * opacity * height_mask);
}
