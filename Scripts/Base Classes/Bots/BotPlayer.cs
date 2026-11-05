namespace Mojave;

public partial class BotPlayer : Player {

	// ------ // ------ // ------ // INSTANTIATION // ------ // ------ // ------ //

	Random random = new Random();

	public BotPlayer(GameManager _manager, string _name, string _colour) : base(_manager, _name, _colour) { 
		type = PlayerType.BOT; 
		SubscribeToBotEvents();
	}

	void SubscribeToBotEvents() {
		manager.OnInitialPlacement += InitialPlacementStarted;
	}

	// ----- // INTER-ROUND PLAYS // ----- //

	public override async void RequestPlay() {
		PlayPlacements();
		PlayAttacks();
		PlayFortify();
	}

	private async void PlayPlacements() {
		await Task.Delay(TimeSpan.FromSeconds(BASE_DELAY));
		manager.SpeakSkip();
	}

	private async void PlayAttacks() {
		
		int attacks_made = 0;
		while (attacks_made < A_MAX_ATTACKS_PER_TURN) {
			
			await Task.Delay(TimeSpan.FromSeconds(BASE_DELAY));
			(Territory source, Territory target)? attack = ChooseAttack();

			if (attack == null) break;

			manager.SpeakBlitz(attack.Value.source, attack.Value.target);
			attacks_made++;
		}
	}

	private async void PlayFortify() {
		await Task.Delay(TimeSpan.FromSeconds(BASE_DELAY));
		manager.SpeakSkip();
	}
}
