uniform float visual_time = 0.0;
uniform vec2 velocity = vec2(7.0, 0.0);
uniform vec2 texture_scale = vec2(512.0);
uniform float opacity = 0.1;
uniform vec4 tint : source_color = vec4(0.7, 0.8, 0.85, 1.0);
uniform vec4 region;
varying vec2 world_point;
// Shared seamless procedural noise is generated once by Godot, never per frame.
uniform sampler2D noise_texture : repeat_enable, filter_linear;
float noise2(vec2 p) { return texture(noise_texture, p * 0.125).r; }
void vertex() { world_point = VERTEX; }
