using Godot;
using System.Collections.Generic;
using System.Linq;
using static RiskUtils;

public partial class GameMaster : Node {

	[ExportGroup(" - Primary - ")]
	[Export] public string map_json_path = "res://Data/map_data.json";
	[Export] public MapRenderer map_renderer;
	[Export] public LabelManager label_manager;
	[Export] public PlayerController player_controller;

	[ExportGroup(" - Constant UI - ")]
	[ExportSubgroup("Player")]
	[Export] public TextureRect ui_player_colour;
	[Export] public Label ui_player_name;
	[ExportSubgroup("Game")]
	[Export] public Label ui_game_state;
	[Export] public Label ui_game_turn;
	[Export] public Label ui_game_additional;
	[ExportSubgroup("Turn")]
	[Export] public Label ui_turn_popup;
	[Export] public Panel ui_turn_popup_bg;
	[ExportGroup("Tabs")]
	[Export] public Control TroopSliderTab;
	[Export] public Control ConquestTab;
	[Export] public Control SkipTab;
	[ExportSubgroup("Troop Slider Tab")]
	[Export] public Label troop_slider_label;
	[Export] public Label troop_slider_subheading;
	[Export] public HSlider troop_slider;
	[ExportSubgroup("Conquest Tab")]
	[Export] public Label conquest_header;
	[Export] public Label conquest_subheader;
	[Export] public Control conquest_buttons;

	private GameManager manager;
	private Player current_player => manager.current_player;

	private Territory current_territory;
	private Territory additional_territory;
	public IReadOnlyDictionary<string, Territory> Territories => manager.Territories;
	public IReadOnlyDictionary<string, Region> Regions => manager.Regions;
	
	private int current_turn => manager.total_turn;
	public State game_state => manager.game_state;
	public SubTurn sub_turn => manager.sub_turn;
	public bool local_turn => manager.local_turn;

	private float turn_popup_time;

	// ----- // SETUP // ----- //

	public override async void _Ready() {
		
		GD.Print(" - Start - ");
		UpdateTurnPopup(2f);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		GD.Print("Initial frame buffered, loading data.");
		LoadJson();
		Subscribe();
		LoadExports();

		GD.Print("\nLoading complete! Starting game.");
		manager.KickStart();
		SetupTabs();
	}

	// JSON

	private void LoadJson() {
		manager = new GameManager();
		if (!FileAccess.FileExists(map_json_path)) {
			GD.PrintErr($"Json not found at '{map_json_path}'!");
			return;
		}
		string json_text = FileAccess.GetFileAsString(map_json_path);
		manager.LoadJson(json_text);
		GD.Print($"Loaded {Regions.Count} regions and {Territories.Count} territories from Json.");
	}

	private void LoadExports() {

		GD.Print($"LabelManager valid: {label_manager != null}.");
		GD.Print($"MapRenderer valid: {map_renderer != null}.");
		GD.Print($"PlayerController valid: {player_controller != null}.");

		label_manager.Setup(this);
		map_renderer.Setup(this, label_manager);
		player_controller.Setup(this, map_renderer, label_manager);
		GD.Print("Exports connected.");
	}

	// Events

	private void Subscribe() {
		manager.OnTerritoryCountChanged += TerritoryCountChanged;
		manager.OnClaimancy += LoadClaimants;
		manager.OnInitialPlacement += LoadInitialPlacement;
		manager.OnPrimary += LoadPrimary;
		manager.OnUIUpdate += UpdateAllUI;
		manager.OnClaimantsTurn += ClaimantsTurn;
		manager.OnPrimaryTurn += PrimaryTurn;
		manager.OnSubTurnChanged += SubTurnChanged;
		manager.OnInitialPlacementTurn += InitialPlacementTurn;
		manager.OnLog += LogMessage;
	}

	// UI

	private void SetupTabs() {
		SetupTroopSliderTab();
		SetupConquestTab();
		DeactivateSkipTab();
	}

	private void SetupTroopSliderTab() {
		DeactivateTroopSliderTab();
		troop_slider.MinValue = 1;
		troop_slider.Value = 1;
		troop_slider.ValueChanged += UpdateTroopSliderText;
		UpdateTroopSliderText();
	}

	private void SetupConquestTab() {
		DeactivateConquestTab();
	}

	// ----- // UPDATE LOOP // ----- //

	public override void _Process(double delta) {
		UpdateTurnPopup((float)delta);
	}

	void UpdateTurnPopup(float delta) {
		float opacity = float.Clamp(turn_popup_time, 0f, 1f);
		turn_popup_time -= delta;
		ui_turn_popup.SelfModulate = new Color(1f, 1f, 1f, opacity);
		Color bg_colour = ui_turn_popup_bg.SelfModulate;
		bg_colour.A = opacity;
		ui_turn_popup_bg.SelfModulate = bg_colour;
	}

	// ----- // SELECTION // ----- //

	public void SelectTerritory(Territory territory) {

		switch(game_state){

			case State.CLAIMANTS:
				manager.SpeakClaim(territory);
				break;

			case State.INITIAL_PLACEMENT:
				manager.SpeakInitialPlacement(territory);
				break;
			
			case State.PRIMARY:
				map_renderer.SelectTerritory(territory);
				switch (sub_turn) {
					case SubTurn.PLACE: SelectTerritoryPlace(territory); break;
					case SubTurn.ATTACK: SelectTerritoryConquest(territory); break;
					case SubTurn.FORTIFY: SelectTerritoryFortify(territory); break;
				}
				current_territory = territory;
				break;
		}
	}

	void SelectTerritoryPlace(Territory territory) { 

		if(!local_turn || territory == current_territory)
			return;

		if(territory == null || territory.Owner != current_player)
			DeactivateTroopSliderTab();
		else
			ActivateTroopSliderTab(current_player.currency);

	}

	bool InvalidAdditonalCurrentTerritoryClick(Territory territory) {
		if (current_territory != null) {
			if (territory == current_territory)
				return true;
		}
		if (additional_territory != null){
			if (territory == additional_territory)
				return true;
		}
		return false;
	}

	bool AdditionalTerritoryClicks(Territory territory) {
		
		if (territory == null) {
			additional_territory = null;
			current_territory = null;
			return false;
		}

		if (territory.Owner == current_player) {
			current_territory = territory;
			additional_territory = null;
		}
		else {
			if (current_territory == null) {
				additional_territory = null;
				return false;
			}
			
			List<Territory> valid_additionals = new();
			switch (sub_turn) {
				case SubTurn.ATTACK:
					valid_additionals = current_territory.neighbours;
					break;
				case SubTurn.FORTIFY:
					valid_additionals = manager.CalculateRoutesFromTerritory(current_territory).Keys.ToList(); // prolly should buffer this for rendering lol
					break;
			}

			if (valid_additionals.Contains(territory))
				additional_territory = territory;
			else {
				current_territory = territory;
				additional_territory = null;
			}
		}

		return true;
	}

	void SelectTerritoryConquest(Territory territory) { 

		if (!local_turn || InvalidAdditonalCurrentTerritoryClick(territory))
			return;

		if (AdditionalTerritoryClicks(territory))
			ActivateConquestTab();
		else
			DeactivateConquestTab();
	}

	void SelectTerritoryFortify(Territory territory) { 
		
		if (!local_turn || InvalidAdditonalCurrentTerritoryClick(territory))
			return;

		if (AdditionalTerritoryClicks(territory))
			ActivateTroopSliderTabFortify();
		else
			DeactivateTroopSliderTab();
	}

	// ----- // UI // ----- //

	public void UpdateAllUI() {
		UpdatePlayerLabel();
		UpdateTurnLabel();
	}

	public void LogMessage(string message) => GD.Print(message);

	// Player //

	public void UpdatePlayerLabel() {
		if (ui_player_name == null || ui_player_colour == null)
			return;
		if (current_player == null) {
			ui_player_name.Text = "";
			ui_player_colour.SelfModulate = new Color(0, 0, 0, 0);
		}
		else {
			ui_player_name.Text = current_player.name;
			ui_player_colour.SelfModulate = Color.FromHtml(current_player.colour);
		}
	}

	// Turns //

	public void UpdateTurnLabel() {
	
		if (ui_game_state == null || ui_game_turn == null || ui_game_additional == null)
			return;
			
		ui_game_state.Text = "";
		ui_game_turn.Text = "";
		ui_game_additional.Text = "";
		
		switch (game_state) {
		
			case State.NULL: 
				ui_game_state.Text = "NULL";
				break;
			
			case State.CLAIMANTS:
				ui_game_state.Text = "Claimants"; 
				ui_game_additional.Text = "Select a tile to claim";
				ui_game_turn.Text = TurnText(); 
				break;

			case State.INITIAL_PLACEMENT:
				ui_game_state.Text = "Initial Placements"; 
				ui_game_additional.Text = $"Select to place a troops. You have {manager.GetRemainingPlacementsPerPlayer()} remaining.";
				ui_game_turn.Text = TurnText(); 
				break;
				
			case State.PRIMARY:
				ui_game_state.Text = "Primary"; 
				ui_game_additional.Text = $"You have {current_player.currency} spare troops";
				ui_game_turn.Text = TurnText(); 
				break;
				
			case State.ENDGAME:
				ui_game_state.Text = "Endgame"; 
				break;
		}
	}

	private string TurnText() => $"Turn {current_turn}";

	public void ActivateTurnPopup() {
		string display_text = current_player.name;
		display_text += (display_text.EndsWith("s") ? "' Turn" : "'s Turn");
		ui_turn_popup.Text = display_text;
		ui_turn_popup_bg.SelfModulate = Color.FromHtml(current_player.colour);
		turn_popup_time = 2f;
		UpdateTurnPopup(0f);
	}

	// Territories //

	private void TerritoryCountChanged(Territory territory, TerritoryChangeType type){
		label_manager.UpdateTroopCount(territory);

		switch (type) {
			case TerritoryChangeType.CLAIM: label_manager.AnimateLabel(territory, LabelAnimation.BOUNCE); break;
			case TerritoryChangeType.PLACEMENT: label_manager.AnimateLabel(territory, LabelAnimation.BOUNCE); break;
			case TerritoryChangeType.CONQUEST: label_manager.AnimateLabel(territory, LabelAnimation.BOUNCE); break;
		}
	}

	// Troop Slider //

	public void ActivateTroopSliderTab(int max) {
		TroopSliderTab.Visible = true;
		troop_slider.Visible = true;
		troop_slider_subheading.Visible = false;
		troop_slider.MaxValue = max;
		troop_slider.TickCount = max;
		troop_slider.Value = 1;
		UpdateTroopSliderText();
	}

	public void ActivateTroopSliderTabFortify() {
		if (current_territory == null)
			return;
		if (additional_territory == null) {
			troop_slider_subheading.Visible = true;
			troop_slider.Visible = false;
			troop_slider_label.Text = $"{current_territory.name} [{current_territory.troop_count}] -> ...";
		}
		else {
			ActivateTroopSliderTab(current_territory.troop_count - MIN_TROOPS);
		}
	}

	public void ActivateConquestTab() {
		if (current_territory == null || current_territory.Owner != current_player || !local_turn) {
			DeactivateConquestTab();
			return;
		}
		ConquestTab.Visible = true;
		bool has_additional = additional_territory != null;
		conquest_subheader.Visible = !has_additional;
		conquest_buttons.Visible = has_additional;
		if (has_additional)
			conquest_header.Text = $"{current_territory.name} [{current_territory.troop_count}] -> {additional_territory.name} [{additional_territory.troop_count}]";
		else
			conquest_header.Text = $"{current_territory.name} [{current_territory.troop_count}] -> ...";
	}
	
	public void ActivateSkipTab() {
		SkipTab.Visible = true;
	}

	public void DeactivateTroopSliderTab() => TroopSliderTab.Visible = false;
	public void DeactivateConquestTab() => ConquestTab.Visible = false;
	public void DeactivateSkipTab() => SkipTab.Visible = false;

	void UpdateTroopSliderText() => UpdateTroopSliderText(troop_slider.Value);
	void UpdateTroopSliderText(double value) {

		troop_slider_label.Text = "";
		
		if(sub_turn == SubTurn.FORTIFY || sub_turn == SubTurn.ATTACK) {
			if (additional_territory != null && current_territory != null)
				troop_slider_label.Text = $"{current_territory.name} [{current_territory.troop_count - (int)value}] -> {additional_territory.name} [{additional_territory.troop_count + (int)value}]";
			return;
		}

		if (sub_turn == SubTurn.PLACE) {
			troop_slider_label.Text = $"Place [{value}] Troop{troop_slider.Value > 1? "s" : ""}";
		}
	}

	// ----- // STATE TRANSITIONS // ----- //

	private void SubTurnChanged() {
		current_territory = null;
		additional_territory = null;
	}

	private void LoadClaimants() {
		UpdateAllUI();
		map_renderer.DisablePlayerHighlight();
		current_territory = null;
		additional_territory = null;
		map_renderer.SelectTerritory(null);
	}

	private void LoadInitialPlacement(){
		UpdateAllUI();
		current_territory = null;
		additional_territory = null;
		map_renderer.SelectTerritory(null);
	}

	private void LoadPrimary() {
		UpdateAllUI();
		current_territory = null;
		additional_territory = null;
		map_renderer.DisablePlayerHighlight();
	}

	private void ClaimantsTurn() { }

	private void InitialPlacementTurn() { 
		map_renderer.HighlightPlayer(current_player);
	}

	private void PrimaryTurn() { 
		ActivateTurnPopup();
	}

	// ----- // UI BUTTONS // ----- //

	public void PlaceTroopsButton() {

	}

	public void AttackButton() {

	}

	public void ReinforceButton() {

	}

	public void SkipButton() {
		manager.SpeakSkip();
	}

	// ----- // GETTERS AND SETTERS // ----- //

	// Get Methods //

	public Territory GetTerritoryByColour(string colour) => manager.GetTerritoryByColour(colour);
	public Territory GetTerritoryByID(string id) => manager.GetTerritoryByID(id);
	public Region GetRegion(string id) => manager.GetRegion(id);
	public IReadOnlyCollection<Territory> GetPlayerTerritories(Player player) => manager.GetPlayerTerritories(player);
}
