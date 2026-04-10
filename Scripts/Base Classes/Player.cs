using Godot;

public class Player {

    protected GameManager manager;
    public int id { get; private set; }
    public string name { get; init; }
    public Color colour { get; init; }

    protected Player(GameManager _manager, string _name, Color _colour) {
        manager = _manager;
        name = _name;
        colour = _colour;
    }

    public void RequestTurn(int game_state) {
        switch (game_state) {
            case 1: RequestClaim(); break;
            case 2: RequestPlay(); break;
        }
    }

    // Overrides //

    protected virtual void RequestClaim() { }
    protected virtual void RequestPlay() { }

    // Getters and Setters //

    public void SetId(int new_id) {
        id = new_id;
    }
}

public class LocalPlayer : Player {

    public LocalPlayer(GameManager _manager, string _name, Color _colour)
        : base(_manager, _name, _colour) { }

    protected override void RequestClaim() { }

    protected override void RequestPlay() { }
}

public class BotPlayer : Player {

    public BotPlayer(GameManager _manager, string _name, Color _colour)
        : base(_manager, _name, _colour) { }

    protected override void RequestClaim() {}

    protected override void RequestPlay() { }
}