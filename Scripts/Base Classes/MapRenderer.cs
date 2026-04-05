using Godot;
using System.Collections.Generic;

public partial class MapRenderer : Sprite2D {

    [Export] public Texture2D colour_map_texture;
    [Export] public Texture2D overlay_texture;

    private List<Territory> territory_order = new();
    private ShaderMaterial shader_material;

    private ImageTexture colour_lut;
    private ImageTexture owner_lut;
    private ImageTexture region_lut;

    private bool _region_mode = false;
    public bool region_mode {
        get => _region_mode;
        set {
            _region_mode = value;
            shader_material?.SetShaderParameter("region_mode", value ? 1 : 0);
        }
    }

    // ----- // CREATION // ----- //

    public override void _Ready() {
        shader_material = Material as ShaderMaterial;
        if (shader_material == null) {
            GD.PrintErr("MapRenderer: node has no ShaderMaterial assigned.");
            return;
        }

        var map_master = GetNode<MapMaster>("/root/MapMaster");
        if (map_master == null) {
            GD.PrintErr("MapRenderer: could not find MapMaster autoload.");
            return;
        }

        // Fix territory order — index must match across all LUTs
        foreach (var territory in map_master.Territories.Values) {
            territory_order.Add(territory);
            territory.OnOwnerChanged += OnTerritoryOwnerChanged;
        }

        BuildColourLut();
        BuildOwnerLut();
        BuildRegionLut();

        shader_material.SetShaderParameter("colour_map", colour_map_texture);
        if (overlay_texture != null)
            shader_material.SetShaderParameter("overlay", overlay_texture);
        shader_material.SetShaderParameter("colour_lut", colour_lut);
        shader_material.SetShaderParameter("owner_lut", owner_lut);
        shader_material.SetShaderParameter("region_lut", region_lut);
        shader_material.SetShaderParameter("territory_count", territory_order.Count);
        shader_material.SetShaderParameter("region_mode", _region_mode ? 1 : 0);
    }

    // ----- // LUT // ----- //

    // Built once — maps each index to the territory's colour map colour.
    private void BuildColourLut() {
        var img = Image.CreateEmpty(territory_order.Count, 1, false, Image.Format.Rgba8);
        for (int i = 0; i < territory_order.Count; i++)
            img.SetPixel(i, 0, territory_order[i].map_colour);
        colour_lut = ImageTexture.CreateFromImage(img);
    }

    // Built on load and updated whenever - maps tiles to it's owning player's colour.
    private void BuildOwnerLut() {
        var img = Image.CreateEmpty(territory_order.Count, 1, false, Image.Format.Rgba8);
        for (int i = 0; i < territory_order.Count; i++) {
            var owner = territory_order[i].Owner;
            img.SetPixel(i, 0, owner != null ? owner.colour : Colors.Transparent);
        }
        owner_lut = ImageTexture.CreateFromImage(img);
    }

    // Built once — maps each territory index to its region's colour.
    private void BuildRegionLut() {
        var img = Image.CreateEmpty(territory_order.Count, 1, false, Image.Format.Rgba8);
        for (int i = 0; i < territory_order.Count; i++) {
            var region = territory_order[i].Region;
            img.SetPixel(i, 0, region != null ? region.colour : Colors.Transparent);
        }
        region_lut = ImageTexture.CreateFromImage(img);
    }

    // LUT Updating //

    private void OnTerritoryOwnerChanged(Territory territory, Player previous, Player current) {
        RefreshOwnership(territory);
    }

    // Updates a single territory's pixel in the owner LUT.
    public void RefreshOwnership(Territory territory) {
        int index = territory_order.IndexOf(territory);
        if (index < 0) return;

        var img = owner_lut.GetImage();
        img.SetPixel(index, 0, territory.Owner != null ? territory.Owner.colour : Colors.Transparent);
        owner_lut.Update(img);
    }

    // Rebuilds the entire owner LUT — use when many territories change at once
    public void RefreshAllOwnership() {
        BuildOwnerLut();
        shader_material.SetShaderParameter("owner_lut", owner_lut);
    }
}