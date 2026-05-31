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
}