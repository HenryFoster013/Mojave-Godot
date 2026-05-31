using System;

public static class RiskUtils {

    public enum State { NULL, CLAIMANTS, INITIAL_PLACEMENT, PRIMARY, ENDGAME }
	public enum SubTurn { PLACE, ATTACK, FORTIFY }
    public enum AttackResult { STANDARD, CONQUEST, INVALID }
    public enum PlayerType { NULL, LOCAL, BOT }

    public const int MIN_TROOPS = 1; 
    public const int DEFAULT_TERRITORIES_PER_TROOP = 3;
    public const int DEFAULT_BASE_INIT_TROOPS = 50;
    public const int DEFAULT_MULT_INIT_TROOPS = 5;

    // Default Colours

    public const string COLOUR_SAND_YELLOW       = "#FFE06B";
    public const string COLOUR_ARIZONA_GOLD      = "#FFD100";
    public const string COLOUR_NUCLEAR_GREEN     = "#00FF77";
    public const string COLOUR_MUTANT_GREEN      = "#00C343";
    public const string COLOUR_QUANTUM_BLUE      = "#45A2FF";
    public const string COLOUR_VAULT_BLUE        = "#0024FF";
    public const string COLOUR_NIGHTSKIN_PURPLE  = "#FFA0EF";
    public const string COLOUR_MADRE_RHUBARB     = "#FF005B";
    public const string COLOUR_LEGION_RED        = "#FF0000";
    public const string COLOUR_MOJAVE_ORANGE     = "#FF5300";
}