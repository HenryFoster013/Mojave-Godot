using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Numerics;

public class Region {

    // ----- // VARIABLES & FIELDS // ----- //

    // Constant JSON Values //

    public string id { get; init; }
    public string name { get; init; }
    public string colour { get; init; }
    public Vector2 centroid { get; init; }
    public IReadOnlyList<string> territory_ids { get; init; }
    public int bonus { get; init; }
    public bool complete { get; private set; }
    public Player owner { get; private set; }

    // Variable Values //

    private List<Territory> territories = new();
    public IReadOnlyList<Territory> Territories => territories;
    internal void AddTerritory(Territory territory) => territories.Add(territory);

    public event System.Action OnCompletionChanged;

    // ----- // FUNCTIONALITY // ----- //

    public static Region FromJson(JsonObject data) {
    
        var centroidData = data["centroid"].AsObject();
        
        var ids = new List<string>();
        foreach (var id in data["tileIds"].AsArray())
            ids.Add(id.GetValue<string>());

        return new Region {
            id = data["id"].GetValue<string>(),
            name = data["name"].GetValue<string>(),
            colour = data["color"].GetValue<string>(),
            bonus = data["bonus"].GetValue<int>(),
            centroid = new Vector2(centroidData["x"].GetValue<float>(), centroidData["y"].GetValue<float>()),
            territory_ids = ids,
        };
    }

    public void CheckCompletion() {
        bool new_completion = CompletionLoop();
        if (complete == new_completion)
            return;
        complete = new_completion;
        OnCompletionChanged?.Invoke();
    }

    bool CompletionLoop() {
        if(territories.Count == 0)
            return false;
        Player first_owner = territories[0].Owner;
        if(first_owner == null)
            return false;
        foreach (Territory territory in territories) {
            if(territory.Owner != first_owner)
                return false;
        }
        owner = first_owner;
        return true;
    }

    // ----- // GETTERS & SETTERS // ----- //

    // Get Methods //

    public override string ToString() => $"Region({id}, {territories.Count} territories)";

    // Set Methods //
}
