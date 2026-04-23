shader_type spatial;
render_mode ambient_light_disabled, shadows_disabled;

uniform vec4 colour : source_color = vec4(1.0);
uniform float highlight : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    ALBEDO = COLOR.rgb * colour.rgb * (0.5 + highlight * 4.0);
    EMISSION = COLOR.rgb * colour.rgb * (0.5 + highlight * 4.0);
}
