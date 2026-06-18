public static class BotUtils {

    // ----- // AI DEFAULTS // ----- //

    public const float BASE_DELAY = 0.025f;
    public const int CENTRAL_TERRITORIES = 4;
    
    // Claimants //

    public const int C_MISTAKE_CHANCE = 5;
    public const int C_CONSOLIDATION_CHANCE = 75;
    public const int C_DISRUPTION_CHANCE = 20;
    public const int C_IMMENENT_DISRUPTION_CHANCE = 60;
    public const int C_TOTAL_DECISION_CHANCE = C_MISTAKE_CHANCE + C_CONSOLIDATION_CHANCE + C_DISRUPTION_CHANCE;

    // Initial Placements //
    public const float INITIAL_PLACEMENT_DELAY = 0.05f;
    public const int IP_CONSOLIDATION_CHANCE = 60;
    public const int IP_DEFENSIVE_CHANCE = 25;
    public const int IP_OFFENSIVE_CHANCE = 15;
    public const int IP_TOTAL_DECISION_CHANCE = IP_CONSOLIDATION_CHANCE + IP_DEFENSIVE_CHANCE + IP_OFFENSIVE_CHANCE;
    public const float IP_HOLD_BONUS_THREAT = 3f;

}