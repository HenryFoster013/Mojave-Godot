using Godot;
using System;
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
	[Export] public Control troop_slider_tab;
	[Export] public Control conquest_tab;
	[Export] public Control skip_tab;
	[ExportSubgroup("Troop Slider Tab")]
	[Export] public Label troop_slider_header;
	[Export] public Label troop_slider_subheader;
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
		UpdateSkipButton();
	}

	// ----- // UPDATE LOOP // ----- //

	public override void _Process(double delta) {
		UpdateTurnPopup((float)delta);
	}

	private void UpdateTurnPopup(float delta) {
		float opacity = float.Clamp(turn_popup_time, 0f, 1f);
		turn_popup_time -= delta;
		ui_turn_popup.SelfModulate = new Color(1f, 1f, 1f, opacity);
		Color bg_colour = ui_turn_popup_bg.SelfModulate;
		bg_colour.A = opacity;
		ui_turn_popup_bg.SelfModulate = bg_colour;
	}

	// ----- // SELECTION // ----- //

	private void ClearTerritories() {
		current_territory = null;
		additional_territory = null;
	}

	private bool ResetAdditonalSelection(Territory territory) {
		current_territory = territory;
		additional_territory = null;
		return false;
	}
	
	public void SelectTerritory(Territory territory) {
		switch(game_state){
			case State.CLAIMANTS: manager.SpeakClaim(territory); break;
			case State.INITIAL_PLACEMENT: manager.SpeakInitialPlacement(territory); break;
			case State.PRIMARY:
				map_renderer.SelectTerritory(territory);
				switch (sub_turn) {
					case SubTurn.PLACE: SelectTerritoryPlace(territory); break;
					case SubTurn.ATTACK: ToggleActionTab(territory, ActivateConquestTab, DeactivateConquestTab); break;
					case SubTurn.FORTIFY: ToggleActionTab(territory, ActivateTroopSliderTabFortify, DeactivateTroopSliderTab); break;
				}
				break;
		}
	}

	// Sub-Turn Selections //

	private void SelectTerritoryPlace(Territory territory) { 
		if(!local_turn || territory == current_territory)
			return;
		if(territory == null || territory.Owner != current_player)
			DeactivateTroopSliderTab();
		else
			ActivateTroopSliderTab(current_player.currency);
		current_territory = territory;
	}

	// Additional Territory Management //

	private void MarkAdditonalTerritory(Territory territory) {
		if (territory == null) {
			ClearTerritories();
			return;
		}
		if (current_territory == null || territory == current_territory) {
			ResetAdditonalSelection(territory);
			return;
		}
		switch (sub_turn) {
			case SubTurn.ATTACK: 
				AdditionalAttackClicks(territory);
				return;
			case SubTurn.FORTIFY: 
				AdditionalFortifyClicks(territory);
				return;
			default: break;
		}
	}

	private void AdditionalFortifyClicks(Territory territory) {
		if (territory.Owner != current_player || current_territory.Owner != current_player || current_territory.troop_count < 2) {
			ResetAdditonalSelection(territory);
			return;
		}
		List<Territory> valid_additionals =  manager.CalculateRoutesFromTerritory(current_territory).Keys.ToList();
		if (valid_additionals.Contains(territory)) {
			additional_territory = territory;
			return;	
		}
		ResetAdditonalSelection(territory);
	}

	private void AdditionalAttackClicks(Territory territory) {
		if (territory.Owner == current_player) {
			ResetAdditonalSelection(territory);
 			return;
		}
		if (current_territory.neighbours.Contains(territory)) {
			if(current_territory.troop_count < 2) {
				ResetAdditonalSelection(territory);
				return;
			}
			additional_territory = territory;
			return;
		}
		ResetAdditonalSelection(territory);
	}

	// ----- // UI // ----- //

	public void UpdateAllUI() {
		UpdatePlayerLabel();
		UpdateTurnLabel();
		UpdateSkipButton();
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
		ui_game_additional.Text = "";
		ui_game_turn.Text = TurnText(); 
		
		switch (game_state) {
			case State.NULL: 
				ui_game_state.Text = "NULL";
				break;
			case State.CLAIMANTS:
				ui_game_state.Text = "Claimants"; 
				ui_game_additional.Text = "Select a tile to claim.";
				break;
			case State.INITIAL_PLACEMENT:
				ui_game_state.Text = "Initial Placements"; 
				ui_game_additional.Text = $"Select to place a troops. You have {manager.GetRemainingPlacementsPerPlayer()} remaining.";
				break;
			case State.PRIMARY:
				ui_game_state.Text = "Primary"; 
				SetPlacementsAdditionalText();
				break;
			case State.ENDGAME:
				ui_game_state.Text = "Endgame"; 
				break;
		}
	}

	private void SetPlacementsAdditionalText() {
		switch (sub_turn) {
			case SubTurn.PLACE:
				ui_game_additional.Text = $"You have {current_player.currency} spare troops.";
				break;
			case SubTurn.ATTACK:
				ui_game_additional.Text = $"Select two tiles to stage an attack.";
				break;
			case SubTurn.FORTIFY:
				ui_game_additional.Text = $"Select a tile to route troops.";
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

	// Generic Action Tabs //

	private void ToggleActionTab(Territory territory, Action valid_action, Action invalid_action) {
		if (!local_turn) return;
		MarkAdditonalTerritory(territory);
		if (current_territory != null && current_territory.Owner == current_player && current_territory.troop_count > 1)
			valid_action();
		else
			invalid_action();
	}

	// Troop Slider //

	private void SetupTroopSliderTab() {
		DeactivateTroopSliderTab();
		troop_slider.MinValue = 1;
		troop_slider.Value = 1;
		troop_slider.ValueChanged += UpdateTroopSliderText;
		UpdateTroopSliderText();
	}

	public void ActivateTroopSliderTab(int max) {
		troop_slider_tab.Visible = true;
		troop_slider.Visible = true;
		troop_slider_subheader.Visible = false;
		troop_slider.MaxValue = max;
		troop_slider.TickCount = max;
		troop_slider.Value = 1;
		UpdateTroopSliderText();
	}

	public void ActivateTroopSliderTabFortify() {

		map_renderer.ClearArrow();
		if (current_territory == null || current_territory.troop_count < 2 || current_territory.Owner != current_player || !local_turn) {
			DeactivateTroopSliderTab();
			return;
		}

		if (additional_territory == null) {
			troop_slider_tab.Visible = true;
			troop_slider_subheader.Visible = true;
			troop_slider_subheader.Text = "Select a second territory to fortify.";
			troop_slider.Visible = false;
			troop_slider_header.Text = $"{current_territory.name} [{current_territory.troop_count}] -> ...";
		}
		else {
			ActivateTroopSliderTab(current_territory.troop_count - MIN_TROOPS);
			map_renderer.DrawArrowBetween(current_territory, additional_territory);
		}
	}

	public void DeactivateTroopSliderTab() {
		troop_slider_tab.Visible = false;
		map_renderer.ClearArrow();
	}

	private void UpdateTroopSliderText() => UpdateTroopSliderText(troop_slider.Value);
	private void UpdateTroopSliderText(double value) {
		troop_slider_header.Text = "";
		if(sub_turn == SubTurn.FORTIFY || sub_turn == SubTurn.ATTACK) {
			if (additional_territory != null && current_territory != null)
				troop_slider_header.Text = $"{current_territory.name} [{current_territory.troop_count - (int)value}] -> {additional_territory.name} [{additional_territory.troop_count + (int)value}]";
			return;
		}
		if (sub_turn == SubTurn.PLACE) {
			troop_slider_header.Text = $"Place [{value}] Troop{(troop_slider.Value > 1 ? "s" : "")}";
		}
	}

	// Conquest Tab //

	private void SetupConquestTab() {
		DeactivateConquestTab();
	}

	public void ActivateConquestTab() {

		map_renderer.ClearArrow();

		if (current_territory == null || current_territory.Owner != current_player || !local_turn || current_territory.troop_count < 2) {
			DeactivateConquestTab();
			return;
		}
		
		conquest_tab.Visible = true;
		bool has_additional = additional_territory != null;
		conquest_subheader.Visible = !has_additional;
		conquest_buttons.Visible = has_additional;

		if (has_additional) {
			conquest_header.Text = $"{current_territory.name} [{current_territory.troop_count}] -> {additional_territory.name} [{additional_territory.troop_count}]";
			map_renderer.DrawArrowBetween(current_territory, additional_territory);
		}
		else
			conquest_header.Text = $"{current_territory.name} [{current_territory.troop_count}] -> ...";
	}

	public void DeactivateConquestTab() {
		map_renderer.ClearArrow();
		conquest_tab.Visible = false;
	}

	// Skip Button //

	private void UpdateSkipButton() {
		if (current_player == null) {
			skip_tab.Visible = false;
			return;
		}
		skip_tab.Visible = (local_turn && game_state == State.PRIMARY && sub_turn != SubTurn.PLACE);
	}

	// ----- // STATE TRANSITIONS // ----- //

	private void SubTurnChanged() {

		if (game_state != State.PRIMARY && sub_turn != SubTurn.NULL)
			return;

		ClearTerritories();
		UpdateAllUI();

		switch (sub_turn) {
			case SubTurn.PLACE:
				map_renderer.HighlightPlayer(current_player);
				break;
			case SubTurn.ATTACK:
				map_renderer.DisablePlayerHighlight();
				break;
		}
	}

	// Load States //

	private void PrimitiveReset() {
		ClearTerritories();
		UpdateAllUI();
		map_renderer.SelectTerritory(null);
		map_renderer.DisablePlayerHighlight();
	}

	private void LoadClaimants() => PrimitiveReset();
	private void LoadInitialPlacement() => PrimitiveReset();
	private void LoadPrimary() => PrimitiveReset();

	// State Turns //

	private void ClaimantsTurn() { }

	private void InitialPlacementTurn() { 
		map_renderer.HighlightPlayer(current_player);
	}

	private void PrimaryTurn() { 
		ActivateTurnPopup();
	}

	// ----- // UI BUTTONS // ----- //

	public void TroopSliderTabConfirm() {
		switch (sub_turn) {
			case SubTurn.PLACE:
				manager.SpeakPlacement(current_territory, (int)troop_slider.Value);
				UpdateTurnLabel();
				break;
		}
		DeactivateTroopSliderTab();
		SelectTerritory(null);
	}

	public void SkipButton() {
		DeactivateConquestTab();
		DeactivateTroopSliderTab();
		manager.SpeakSkip();
	}

	// ----- // GETTERS AND SETTERS // ----- //

	// Get Methods //

	public Territory GetTerritoryByColour(string colour) => manager.GetTerritoryByColour(colour);
	public Territory GetTerritoryByID(string id) => manager.GetTerritoryByID(id);
	public Region GetRegion(string id) => manager.GetRegion(id);
	public IReadOnlyCollection<Territory> GetPlayerTerritories(Player player) => manager.GetPlayerTerritories(player);
	public List<Territory> GetRouteBetweenTerritories(Territory start, Territory end) => manager.GetRouteBetweenTerritories(start, end);
}
