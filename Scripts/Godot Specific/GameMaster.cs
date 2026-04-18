using Godot;
using System.Collections.Generic;

public partial class GameMaster : Node {

	[Export] public string map_json_path = "res://Board/map_data.json";
	[Export] public MapRenderer map_renderer;
	[Export] public LabelManager label_manager;
	[Export] public PlayerController player_controller;

	[Export] public TextureRect ui_player_colour;
	[Export] public Label ui_player_name;
	[Export] public Label ui_game_state;
	[Export] public Label ui_game_turn;
	[Export] public Label ui_game_additional;
	[Export] public TextureRect ui_game_add_divider;

	private GameManager manager;
	private Player current_player => manager.current_player;
	private int current_turn => manager.total_turn;
	public IReadOnlyDictionary<string, Territory> Territories => manager.Territories;
	public IReadOnlyDictionary<string, Region> Regions => manager.Regions;

	// ----- // SETUP // ----- //

	public override async void _Ready() {
		GD.Print(" - Start - ");
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		GD.Print("Initial frame buffered, loading data.");
		LoadJson();
		LoadExports();
		GD.Print("\nLoading complete! Starting game.");
		manager.KickStart(true);
	}

	private void LoadJson() {
		manager = new GameManager(this);
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
		GD.Print($"MapRenderer valid: {label_manager != null}.");
		GD.Print($"PlayerController valid: {label_manager != null}.");

		label_manager.Setup(this);
		map_renderer.Setup(this, label_manager);
		player_controller.Setup(this, map_renderer, label_manager);
		GD.Print("Exports connected.");
	}

	// ----- // SELECTION // ----- //

	public void SelectTerritory(Territory territory) {

		map_renderer.SelectTerritory(territory);

		if (territory == null)
			return;
		if (manager.game_state == GameManager.state_type.CLAIMANTS) {
			manager.SpeakClaim(territory);
		}
	}

	// ----- // UI // ----- //

	public void UpdateAllUI() {
		UpdatePlayerLabel();
		UpdateTurnLabel();
	}

	public void UpdatePlayerLabel() {
		if (ui_player_name == null || ui_player_colour == null)
			return;
		if (current_player == null) {
			ui_player_name.Text = "";
			ui_player_colour.Modulate = new Color(0, 0, 0, 0);
		}
		else {
			ui_player_name.Text = current_player.name;
			ui_player_colour.Modulate = current_player.colour;
		}
	}

	public void UpdateTurnLabel() {
	
		if (ui_game_state == null || ui_game_turn == null || ui_game_additional == null)
			return;
			
		ui_game_state.Text = "";
		ui_game_turn.Text = "";
		ui_game_additional.Text = "";
		ui_game_add_divider.Modulate = new Color(0, 0, 0, 0);
		
		switch (manager.game_state) {
		
			case GameManager.state_type.NULL: 
				ui_game_state.Text = "NULL"; 
				break;
			
			case GameManager.state_type.CLAIMANTS:
				ui_game_state.Text = "Claimants"; 
				ui_game_turn.Text = TurnText(); 
				break;
				
			case GameManager.state_type.PRIMARY:
				ui_game_state.Text = "Primary"; 
				ui_game_turn.Text = TurnText(); 
				break;
				
			case GameManager.state_type.ENDGAME:
				ui_game_state.Text = "Endgame"; 
				break;
		}

		if (ui_game_additional.Text != "") ui_game_add_divider.Modulate = new Color(1, 1, 1, 1);
	}

	private string TurnText() => $"Turn {current_turn}";

	// ----- // STATE TRANSITIONS // ----- //

	public void LoadClaimants() {
		UpdateAllUI();
	}

	public void LoadPrimary() {
		UpdateAllUI();
	}

	// ----- // GETTERS AND SETTERS // ----- //

	// Get Methods //

	public Territory GetTerritoryByColour(string colour) => manager.GetTerritoryByColour(colour);
	public Territory GetTerritoryByID(string id) => manager.GetTerritoryByID(id);
	public Region GetRegion(string id) => manager.GetRegion(id);
}
