using Godot;

public class Player {

    protected GameManager manager;
    public int id { get; init; }
    public string name { get; init; }
    public Color colour { get; init; }

    protected Player(GameManager _manager, int _id, string _name, Color _colour) {
        manager = _manager;
        id = _id;
        name = _name;
        colour = _colour;
    }

    public void RequestTurn(int game_state) {
        switch (game_state) {
            case 1: RequestClaim(); break;
            case 2: RequestPlay(); break;
        }
    }

    protected virtual void RequestClaim() { }
    protected virtual void RequestPlay() { }
}

public class LocalPlayer : Player {

    public LocalPlayer(GameManager _manager, int _id, string _name, Color _colour)
        : base(_manager, _id, _name, _colour) { }

    protected override void RequestClaim() { }

    protected override void RequestPlay() { }
}

public class BotPlayer : Player {

    public BotPlayer(GameManager _manager, int _id, string _name, Color _colour)
        : base(_manager, _id, _name, _colour) { }

    protected override void RequestClaim() {}

    protected override void RequestPlay() { }
}