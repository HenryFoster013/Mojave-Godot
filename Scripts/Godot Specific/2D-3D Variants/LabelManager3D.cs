using Godot;

public partial class LabelManager3D : LabelManager {

    [Export] public float map_scale = 5f;

    protected override Vector3 WorldPosition(Vector2 centroid) {
        float x = (centroid.X / 512f) * map_scale - map_scale / 2f;
        float z = -(centroid.Y / 512f) * map_scale + map_scale / 2f;
        return new Vector3(x, 0, z);
    }

    protected override void SetLabelText(Node labelNode, string text)
        => labelNode.GetChild<Label3D>(0).Text = text;

    protected override void AddLabelToHolder(Node holder, Node label, Vector3 world_pos, float scale) {
        var holder3D = (Node3D)holder;
        var label3D = (Node3D)label;
        holder3D.AddChild(label3D);
        label3D.GlobalPosition = world_pos;
        label3D.Scale = Vector3.One * scale;
    }

    protected override void SetHolderVisible(Node holder, bool visible)
        => ((Node3D)holder).Visible = visible;
    
    public override void UpdateLabels() {
        SetHolderVisible(tile_label_holder, region_mode && camera_zoom <= zoom_limit);
        SetHolderVisible(region_label_holder, region_mode && camera_zoom > zoom_limit);
        SetHolderVisible(troop_label_holder, !region_mode);
    }
}
