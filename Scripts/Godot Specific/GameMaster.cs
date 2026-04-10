using Godot;
using System.Collections.Generic;

public partial class GameMaster : Node {

	[Export] public string map_json_path = "res://Board/map_data.json";
	[Export] public MapRenderer map_renderer;

	private GameManager manager;
	public IReadOnlyDictionary<string, Territory> Territories => manager.Territories;
	public IReadOnlyDictionary<string, Region> Regions => manager.Regions;

	// Start //

	public override void _Ready() {

		manager = new GameManager(this);
		if (!FileAccess.FileExists(map_json_path)) {
			GD.PrintErr($"GameMaster: map file not found at '{map_json_path}'.");
			return;
		}

		string json_text = FileAccess.GetFileAsString(map_json_path);
		manager.Load(json_text);
		GD.Print($"GameMaster: loaded {Regions.Count} regions and {Territories.Count} territories.");
	}

	// Selection Calls //

	public void SelectTerritory(Territory territory) {

		map_renderer.SelectTerritory(territory);
		
		if (territory == null)
			return;
		if (manager.game_state == GameManager.state_type.CLAIMANTS) {
			manager.LocalClaim(territory);
		}
	}

	// Scene Transitions //

	public void LoadLobby() { }
	public void LoadClaimants() { }
	public void LoadPrimary() { }

	// Get Methods //

	public Territory GetTerritoryByColour(string colour) => manager.GetTerritoryByColour(colour);
	public Territory GetTerritoryByID(string id) => manager.GetTerritoryByID(id);
	public Region GetRegion(string id) => manager.GetRegion(id);
}
