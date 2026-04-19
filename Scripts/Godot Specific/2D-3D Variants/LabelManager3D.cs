using Godot;

public partial class LabelManager3D : LabelManager {

    [Export] public float label_height = 0.1f;
    [Export] public float map_scale = 1f;

    protected override Vector3 WorldPosition(Vector2 centroid) {
        float scalar = 4f * map_scale;
        float x = (centroid.X * scalar) - 1024 * map_scale;
        float z = (centroid.Y * scalar) - 1024 * map_scale;
        return new Vector3(x, label_height, z);
    }

    protected override void AddLabelToHolder(Node holder, Node label, Vector3 world_pos, float scale) {
        return; // TEMP
        var holder3D = (Node3D)holder;
        var label3D = (Node3D)label;
        holder3D.AddChild(label3D);
        label3D.GlobalPosition = world_pos;
        label3D.Scale = Vector3.One * scale;
    }

    protected override void SetHolderVisible(Node holder, bool visible)
        => ((Node3D)holder).Visible = visible;
}
