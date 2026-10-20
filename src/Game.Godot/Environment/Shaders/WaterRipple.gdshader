shader_type canvas_item;
render_mode unshaded;
uniform float visual_time = 0.0;
uniform float ripple_strength = 0.0015;
uniform float ripple_speed = 0.35;
uniform float wave_frequency = 45.0;
uniform vec2 flow = vec2(0.0);
uniform float highlight_strength = 0.025;
uniform vec4 water_tint : source_color = vec4(1.0);
uniform sampler2D surface_mask : filter_linear;
uniform bool use_mask = false;
void fragment() {
    float mask = use_mask ? texture(surface_mask, UV).r : 1.0;
    // Flow moves wave phase, keeping an isolated reflection inside its source image.
    vec2 phase_uv = UV + flow * visual_time;
    vec2 uv = UV;
    float wave = sin(dot(phase_uv, vec2(0.35, 1.0)) * wave_frequency + visual_time * ripple_speed);
    uv += vec2(wave, sin(phase_uv.x * wave_frequency * 0.7 - visual_time * ripple_speed)) * ripple_strength * mask;
    vec4 surface = texture(TEXTURE, clamp(uv, vec2(0.001), vec2(0.999)));
    surface.rgb += pow(max(wave, 0.0), 8.0) * highlight_strength * mask;
    COLOR = surface * water_tint * COLOR;
}
