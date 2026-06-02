using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Numerics;

public class Territory {

    // ----- // VARIABLES & FIELDS // ----- //

    // Constant JSON Values //

    public string id { get; init; }
    public string name { get; init; }
    public string map_colour { get; init; }
    public string owning_region_id { get; init; }
    public Vector2 centroid { get; init; }
    public IReadOnlyList<string> neighbour_ids { get; init; }
    public int render_order;

    // Variable Values //

    private Player owner;
    public Region region  { get; private set; }
    public int troop_count { get; private set; }

    private List<Territory> _neighbours = new();
    public IReadOnlyList<Territory> neighbours => _neighbours;

    public event System.Action<Territory, Player, Player> OnTerritoryOwnerChanged;

    // ----- // FUNCTIONALITY // ----- //

    public static Territory FromJson(JsonObject data) {

        var centroidData = data["centroid"].AsObject();

        var neighbourIds = new List<string>();
        foreach (var id in data["neighborIds"].AsArray())
            neighbourIds.Add(id.GetValue<string>());

        return new Territory {
            id = data["id"].GetValue<string>(),
            name = data["name"].GetValue<string>(),
            map_colour = data["color"].GetValue<string>(),
            owning_region_id = data["regionId"].GetValue<string>(),
            centroid = new Vector2(centroidData["x"].GetValue<float>(), centroidData["y"].GetValue<float>()),
            neighbour_ids = neighbourIds,
        };
    }

    public void AddTroops(int amount) => troop_count += amount;
    public void RemoveTroops(int amount) => troop_count = Math.Max(0, troop_count - amount);
    internal void AddNeighbour(Territory territory) => _neighbours.Add(territory);

    // ----- // GETTERS AND SETTERS // ----- //

    // Set Methods //

    public Player Owner {
        get => owner;
        set {
            if (owner == value) return;
            Player previous = owner;
            owner = value;
            OnTerritoryOwnerChanged?.Invoke(this, previous, owner);
        }
    }

    internal void SetRegion(Region r) => region = r;
    public void SetTroops(int count) => troop_count = count;

    // Get Methods //

    public override string ToString() => $"Territory({id}, Owner={Owner?.name ?? "none"})";
    public bool IsOwned => owner != null;
    public bool IsAdjacentTo(Territory territory) => neighbours.Contains(territory);
}
