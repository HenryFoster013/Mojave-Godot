namespace Mojave;

public partial class BotPlayer : Player {

	private Territory ip_buffered_central_territory;

    // ------ // ------ // ------ // REQUEST // ------ // ------ // ------ //

	public override async void RequestPlacement() {

		await Task.Delay(TimeSpan.FromSeconds(INITIAL_PLACEMENT_DELAY));
		int dice_roll = random.Next(IP_TOTAL_DECISION_CHANCE);
		
		if (dice_roll < IP_CONSOLIDATION_CHANCE)
			PlacementConsolidation();
		else if (dice_roll < IP_CONSOLIDATION_CHANCE + IP_DEFENSIVE_CHANCE)
			PlacementDefensive();
		else
			PlacementOffensive();
	}

    // ------ // ------ // ------ // FUNCTIONALITY // ------ // ------ // ------ //

	private void InitialPlacementStarted() {
		ip_buffered_central_territory = IdentifyCentralTerritories().FirstOrDefault();
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
}
