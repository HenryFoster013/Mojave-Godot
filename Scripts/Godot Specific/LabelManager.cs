using Godot;
using System.Collections.Generic;

public abstract partial class LabelManager : Node {

    protected GameMaster game_master;
    [Export] public Node troop_label_holder;
    [Export] public Node tile_label_holder;
    [Export] public Node region_label_holder;
    [Export] public PackedScene map_label;
    [Export] public float zoom_limit = 0.6f;

    protected readonly List<Node> troop_labels = new();
    public float camera_zoom;
    public bool region_mode;

    public virtual void Setup(GameMaster _game_master) {
        game_master = _game_master;

        foreach (var territory in game_master.Territories.Values) {
            var label = map_label.Instantiate<Node>();
            AddLabelToHolder(tile_label_holder, label, WorldPosition(territory.centroid), 0.75f);
            SetLabelText(label, territory.name);

            var label_troops = map_label.Instantiate<Node>();
            AddLabelToHolder(troop_label_holder, label_troops, WorldPosition(territory.centroid), 1.2f);
            SetLabelText(label_troops, "");
            troop_labels.Add(label_troops);
        }

        foreach (var region in game_master.Regions.Values) {
            var label = map_label.Instantiate<Node>();
            AddLabelToHolder(region_label_holder, label, WorldPosition(region.centroid), 2f);
            SetLabelText(label, region.name);
        }

        region_mode = false;
    }

    protected abstract void SetLabelText(Node labelNode, string text);
    protected abstract void AddLabelToHolder(Node holder, Node label, Vector3 world_pos, float scale);
    protected abstract Vector3 WorldPosition(Vector2 centroid);
    public abstract void UpdateLabels();
    protected abstract void SetHolderVisible(Node holder, bool visible);

    public void UpdateTroopCount(Territory territory)
        => SetLabelText(troop_labels[territory.render_order], territory.troop_count.ToString());
}
