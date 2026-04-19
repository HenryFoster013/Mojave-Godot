shader_type spatial;
render_mode ambient_light_disabled, shadows_disabled;

uniform sampler2D albedo_texture : source_color, filter_nearest, repeat_disable;
uniform sampler2D id_map : filter_nearest, repeat_disable;
uniform sampler2D owner_lut : source_color, filter_nearest, repeat_disable;
uniform sampler2D region_lut : source_color, filter_nearest, repeat_disable;
uniform sampler2D highlight_lut : filter_nearest, repeat_disable;
uniform sampler2D overlay : source_color, filter_nearest, repeat_disable;
uniform sampler2D highlight_overlay : source_color, filter_nearest, repeat_disable;
uniform int territory_count = 115;
uniform int region_mode = 0;

void fragment() {
    vec4 base_colour = texture(albedo_texture, UV);
    vec4 over_colour = texture(overlay, UV);

    vec4 id_sample = texture(id_map, UV);
    float highlight = 0.0;

    if (id_sample.a > 0.01) {
        int match_index = int(round(id_sample.r * 255.0));
        float lut_size = float(territory_count);
        float uv_x = (float(match_index) + 0.5) / lut_size;

        vec4 tint;
        if (region_mode == 1) {
            tint = texture(region_lut, vec2(uv_x, 0.5));
        } else {
            tint = texture(owner_lut, vec2(uv_x, 0.5));
        }

        base_colour.rgb *= tint.rgb;
        highlight = texture(highlight_lut, vec2(uv_x, 0.5)).r;
    }

    vec3 final_colour;
    if (over_colour.a > 0.01) {
        final_colour = mix(base_colour, over_colour, over_colour.a).rgb;
    } else {
        final_colour = base_colour.rgb;
    }

    // Render highlight_overlay on top for selected tiles
    if (highlight > 0.85) {
        vec4 hl_colour = texture(highlight_overlay, UV);
        if (hl_colour.a > 0.01) {
            final_colour = mix(final_colour, hl_colour.rgb, hl_colour.a * highlight);
        }
    }

    ALBEDO = final_colour;
    EMISSION = final_colour * (1.0 + highlight * 4.0);
}
