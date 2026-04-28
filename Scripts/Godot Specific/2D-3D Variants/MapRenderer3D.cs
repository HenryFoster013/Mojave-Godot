using Godot;
using System.Collections.Generic;

public partial class MapRenderer3D : MapRenderer {

    [Export] public MeshInstance3D mesh;
    [Export] public Vector2 map_world_size = new Vector2(2048, 2048);
    [Export] public Vector2 map_pixel_size = new Vector2(2048, 2048);

    [ExportGroup("Props")]
    [Export] public ShaderMaterial prop_material;
    [Export] public string[] prop_names = {};
    [Export] public MeshInstance3D[] prop_meshes = {};
    
    protected override void AdditionalSetup() {
        SetupProps();
    }

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

    void SetupProps() {
    
        if (prop_meshes.Length != prop_names.Length) {
            GD.PushError("Prop names and meshes do not match!");
            return;
        }
    
        for (int i = 0; i < prop_meshes.Length; i++) {
            var territory = game_master.GetTerritoryByID(prop_names[i]);
            ShaderMaterial new_mat = (ShaderMaterial)prop_material.Duplicate();
            float lut_u = (territory.render_order + 0.5f) / (float)territory_order.Count;
            
            new_mat.SetShaderParameter("lut_u", lut_u);
            new_mat.SetShaderParameter("owner_lut", owner_lut);
            new_mat.SetShaderParameter("highlight_lut", highlight_lut);
            new_mat.SetShaderParameter("region_lut", region_lut);
            
            prop_meshes[i].SetSurfaceOverrideMaterial(0, new_mat);
            territory.SetPropMaterial(new_mat);
            region_shaders.Add(new_mat);
        }
    }
}
