namespace Mojave;

/*
    The following code is heavily commented,
    not because it is AI slop but because it is going to get complex v fast and I'm struggling to keep track of it.
*/

public partial class BotPlayer : Player {
    
    private List<BotObjective> current_objectives = new();

	private void CollectObjectives(int troop_budget) {
        current_objectives = [];
        current_objectives.AddRange(CollectConquerContinentOpportunities());
		current_objectives.AddRange(CollectConquerPlayerOpportunities());
		current_objectives.AddRange(CollectSettleOpportunities());
		current_objectives.AddRange(CollectBreakContinentOpportunities());
		current_objectives.AddRange(CollectWeakenPlayerOpportunities());
		current_objectives.AddRange(CollectInvokeChaosOpportunities());
		current_objectives.AddRange(CollectPreventContinentCompletionOpportunities());
		current_objectives.AddRange(CollectCompleteContinentOpportunities());
		current_objectives.AddRange(CollectBuildReserveOpportunities());
		current_objectives.AddRange(CollectBuildOpposingForceOpportunities());
		current_objectives.AddRange(CollectPocketContinentOpportunities());

        int spare_budget = 0;

        // Here

    }

    // ----- // ----- // COLLECT OPPORTUNITIES // ----- // ----- //

    private List<BotObjective> CollectConquerContinentOpportunities() {
        return [];
    }

	private List<BotObjective> CollectConquerPlayerOpportunities() {
		return [];
	}

	private List<BotObjective> CollectSettleOpportunities() {
		return [];
	}

	private List<BotObjective> CollectBreakContinentOpportunities() {
		return [];
	}

	private List<BotObjective> CollectWeakenPlayerOpportunities() {
		return [];
	}

	private List<BotObjective> CollectInvokeChaosOpportunities() {
		return [];
	}

	private List<BotObjective> CollectPreventContinentCompletionOpportunities() {
		return [];
	}

	private List<BotObjective> CollectCompleteContinentOpportunities() {
		return [];
	}

	private List<BotObjective> CollectBuildReserveOpportunities() {
		return [];
	}

	private List<BotObjective> CollectBuildOpposingForceOpportunities() {
		return [];
	}

	private List<BotObjective> CollectPocketContinentOpportunities() {
		return [];
	}
    
}

public class BotObjective {

	public enum ObjectiveType {
		ConquerContinent, // There is an opportunity to secure a continent bonus
		ConquerPlayer, // There is an opportunity to wipe a player off the map
		Settle, // The player owns an area of land that is sparesely defended (particularily if its a continent)
		BreakContinent, // The player has an opportunity to break a bots completion bonus
		WeakenPlayer, // The player borders / has an easy route to a powerful opponent that it can break several bonuses / destroy pockets from
		InvokeChaos, // The player has the opportunity to march in one long line to disrupt several continents
		PreventContinentCompletion, // The player holds land in a continent that is under threat and should place forces there to prevent an opponent getting the bonus
		CompleteContinent, // The player has an opportunity to complete a continent
		BuildReserve, // The player has relatively low reserves
		BuildOpposingForce, // The player sees an opportunity to build a reserve of troops near a border for future conquest it is currently too weak to achieve
		PocketContinent, // The player sees an opportunity to break and sit in a continent to steal a bonus and prevent long term completion
	}

	public ObjectiveType type { get; }
	public int value { get; private set; } // The overall weighted score
	public int total_recruit_cost => desired_placements.Values.Sum(); // The cost in new troops
    public int additional_gifted_recruits; // Recruits added after the fact if there are spare
	public readonly Dictionary<Territory, int> desired_placements = []; // The territories to place at and their cost
	public readonly List<Territory> reserves_expended = []; // Existing troop reserves used by this objective (must not collide with other objectives)
	public readonly List<(Territory, Territory)> desired_attack_order = []; // The ideal attack sequence

    public bool AcceptsAdditionalRecruits() { // What attack types will soak up spare troops
        switch (type) {
            case ObjectiveType.ConquerContinent:           return  true;
            case ObjectiveType.ConquerPlayer:              return  true;
            case ObjectiveType.Settle:                     return false;
            case ObjectiveType.BreakContinent:             return false;
            case ObjectiveType.WeakenPlayer:               return false;
            case ObjectiveType.InvokeChaos:                return false;
            case ObjectiveType.PreventContinentCompletion: return  true;
            case ObjectiveType.CompleteContinent:          return  true;
            case ObjectiveType.BuildReserve:               return  true;
            case ObjectiveType.BuildOpposingForce:         return  true;
            case ObjectiveType.PocketContinent:            return  true;
        }
        return false;
    }

	public BotObjective(ObjectiveType objective_type, int objective_value) {
		type = objective_type;
		value = objective_value;
	}

    public bool CollidesWith(BotObjective objective) {
        foreach (Territory territory in reserves_expended) {
            if (objective.reserves_expended.Contains(territory)) return true;
        }
        return false;
    }
}