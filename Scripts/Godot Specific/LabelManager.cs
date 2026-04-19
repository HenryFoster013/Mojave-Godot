using Godot;
using System.Collections.Generic;

public abstract partial class LabelManager : Node {

    protected GameMaster game_master;
    [Export] public Node troop_label_holder;
    [Export] public Node tile_label_holder;
    [Export] public Node region_label_holder;
    [Export] public PackedScene troop_label;
    [Export] public PackedScene map_label;

    protected readonly List<Label> troop_labels = new();
    protected const float zoom_limit = 0.6f;
    public float camera_zoom;
    public bool region_mode;

    public virtual void Setup(GameMaster _game_master) {
        game_master = _game_master;

        foreach (var territory in game_master.Territories.Values) {
            var label = map_label.Instantiate<Node>();
            AddLabelToHolder(tile_label_holder, label, WorldPosition(territory.centroid), 0.75f);
            label.GetChild<Label>(0).Text = territory.name;

            var label_troops = troop_label.Instantiate<Node>();
            AddLabelToHolder(troop_label_holder, label_troops, WorldPosition(territory.centroid), 1.2f);
            label_troops.GetChild<Label>(0).Text = "";
            troop_labels.Add(label_troops.GetChild<Label>(0));
        }

        foreach (var region in game_master.Regions.Values) {
            var label = map_label.Instantiate<Node>();
            AddLabelToHolder(region_label_holder, label, WorldPosition(region.centroid), 2f);
            label.GetChild<Label>(0).Text = region.name;
        }

        region_mode = false;
    }

    protected abstract void AddLabelToHolder(Node holder, Node label, Vector3 world_pos, float scale);
    protected abstract Vector3 WorldPosition(Vector2 centroid);

    public void UpdateLabels() {
        SetHolderVisible(tile_label_holder, region_mode && camera_zoom > zoom_limit);
        SetHolderVisible(region_label_holder, region_mode && camera_zoom <= zoom_limit);
        SetHolderVisible(troop_label_holder, !region_mode);
    }

    protected abstract void SetHolderVisible(Node holder, bool visible);

    public void UpdateTroopCount(Territory territory)
        => troop_labels[territory.render_order].Text = territory.troop_count.ToString();
}
