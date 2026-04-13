using Godot;
using System.Collections.Generic;

public partial class GameMaster : Node {

	[Export] public string map_json_path = "res://Board/map_data.json";
	[Export] public MapRenderer map_renderer;
	[Export] public LabelManager label_manager;

	[Export] public TextureRect ui_player_colour;
	[Export] public Label ui_player_name;
	[Export] public Label ui_game_state;
	[Export] public Label ui_game_additional;

	private GameManager manager;
	public IReadOnlyDictionary<string, Territory> Territories => manager.Territories;
	public IReadOnlyDictionary<string, Region> Regions => manager.Regions;

	int turn_counter = 0;
	Player current_player;

	// Start //

	public override void _Ready() {

		manager = new GameManager(this);
		if (!FileAccess.FileExists(map_json_path)) {
			GD.PrintErr($"GameMaster: map file not found at '{map_json_path}'.");
			return;
		}

		string json_text = FileAccess.GetFileAsString(map_json_path);
		manager.Load(json_text);
		GD.Print($"GameMaster: loaded {Regions.Count} regions and {Territories.Count} territories.");
	}

	// Selection Calls //

	public void SelectTerritory(Territory territory) {

		map_renderer.SelectTerritory(territory);

		if (territory == null)
			return;
		if (manager.game_state == GameManager.state_type.CLAIMANTS) {
			manager.LocalClaim(territory);
		}
	}

	public void NewPlayerTurn(Player player) {
		current_player = player;
		turn_counter++;
		UpdateUI();

	}

	// UI Management //

	void UpdateUI() {
		UpdatePlayerLabels();
		UpdateGameInfoLabels();
	}

	void UpdatePlayerLabels() {
		if(ui_player_name == null || ui_player_colour == null)
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

	void UpdateGameInfoLabels() {
		if(ui_game_state == null || ui_game_additional == null)
			return;
		ui_game_state.Text = "";
		ui_game_additional.Text = "";
		switch (manager.game_state) {
			case GameManager.state_type.NULL: ui_game_state.Text = "NULL"; break;
			case GameManager.state_type.LOBBY: ui_game_state.Text = "Lobby"; break;
			case GameManager.state_type.CLAIMANTS: ui_game_state.Text = "Claimants"; ui_game_additional.Text = TurnText(); break;
			case GameManager.state_type.PRIMARY: ui_game_state.Text = "Primary"; ui_game_additional.Text = TurnText(); break;
			case GameManager.state_type.ENDGAME: ui_game_state.Text = "Endgame"; break;
		}
	}

	string TurnText() => $"Turn {turn_counter}";

	// Scene Transitions //

	public void LoadLobby() { }
	public void LoadClaimants() { }
	public void LoadPrimary() { }

	// Get Methods //

	public Territory GetTerritoryByColour(string colour) => manager.GetTerritoryByColour(colour);
	public Territory GetTerritoryByID(string id) => manager.GetTerritoryByID(id);
	public Region GetRegion(string id) => manager.GetRegion(id);
}
