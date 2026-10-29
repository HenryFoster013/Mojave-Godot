public static class BotUtils {

	// ----- // AI DEFAULTS // ----- //

	public const float BASE_DELAY          = 0.025f; // Seconds a bot waits before acting on a claim or skip (keeps bot turns from being instant)
	public const int   CENTRAL_TERRITORIES = 4;      // How many of our most central territories IdentifyCentralTerritories returns

	// Claimants //
	// Chances are weights rolled against C_TOTAL_DECISION_CHANCE (currently 100, so they read as percentages).
	// Each roll in RequestClaim is made independently, so the effective odds are lower than the raw values.

	public const int C_MISTAKE_CHANCE             = 5;  // Chance to claim a completely random free territory, giving bots a human-like slip
	public const int C_CONSOLIDATION_CHANCE       = 75; // Chance to claim a free territory next to ones we already own (preferring our best region)
	public const int C_DISRUPTION_CHANCE          = 20; // Chance to claim a free territory next to an opponent to break up their area; only counts towards the total, as disruption is the fallback
	public const int C_IMMENENT_DISRUPTION_CHANCE = 60; // Chance to claim a free territory that an opponent needs to complete a region (rolled against the total, so 60 = 60%)
	public const int C_TOTAL_DECISION_CHANCE      = C_MISTAKE_CHANCE + C_CONSOLIDATION_CHANCE + C_DISRUPTION_CHANCE; // Upper bound of the claim dice roll

	// Initial Placements //
	// The IP_*_CHANCE values are weights rolled against IP_TOTAL_DECISION_CHANCE (currently 100).

	public const float INITIAL_PLACEMENT_DELAY  = 0.05f; // Seconds a bot waits before placing an initial troop
	public const int   IP_CONSOLIDATION_CHANCE  = 60;    // Chance to reinforce our most central territory, building up one strong area
	public const int   IP_DEFENSIVE_CHANCE      = 25;    // Chance to reinforce our most threatened border territory
	public const int   IP_OFFENSIVE_CHANCE      = 15;    // Chance to reinforce a territory that borders a target we want to attack
	public const int   IP_TOTAL_DECISION_CHANCE = IP_CONSOLIDATION_CHANCE + IP_DEFENSIVE_CHANCE + IP_OFFENSIVE_CHANCE; // Upper bound of the placement dice roll
	public const float IP_HOLD_BONUS_THREAT     = 3f;    // Flat threat added to every tile in a region we fully own, since losing one breaks the bonus
	public const float IP_PARTIAL_BONUS_THREAT  = 1.5f;  // Threat added to tiles in a region we partly own, multiplied by the fraction of the region we hold
	public const float IP_BONUS_THREAT_WEIGHT   = 0.5f;  // Extra threat per point of region.bonus on tiles in a fully held region, so valuable regions are defended first

	// Region bias //
	// These feed CalculateRegionPriorityScore, which ranks how much we want to invest in each region.

	public const float REGION_OWNERSHIP_WEIGHT = 10f; // Multiplied by the fraction of the region we own; the main driver, favouring regions we are close to completing
	public const float REGION_HELD_BONUS       = 6f;  // Flat bonus when we own every territory in the region
	public const float REGION_SMALLNESS_WEIGHT = 4f;  // Divided by the region's territory count, so smaller regions (easier to take and hold) score higher
	public const float REGION_LOW_TROOP_WEIGHT = 3f;  // Scaled by ownership and divided by average troops per owned tile, so thinly defended regions get priority
	public const float REGION_BONUS_WEIGHT     = 2f;  // Multiplied by region.bonus per territory, so high-value, compact regions score higher

}