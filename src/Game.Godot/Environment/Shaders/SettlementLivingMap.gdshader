shader_type canvas_item;
render_mode unshaded;

// Animate only existing painted colors; no screen texture or simulation clock.
// 64 held frames close exactly at eight seconds.
uniform float visual_time = 0.0;
uniform float strength = 0.0;
varying vec4 vertex_color;

void vertex() {
    vertex_color = COLOR;
}

float ellipse(vec2 p, vec2 center, vec2 radius) {
    return 1.0 - smoothstep(0.8, 1.0, length((p - center) / radius));
}

void fragment() {
    vec2 p = UV * vec2(1672.0, 941.0);
    vec4 source = texture(TEXTURE, UV);
    float phase = mod(floor(visual_time * 8.0), 64.0) / 64.0 * TAU;
    // The cyan river winds across the bottom and into the right-hand forest.
    // Restrict by both location and pigment, leaving banks, bridges and pads still.
    float river_region = max(smoothstep(350.0, 420.0, p.y),
        ellipse(p, vec2(1440.0, 350.0), vec2(190.0, 105.0)));
    float water = river_region * smoothstep(0.08, 0.25, source.b - source.r)
        * smoothstep(0.02, 0.17, source.g - source.r);
    float foliage = smoothstep(0.04, 0.18, source.g - max(source.r, source.b));
    // Bare earth in every construction clearing remains untouched.
    float clearing = ellipse(p, vec2(820.0, 530.0), vec2(290.0, 230.0));
    foliage *= 1.0 - clearing;
    vec2 displacement = vec2(
        foliage * sin(phase + p.y * 0.025) * 1.15,
        water * sin(p.y * 0.10 - phase * 4.0 + p.x * 0.014) * 1.4);
    vec4 painted = texture(TEXTURE, UV + displacement * strength * TEXTURE_PIXEL_SIZE);
    float current = sin(p.y * 0.14 + p.x * 0.035 - phase * 4.0);
    painted.rgb *= 1.0 + water * current * 0.085 * strength;
    // The forge opening and existing cyan crystals breathe, not entire buildings.
    float forge = ellipse(p, vec2(948.0, 327.0), vec2(36.0, 28.0));
    float fire = forge * smoothstep(0.1, 0.35, source.r - source.b);
    float crystal = (1.0 - river_region) * smoothstep(0.18, 0.42, source.b - source.r);
    painted.rgb += strength * (fire * vec3(0.12, 0.045, 0.0)
        * (0.5 + 0.5 * sin(phase * 7.0))
        + crystal * vec3(0.015, 0.055, 0.08) * (0.5 + 0.5 * sin(phase * 2.0)));
    COLOR = vertex_color * mix(source, painted, step(0.001, strength));
}
