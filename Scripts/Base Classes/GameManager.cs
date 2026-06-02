using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using static RiskUtils;

public class GameManager {

	public State game_state { get; private set; }
	public SubTurn sub_turn { get; private set; }
	private static readonly Random random = new Random();

	public int current_player_turn { get; private set; }
	public int total_turn { get; private set; }
	private List<Player> players = new();
	public Player current_player => current_player_turn > -1 ? players[current_player_turn] : null;

	private readonly Dictionary<Player, HashSet<Territory>> player_territories = new();
	private HashSet<Territory> free_territories;

	private readonly Dictionary<string, Territory> territories = new();
	private readonly Dictionary<string, Territory> territories_id = new();
	private readonly Dictionary<string, Region> regions = new();

	public IReadOnlyDictionary<string, Territory> Territories => territories;
	public IReadOnlyDictionary<string, Territory> Territories_ID => territories_id;
	public IReadOnlyDictionary<string, Region> Regions => regions;

	public event Action OnUIUpdate;
	public event Action OnClaimancy;
	public event Action OnInitialPlacement;
	public event Action OnPrimary;
	public event Action OnClaimantsTurn;
	public event Action OnInitialPlacementTurn;
	public event Action OnPrimaryTurn;
	public event Action<Territory, TerritoryChangeType> OnTerritoryCountChanged;
	public event Action<string> OnLog;

	public bool local_turn => current_player.type == PlayerType.LOCAL;
	int init_placement_count, init_placement_max, init_base_troops, init_mult_troops;

	public int territories_per_troop { get; private set; }
	public string map_name { get; private set; }
	public string map_author { get; private set; }

	// ----- // SETUP // ----- //

	public void LoadJson(string json_text) {
		ResetBaseInfo();
		BuildMap(json_text);
		GenerateTestPlayers();
		ConsolidatePlayerIds();
	}

	private void ResetBaseInfo() {
		current_player_turn = -1;
		total_turn = -1;
		game_state = State.NULL;
	}

	private void GenerateTestPlayers() {
		players = new List<Player>();
		//players.Add(new LocalPlayer(this, "Henry", COLOUR_LEGION_RED));
		players.Add(new BotPlayer(this, "Chuck", COLOUR_LEGION_RED));
		players.Add(new BotPlayer(this, "Thomas", COLOUR_QUANTUM_BLUE));
		players.Add(new BotPlayer(this, "Andre", COLOUR_NUCLEAR_GREEN));
		players.Add(new BotPlayer(this, "Arshia", COLOUR_SAND_YELLOW));
		OnLog?.Invoke("Test players created.");
	}

	private void ConsolidatePlayerIds() {
		if (players.Count == 0)
			return;

		player_territories.Clear();
		free_territories= new HashSet<Territory>(territories.Values);

		int initial_currency = Territories.Count / (players.Count + 1);
		
		for (int i = 0; i < players.Count; i++) {
			players[i].SetId(i);
			players[i].SetCurrency(initial_currency);
			player_territories[players[i]] = new HashSet<Territory>();
		}
		OnLog?.Invoke($"Players consolidated: {players}");
	}

	public void KickStart(bool random_claims) {
		ResetBaseInfo();
		StartClaimancy(random_claims);
	}

	// ----- // MAP CREATION // ----- //

	private void BuildMap(string json_text) {
		var root = JsonNode.Parse(json_text)?.AsObject() ?? throw new Exception("MapManager: failed to parse map JSON.");
		FillData(root);
	}

	private void FillData(JsonObject root) {

		map_name = root["metadata"]?["name"]?.GetValue<string>() ?? "Unknown";
		map_author = root["metadata"]?["author"]?.GetValue<string>() ?? "Unknown";
		territories_per_troop = root["metadata"]?["territoriesPerTroop"]?.GetValue<int>() ?? DEFAULT_TERRITORIES_PER_TROOP;
		init_base_troops = root["metadata"]?["initialBaseTroops"]?.GetValue<int>() ?? DEFAULT_BASE_INIT_TROOPS;
		init_mult_troops = root["metadata"]?["initialTroopDeduction"]?.GetValue<int>() ?? DEFAULT_MULT_INIT_TROOPS;

		foreach (var entry in root["regions"].AsArray()) {
			var region = Region.FromJson(entry.AsObject());
			regions[region.id] = region;
		}

		foreach (var entry in root["tiles"].AsArray()) {
			var territory = Territory.FromJson(entry.AsObject());
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

	// ----- //  GENERIC FUNCTIONS // ----- //

	int DiceRoll() => random.Next(0, 6) + 1;
	int[] GetRolls(Territory territory, bool attacking) {

		int[] rolls = {0, 0, 0};

		if (attacking) {
			int working_troops = territory.troop_count - MIN_TROOPS;
			if (working_troops > 2) rolls[2] = DiceRoll();
			if (working_troops > 1) rolls[1] = DiceRoll();
			if (working_troops > 0) rolls[0] = DiceRoll();
		}
		else {
			if (territory.troop_count > 1) rolls[1] = DiceRoll();
			if(territory.troop_count > 0) rolls[0] = DiceRoll();
		}

		Array.Sort(rolls);
		Array.Reverse(rolls);
		return rolls;
	}

	bool IsRollEmpty(int[] attacking, int[] defending, int position) {
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

	// ----- // CLAIMANCY // ----- //

	void StartClaimancy(bool random_claims) {
		total_turn = 0;
		if (random_claims) {
			OnLog?.Invoke("Assigning Random Claims.");
			AssignRandomClaims();
			return;
		}
		OnLog?.Invoke("Starting Claimancy.");
		game_state = State.CLAIMANTS;
		OnClaimancy?.Invoke();
		LoadTurn();
	}

	private void ClaimTerritory(Player player, Territory territory) {
		SetTerritoryOwnership(player, territory);
		territory.SetTroops(MIN_TROOPS);
		OnTerritoryCountChanged?.Invoke(territory, TerritoryChangeType.CLAIM);
		OnLog?.Invoke($"{player.name} claimed {territory.name}.");
	}

	private void SetTerritoryOwnership(Player player, Territory territory){

		if (territory.Owner == null)
			free_territories.Remove(territory);
		else
			player_territories[territory.Owner].Remove(territory);
		
		territory.Owner = player;
		player_territories[player].Add(territory);
		territory.region?.CheckCompletion();
	}

	private void AssignRandomClaims() {

		OnLog?.Invoke("Assigning random claims.");

		var shuffled = Shuffle(new List<Territory>(territories.Values));
		for (int i = 0; i < shuffled.Count; i++) {
			IterateTurn();
			ClaimTerritory(current_player, shuffled[i]);
		}

		// Commenting here to 'skip forward' in the game lols
		StartInitialPlacement();
		//StartPrimary();
		LoadTurn();
	}

	private bool AllClaimed() => free_territories.Count == 0;

	// ----- // INITIAL PLACEMENT // ----- //

	private void StartInitialPlacement(){
		total_turn = 0;
		current_player_turn = 0;
		init_placement_max = players.Count * (init_base_troops - (init_mult_troops * players.Count));
		init_placement_count = 0;
		sub_turn = SubTurn.PLACE;
		game_state = State.INITIAL_PLACEMENT;
		OnInitialPlacement?.Invoke();
	}

	private void InitialPlacementTerritory(Territory territory) {
		territory.AddTroops(1);
		init_placement_count++;
		OnTerritoryCountChanged?.Invoke(territory, TerritoryChangeType.PLACEMENT);
		OnLog?.Invoke($"{territory.Owner.name} placed at {territory.name}.");
	}

	private bool AllInitiallyPlaced() => init_placement_count >= init_placement_max;

	// ----- // PRIMARY // ----- //

	void StartPrimary() {
		total_turn = 0;
		sub_turn = SubTurn.PLACE;
		game_state = State.PRIMARY;
		OnPrimary?.Invoke();
	}

	public void CashInTroops(int amount, Territory territory) {
		if (sub_turn != SubTurn.PLACE) return;
		if (!current_player.CanAfford(amount) || territory.Owner != current_player) return;
		territory.AddTroops(amount);
		OnLog?.Invoke($"{current_player.name} cashed in {amount} troops at {territory.name}.");
		if (!current_player.SpareChange())
			IterateSubTurn();
	}

	public void AttackTile(Territory from_terri, Territory to_terri) {
		if (from_terri.Owner != current_player || to_terri.Owner == current_player) return;
		if (from_terri.troop_count <= MIN_TROOPS) return;
		if (!from_terri.IsAdjacentTo(to_terri)) return;

		AttackRound(from_terri, to_terri);
		
		// Finish this
		switch (CheckAttackState(from_terri, to_terri)) {
			case AttackResult.STANDARD:
				break;
			case AttackResult.INVALID:
				break;
			case AttackResult.CONQUEST:
				break;
		}
	}

	void AttackRound(Territory from_terri, Territory to_terri) {
	
		int attack_losses = 0;
		int defense_losses = 0;
		
		int[] attacking = GetRolls(from_terri, true);
		int[] defending = GetRolls(to_terri, false);
		if (IsRollEmpty(attacking, defending, 0)) return;		
		
		if (attacking[0] > defending[0]) defense_losses++; else attack_losses++;
		if (!IsRollEmpty(attacking, defending, 1)) {
			if (attacking[1] > defending[1]) defense_losses++; else attack_losses++;
		}
		if (!IsRollEmpty(attacking, defending, 2)) {
			if (attacking[2] > defending[2]) defense_losses++; else attack_losses++;
		}
		
		from_terri.RemoveTroops(attack_losses);
		to_terri.RemoveTroops(defense_losses);
	}

	AttackResult CheckAttackState(Territory from_terri, Territory to_terri) {
		if (from_terri.troop_count < 1) return AttackResult.INVALID;
		if (to_terri.troop_count < 1) return AttackResult.CONQUEST;
		return AttackResult.STANDARD;
	}

	// ----- // TURNS // ----- //

	private void LoadTurn() {
		if (players.Count == 0)
			return;
		IterateTurn();
		switch (game_state) {
			case State.CLAIMANTS: ClaimantsTurn(); break;
			case State.INITIAL_PLACEMENT: InitialPlacementTurn(); break;
			case State.PRIMARY: PrimaryTurn(); break;
		}
		OnUIUpdate?.Invoke();
	}

	private void IterateTurn() {
		sub_turn = SubTurn.PLACE;
		current_player_turn++;
		total_turn++;

		if (current_player_turn >= players.Count) {
			current_player_turn = 0;
		}

		OnUIUpdate?.Invoke();
	}

	private void IterateSubTurn() {
		if (game_state != State.PRIMARY)
			return;

		switch (sub_turn) {
			case SubTurn.PLACE: sub_turn = SubTurn.ATTACK; break;
			case SubTurn.ATTACK: sub_turn = SubTurn.FORTIFY; break;
			case SubTurn.FORTIFY: IterateTurn(); break;
		}

		OnUIUpdate?.Invoke();
	}

	private void ClaimantsTurn() {
		OnClaimantsTurn?.Invoke();
		if (AllClaimed()) {
			StartInitialPlacement();
			return;
		}
		OnLog?.Invoke($"{current_player.name}'s turn to claim.");
		current_player.RequestClaim();
	}

	private void InitialPlacementTurn() {
		OnInitialPlacementTurn?.Invoke();
		if (AllInitiallyPlaced()) {
			StartPrimary();
			return;
		}
		current_player.RequestPlacement();
	}

	private void PrimaryTurn() {
		OnPrimaryTurn?.Invoke();
		current_player.AddCurrency(CalculatePlayerProfit(current_player));
	}

	// ----- // SPOKEN FROM PLAYERS // ----- //

	// Claims //

	public bool SpeakClaim(Territory territory) {
		if (!local_turn)
			return ErrorWrapper("SpeakClaim Fail: Attempted to claim locally from external source");
		return SpeakClaim(current_player, territory);
	}

	public bool SpeakClaim(Player player, Territory territory) {
		if (game_state != State.CLAIMANTS)
			return ErrorWrapper($"SpeakClaim Fail: Game is not in the Claimants State. [{player.name}]");
		if(territory == null)
			return ErrorWrapper($"SpeakClaim Fail: Territory is null. [{player.name}]");
		if (player != players[current_player_turn])
			return ErrorWrapper($"SpeakClaim Fail: It is not our turn. [{player.name}]");
		if (territory.Owner != null)
			return ErrorWrapper($"SpeakClaim Fail: Territory is owned. [{player.name}], Territory: [{territory.name}], [{territory.Owner.name}]");
		ClaimTerritory(player, territory);
		LoadTurn();
		return true;
	}

	// Placement //

	public bool SpeakPlacement(Territory territory) {
		if (!local_turn)
			return ErrorWrapper("SpeakPlacement Fail: .");
		return SpeakPlacement(current_player, territory);
	}

	public bool SpeakPlacement(Player player, Territory territory) {
		if (game_state != State.INITIAL_PLACEMENT)
			return ErrorWrapper($"SpeakPlacement Fail: Game is not in Initial Placments state. [{player.name}]");
		if (AllInitiallyPlaced())
			return ErrorWrapper($"SpeakPlacement Fail: All initial placements completed. [{player.name}]");
		if(territory == null)
			return ErrorWrapper($"SpeakPlacement Fail: The territory is null. [{player.name}]");
		if (player != players[current_player_turn])
			return ErrorWrapper($"SpeakPlacement Fail: It is not our turn. [{player.name}]");
		if (territory.Owner != player)
			return ErrorWrapper($"SpeakPlacement Fail: Territory is not owned by us. [{player.name}]");
		InitialPlacementTerritory(territory);
		LoadTurn();
		return true;
	}

	// ----- // GENERIC METHODS // ----- //

	public List<T> Shuffle<T>(List<T> list) {
		for (int i = list.Count - 1; i > 0; i--) {
			int j = random.Next(i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
		return list;
	}

	public bool ErrorWrapper(string message) {
		OnLog?.Invoke(message);
		return false;
	}

	// ----- // GETTERS AND SETTERS // ----- //

	// --- // Get Methods // --- //

	private int CalculatePlayerProfit(Player player) {

		int result = Math.Max(GetPlayerTerritories(player).Count / territories_per_troop, 3);

		foreach (Region region in regions.Values) {
			if (region.complete) {
				if (region.owner == player) {
					result += region.bonus;
				}
			}
		}
		
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

	public int GetRemainingPlacementsPerPlayer(){ // Not a true metric, just used by UI
		return (init_placement_max / players.Count) - ((total_turn - 1) / players.Count);
	}

	// --- // Territory List Processing // --- //

	public IReadOnlyCollection<Territory> GetPlayerTerritories(Player player) {
		return player_territories.TryGetValue(player, out var set) ? set : Array.Empty<Territory>();
	}

	public IReadOnlyCollection<Territory> GetMissingRegionPieces(Player player) {
		
		var result =  new HashSet<Territory>();
		var owned = player_territories[player];

		foreach (Region region in regions.Values) {
			Territory missing = null;
			foreach (Territory territory in region.Territories) {
				if (!owned.Contains(territory)) {
					if (missing != null) { 
						missing = null; 
						break; 
					}
					missing = territory;
				}
			}
			if (missing != null)
				result.Add(missing);
		}

		return result;
	}

	public IReadOnlyCollection<Territory> GetOtherMissingRegionPieces(Player player) {

		var result =  new HashSet<Territory>();
		foreach (Player other_player in players) {
			if (other_player != player && other_player != null){
				foreach (Territory territory in GetMissingRegionPieces(other_player)) 
					result.Add(territory);
			}
		}

		return result;
	}

	public IReadOnlyCollection<Territory> GetFreeTerritories() => free_territories;
}
