shader_type spatial;
render_mode ambient_light_disabled, shadows_disabled;

uniform sampler2D owner_lut : source_color, filter_nearest, repeat_disable;
uniform sampler2D region_lut : source_color, filter_nearest, repeat_disable;
uniform sampler2D highlight_lut : filter_nearest, repeat_disable;
uniform float lut_u = 0.0;
uniform int region_mode = 0;

void fragment() {
    vec4 tint;
    if (region_mode == 1) {
        tint = texture(region_lut, vec2(lut_u, 0.5));
    } else {
        tint = texture(owner_lut, vec2(lut_u, 0.5));
    }

    float highlight = texture(highlight_lut, vec2(lut_u, 0.5)).r;

    ALBEDO = COLOR.rgb * tint.rgb * (0.2 + highlight * 1.0);
    EMISSION = COLOR.rgb * tint.rgb * (0.2 + highlight * 1.0);
}
