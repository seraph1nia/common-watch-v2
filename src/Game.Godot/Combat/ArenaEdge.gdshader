shader_type canvas_item;
render_mode unshaded;

uniform vec4 inner_bounds;
uniform vec4 outer_bounds;
varying vec2 arena_position;

void vertex() { arena_position = VERTEX; }

void fragment() {
    vec2 near_edge = smoothstep(outer_bounds.xy, inner_bounds.xy, arena_position);
    vec2 far_edge = 1.0 - smoothstep(inner_bounds.zw, outer_bounds.zw, arena_position);
    COLOR.a *= near_edge.x * near_edge.y * far_edge.x * far_edge.y;
}
