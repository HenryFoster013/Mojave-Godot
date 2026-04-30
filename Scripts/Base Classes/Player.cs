using Godot;

public class Player {

    protected GameManager manager;
    public int id { get; private set; }
    public string name { get; init; }
    public Color colour { get; init; }

    public enum player_type { NULL, LOCAL, BOT }
    public player_type type;

    public int currency { get; private set; }

    // ----- // INSTANTIATION // ----- //

    protected Player(GameManager _manager, string _name, Color _colour) {
        manager = _manager;
        name = _name;
        colour = _colour;
        type = player_type.NULL;
    }

    // ----- // OVERRIDES // ----- //

    public virtual void RequestClaim() { }
    public virtual void RequestPlay() { }

    // ----- // GETTERS AND SETTERS // ----- //

    // Set Methods //

    public void AddCurrency(int amount) => currency += amount;
    public void SetCurrency(int amount) => currency = amount;
    public bool CanAfford(int amount) => currency >= amount;
    public bool SpareChange() => currency > 0;

    public void SubCurrency(int sub) {
        if (currency - sub < 0)
            return;
        currency -= sub;
    }

    // Get Methods //

    public void SetId(int new_id) => id = new_id;
}

public class LocalPlayer : Player {

    public LocalPlayer(GameManager _manager, string _name, Color _colour)
        : base(_manager, _name, _colour) { type = player_type.LOCAL; }

    public override void RequestClaim() { }
    public override void RequestPlay() { }
}

public class BotPlayer : Player {

    public BotPlayer(GameManager _manager, string _name, Color _colour)
        : base(_manager, _name, _colour) { type = player_type.BOT; }

    public override void RequestClaim() {}
    public override void RequestPlay() { }
}
