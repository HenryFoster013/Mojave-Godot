shader_type canvas_item;

// The colour map image — each pixel's colour identifies a territory
uniform sampler2D colour_map : filter_nearest, repeat_disable;

// 1D texture, one pixel per territory — stores the territory's map colour
// Used to match colour map pixels to territory indices
uniform sampler2D colour_lut : filter_nearest, repeat_disable;

// 1D texture, one pixel per territory — stores the owning player's colour
// This is the only texture updated at runtime when ownership changes
uniform sampler2D owner_lut : filter_nearest, repeat_disable;

uniform int territory_count = 115;

// How close two colours need to be to count as a match (0.0 = exact)
// Keep at 0.0 for exact lookup since your colours are all unique
uniform float match_threshold = 0.0;

void fragment() {
    vec4 base_colour = COLOR;
    vec4 map_colour  = texture(colour_map, UV);

    // Skip black pixels (borders and background)
    if (dot(map_colour.rgb, map_colour.rgb) < 0.01) {
        COLOR = base_colour;
    }
	else{

		// Search the colour LUT for a matching territory
		float lut_size = float(territory_count);
		int match_index = -1;

		for (int i = 0; i < territory_count; i++) {
			float uv_x = (float(i) + 0.5) / lut_size;
			vec4 lut_colour = texture(colour_lut, vec2(uv_x, 0.5));

			vec3 diff = abs(lut_colour.rgb - map_colour.rgb);
			if (diff.r <= match_threshold && diff.g <= match_threshold && diff.b <= match_threshold) {
				match_index = i;
				break;
			}
		}

		// No match found — render base as-is
		if (match_index < 0) {
			COLOR = base_colour;
		}
		else{

			// Sample the owner LUT at the matched index
			float owner_uv_x  = (float(match_index) + 0.5) / lut_size;
			vec4 owner_colour = texture(owner_lut, vec2(owner_uv_x, 0.5));

			// If no owner (alpha = 0), render base as-is
			if (owner_colour.a < 0.01) {
				COLOR = base_colour;
			}
			else{
				// Multiply owner colour over base map
				COLOR = base_colour * owner_colour;
			}
		}
	}
}
