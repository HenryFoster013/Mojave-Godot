using Godot;
using System;
using System.Collections.Generic;

public class GameManager {

	private GameMaster game_master;
	public enum state_type { NULL, CLAIMANTS, PRIMARY, ENDGAME }
	public state_type game_state { get; private set; }
	private static readonly Random random = new Random();

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

	bool initial_turn;
	int sub_turn;

	// ----- // SETUP // ----- //

	public GameManager(GameMaster master) { game_master = master; }

	public void LoadJson(string json_text) {
		ResetBaseInfo();
		BuildMap(json_text);
		GeneratedTestPlayers();
		ConsolidatePlayerIds();
	}

	private void ResetBaseInfo() {
		initial_turn = true;
		current_player_turn = -1;
		total_turn = -1;
		game_state = state_type.NULL;
	}

	private void GeneratedTestPlayers() {
		players = new List<Player>();
		players.Add(new LocalPlayer(this, "Henry", Colors.Red));
		players.Add(new LocalPlayer(this, "Thomas", Colors.Blue));
		players.Add(new LocalPlayer(this, "Andre", Colors.Green));
		players.Add(new LocalPlayer(this, "Arshia", Colors.Yellow));
		GD.Print("Test players created.");
	}

	private void ConsolidatePlayerIds() {
		if (players.Count == 0)
			return;

		int initial_currency = Territories.Count / (players.Count + 1);
		
		for (int i = 0; i < players.Count; i++) {
			players[i].SetId(i);
			players[i].SetCurrency(initial_currency);
		}
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

	// ----- //  GENERIC FUNCTIONS // ----- //

	int DiceRoll() => random.Next(1,7);
	int[] GetRolls(Territory territory, bool attacking) {

		int[] rolls = {0, 0, 0};

		if (attacking) {
			if (territory.troop_count >= 4) rolls[2] = DiceRoll();
			if (territory.troop_count >= 3) rolls[1] = DiceRoll();
			if (territory.troop_count >= 2) rolls[0] = DiceRoll();
		}
		else {
			if (territory.troop_count >= 2) rolls[1] = DiceRoll();
			if(territory.troop_count >= 1) rolls[0] = DiceRoll();
		}

		Array.Sort(rolls);
		Array.Reverse(rolls);
		return rolls;
	}

	bool ZerodRoll(int[] attacking, int[] defending, int position) {
		if (attacking[position] == 0 || defending[position] == 0)
			return true;
		return false;
	}

	bool ValidatePlayer(Player player) {
		if (player == null)
			return false;
		if (player != current_player)
			return false;
		return true;
	}

	// ----- // PRIMARY // ----- //

	void StartPrimary() {
		total_turn = 0;
		sub_turn = 0;
		game_state = state_type.PRIMARY;
		game_master.LoadPrimary();
	}

	public void CashInTroops(int amount, Territory territory) {
		if (sub_turn != 0) return;
		if (!current_player.CanAfford(amount) || territory.Owner != current_player) return;
		territory.AddTroops(amount);
		GD.Print($"{current_player.name} cashed in {amount} troops at {territory.name}.");
		if (!current_player.SpareChange())
			IterateSubTurn();
	}

	public void AttackTile(Territory from_terri, Territory to_terri, bool automatic) {
		if (from_terri.Owner != current_player || to_terri.Owner == current_player) return;
		if (from_terri.troop_count <= 1) return;

		AttackRound(from_terri, to_terri);
		// Check thingy here
		
	}

	void AttackRound(Territory from_terri, Territory to_terri) {
	
		int attack_losses = 0;
		int defense_losses = 0;
		
		int[] attacking = GetRolls(from_terri, true);
		int[] defending = GetRolls(to_terri, false);
		if (ZerodRoll(attacking, defending, 0)) return;		
		
		if (attacking[0] > defending[0]) defense_losses++; else attack_losses++;
		if (!ZerodRoll(attacking, defending, 1)) {
			if (attacking[1] > defending[1]) defense_losses++; else attack_losses++;
		}
		if (!ZerodRoll(attacking, defending, 2)) {
			if (attacking[2] > defending[2]) defense_losses++; else attack_losses++;
		}
		
		from_terri.RemoveTroops(attack_losses);
		to_terri.RemoveTroops(defense_losses);
	}

	string CheckAttackState(Territory from_terri, Territory to_terri) {
		if (from_terri.troop_count < 1) return "INVALID";
		if (to_terri.troop_count < 1) return "CONQUEST";
		return "STANDARD";
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
	  sub_turn = 0;
		current_player_turn++;
		total_turn++;
		if (current_player_turn >= players.Count) {
			current_player_turn = 0;
			if (game_state == state_type.PRIMARY)
				initial_turn = false;
		}
		game_master.UpdateAllUI();
	}

	private void IterateSubTurn() {
		if (game_state != state_type.PRIMARY)
			return;
		sub_turn++;
		if (sub_turn > 2 || initial_turn)
			IterateTurn();
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
		if (!initial_turn)
			current_player.AddCurrency(CalculatePlayerProfit(current_player));
		game_master.UpdateAddTroopPlacementText();
		game_master.ActivateTurnPopup();
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

		foreach (Region region in regions.Values) {
			if (region.complete) {
				if (region.owner == player) {
					result += region.bonus;
				}
			}
		}
		
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
