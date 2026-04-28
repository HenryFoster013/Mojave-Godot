using Godot;

public partial class PlayerController3D : PlayerController {

	[Export] public Camera3D camera;
	Vector3 cam_pos;
	float cam_zoom;
	
	const float cam_speed = 0.8f;
	const float zoom_speed = 1f;
	const float bounds_size = 5f;
	
	const float zoom_min = 0.55f;
	const float zoom_lower_switch = 1.0f;
	const float zoom_upper_switch = 2.05f;
	const float zoom_max = 3.75f;
	
	const float bottom_rotation = -50f;
	const float top_rotation = -90f;

	public override void Setup(GameMaster _game_master, MapRenderer _map_renderer, LabelManager _label_manager) {
		base.Setup(_game_master, _map_renderer, _label_manager);
		cam_pos = Vector3.Zero;
		cam_zoom = zoom_max;
		CacheRotations();
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
			float speed = cam_speed * speed_mult * (cam_zoom + 1);
			cam_pos += direction.Normalized() * speed * fdelta;
		}

		cam_pos = new Vector3(cam_pos.X, cam_zoom, cam_pos.Z);
		cam_pos.X = float.Clamp(cam_pos.X, -bounds_size, bounds_size);
		cam_pos.Z = float.Clamp(cam_pos.Z, -bounds_size, bounds_size);
		
		camera.Position = cam_pos;
		camera.Rotation = new Vector3(CalculateCameraTilt(), 0, 0);
		label_manager.camera_zoom = cam_zoom;
	}

	private float cached_top, cached_bottom, cached_bound;
	private void CacheRotations() {
		cached_top = Mathf.DegToRad(top_rotation);
		cached_bottom = Mathf.DegToRad(bottom_rotation);
		cached_bound = zoom_upper_switch - zoom_lower_switch;
	}

	private float CalculateCameraTilt() {
		if (cam_zoom > zoom_upper_switch)
			return cached_top;
		if (cam_zoom < zoom_lower_switch)
			return cached_bottom;

		float time = float.Clamp((cam_zoom - zoom_lower_switch) / cached_bound, 0f, 1f);
		time = time * time * (3f - 2f * time);
		return float.Lerp(cached_bottom, cached_top, time);
	}

	protected override void CameraZoom() { }
}
