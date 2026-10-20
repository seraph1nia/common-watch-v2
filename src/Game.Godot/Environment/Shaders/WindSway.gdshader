shader_type canvas_item;
render_mode unshaded;
uniform float visual_time = 0.0;
uniform float strength = 1.0;
uniform float frequency = 0.8;
uniform float phase_offset = 0.0;
uniform float anchor_stiffness = 3.0;
uniform vec2 wind_direction = vec2(1.0, 0.0);
uniform bool banner = false;
void vertex() {
    float free_edge = banner ? UV.x : 1.0 - UV.y;
    VERTEX += wind_direction * sin(visual_time * frequency + free_edge * 3.0 + phase_offset) * strength * pow(free_edge, anchor_stiffness);
}
