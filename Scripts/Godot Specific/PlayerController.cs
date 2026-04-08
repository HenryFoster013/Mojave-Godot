using Godot;

public partial class PlayerController : Node {

	[Export] public MapRenderer map_renderer;
	[Export] public LabelManager label_manager;
	[Export] public Camera2D camera;

	Vector2 cam_pos, cam_bounds;
	float cam_zoom;
	float fdelta;

	Territory selected_territory;

	const float cam_speed = 500f;
	const float zoom_min = 0.275f;
	const float zoom_max = 1.75f;
	const float zoom_speed = 1f;
	const float bounds_size = 1000f;

	// ----- // START // ----- //

	public override void _Ready() {
		SetProcessInput(true);
		cam_bounds = new Vector2(bounds_size, bounds_size);
		cam_pos = Vector2.Zero;
		cam_zoom = zoom_min;
	}

	// ----- // UPDATE // ----- //

	// Occurs on key press
	public override void _Input(InputEvent e) {
		ToggleRegions(e);
		WorldClicks(e);
	}

	// Occurs per frame
	public override void _Process(double delta) {
		fdelta = (float)delta;
		CameraMovement();
		CameraZoom();
		label_manager.UpdateLabels();
	}

	// ----- // CONTROLS // ----- //

	void ToggleRegions(InputEvent e) {
		if (e.IsActionPressed("ToggleRegions")) {
			map_renderer.region_mode = !map_renderer.region_mode;
			GD.Print($"Region mode: {map_renderer.region_mode}");
		}
	}

	void WorldClicks(InputEvent e) {
		if (e.IsActionPressed("LeftClick")) {
			var mouse_event = (InputEventMouseButton)e;
			Vector2 click_pos = GetViewport().CanvasTransform.AffineInverse() * mouse_event.Position;
			SelectTerritory(map_renderer.GetTerritoryAtCoords(click_pos));
		}
	}

	void SelectTerritory(Territory territory) {
		selected_territory = territory;
		map_renderer.SelectTerritory(selected_territory);

		if (selected_territory != null)
			GD.Print($"Selected {selected_territory.name}");
		else
			GD.Print("Unselected territories");
	}

	// ----- // CAMERA // ----- //

	void CameraMovement() {

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

	void CameraZoom() {

		float zoom_mod = 0;
		if (Input.IsActionPressed("Zoom+")) zoom_mod += 1;
		if (Input.IsActionPressed("Zoom-")) zoom_mod -= 1;

		cam_zoom += zoom_speed * fdelta * zoom_mod * cam_zoom;
		cam_zoom = float.Clamp(cam_zoom, zoom_min, zoom_max);
		camera.Zoom = Vector2.One * cam_zoom;
		label_manager.camera_zoom = cam_zoom;
	}
}
