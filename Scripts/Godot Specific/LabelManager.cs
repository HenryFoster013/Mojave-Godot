using Godot;
using System.Collections.Generic;

public partial class LabelManager : Node {

	private GameMaster game_master;

	[Export] public Node2D troop_label_holder;
	[Export] public Node2D tile_label_holder;
	[Export] public Node2D region_label_holder;
	[Export] public PackedScene troop_label;
	[Export] public PackedScene map_label;

	private readonly List<Label> troop_labels = new();

	private const float zoom_limit = 0.6f;
	public float camera_zoom;
	public bool region_mode;

	public void Setup(GameMaster _game_master) {

		game_master = _game_master;

		foreach (var territory in game_master.Territories.Values) {
			var label = map_label.Instantiate<Node2D>();
			tile_label_holder.AddChild(label);
			label.GlobalPosition = PixelspaceToWorldspace(territory.centroid);
			label.Scale = Vector2.One * 0.75f;
			label.GetChild<Label>(0).Text = territory.name;

			var label_troops = troop_label.Instantiate<Node2D>();
			troop_label_holder.AddChild(label_troops);
			label_troops.GlobalPosition = PixelspaceToWorldspace(territory.centroid);
			troop_label_holder.Scale = Vector2.One * 1.2f;
			label_troops.GetChild<Label>(0).Text = "";
			troop_labels.Add(label_troops.GetChild<Label>(0));
		}

		foreach (var region in game_master.Regions.Values) {
			var label = map_label.Instantiate<Node2D>();
			region_label_holder.AddChild(label);
			label.GlobalPosition = PixelspaceToWorldspace(region.centroid);
			label.Scale = Vector2.One * 2f;
			label.GetChild<Label>(0).Text = region.name;
		}

		region_mode = false;
	}

	public void UpdateLabels() {
		tile_label_holder.Visible = region_mode && camera_zoom > zoom_limit;
		region_label_holder.Visible = region_mode && camera_zoom <= zoom_limit;
		troop_label_holder.Visible = !region_mode;
	}

	private Vector2 PixelspaceToWorldspace(Vector2 centroid) {
		float scalar = 4f;
		float x = (centroid.X * scalar)- 1024;
		float y = 1024 - (centroid.Y * scalar);
		return new Vector2(x, y);
	}

	public void UpdateTroopCount(Territory territory) {
		troop_labels[territory.render_order].Text = territory.troop_count.ToString();
	}
}
