using Godot;

public partial class PlayerController2D : PlayerController {

    [Export] public Camera2D camera;
    Vector2 cam_pos, cam_bounds;
    float cam_zoom;
    
    const float cam_speed = 500f;
    const float zoom_min = 0.275f;
    const float zoom_max = 1.75f;
    const float zoom_speed = 1f;
    const float bounds_size = 2000f;

    public override void Setup(GameMaster _game_master, MapRenderer _map_renderer, LabelManager _label_manager) {
        base.Setup(_game_master, _map_renderer, _label_manager);
        cam_bounds = new Vector2(bounds_size, bounds_size);
        cam_pos = Vector2.Zero;
        cam_zoom = zoom_min;
    }

    protected override void WorldClicks(InputEvent e) {
        if (e.IsActionPressed("LeftClick")) {
            var mouse_event = (InputEventMouseButton)e;
            Vector2 click_pos = GetViewport().CanvasTransform.AffineInverse() * mouse_event.Position;
            SelectTerritory(map_renderer.GetTerritoryAtCoords(click_pos));
        }
    }

    protected override void CameraMovement() {
        Vector2 direction = Vector2.Zero;
        float speed_mult = 1f;
        if (Input.IsActionPressed("Up")) direction.Y -= 1;
        if (Input.IsActionPressed("Down")) direction.Y += 1;
        if (Input.IsActionPressed("Left")) direction.X -= 1;
        if (Input.IsActionPressed("Right")) direction.X += 1;
        if (Input.IsActionPressed("Shift")) speed_mult = 1.5f;
        if (direction != Vector2.Zero) {
            float speed = cam_speed * speed_mult / cam_zoom;
            cam_pos += direction.Normalized() * speed * fdelta;
            cam_pos = cam_pos.Clamp(-cam_bounds, cam_bounds);
        }
        camera.Position = cam_pos;
    }

    protected override void CameraZoom() {
        float zoom_mod = 0;
        if (Input.IsActionPressed("Zoom+")) zoom_mod += 1;
        if (Input.IsActionPressed("Zoom-")) zoom_mod -= 1;
        cam_zoom += zoom_speed * fdelta * zoom_mod * cam_zoom;
        cam_zoom = float.Clamp(cam_zoom, zoom_min, zoom_max);
        camera.Zoom = Vector2.One * cam_zoom;
        label_manager.camera_zoom = cam_zoom;
    }
}
