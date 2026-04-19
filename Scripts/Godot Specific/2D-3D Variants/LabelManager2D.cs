using Godot;

public partial class LabelManager2D : LabelManager {

    protected override Vector3 WorldPosition(Vector2 centroid) {
        float scalar = 4f;
        float x = (centroid.X * scalar) - 1024;
        float y = 1024 - (centroid.Y * scalar);
        return new Vector3(x, y, 0);
    }

    protected override void AddLabelToHolder(Node holder, Node label, Vector3 world_pos, float scale) {
        var holder2D = (Node2D)holder;
        var label2D = (Node2D)label;
        holder2D.AddChild(label2D);
        label2D.GlobalPosition = new Vector2(world_pos.X, world_pos.Y);
        label2D.Scale = Vector2.One * scale;
    }

    protected override void SetHolderVisible(Node holder, bool visible)
        => ((Node2D)holder).Visible = visible;
}
