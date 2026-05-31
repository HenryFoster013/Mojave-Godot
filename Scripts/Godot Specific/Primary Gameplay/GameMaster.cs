using Godot;
using System.Collections.Generic;
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
	[ExportSubgroup("Troop Slider")]
	[Export] public Control ui_troop_slider_parent;
	[Export] public Label ui_troop_slider_label;
	[Export] public HSlider ui_troop_slider;

	private GameManager manager;
	private Player current_player => manager.current_player;

	private Territory current_territory;
	public IReadOnlyDictionary<string, Territory> Territories => manager.Territories;
	public IReadOnlyDictionary<string, Region> Regions => manager.Regions;
	
	private int current_turn => manager.total_turn;
	public State game_state => manager.game_state;
	public SubTurn sub_turn => manager.sub_turn;

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
		manager.KickStart(true);
		SetupUI();
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
		manager.OnInitialPlacementTurn += InitialPlacementTurn;
		manager.OnLog += LogMessage;
	}

	// UI

	private void SetupUI() {
		SetupTroopSlider();
	}

	private void SetupTroopSlider() {
		DeactivateTroopSlider();
		ui_troop_slider.MinValue = 1;
		ui_troop_slider.Value = 1;
		ui_troop_slider.ValueChanged += UpdateTroopSliderText;
		UpdateTroopSliderText();
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
				manager.SpeakPlacement(territory);
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

		if(!manager.local_turn || territory == current_territory)
			return;

		if(territory == null || territory.Owner != current_player)
			DeactivateTroopSlider();
		else
			ActivateTroopSlider("PLACE", current_player.currency);

	}

	void SelectTerritoryConquest(Territory territory) { }

	void SelectTerritoryFortify(Territory territory) { }

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

	public void ActivateTroopSlider(string type, int max) {
		ui_troop_slider_parent.Visible = true;
		ui_troop_slider.MaxValue = max;
		ui_troop_slider.TickCount = max;
		ui_troop_slider.Value = 1;
		UpdateTroopSliderText();
	}

	public void DeactivateTroopSlider() => ui_troop_slider_parent.Visible = false;

	void UpdateTroopSliderText() => UpdateTroopSliderText(ui_troop_slider.Value);
	void UpdateTroopSliderText(double value) {
		
		string s_val = "";
		string verb = "PLACE";
		switch (sub_turn){
			case SubTurn.ATTACK:
				verb = "REINFORCE";
				break;
			case SubTurn.FORTIFY:
				verb = "FORTIFY";
				break;
		}

		if (ui_troop_slider.Value > 1)
			s_val = "S";
		ui_troop_slider_label.Text = $"{verb} [{value}] TROOP{s_val}";
	}

	// ----- // STATE TRANSITIONS // ----- //

	private void LoadClaimants() {
		UpdateAllUI();
		map_renderer.DisablePlayerHighlight();
		current_territory = null;
		map_renderer.SelectTerritory(null);
	}

	private void LoadInitialPlacement(){
		UpdateAllUI();
		current_territory = null;
		map_renderer.SelectTerritory(null);
	}

	private void LoadPrimary() {
		UpdateAllUI();
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



	// ----- // GETTERS AND SETTERS // ----- //

	// Get Methods //

	public Territory GetTerritoryByColour(string colour) => manager.GetTerritoryByColour(colour);
	public Territory GetTerritoryByID(string id) => manager.GetTerritoryByID(id);
	public Region GetRegion(string id) => manager.GetRegion(id);
	public IReadOnlyCollection<Territory> GetPlayerTerritories(Player player) => manager.GetPlayerTerritories(player);
}
