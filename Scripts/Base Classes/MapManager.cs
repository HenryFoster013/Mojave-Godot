using Godot;
using System.Collections.Generic;

public class MapManager {

	private readonly Dictionary<string, Territory> territories = new();
	private readonly Dictionary<string, Region> regions = new();

	public IReadOnlyDictionary<string, Territory> Territories => territories;
	public IReadOnlyDictionary<string, Region> Regions => regions;

	// ----- // FUNCTIONALITY // ----- //

	// Setup //

	public void Load(string json_text) {

		var parsed = Json.ParseString(json_text);
		if (parsed.VariantType != Variant.Type.Dictionary)
			throw new System.Exception("MapManager: failed to parse map JSON.");
		var root = parsed.AsGodotDictionary();

		FillData(root);
	}

	private void FillData(Godot.Collections.Dictionary root) {

		foreach (var entry in root["regions"].AsGodotArray()) {
			var region = Region.FromJson(entry.AsGodotDictionary());
			regions[region.id] = region;
		}

		foreach (var entry in root["tiles"].AsGodotArray()) {
			var territory = Territory.FromJson(entry.AsGodotDictionary());
			territories[territory.id] = territory;
		}

		foreach (var territory in territories.Values) {
			foreach (var neighbour_id in territory.neighbour_ids) {
				if (territories.TryGetValue(neighbour_id, out var neighbour))
					territory.AddNeighbour(neighbour);
				else
					throw new System.Exception($"MapLoader: territory '{territory.id}' references unknown neighbour '{neighbour_id}'.");
			}
		}

		foreach (var region in regions.Values) {
			foreach (var territory_id in region.territory_ids) {
				if (territories.TryGetValue(territory_id, out var territory)) {
					region.AddTerritory(territory);
					territory.SetRegion(region);
				}
				else
					throw new System.Exception($"MapLoader: region '{region.id}' references unknown territory '{territory_id}'.");
			}
		}
	}

	// ----- // GETTERS AND SETTERS // ----- //

	// Get Methods //

	public Territory GetTerritory(string id) {
		territories.TryGetValue(id, out var territory);
		return territory;
	}

	public Region GetRegion(string id) {
		regions.TryGetValue(id, out var region);
		return region;
	}
}
