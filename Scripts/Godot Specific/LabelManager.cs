using Godot;
using System;

public partial class LabelManager : Node {
	[Export] public Node2D tile_label_holder;
	[Export] public Node2D region_label_holder;
	[Export] public PackedScene tile_label;
	[Export] public PackedScene region_label;

	bool _region_mode;
	public bool region_mode {
		get => _region_mode;
		set {
			_region_mode = value;
			UpdateRegionMode();
		}
	}

	public override void _Ready() {
		var map_master = GetNode<MapMaster>("/root/MapMaster");
		if (map_master == null) return;

		foreach (var territory in map_master.Territories.Values) {
			var label = tile_label.Instantiate<Node2D>();
			tile_label_holder.AddChild(label);
			label.GlobalPosition = PixelspaceToWorldspace(territory.centroid);
		}

		foreach (var region in map_master.Regions.Values) {
			var label = region_label.Instantiate<Node2D>();
			region_label_holder.AddChild(label);
			label.GlobalPosition = PixelspaceToWorldspace(region.centroid);
		}

		region_mode = false;
	}

	public void UpdateRegionMode() {
		tile_label_holder.Visible = !_region_mode;
		region_label_holder.Visible = _region_mode;
	}

	Vector2 PixelspaceToWorldspace(Vector2 centroid) {
		float scalar = 4f;
		float x = (centroid.X * scalar)- 1024;
		float y = 1024 - (centroid.Y * scalar);
		return new Vector2(x, y);
	}
}
