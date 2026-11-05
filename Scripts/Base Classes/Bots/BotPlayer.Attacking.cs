namespace Mojave;

public partial class BotPlayer : Player {

	// ------ // ------ // ------ // SELECTION // ------ // ------ // ------ //

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

	// ------ // ------ // ------ // TROOP DIVISION // ------ // ------ // ------ //

	public override void RequestConquestDivision(Territory from_territory, Territory to_territory) {

		int available = from_territory.troop_count - 1;
		if (available < 1) return; // Nothing to move (assumes the manager enforces the minimum move itself)

		bool from_is_exposed = from_territory.neighbours.Any(n => n.Owner != this && n != to_territory);
		bool to_is_exposed   = to_territory.neighbours.Any(n => n.Owner != this);

		int to_move;
		if (to_is_exposed && !from_is_exposed) {
			
			// Push all forward //

			to_move = available;
		}
		else if (!to_is_exposed && from_is_exposed) {
			
			// Leave all behind //

			to_move = 1;
		}
		else { 
			
			// Divide Evenly //

			float from_exposure = CalculateExposure(from_territory);
			float to_exposure   = CalculateExposure(to_territory);
			float to_share = to_exposure / (from_exposure + to_exposure);
			to_move = Math.Clamp((int)MathF.Round(available * to_share), 1, available);
		}

		manager.SpeakConquestDivision(from_territory, to_territory, to_move);
	}
}
