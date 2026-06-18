using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static RiskUtils;
using static BotUtils;

public partial class BotPlayer : Player {

	////////////////////////////|| INSTANTIATION ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // INSTANTIATION // ----- //

	Random random = new Random();

	public BotPlayer(GameManager _manager, string _name, string _colour) : base(_manager, _name, _colour) { 
		type = PlayerType.BOT; 
		SubscribeToBotEvents();
	}

	void SubscribeToBotEvents() {
		manager.OnInitialPlacement += InitialPlacementStarted;
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
		manager.SpeakClaim(this, GetRandomFreeTerritory());
	}

	////////////////////////////|| PLACEMENTS ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // REQUESTS // ----- //

	public override async void RequestPlacement() {

		await Task.Delay(TimeSpan.FromSeconds(BASE_DELAY));

		int dice_roll = random.Next(IP_TOTAL_DECISION_CHANCE);

		if (dice_roll < IP_CONSOLIDATION_CHANCE)
			PlacementConsolidation();
		else if (dice_roll < IP_CONSOLIDATION_CHANCE + IP_DEFENSIVE_CHANCE)
			PlacementDefensive();
		else
			PlacementOffensive();
		
	}
	
	// ----- // PLACEMENTS // ----- //

	private Territory ip_buffered_central_territory;

	private void InitialPlacementStarted() {
		ip_buffered_central_territory = IdentifyCentralTerritories()[0];
	}

	private void PlacementConsolidation() {
		if(ip_buffered_central_territory != null) {
			manager.SpeakInitialPlacement(this, ip_buffered_central_territory);
			return;
		}
		PlaceRandom();
	}

	private void PlacementDefensive() {
		Territory territory = GetThreatenedBorderTerritory();
		if(territory != null) {
			manager.SpeakInitialPlacement(this, territory);
			return;
		}
		PlaceRandom();
	}

	private void PlacementOffensive()  {
		Territory territory = GetReinforcementTarget();
		if(territory != null) {
			manager.SpeakInitialPlacement(this, territory);
			return;
		}
		PlaceRandom();
	}

	private void PlaceRandom() {
		manager.SpeakInitialPlacement(this, GetRandomOwnedTerritory());
	}

	////////////////////////////|| PLAYS ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // PLAYS // ----- //

	public override void RequestPlay() { }

}
