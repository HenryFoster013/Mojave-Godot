shader_type spatial;
render_mode unshaded;

uniform sampler2D albedo_texture : source_color;

void fragment() {
    ALBEDO = texture(albedo_texture, UV).rgb;
}
