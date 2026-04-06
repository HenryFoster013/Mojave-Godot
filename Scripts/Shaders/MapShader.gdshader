shader_type canvas_item;

uniform sampler2D colour_map : filter_nearest, repeat_disable;
uniform sampler2D overlay : filter_nearest, repeat_disable;

// 1D texture, one pixel per territory — stores the territory's map colour
uniform sampler2D colour_lut : filter_nearest, repeat_disable;
uniform sampler2D owner_lut : filter_nearest, repeat_disable;
uniform sampler2D region_lut : filter_nearest, repeat_disable;

uniform int territory_count = 115;
uniform float match_threshold = 0.0;
uniform int region_mode = 0; 

void fragment() {
    vec4 base_colour = COLOR;
    vec4 map_colour  = texture(colour_map, UV);
    vec4 over_colour = texture(overlay, UV);
    bool is_black = dot(map_colour.rgb, map_colour.rgb) < 0.01;

    float lut_size  = float(territory_count);
    int match_index = -1;

    if (!is_black) {
        for (int i = 0; i < territory_count; i++) {
            float uv_x      = (float(i) + 0.5) / lut_size;
            vec4 lut_colour = texture(colour_lut, vec2(uv_x, 0.5));

            vec3 diff = abs(lut_colour.rgb - map_colour.rgb);
            if (diff.r <= match_threshold && diff.g <= match_threshold && diff.b <= match_threshold) {
                match_index = i;
                break;
            }
        }
    }

    vec4 tint = vec4(0.0);
    if (!is_black && match_index >= 0) {
        float uv_x = (float(match_index) + 0.5) / lut_size;
        if (region_mode == 1) {
            tint = texture(region_lut, vec2(uv_x, 0.5));
        } else {
            tint = texture(owner_lut, vec2(uv_x, 0.5));
        }
    }

    vec4 map_result;
    if (tint.a < 0.01) {
        map_result = base_colour;
    } else {
        map_result = base_colour * tint;
    }

    if (over_colour.a > 0.01) {
        COLOR = mix(map_result, over_colour, over_colour.a);
    } else {
        COLOR = map_result;
    }
}
