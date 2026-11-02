using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BotUtils;

public partial class BotPlayer : Player {

	////////////////////////////|| ATTACKING ||////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	// ----- // PLAY LOOP // ----- //

	/*  Replace your current RequestPlay / SkipTurn with this.
		Flow: attack repeatedly while a good attack exists (capped), then skip the remaining phases.
	*/

	public override async void RequestPlay() {

		int attacks_made = 0;

		while (attacks_made < A_MAX_ATTACKS_PER_TURN) {

			await Task.Delay(TimeSpan.FromSeconds(BASE_DELAY));

			(Territory source, Territory target)? attack = ChooseAttack();
			if (attack == null) break;

			manager.SpeakBlitz(attack.Value.source, attack.Value.target);
			attacks_made++;
		}

		SkipTurn();
	}

	// ----- // CHOOSING // ----- //

	private (Territory source, Territory target)? ChooseAttack() {

		List<(Territory source, Territory target)> candidates = [.. IdentifyInvasionTargets()
			.Where(pair => IsWorthAttacking(pair.source, pair.target))
			.OrderByDescending(pair => CalculateAttackScore(pair.source, pair.target))];

		if (candidates.Count == 0)
			return null;

		if (candidates.Count > 1 && random.Next(100) < A_MISTAKE_CHANCE)
			return candidates[random.Next(candidates.Count)];

		return candidates[0];
	}

	private bool IsWorthAttacking(Territory source, Territory target) {

		if (source.troop_count < RiskUtils.MIN_TROOPS + 1)
			return false;

		int attackers = source.troop_count - RiskUtils.MIN_TROOPS;
		if (attackers - target.troop_count < A_MIN_TROOP_ADVANTAGE)
			return false;

		// Don't strip a tile that is under real threat unless the attack is clearly overwhelming.
		if (CalculateThreatScore(source) > A_MAX_SOURCE_THREAT && attackers < target.troop_count * 2)
			return false;

		return true;
	}

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

	// ----- // AFTER CONQUEST // ----- //

	public override void RequestConquestDivision(Territory from_territory, Territory to_territory) {

		int available = from_territory.troop_count - 1;
		if (available < 1) return; // Nothing to move (assumes the manager enforces the minimum move itself)

		bool from_is_exposed = from_territory.neighbours.Any(n => n.Owner != this && n != to_territory);
		bool to_is_exposed   = to_territory.neighbours.Any(n => n.Owner != this);

		int to_move;
		if (to_is_exposed && !from_is_exposed) // Push all forward
			to_move = available;
		else if (!to_is_exposed && from_is_exposed) // Leave all behind
			to_move = 1;
		else { // Divide Evenly
			float from_exposure = CalculateExposure(from_territory);
			float to_exposure   = CalculateExposure(to_territory);
			float to_share = to_exposure / (from_exposure + to_exposure);
			to_move = Math.Clamp((int)MathF.Round(available * to_share), 1, available);
		}

		manager.SpeakConquestDivision(from_territory, to_territory, to_move);
	}


	// Scores a tile on how exposed it is, counts the troops that neighbour it (ignoring holding troops).
	private float CalculateExposure(Territory territory) {
		return 1f + territory.neighbours.Where(n => n.Owner != this).Sum(n => n.troop_count - RiskUtils.MIN_TROOPS);
	}
}
