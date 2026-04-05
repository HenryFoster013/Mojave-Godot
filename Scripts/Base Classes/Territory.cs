using Godot;
using System.Collections.Generic;

public class Territory {

    // ----- // VARIABLES & FIELDS // ----- //

    // Constant JSON Values //

    public string id { get; init; }
    public string name { get; init; }
    public Color map_colour { get; init; }
    public string owning_region_id { get; init; }
    public Vector2 centroid { get; init; }
    public IReadOnlyList<string> neighbour_ids { get; init; }

    // Variable Values //

    private Player owner;
    private Region region;
    public Region Region => region;
    public int troop_count { get; private set; }

    private List<Territory> _neighbours = new();
    public IReadOnlyList<Territory> neighbours => _neighbours;

    // ----- // FUNCTIONALITY // ----- //

    public static Territory FromJson(Godot.Collections.Dictionary data) {

        var centroidData = data["centroid"].AsGodotDictionary();
        Color mapColour = Color.FromString(data["color"].AsString(), Colors.Magenta);

        var neighbourIds = new List<string>();
        foreach (var id in data["neighborIds"].AsGodotArray())
            neighbourIds.Add(id.AsString());

        return new Territory {
            id = data["id"].AsString(),
            name = data["name"].AsString(),
            map_colour = mapColour,
            owning_region_id = data["regionId"].AsString(),
            centroid = new Vector2(
                              centroidData["x"].AsSingle(),
                              centroidData["y"].AsSingle()),
            neighbour_ids = neighbourIds,
        };
    }

    public void AddTroops(int amount) => troop_count += amount;
    public void RemoveTroops(int amount) => troop_count = Mathf.Max(0, troop_count - amount);
    internal void AddNeighbour(Territory territory) => _neighbours.Add(territory);

    public event System.Action<Territory, Player, Player> OnOwnerChanged;

    // ----- // GETTERS AND SETTERS // ----- //

    // Set Methods //

    public Player Owner {
        get => owner;
        set {
            if (owner == value) return;
            Player previous = owner;
            owner = value;
            OnOwnerChanged?.Invoke(this, previous, owner);
        }
    }

    internal void SetRegion(Region r) => region = r;
    public void SetTroops(int count) => troop_count = count;

    // Get Methods //

    public override string ToString() => $"Territory({id}, Owner={Owner?.name ?? "none"})";
    public bool IsOwned => owner != null;
}