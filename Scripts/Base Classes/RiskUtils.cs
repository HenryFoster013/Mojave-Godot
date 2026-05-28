public static class RiskUtils {

    public enum State { NULL, CLAIMANTS, PRIMARY, ENDGAME }
	public enum SubTurn { PLACE, ATTACK, FORTIFY }
    public enum AttackResult { STANDARD, CONQUEST, INVALID }
    public enum PlayerType { NULL, LOCAL, BOT }

    public const int MIN_TROOPS = 1; 
    public const int TERRITORIES_PER_TROOP = 3;
}