shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float visual_time = 0.0;
uniform float pulse_frequency = 1.5;
uniform float pulse_amplitude = 0.12;
varying float phase;
void vertex() { phase = VERTEX.x * 0.019 + VERTEX.y * 0.013; }
void fragment() {
    float pulse = 1.0 + pulse_amplitude * (sin(visual_time * pulse_frequency + phase) + 0.3 * sin(visual_time * 3.17 + phase));
    COLOR *= vec4(vec3(pulse), 1.0);
}
