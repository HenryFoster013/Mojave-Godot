using static BotUtils;

public partial class BotPlayer : Player {

	private bool IsWorthAttacking(Territory source, Territory target) {

		if (source.troop_count < RiskUtils.MIN_TROOPS + 1) return false;

		int attackers = source.troop_count - RiskUtils.MIN_TROOPS;
		if (attackers - target.troop_count < A_MIN_TROOP_ADVANTAGE) return false;

		if (CalculateThreatScore(source) > A_MAX_SOURCE_THREAT && attackers < target.troop_count * 2) return false;

		return true;
	}
}
