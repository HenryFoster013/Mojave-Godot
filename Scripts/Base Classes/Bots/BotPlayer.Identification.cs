namespace Mojave;

public partial class BotPlayer : Player {

	/*
	 Returns a random territory owned by this bot.
	 Used by:
	 - PlaceRandom; requires the bot to own at least one territory.
	 */
	private Territory GetRandomOwnedTerritory() {
		List<Territory> possible_territories = [.. manager.GetPlayerTerritories(this)];
		return possible_territories[random.Next(possible_territories.Count)];
	}

	/*
	 Returns a random unclaimed territory.
	 Used by:
	 - ClaimRandom; requires at least one free territory.
	 */
	private Territory GetRandomFreeTerritory() {
		List<Territory> null_territories = [.. manager.GetFreeTerritories()];
		return null_territories[random.Next(null_territories.Count)];
	}

	/*
	 Finds this bot's territories bordering another owner.
	 Used by:
	 - IdentifyThreatenedBorderTerritories as the defensive candidate set.
	 */
	private List<Territory> IdentifyBorderTerritories() {
		return [.. manager.GetPlayerTerritories(this).Where(t => t.neighbours.Any(n => n.Owner != t.Owner))];
	}

	/*
	 Orders this bot's border territories from most to least threatened.
	 Used by:
	 - GetThreatenedBorderTerritory to select the highest-risk candidate.
	 */
	private List<Territory> IdentifyThreatenedBorderTerritories() {
		return [.. IdentifyBorderTerritories().OrderByDescending(t => CalculateThreatScore(t))];
	}

	/*
	 Selects the most threatened border territory, or returns null when none exist.
	 Used by:
	 - PlacementDefensive; check for null before placing.
	 */
	private Territory GetThreatenedBorderTerritory() {
		List<Territory> threatened = IdentifyThreatenedBorderTerritories();
		if (threatened.Count > 0)
			return threatened[0];
		return null;
	}

	/*
	 Ranks this bot's territories for strategic centrality.
	 Used by:
	 - InitialPlacementStarted to cache the top candidate for consolidation.
	 */
	private List<Territory> IdentifyCentralTerritories() {

		return [.. manager.GetPlayerTerritories(this)
			.OrderByDescending(territory => CalculateRegionPriorityScore(territory.region))
			.ThenByDescending(territory => CalculateOwnedNeighbourScore(territory, 1))
			.ThenByDescending(territory => CalculateOwnedNeighbourScore(territory, 2))
			.Take(CENTRAL_TERRITORIES)];
	}

	/*
	 Orders potential invasion source-target pairs by strategic value.
	 Used by:
	 - ChooseAttack before it filters out attacks that are not viable.
	 */
	private List<(Territory source, Territory target)> IdentifyInvasionTargets() {
		return [.. manager.GetPlayerTerritories(this)
			.SelectMany(t => t.neighbours
				.Where(n => n.Owner != this)
				.Select(n => (source: t, target: n)))
			.OrderByDescending(pair => IsHeldByOpponent(pair.target.region))
			.ThenByDescending(pair => CalculateRegionCompletionScore(pair.target.region))
			.ThenByDescending(pair => pair.source.troop_count - pair.target.troop_count)];
	}

	/*
	 Checks whether a single opponent owns every territory in a region.
	 Used by:
	 - IdentifyInvasionTargets and CalculateAttackScore to prioritize region-breaking attacks.
	 */
	private bool IsHeldByOpponent(Region region) {
		Player first = region.Territories.FirstOrDefault()?.Owner;
		return first != null && first != this && region.Territories.All(t => t.Owner == first);
	}

	/*
	 Selects the first consolidation target, or returns null when none exist.
	 Used by:
	 - ClaimConsolidation with claimancy_mode set to true.
	 */
	public Territory GetConsolidationTarget(bool claimancy_mode) {
		List<Territory> targets = GetAllConsolidationTargets(claimancy_mode);
		if (targets.Count > 0)
			return targets[0];
		return null;
	}

	/*
	 Builds ordered target candidates adjacent to this bot's territories.
	 Used by:
	 - GetConsolidationTarget with true for unclaimed claim targets.
	 - GetReinforcementTargets with false for territories not owned by this bot.
	 */
	public List<Territory> GetAllConsolidationTargets(bool claimancy_mode) {

		List<Territory> priority_list = [.. manager.GetPlayerTerritories(this)
			.GroupBy(t => t.region)
			.OrderByDescending(g => CalculateRegionPriorityScore(g.Key))
			.ThenByDescending(g => g.Count())
			.SelectMany(g => GameManager.Shuffle(g.ToList()))];

		List<Territory> potential_targets = [];
		HashSet<Territory> touched_territories = [];

		/*
		 Adds an eligible territory only once.
		 Used by:
		 - GetAllConsolidationTargets while collecting adjacent candidates.
		 */
		void TestAdd(Territory territory) {
			if (touched_territories.Add(territory)) {
				if((claimancy_mode && territory.Owner == null) || (!claimancy_mode && territory.Owner != this))
					potential_targets.Add(territory);
			}
		}

		foreach (Territory territory in priority_list) {
			foreach (Territory neighbour in GameManager.Shuffle(territory.neighbours.ToList())) {
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

	/*
	 Finds owned neighbors of consolidation targets and ranks them by region priority.
	 Used by:
	 - GetReinforcementTarget to select a placement source.
	 */
	public List<Territory> GetReinforcementTargets() {

		List<Territory> consolidation_targets = GetAllConsolidationTargets(false);
		List<Territory> potential_reinforcements = [];
		HashSet<Territory> touched_territories = [];

		foreach (Territory target in consolidation_targets){

			IEnumerable<Territory> owned_neighbours = target.neighbours
				.Where(n => n.Owner == this)
				.OrderBy(n => n.troop_count);

			foreach (Territory neighbour in owned_neighbours) {
				if (touched_territories.Add(neighbour))
					potential_reinforcements.Add(neighbour);
			}
		}

		return [.. potential_reinforcements.OrderByDescending(t => CalculateRegionPriorityScore(t.region))];
	}

	/*
	 Selects the highest-ranked reinforcement source, or returns null when none exist.
	 Used by:
	 - PlacementOffensive; check for null before placing.
	 */
	public Territory GetReinforcementTarget() {
		List<Territory> targets = GetReinforcementTargets();
		if (targets.Count > 0)
			return targets[0];
		return null;
	}
}
