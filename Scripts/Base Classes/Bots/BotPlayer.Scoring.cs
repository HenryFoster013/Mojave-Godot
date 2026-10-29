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
		var foreign = region.Territories.Where(t => t.Owner != this && t.Owner != null).ToList();
		if (foreign.Count == 0) return 0;
		return foreign.GroupBy(t => t.Owner).Max(g => (float)g.Count() / totalTerritories);
	}

	private float CalculateThreatScore(Territory territory) {
		
		/*  Determines mathematically how threatened an individual tile is.
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
		if (OwnsWholeRegion(territory.region))
			bonus_mod = IP_HOLD_BONUS_THREAT + (IP_BONUS_THREAT_WEIGHT * territory.region.bonus);
		else
			bonus_mod = IP_PARTIAL_BONUS_THREAT * CalculateRegionOwnershipScore(territory.region);
		
		Dictionary<Player, int> owner_dict = [];
		float total_offensive_troops = 0;

		foreach (Territory neighbour in territory.neighbours) {
			if (neighbour.Owner != this) {
				foreign_neighbours++;
				if (owner_dict.TryGetValue(neighbour.Owner, out int value))
					owner_dict[neighbour.Owner] = ++value;
				else
					owner_dict[neighbour.Owner] = 1;
				if (neighbour.troop_count > 1)
					total_offensive_troops += neighbour.troop_count - 1;
			}
		}
		if(owner_dict.Count != 0)
			foreign_neighbours_cohesion = (float)owner_dict.Average(kvp => kvp.Value) / territory.neighbours.Count;
		float troop_gradient = total_offensive_troops - territory.troop_count;
		
		return (foreign_neighbours * foreign_neighbours_cohesion) + bonus_mod + troop_gradient;
	}

	private bool OwnsWholeRegion(Region region) {
		return region.Territories.Count > 0 && region.Territories.All(t => t.Owner == this);
	}

	private float CalculateRegionOwnershipScore(Region region) {
		if (region.Territories.Count == 0) return 0;
		return (float)region.Territories.Count(t => t.Owner == this) / region.Territories.Count;
	}

	private float CalculateRegionPriorityScore(Region region) {

		/*  How much we want to invest in a region.
	    	Accounts for:
				- How much of the region we own
				- A flat bonus if we hold all of it
				- Smaller regions are easier to take and hold
				- Low troops per owned tile means the region is vulnerable (higher priority)
	    	Regions we own nothing in score 0.
		*/

		List<Territory> owned = [.. region.Territories.Where(t => t.Owner == this)];
		if (owned.Count == 0)
			return 0;

		float ownership = (float)owned.Count / region.Territories.Count;
		float held_bonus = OwnsWholeRegion(region) ? REGION_HELD_BONUS : 0f;
		float smallness = REGION_SMALLNESS_WEIGHT / region.Territories.Count;
		float avg_troops = (float)owned.Sum(t => t.troop_count) / owned.Count;
		float low_troop = REGION_LOW_TROOP_WEIGHT * ownership / Math.Max(1f, avg_troops);
		float bonus_value = REGION_BONUS_WEIGHT * region.bonus / region.Territories.Count;

		return (ownership * REGION_OWNERSHIP_WEIGHT) + held_bonus + smallness + low_troop + bonus_value;
	}
}