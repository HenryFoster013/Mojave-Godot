namespace Mojave;

public partial class BotPlayer : Player {

	/*
	 Counts this bot's neighbors recursively to the requested depth.
	 Used by:
	 - IdentifyCentralTerritories with shallow depths to rank territories.
	 */
    private int CalculateOwnedNeighbourScore(Territory territory, int depth) {

		if (depth < 1) 
			return 0;
		return territory.neighbours.Sum(n => (n.Owner == this ? 1 : 0) + CalculateOwnedNeighbourScore(n, depth - 1));
	}

	/*
	 Measures the largest opponent-owned share of a region.
	 Used by:
	 - IdentifyInvasionTargets to rank region-completion opportunities.
	 */
	private float CalculateRegionCompletionScore(Region region) {
		int totalTerritories = region.Territories.Count;
		var foreign = region.Territories.Where(t => t.Owner != this && t.Owner != null).ToList();
		if (foreign.Count == 0) return 0;
		return foreign.GroupBy(t => t.Owner).Max(g => (float)g.Count() / totalTerritories);
	}

	/*
	 Scores the threat to a territory owned by this bot; higher scores mean greater exposure.
	 Used by:
	 - IdentifyThreatenedBorderTerritories to rank defensive targets.
	 - IsWorthAttacking to reject risky attack sources.
	 */
	private float CalculateThreatScore(Territory territory) {
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

	/*
	 Checks whether this bot owns every territory in a region.
	 Used by:
	 - CalculateThreatScore and CalculateRegionPriorityScore for full-control bonuses.
	 */
	private bool OwnsWholeRegion(Region region) {
		return region.Territories.Count > 0 && region.Territories.All(t => t.Owner == this);
	}

	/*
	 Returns this bot's share of a region as a proportion from zero to one.
	 Used by:
	 - CalculateThreatScore, CalculateAttackScore, and CalculateRegionPriorityScore.
	 */
	private float CalculateRegionOwnershipScore(Region region) {
		if (region.Territories.Count == 0) return 0;
		return (float)region.Territories.Count(t => t.Owner == this) / region.Territories.Count;
	}

	/*
	 Scores the strategic value of investing in a region; higher scores rank first.
	 Used by:
	 - IdentifyCentralTerritories, GetAllConsolidationTargets, and GetReinforcementTargets.
	 */
	private float CalculateRegionPriorityScore(Region region) {

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

	/*
	 Scores an attack from a source territory against a target; higher scores rank first.
	 Used by:
	 - ChooseAttack after filtering out attacks that are not viable.
	 */
	private float CalculateAttackScore(Territory source, Territory target) {

		float score = 0;

		int attackers = source.troop_count - 1;
		score += A_ADVANTAGE_WEIGHT * (attackers - target.troop_count);

		if (IsHeldByOpponent(target.region)) score += A_BREAK_BONUS_WEIGHT + (A_BREAK_REGION_VALUE_WEIGHT * target.region.bonus);

		float ownership = CalculateRegionOwnershipScore(target.region);
		score += A_COMPLETION_WEIGHT * ownership;

		int missing = target.region.Territories.Count(t => t.Owner != this);
		if (missing == 1) score += A_FINISH_REGION_BONUS;
		if (target.troop_count == 1) score += A_WEAK_DEFENDER_BONUS;

		return score;
	}

	/*
	 Checks whether an attack meets the troop and source-risk thresholds.
	 Used by:
	 - ChooseAttack to filter candidates before scoring them.
	 */
	private bool IsWorthAttacking(Territory source, Territory target) {

		if (source.troop_count < RiskUtils.MIN_TROOPS + 1) return false;

		int attackers = source.troop_count - RiskUtils.MIN_TROOPS;
		if (attackers - target.troop_count < A_MIN_TROOP_ADVANTAGE) return false;

		if (CalculateThreatScore(source) > A_MAX_SOURCE_THREAT && attackers < target.troop_count * 2) return false;

		return true;
	}

	/*
	 Estimates enemy troop exposure around a territory.
	 Used by:
	 - RequestConquestDivision to compare exposure when splitting troops after conquest.
	 */
	private float CalculateExposure(Territory territory) {
		return 1f + territory.neighbours.Where(n => n.Owner != this).Sum(n => n.troop_count - RiskUtils.MIN_TROOPS);
	}
}