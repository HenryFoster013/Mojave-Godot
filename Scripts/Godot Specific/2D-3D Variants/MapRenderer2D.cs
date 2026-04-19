using Godot;

public partial class MapRenderer2D : MapRenderer {

    [Export] public Sprite2D sprite;

    protected override ShaderMaterial GetShaderMaterial()
        => sprite.Material as ShaderMaterial;

    public override Territory GetTerritoryAtCoords(Vector2 world_pos) {
        Vector2 pixel_pos = sprite.ToLocal(world_pos) + (Vector2.One * 1024);
        if (pixel_pos.X < 0 || pixel_pos.Y < 0 || pixel_pos.X > 2048 || pixel_pos.Y > 2048)
            return null;
        Color colour = colour_map_image.GetPixel((int)pixel_pos.X, (int)pixel_pos.Y);
        GD.Print($"Coords: {pixel_pos}, Colour: {colour}");
        return game_master.GetTerritoryByColour(FormatColour(colour));
    }
}
