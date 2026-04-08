using Godot;
using System.Collections.Generic;

public class Region {

    // ----- // VARIABLES & FIELDS // ----- //

    // Constant JSON Values //

    public string id { get; init; }
    public string name { get; init; }
    public Color colour { get; init; }
    public Vector2 centroid { get; init; }
    public IReadOnlyList<string> territory_ids { get; init; }

    // Variable Values //

    private List<Territory> territories = new();
    public IReadOnlyList<Territory> Territories => territories;
    internal void AddTerritory(Territory territory) => territories.Add(territory);

    // ----- // FUNCTIONALITY // ----- //

    public static Region FromJson(Godot.Collections.Dictionary data) {
        Color color = Color.FromString(data["color"].AsString(), Colors.Magenta);

        var centroidData = data["centroid"].AsGodotDictionary();

        var ids = new List<string>();
        foreach (var id in data["tileIds"].AsGodotArray())
            ids.Add(id.AsString());

        return new Region {
            id = data["id"].AsString(),
            name = data["name"].AsString(),
            colour = color,
            centroid = new Vector2(
                centroidData["x"].AsSingle(),
                centroidData["y"].AsSingle()),
            territory_ids = ids,
        };
    }

    // ----- // GETTERS & SETTERS // ----- //

    // Get Methods //

    public override string ToString() => $"Region({id}, {territories.Count} territories)";

    // Set Methods //
}