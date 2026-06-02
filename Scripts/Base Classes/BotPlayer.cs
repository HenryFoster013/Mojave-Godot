using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RiskUtils;
using static BotUtils;

public class BotPlayer : Player {

	////////////////////////////|| INSTANTIATION ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // INSTANTIATION // ----- //

	Random random = new Random();
	private List<Territory> central_territories;

	public BotPlayer(GameManager _manager, string _name, string _colour) : base(_manager, _name, _colour) { 
		type = PlayerType.BOT; 
		SubscribeToBotEvents();
	}

	void SubscribeToBotEvents() {
		manager.OnInitialPlacement += InitialPlacementStarted;
	}

	////////////////////////////|| GENERAL ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // GENERAL // ----- // 

	public Territory GetConsolidationTarget(bool claimancy_mode) {
		List<Territory> targets = GetAllConsolidationTargets(claimancy_mode);
		if (targets.Count > 0)
			return targets[0];
		return null;
	}

	public List<Territory> GetAllConsolidationTargets(bool claimancy_mode) {

		
		/*  Gets all owned territories, groups them by region and shuffles the groups internally.
			Concats each group (largest first) into a single list.
			Loops through each territory and checks if they have any unclaimed neighbours inside the same region
			Loops through again and adds any unclaimed neighbours outside of the same region
			Returns the list of potential targets.
		*/

		List<Territory> priority_list = manager.GetPlayerTerritories(this)
			.GroupBy(t => t.region)
			.OrderByDescending(g => g.Count())
			.SelectMany(g => manager.Shuffle(g.ToList()))
			.ToList();

		List<Territory> potential_targets = new();
		HashSet<Territory> touched_territories = new();

		void TestAdd(Territory territory) {
			if (touched_territories.Add(territory)) {
				if((claimancy_mode && territory.Owner == null) || (!claimancy_mode && territory.Owner != this))
					potential_targets.Add(territory);
			}
		}

		foreach (Territory territory in priority_list) {
			foreach (Territory neighbour in manager.Shuffle(territory.neighbours.ToList())) {
				if (neighbour.region == territory.region)
					TestAdd(neighbour);
			}
		}

		foreach (Territory territory in priority_list) {
			foreach (Territory neighbour in territory.neighbours)
				TestAdd(neighbour);
		}

		return potential_targets;
	}

	private int CalculateOwnedNeighbourScore(Territory territory, int depth) {

		/* Calculates how many neighbours we own, then how many neighbours of the neighbours we own.
		   Recursive, depth indicates how many sets of neighbours to count.
		*/

		if (depth < 1) 
			return 0;
		return territory.neighbours.Sum(n => (n.Owner == this ? 1 : 0) + CalculateOwnedNeighbourScore(n, depth - 1));
	}

	private void SelectCentralTerritories() {

		/* Territories are chosen based on their depth score, then their second depth score.
		   Depth score is counted twice to indicate territories reachable by multiple paths, hence it is more strategically central.
		   Selects the top few.
		*/

		central_territories = manager.GetPlayerTerritories(this)
			.OrderByDescending(territory => CalculateOwnedNeighbourScore(territory, 1))
			.ThenByDescending(territory => CalculateOwnedNeighbourScore(territory, 2))
			.Take(CENTRAL_TERRITORIES)
			.ToList();
	}


	////////////////////////////|| CLAIMS ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // REQUEST // ----- //

	public override async void RequestClaim() {

		await Task.Delay(TimeSpan.FromSeconds(BASE_DELAY));

        if (RollClaimOurMissingPiece()) return;
        if (RollClaimTheirMissingPiece()) return;
        if (RollClaimRandom()) return;
        if (RollClaimConsolidation()) return;
        ClaimDisruption();
	}

    // ----- // ROLLS // ----- //

    private bool RollClaimOurMissingPiece() {
        List<Territory> our_missing_pieces = manager.GetMissingRegionPieces(this).Where(t => t.Owner == null).ToList();
        if (our_missing_pieces.Count > 0) {
			manager.SpeakClaim(this, our_missing_pieces[random.Next(our_missing_pieces.Count)]);
			return true;
		}
        return false;
    }

    private bool RollClaimTheirMissingPiece() {
        int dice_roll = random.Next(C_TOTAL_DECISION_CHANCE);
        List<Territory> their_missing_pieces = manager.GetOtherMissingRegionPieces(this).Where(t => t.Owner == null).ToList();
        if (their_missing_pieces.Count > 0 && dice_roll < C_IMMENENT_DISRUPTION_CHANCE) {
			manager.SpeakClaim(this, their_missing_pieces[random.Next(their_missing_pieces.Count)]);
			return true;
		}
        return false;
    }

    private bool RollClaimRandom() {
        int dice_roll = random.Next(C_TOTAL_DECISION_CHANCE);
        if (manager.GetPlayerTerritories(this).Count == 0 || dice_roll < C_MISTAKE_CHANCE) {
			ClaimRandom();
			return true;
		}
        return false;
    }

    private bool RollClaimConsolidation(){
        int dice_roll = random.Next(C_TOTAL_DECISION_CHANCE);
        if (dice_roll < C_MISTAKE_CHANCE + C_CONSOLIDATION_CHANCE) {
			ClaimConsolidation();
			return true;
		}
        return false;
    }

    // ----- // CLAIMS // ----- //

	private void ClaimDisruption() {

		/* Selects a random free tile that neighbours an opponents tile.
		   Designed to break up an opponents area.
		*/

		IReadOnlyCollection<Territory> null_territories = manager.GetFreeTerritories();

		int start_pointer = random.Next(null_territories.Count);
		int current_pointer = start_pointer;
		IList<Territory> null_list = null_territories.ToList();

		do {
			Territory candidate = null_list[current_pointer];
			foreach (Territory neighbour in candidate.neighbours) {
				if (neighbour.Owner != null && neighbour.Owner != this){
					manager.SpeakClaim(this, candidate);
					return;
				}
			}
			current_pointer = (current_pointer + 1) % null_list.Count;
			
		} while (current_pointer != start_pointer);

		ClaimRandom();
	}

	private void ClaimConsolidation() {
		Territory target = GetConsolidationTarget(true);
		if(target != null) {
			manager.SpeakClaim(this, target);
			return;
		}
		ClaimRandom();
	}

	private void ClaimRandom() {
		List<Territory> null_territories = manager.GetFreeTerritories().ToList();
		manager.SpeakClaim(this, null_territories[random.Next(null_territories.Count)]);
	}

	////////////////////////////|| PLACEMENTS ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // PLACEMENTS // ----- //

	private void InitialPlacementStarted() {

	}

	public override void RequestPlacement() {

	}

	////////////////////////////|| PLAYS ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // PLAYS // ----- //

	public override void RequestPlay() { 
		
	}
}
