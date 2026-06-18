using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static RiskUtils;
using static BotUtils;

public partial class BotPlayer : Player {

    private int CalculateOwnedNeighbourScore(Territory territory, int depth) {

		/* Calculates how many neighbours we own, then how many neighbours of the neighbours we own.
		   Recursive, depth indicates how many sets of neighbours to count.
		*/

		if (depth < 1) 
			return 0;
		return territory.neighbours.Sum(n => (n.Owner == this ? 1 : 0) + CalculateOwnedNeighbourScore(n, depth - 1));
	}

	private float CalculateRegionCompletionScore(Region region) {
		int totalTerritories = region.Territories.Count;

		return region.Territories
			.Where(t => t.Owner != this && t.Owner != null)
			.GroupBy(t => t.Owner)
			.Max(g => (float)g.Count() / totalTerritories);
	}

	private float CalculateThreatScore(Territory territory) {
		
		/* Determines mathematically how threatened an individual tile is.
		   Accounts for:
				- The number of foreign neighbours
				- The cohesion of foreign neighbours (if they are all the same that is more threatening)
				- If breaking the territory will break a bonus
				- The cohesion of the surrounding area
				- The difference between the defending troops and potential attacking troops
		*/
		
		if (territory.Owner != this)
			return 0;
		if (territory.neighbours.Count == 0)
			return 0;

		float foreign_neighbours = 0;
		float  foreign_neighbours_cohesion = 0.5f;

		float bonus_mod = 0;
		if (territory.region.complete)
			bonus_mod = IP_HOLD_BONUS_THREAT;
		
		Dictionary<Player, int> owner_dict = new Dictionary<Player, int>();
		float total_offensive_troops = 0;

		foreach (Territory neighbour in territory.neighbours) {
			if (neighbour.Owner != this) {
				foreign_neighbours++;
				if (owner_dict.ContainsKey(neighbour.Owner))
					owner_dict[territory.Owner]++;
				else
					owner_dict[territory.Owner] = 1;
				if (neighbour.troop_count > 1)
					total_offensive_troops += neighbour.troop_count - 1;
			}
		}
		if(owner_dict.Count != 0)
			foreign_neighbours_cohesion = (float)owner_dict.Average(kvp => kvp.Value) / territory.neighbours.Count;
		float troop_gradient = territory.troop_count - total_offensive_troops;
		
		return (foreign_neighbours * foreign_neighbours_cohesion) + bonus_mod + troop_gradient;
	}
}