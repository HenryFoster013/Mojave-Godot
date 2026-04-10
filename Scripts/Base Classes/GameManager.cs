using Godot;
using System.Collections.Generic;

public class GameManager {

	private GameMaster game_master;
	public int game_state { get; private set; }
	/*
        -1 = none, just idle
        0 = lobby
        1 = claimants
        2 = primary loop
        3 = game end
    */

	public int current_player_turn { get; private set; }
	private List<Player> players = new List<Player>();

	private readonly Dictionary<string, Territory> territories = new();
	private readonly Dictionary<string, Territory> territories_id = new();
	private readonly Dictionary<string, Region> regions = new();

	public IReadOnlyDictionary<string, Territory> Territories => territories;
	public IReadOnlyDictionary<string, Territory> Territories_ID => territories_id;
	public IReadOnlyDictionary<string, Region> Regions => regions;

	// ----- // SETUP // ----- //

	public GameManager() { }
	public GameManager(GameMaster master) {game_master = master;}

	public void Load(string json_text) {
		BuildMap(json_text);
		GeneratedTestPlayers();
		LoadGameState(1);
	}

	// Map Creation //

	void BuildMap(string json_text) {
		var parsed = Json.ParseString(json_text);
		if (parsed.VariantType != Variant.Type.Dictionary)
			throw new System.Exception("MapManager: failed to parse map JSON.");
		var root = parsed.AsGodotDictionary();
		FillData(root);
	}

	private void FillData(Godot.Collections.Dictionary root) {

		foreach (var entry in root["regions"].AsGodotArray()) {
			var region = Region.FromJson(entry.AsGodotDictionary());
			regions[region.id] = region;
		}

		foreach (var entry in root["tiles"].AsGodotArray()) {
			var territory = Territory.FromJson(entry.AsGodotDictionary());
			territories[territory.map_colour] = territory;
			territories_id[territory.id] = territory;
		}

		foreach (var territory in territories.Values) {
			foreach (var neighbour_id in territory.neighbour_ids) {
				if (territories_id.TryGetValue(neighbour_id, out var neighbour))
					territory.AddNeighbour(neighbour);
				else
					throw new System.Exception($"MapLoader: territory '{territory.id}' references unknown neighbour '{neighbour_id}'.");
			}
		}

		foreach (var region in regions.Values) {
			foreach (var territory_id in region.territory_ids) {
				if (territories_id.TryGetValue(territory_id, out var territory)) {
					region.AddTerritory(territory);
					territory.SetRegion(region);
				}
				else
					throw new System.Exception($"MapLoader: region '{region.id}' references unknown territory '{territory_id}'.");
			}
			region.CheckCompletion();
		}
	}

	// ----- // RUNTIME FUNCTIONALITY // ----- //

	void LoadGameState(int new_state) {
		if (new_state == game_state)
			return;
		game_state = new_state;
		switch (game_state) {
			case 1: LoadClaimants(); break;
		}
	}

	void GeneratedTestPlayers() {
		players = new List<Player>();
		players.Add(new LocalPlayer(this, "Henry", Colors.Red));
		players.Add(new LocalPlayer(this, "Thomas", Colors.Blue));
		players.Add(new LocalPlayer(this, "Andre", Colors.Green));
		GD.Print("Test players created.");
	}

	void ConsolidatePlayerIds() {
		if (players.Count == 0)
			return;
		for (int i = 0; i < players.Count; i++) {
			players[i].SetId(i);
		}
		GD.Print($"Players consolidated: {players}");
	}

	void LoadClaimants() {
		ConsolidatePlayerIds();
		current_player_turn = 0;
		
		if(game_master != null)
			game_master.LoadClaimants();
	}

	// ----- // GETTERS AND SETTERS // ----- //

	// Get Methods //

	public Territory GetTerritoryByColour(string colour) {
		if (colour == "#000000")
			return null;
		territories.TryGetValue(colour, out var territory);
		return territory;
	}

	public Territory GetTerritoryByID(string id) {
		territories_id.TryGetValue(id, out var territory);
		return territory;
	}

	public Region GetRegion(string id) {
		regions.TryGetValue(id, out var region);
		return region;
	}
}
