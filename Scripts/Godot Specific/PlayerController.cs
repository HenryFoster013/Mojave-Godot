using Godot;

public abstract partial class PlayerController : Node {
    protected GameMaster game_master;
    protected MapRenderer map_renderer;
    protected LabelManager label_manager;
    protected bool active = false;
    protected float fdelta;
    protected Territory selected_territory;

    public virtual void Setup(GameMaster _game_master, MapRenderer _map_renderer, LabelManager _label_manager) {
        game_master = _game_master;
        map_renderer = _map_renderer;
        label_manager = _label_manager;
        active = true;
        SetProcessInput(true);
    }

    public override void _Input(InputEvent e) {
        if (!active) return;
        ToggleRegions(e);
        WorldClicks(e);
    }

    public override void _Process(double delta) {
        if (!active) return;
        fdelta = (float)delta;
        CameraMovement();
        CameraZoom();
        label_manager.UpdateLabels();
    }

    void ToggleRegions(InputEvent e) {
        if (e.IsActionPressed("ToggleRegions")) {
            map_renderer.region_mode = !map_renderer.region_mode;
            GD.Print($"Region mode: {map_renderer.region_mode}");
        }
    }

    protected void SelectTerritory(Territory territory) {
        selected_territory = territory;
        game_master.SelectTerritory(selected_territory);
        if (selected_territory != null)
            GD.Print($"Selected {selected_territory.name}");
        else
            GD.Print("Unselected territories");
    }

    protected abstract void WorldClicks(InputEvent e);
    protected abstract void CameraMovement();
    protected abstract void CameraZoom();
}
