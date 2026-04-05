using Godot;
using System.Collections.Generic;

/// <summary>
/// Attach to a Sprite2D or TextureRect that has the base map as its texture.
/// Builds the colour and owner LUT textures and passes them to the shader.
/// Call RefreshOwnership() whenever a territory changes hands.
/// </summary>
public partial class MapRenderer : Sprite2D
{
    [Export] public Texture2D colour_map_texture;

    // The ordered list of territories — index here matches index in both LUTs
    private List<Territory> territory_order = new();

    private ImageTexture colour_lut;
    private ImageTexture owner_lut;

    private ShaderMaterial shader_material;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        shader_material = Material as ShaderMaterial;
        if (shader_material == null)
        {
            GD.PrintErr("MapRenderer: node has no ShaderMaterial assigned.");
            return;
        }

        // Get MapMaster from autoload
        var map_master = GetNode<MapMaster>("/root/MapMaster");
        if (map_master == null)
        {
            GD.PrintErr("MapRenderer: could not find MapMaster autoload.");
            return;
        }

        // Fix territory order — must stay consistent between both LUTs
        foreach (var territory in map_master.Territories.Values)
        {
            territory_order.Add(territory);
            territory.OnOwnerChanged += OnTerritoryOwnerChanged;
        }

        BuildColourLut();
        BuildOwnerLut();

        shader_material.SetShaderParameter("colour_map",       colour_map_texture);
        shader_material.SetShaderParameter("colour_lut",       colour_lut);
        shader_material.SetShaderParameter("owner_lut",        owner_lut);
        shader_material.SetShaderParameter("territory_count",  territory_order.Count);
    }

    // -------------------------------------------------------------------------
    // LUT building
    // -------------------------------------------------------------------------

    /// <summary>
    /// Built once — maps each index to the territory's colour map colour.
    /// Never changes after load.
    /// </summary>
    private void BuildColourLut()
    {
        var img = Image.Create(territory_order.Count, 1, false, Image.Format.Rgba8);

        for (int i = 0; i < territory_order.Count; i++)
            img.SetPixel(i, 0, territory_order[i].map_colour);

        colour_lut = ImageTexture.CreateFromImage(img);
    }

    /// <summary>
    /// Built on load and updated whenever ownership changes.
    /// Transparent pixel = unowned territory.
    /// </summary>
    private void BuildOwnerLut()
    {
        var img = Image.Create(territory_order.Count, 1, false, Image.Format.Rgba8);

        for (int i = 0; i < territory_order.Count; i++)
        {
            var owner = territory_order[i].Owner;
            img.SetPixel(i, 0, owner != null ? owner.colour : Colors.Transparent);
        }

        owner_lut = ImageTexture.CreateFromImage(img);
    }

    // -------------------------------------------------------------------------
    // Ownership updates
    // -------------------------------------------------------------------------

    private void OnTerritoryOwnerChanged(Territory territory, Player previous, Player current)
    {
        RefreshOwnership(territory);
    }

    /// <summary>
    /// Updates a single territory's pixel in the owner LUT.
    /// Much cheaper than rebuilding the whole texture.
    /// </summary>
    public void RefreshOwnership(Territory territory)
    {
        int index = territory_order.IndexOf(territory);
        if (index < 0) return;

        var img = owner_lut.GetImage();
        var owner = territory.Owner;
        img.SetPixel(index, 0, owner != null ? owner.colour : Colors.Transparent);
        owner_lut.Update(img);
    }

    /// <summary>
    /// Rebuilds the entire owner LUT — use this if many territories change at once
    /// e.g. loading a save file.
    /// </summary>
    public void RefreshAllOwnership()
    {
        BuildOwnerLut();
        shader_material.SetShaderParameter("owner_lut", owner_lut);
    }
}
