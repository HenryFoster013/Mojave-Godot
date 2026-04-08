using Godot;
using System.Collections.Generic;

public partial class MapRenderer : Sprite2D {

	[Export] public Texture2D colour_map_texture;
	[Export] public Texture2D overlay_texture;
	[Export] public LabelManager label_manager;

	private List<Territory> territory_order = new();
	private ShaderMaterial shader_material;

	private ImageTexture id_map;
	private ImageTexture owner_lut;
	private ImageTexture region_lut;

	private Image _ownerImage;

	private bool _region_mode = false;
	public bool region_mode {
		get => _region_mode;
		set {
			_region_mode = value;
			label_manager.region_mode = value;
			shader_material?.SetShaderParameter("region_mode", value ? 1 : 0);
		}
	}

	public override void _Ready() {
		shader_material = Material as ShaderMaterial;
		if (shader_material == null) return;

		var map_master = GetNode<MapMaster>("/root/MapMaster");
		if (map_master == null) return;

		foreach (var territory in map_master.Territories.Values) {
			territory_order.Add(territory);
			territory.OnOwnerChanged += OnTerritoryOwnerChanged;
		}

		BuildIdMap();
		BuildOwnerLut();
		BuildRegionLut();

		shader_material.SetShaderParameter("id_map", id_map);
		if (overlay_texture != null)
			shader_material.SetShaderParameter("overlay", overlay_texture);

		shader_material.SetShaderParameter("owner_lut", owner_lut);
		shader_material.SetShaderParameter("region_lut", region_lut);
		shader_material.SetShaderParameter("territory_count", territory_order.Count);
		shader_material.SetShaderParameter("region_mode", _region_mode ? 1 : 0);
	}

	private void BuildIdMap() {
		if (colour_map_texture == null) return;

		Image sourceImg = colour_map_texture.GetImage();
		int width = sourceImg.GetWidth();
		int height = sourceImg.GetHeight();

		Image idImg = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);

		var colorToIndex = new Dictionary<Color, int>();
		for (int i = 0; i < territory_order.Count; i++) {
			colorToIndex[territory_order[i].map_colour] = i;
		}

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				Color c = sourceImg.GetPixel(x, y);

				bool isBlack = c.R < 0.01f && c.G < 0.01f && c.B < 0.01f;

				if (!isBlack && c.A > 0.01f && colorToIndex.TryGetValue(c, out int index)) {
					float val = index / 255.0f;
					idImg.SetPixel(x, y, new Color(val, 0, 0, 1.0f));
				}
				else {
					idImg.SetPixel(x, y, new Color(0, 0, 0, 0));
				}
			}
		}

		id_map = ImageTexture.CreateFromImage(idImg);
	}

	private void BuildOwnerLut() {
		_ownerImage = Image.CreateEmpty(territory_order.Count, 1, false, Image.Format.Rgba8);
		for (int i = 0; i < territory_order.Count; i++) {
			var owner = territory_order[i].Owner;
			_ownerImage.SetPixel(i, 0, owner != null ? owner.colour : Colors.Red);
		}
		owner_lut = ImageTexture.CreateFromImage(_ownerImage);
	}

	private void BuildRegionLut() {
		var img = Image.CreateEmpty(territory_order.Count, 1, false, Image.Format.Rgba8);
		for (int i = 0; i < territory_order.Count; i++) {
			var region = territory_order[i].Region;
			img.SetPixel(i, 0, region != null ? region.colour : Colors.Green);
		}
		region_lut = ImageTexture.CreateFromImage(img);
	}

	private void OnTerritoryOwnerChanged(Territory territory, Player previous, Player current) {
		RefreshOwnership(territory);
	}

	public void RefreshOwnership(Territory territory) {
		int index = territory_order.IndexOf(territory);
		if (index < 0 || _ownerImage == null) return;

		_ownerImage.SetPixel(index, 0, territory.Owner != null ? territory.Owner.colour : Colors.Red);
		owner_lut.Update(_ownerImage);
	}

	public void RefreshAllOwnership() {
		BuildOwnerLut();
		shader_material.SetShaderParameter("owner_lut", owner_lut);
	}
}
