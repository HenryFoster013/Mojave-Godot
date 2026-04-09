shader_type canvas_item;

uniform sampler2D id_map : filter_nearest, repeat_disable;
uniform sampler2D owner_lut : filter_nearest, repeat_disable;
uniform sampler2D region_lut : filter_nearest, repeat_disable;
uniform sampler2D highlight_lut : filter_nearest, repeat_disable;
uniform sampler2D overlay : filter_nearest, repeat_disable;

uniform int territory_count = 115;
uniform int region_mode = 0;

void fragment() {
    vec4 base_colour = COLOR;
    vec4 over_colour = texture(overlay, UV);
    
    vec4 id_sample = texture(id_map, UV);
    
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
        base_colour.rgb *= (1.0 + texture(highlight_lut, vec2(uv_x, 0.5)).r);
    }


    if (over_colour.a > 0.01) {
        COLOR = mix(base_colour, over_colour, over_colour.a);
    } else {
        COLOR = base_colour;
    }
}