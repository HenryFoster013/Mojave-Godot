using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static RiskUtils;
using static BotUtils;

public partial class BotPlayer : Player {

	private Territory GetRandomOwnedTerritory() {
		List<Territory> possible_territories = manager.GetPlayerTerritories(this).ToList();
		return possible_territories[random.Next(possible_territories.Count)];
	}

	private Territory GetRandomFreeTerritory() {
		List<Territory> null_territories = manager.GetFreeTerritories().ToList();
		return null_territories[random.Next(null_territories.Count)];
	}

	private List<Territory> IdentifyBorderTerritories() {
		return manager.GetPlayerTerritories(this)
			.Where(t => t.neighbours.Any(n => n.Owner != t.Owner))
			.ToList();
	}

	private List<Territory> IdentifyThreatenedBorderTerritories() {
		return IdentifyBorderTerritories()
			.OrderByDescending(t => CalculateThreatScore(t))
			.ToList();
	}

	private Territory GetThreatenedBorderTerritory() {
		return IdentifyThreatenedBorderTerritories()[0];
	}

	private List<Territory> IdentifyCentralTerritories() {

		/* Territories are chosen based on their depth score, then their second depth score.
		   Depth score is counted twice to indicate territories reachable by multiple paths, hence it is more strategically central.
		   Selects the top few.
		*/

		return manager.GetPlayerTerritories(this)
			.OrderByDescending(territory => CalculateOwnedNeighbourScore(territory, 1))
			.ThenByDescending(territory => CalculateOwnedNeighbourScore(territory, 2))
			.Take(CENTRAL_TERRITORIES)
			.ToList();
	}

	private List<(Territory source, Territory target)> IdentifyInvasionTargets() {
		return manager.GetPlayerTerritories(this)
			.SelectMany(t => t.neighbours
				.Where(n => n.Owner != this)
				.Select(n => (source: t, target: n)))
			.OrderByDescending(pair => pair.target.region.complete)
			.ThenByDescending(pair => CalculateRegionCompletionScore(pair.target.region))
			.ThenByDescending(pair => pair.source.troop_count - pair.target.troop_count)
			.ToList();
	}

	// ----- // CONSOLIDATION // ----- //

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

	public List<Territory> GetReinforcementTargets() {

		// Uses GetConsolidationTargets to return the best neighbour to attack from

		List<Territory> consolidation_targets = GetAllConsolidationTargets(false);
		List<Territory> potential_reinforcements = new();
		HashSet<Territory> touched_territories = new();

		foreach (Territory target in consolidation_targets){

			IEnumerable<Territory> owned_neighbours = target.neighbours
				.Where(n => n.Owner == this)
				.OrderBy(n => n.troop_count);

			foreach (Territory neighbour in owned_neighbours) {
				if (touched_territories.Add(neighbour))
					potential_reinforcements.Add(neighbour);
			}
		}

		return potential_reinforcements;
	}

	public Territory GetReinforcementTarget() {
		List<Territory> targets = GetReinforcementTargets();
		if (targets.Count > 0)
			return targets[0];
		return null;
	}
}
