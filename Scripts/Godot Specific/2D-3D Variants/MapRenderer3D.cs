using Godot;

public partial class MapRenderer3D : MapRenderer {

    [Export] public MeshInstance3D mesh;
    [Export] public Vector2 map_world_size = new Vector2(2048, 2048);
    [Export] public Vector2 map_pixel_size = new Vector2(2048, 2048);

    protected override ShaderMaterial GetShaderMaterial()
        => mesh.GetSurfaceOverrideMaterial(0) as ShaderMaterial;

    public override Territory GetTerritoryAtCoords(Vector2 world_pos) {

        Vector2 normalized = (world_pos + map_world_size / 2f) / map_world_size;
        Vector2 pixel_pos = normalized * map_pixel_size;

        if (pixel_pos.X < 0 || pixel_pos.Y < 0 || pixel_pos.X >= map_pixel_size.X || pixel_pos.Y >= map_pixel_size.Y)
            return null;

        Color colour = colour_map_image.GetPixel((int)pixel_pos.X, (int)pixel_pos.Y);
        GD.Print($"Coords: {pixel_pos}, Colour: {colour}");
        return game_master.GetTerritoryByColour(FormatColour(colour));
    }
}
