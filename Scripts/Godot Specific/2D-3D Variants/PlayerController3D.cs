using Godot;

public partial class PlayerController3D : PlayerController {

    [Export] public Camera3D camera;
    Vector3 cam_pos;
    float cam_zoom;
    
    const float cam_speed = 1.3f;
    const float zoom_min = 0.75f;
    const float zoom_max = 3.75f;
    const float zoom_speed = 1f;
    const float bounds_size = 5f;

    public override void Setup(GameMaster _game_master, MapRenderer _map_renderer, LabelManager _label_manager) {
        base.Setup(_game_master, _map_renderer, _label_manager);
        cam_pos = Vector3.Zero;
    }

    protected override void WorldClicks(InputEvent e) {
        if (e.IsActionPressed("LeftClick")) {
            var space = camera.GetWorld3D().DirectSpaceState;
            var mouse_pos = GetViewport().GetMousePosition();
            var origin = camera.ProjectRayOrigin(mouse_pos);
            var end = origin + camera.ProjectRayNormal(mouse_pos) * 30f;
            var query = PhysicsRayQueryParameters3D.Create(origin, end);
            var result = space.IntersectRay(query);
            if (result.Count > 0) {
                Vector3 hit = (Vector3)result["position"];
                Vector2 coords = new Vector2(hit.X, hit.Z);
                SelectTerritory(map_renderer.GetTerritoryAtCoords(coords));
            } else {
                SelectTerritory(null);
            }
        }
    }

    protected override void CameraMovement() {

        float zoom_mod = 0;
        if (Input.IsActionPressed("Zoom+")) zoom_mod -= 1;
        if (Input.IsActionPressed("Zoom-")) zoom_mod += 1;

        cam_zoom += zoom_speed * zoom_mod * fdelta * cam_zoom;
        cam_zoom = float.Clamp(cam_zoom, zoom_min, zoom_max);
    
        Vector3 direction = Vector3.Zero;
        float speed_mult = 1f;
        if (Input.IsActionPressed("Up")) direction -= new Vector3(0,0,1);
        if (Input.IsActionPressed("Down")) direction += new Vector3(0,0,1);
        if (Input.IsActionPressed("Left")) direction -= new Vector3(1,0,0);
        if (Input.IsActionPressed("Right")) direction += new Vector3(1,0,0);
        if (Input.IsActionPressed("Shift")) speed_mult = 1.5f;
        
        if (direction != Vector3.Zero) {
            float speed = cam_speed * speed_mult * cam_zoom;
            cam_pos += direction.Normalized() * speed * fdelta;
        }

        cam_pos = new Vector3(cam_pos.X, cam_zoom, cam_pos.Z);
        cam_pos.X = float.Clamp(cam_pos.X, -bounds_size, bounds_size);
        cam_pos.Z = float.Clamp(cam_pos.Z, -bounds_size, bounds_size);
        
        camera.Position = cam_pos;
        label_manager.camera_zoom = cam_zoom;
    }

    protected override void CameraZoom() { }
}
