using Godot;
using System.Collections.Generic;

public class GameManager {

	private GameMaster game_master;
	public enum state_type { NULL, CLAIMANTS, PRIMARY, ENDGAME }
	public state_type game_state { get; private set; }

	public int current_player_turn { get; private set; }
	public int total_turn { get; private set; }
	private List<Player> players = new();
	public Player current_player => current_player_turn > -1 ? players[current_player_turn] : null;

	private readonly Dictionary<string, Territory> territories = new();
	private readonly Dictionary<string, Territory> territories_id = new();
	private readonly Dictionary<string, Region> regions = new();

	public IReadOnlyDictionary<string, Territory> Territories => territories;
	public IReadOnlyDictionary<string, Territory> Territories_ID => territories_id;
	public IReadOnlyDictionary<string, Region> Regions => regions;

	// ----- // SETUP // ----- //

	public GameManager(GameMaster master) { game_master = master; }

	public void LoadJson(string json_text) {
		ResetBaseInfo();
		BuildMap(json_text);
		GeneratedTestPlayers();
		ConsolidatePlayerIds();
	}

	private void ResetBaseInfo() {
		current_player_turn = -1;
		total_turn = -1;
		game_state = state_type.NULL;
	}

	private void GeneratedTestPlayers() {
		players = new List<Player>();
		players.Add(new LocalPlayer(this, "Henry", Colors.Red));
		players.Add(new LocalPlayer(this, "Thomas", Colors.Blue));
		players.Add(new LocalPlayer(this, "Andre", Colors.Green));
		GD.Print("Test players created.");
	}

	private void ConsolidatePlayerIds() {
		if (players.Count == 0)
			return;
		for (int i = 0; i < players.Count; i++)
			players[i].SetId(i);
		GD.Print($"Players consolidated: {players}");
	}

	public void KickStart(bool random_claims) {
		ResetBaseInfo();
		StartClaimancy(random_claims);
	}

	// ----- // MAP CREATION // ----- //

	private void BuildMap(string json_text) {
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

	// ----- // CLAIMANCY // ----- //

	void StartClaimancy(bool random_claims) {
		total_turn = 0;
		if (random_claims) {
			GD.Print("Assigning Random Claims.");
			AssignRandomClaims();
			return;
		}
		GD.Print("Starting Claimancy.");
		game_state = state_type.CLAIMANTS;
		game_master.LoadClaimants();
		LoadTurn();
	}

	private void ClaimTerritory(Player player, Territory territory) {
		territory.Owner = player;
		territory.SetTroops(1);
		game_master.label_manager.UpdateTroopCount(territory);
		GD.Print($"{player.name} claimed {territory.name}.");
	}

	private void AssignRandomClaims() {

		GD.Print("Assigning random claims.");

		var shuffled = Shuffle(new List<Territory>(territories.Values));
		for (int i = 0; i < shuffled.Count; i++) {
			IterateTurn();
			ClaimTerritory(current_player, shuffled[i]);
		}

		StartPrimary();
		LoadTurn();
	}

	private bool AllClaimed() {
		foreach (Territory territory in territories.Values) {
			if (territory.Owner == null)
				return false;
		}
		return true;
	}

	// ----- // PRIMARY // ----- //

	void StartPrimary() {
		total_turn = 0;
		game_state = state_type.PRIMARY;
		game_master.LoadPrimary();
	}

	// ----- // TURNS // ----- //

	private void LoadTurn() {
		if (players.Count == 0)
			return;
		IterateTurn();
		switch (game_state) {
			case state_type.CLAIMANTS: ClaimantsTurn(); break;
			case state_type.PRIMARY: PrimaryTurn(); break;
		}
	}

	private void IterateTurn() {
		current_player_turn++;
		total_turn++;
		if (current_player_turn >= players.Count)
			current_player_turn = 0;
		game_master.UpdateAllUI();
	}

	private void ClaimantsTurn() {
		if (AllClaimed()) {
			StartPrimary();
			return;
		}
		GD.Print($"{current_player.name}'s turn to claim.");
		current_player.RequestClaim();
	}

	private void PrimaryTurn() {
		game_master.ActivateTurnPopup();
		current_player.AddCurrency(CalculatePlayerProfit(current_player));
	}

	// ----- // SPOKEN FROM PLAYERS // ----- //

	public bool SpeakClaim(Territory territory) {
		if (current_player.type != Player.player_type.LOCAL)
			return false;
		return SpeakClaim(current_player, territory);
	}

	public bool SpeakClaim(Player player, Territory territory) {
		if (player != players[current_player_turn])
			return false;
		if (territory.Owner != null)
			return false;
		ClaimTerritory(player, territory);
		LoadTurn();
		return true;
	}

	// ----- // GENERIC METHODS // ----- //

	public List<T> Shuffle<T>(List<T> list) {
		var rng = new System.Random();
		for (int i = list.Count - 1; i > 0; i--) {
			int j = rng.Next(i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
		return list;
	}

	// ----- // GETTERS AND SETTERS // ----- //

	// Get Methods //

	private int CalculatePlayerProfit(Player player) {
		int total = 0;
		foreach (Territory territory in territories.Values) {
			if (territory.Owner == player) {
				total++;
			}
		}
		int result = total / 3;
		if (result < 1)
			return 1;
		return result;
	}

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
