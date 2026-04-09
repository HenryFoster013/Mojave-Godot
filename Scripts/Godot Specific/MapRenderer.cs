using Godot;
using System.Collections.Generic;

public partial class MapRenderer : Sprite2D {

	[Export] public Texture2D colour_map_texture;
	[Export] public Texture2D overlay_texture;
	[Export] public LabelManager label_manager;

	private List<Territory> territory_order = new();
	private Territory selected_cache;
	Image colour_map_image;
	private ShaderMaterial shader_material;
	private GameMaster game_master;

	private ImageTexture id_map;
	private ImageTexture owner_lut;
	private ImageTexture region_lut;
	private ImageTexture highlight_lut;

	private Image _ownerImage;
	private Image _highlightImage;

	private bool _region_mode = false;
	public bool region_mode {
		get => _region_mode;
		set {
			_region_mode = value;
			label_manager.region_mode = value;
			shader_material?.SetShaderParameter("region_mode", value ? 1 : 0);
			SetHighlights();
		}
	}

	// ----- // START // ----- //

	public override void _Ready() {
		colour_map_image = colour_map_texture.GetImage();
		shader_material = Material as ShaderMaterial;
		if (shader_material == null) return;

		game_master = GetNode<GameMaster>("/root/GameMaster");
		if (game_master == null) return;

		int render_order = 0;
		foreach (var territory in game_master.Territories.Values) {
			territory_order.Add(territory);
			territory.render_order = render_order;
			render_order++;
			territory.OnOwnerChanged += OnTerritoryOwnerChanged;
		}

		BuildIdMap();
		BuildOwnerLut();
		BuildRegionLut();
		BuildHighlightLut();

		shader_material.SetShaderParameter("id_map", id_map);
		if (overlay_texture != null)
			shader_material.SetShaderParameter("overlay", overlay_texture);

		shader_material.SetShaderParameter("owner_lut", owner_lut);
		shader_material.SetShaderParameter("region_lut", region_lut);
		shader_material.SetShaderParameter("highlight_lut", highlight_lut);
		shader_material.SetShaderParameter("territory_count", territory_order.Count);
		shader_material.SetShaderParameter("region_mode", _region_mode ? 1 : 0);
	}

	// Building LUTs //

	private void BuildIdMap() {
		if (colour_map_texture == null) return;

		Image sourceImg = colour_map_texture.GetImage();
		int width = sourceImg.GetWidth();
		int height = sourceImg.GetHeight();

		Image idImg = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);

		var colorToIndex = new Dictionary<string, int>();
		for (int i = 0; i < territory_order.Count; i++) {
			colorToIndex[territory_order[i].map_colour] = i;
		}

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				Color c = sourceImg.GetPixel(x, y);

				bool isBlack = c.R < 0.01f && c.G < 0.01f && c.B < 0.01f;

				if (!isBlack && c.A > 0.01f && colorToIndex.TryGetValue(FormatColour(c), out int index)) {
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
			_ownerImage.SetPixel(i, 0, owner != null ? owner.colour : Colors.WebGray);
		}
		owner_lut = ImageTexture.CreateFromImage(_ownerImage);
	}

	private void BuildRegionLut() {
		var img = Image.CreateEmpty(territory_order.Count, 1, false, Image.Format.Rgba8);
		for (int i = 0; i < territory_order.Count; i++) {
			var region = territory_order[i].region;
			img.SetPixel(i, 0, region != null ? region.colour : Colors.WebGray);
		}
		region_lut = ImageTexture.CreateFromImage(img);
	}

	private void BuildHighlightLut() {
		_highlightImage = Image.CreateEmpty(territory_order.Count, 1, false, Image.Format.Rf);
		for (int i = 0; i < territory_order.Count; i++)
			_highlightImage.SetPixel(i, 0, new Color(0f, 0f, 0f, 1f));
		highlight_lut = ImageTexture.CreateFromImage(_highlightImage);
		SetHighlights();
	}

	// ----- // LUT REFRESHING // ----- //

	private void OnTerritoryOwnerChanged(Territory territory, Player previous, Player current) {
		RefreshOwnership(territory);
	}

	public void RefreshOwnership(Territory territory) {
		int index = territory_order.IndexOf(territory);
		if (index < 0 || _ownerImage == null) return;

		_ownerImage.SetPixel(index, 0, territory.Owner != null ? territory.Owner.colour : Colors.WebGray);
		owner_lut.Update(_ownerImage);
		SetHighlights();
	}

	public void RefreshAllOwnership() {
		BuildOwnerLut();
		shader_material.SetShaderParameter("owner_lut", owner_lut);
		SetHighlights();
	}

	public void SetHighlights() => SetHighlights(selected_cache);
	public void SetHighlights(Territory selected) {

		selected_cache = selected;

		if (region_mode) {
			for (int i = 0; i < territory_order.Count; i++)
				_highlightImage.SetPixel(i, 0, new Color(0.5f, 0f, 0f, 1f));
		}
		else {
			if (selected != null) {
				for (int i = 0; i < territory_order.Count; i++)
					_highlightImage.SetPixel(i, 0, new Color(0f, 0f, 0f, 1f));

				_highlightImage.SetPixel(selected.render_order, 0, new Color(0.7f, 1f, 1f, 1f));
				foreach (Territory territory in selected.neighbours)
					_highlightImage.SetPixel(territory.render_order, 0, new Color(0.4f, 1f, 1f, 1f));
			}
			else {
				for (int i = 0; i < territory_order.Count; i++) {
					if (territory_order[i].region.complete)
						_highlightImage.SetPixel(i, 0, new Color(0.5f, 0f, 0f, 1f));
					else
						_highlightImage.SetPixel(i, 0, new Color(0f, 0f, 0f, 1f));
				}
			}
		}

		highlight_lut.Update(_highlightImage);
		shader_material.SetShaderParameter("highlight_lut", highlight_lut);
	}

	// ----- // MISC // ----- //

	string FormatColour(Color colour) {
		return "#" + colour.ToHtml(false).ToUpper();
	}

	public void SelectTerritory(Territory territory) {
		int render_order = -1;
		if (territory != null)
			render_order = territory.render_order;

		SetHighlights(territory);
	}

	public Territory GetTerritoryAtCoords(Vector2 world_pos) {
		Vector2 pixel_pos = ToLocal(world_pos) + (Vector2.One * 1024);
		if (pixel_pos.X < 0 || pixel_pos.Y < 0 || pixel_pos.X > 2048 || pixel_pos.Y > 2048)
			return null;

		Color colour = colour_map_image.GetPixel((int)pixel_pos.X, (int)pixel_pos.Y);
		GD.Print($"Coords: {pixel_pos}, Colour: {colour}");
		return game_master.GetTerritoryByColour(FormatColour(colour));
	}
}
