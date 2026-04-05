using Godot;
using System.Collections.Generic;

public partial class MapMaster : Node {

    [Export] public string map_json_path = "res://data/map.json";

    private MapManager manager
    public IReadOnlyDictionary<string, Territory> Territories => manager.Territories;
    public IReadOnlyDictionary<string, Region> Regions => manager.Regions;

    public override void _Ready() {

        manager = new MapManager();
        if (!FileAccess.FileExists(map_json_path)) {
            GD.PrintErr($"MapMaster: map file not found at '{map_json_path}'.");
            return;
        }

        string json_text = FileAccess.GetFileAsString(map_json_path);
        manager.Load(json_text);
        GD.Print($"MapMaster: loaded {Regions.Count} regions and {Territories.Count} territories.");
    }

    // Get Methods //

    public Territory GetTerritory(string id) => manager.GetTerritory(id);
    public Region GetRegion(string id) => manager.GetRegion(id);
}
