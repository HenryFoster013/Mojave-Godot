using System;
using static RiskUtils;

public class Player {

    protected GameManager manager;
    public int id { get; private set; }
    public string name { get; init; }
    public string colour { get; init; }

    public PlayerType type;

    public int currency { get; private set; }

    // ----- // INSTANTIATION // ----- //

    protected Player(GameManager _manager, string _name, string _colour) {
        manager = _manager;
        name = _name;
        colour = _colour;
        type = PlayerType.NULL;
    }

    // ----- // OVERRIDES // ----- //

    public virtual void RequestClaim() { }
    public virtual void RequestPlacement() { }
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

    public LocalPlayer(GameManager _manager, string _name, string _colour)
        : base(_manager, _name, _colour) { type = PlayerType.LOCAL; }

    public override void RequestClaim() { }
    public override void RequestPlacement() { }
    public override void RequestPlay() { }
}

public class BotPlayer : Player {

    public BotPlayer(GameManager _manager, string _name, string _colour)
        : base(_manager, _name, _colour) { type = PlayerType.BOT; }

    Random random = new Random();

    // ----- // CLAIMS // ----- //

    public override void RequestClaim() {

        int dice_roll = random.Next(TOTAL_DECISION_CHANCE);

        IReadOnlyCollection<Territory> our_missing_pieces = manager.GetMissingRegionPiece(this);
        IReadOnlyCollection<Territory> their_missing_pieces = manager.GetOtherMissingRegionPieces(this);

        if (our_missing_piece.Count > 0) 
            return ClaimOurMissingPiece(our_missing_pieces);

        if (their_missing_piece.Count > 0 && dice_roll < MISTAKE_CHANCE)
            return ClaimTheirMissingPiece(their_missing_pieces, dice_roll);

        IReadOnlyCollection our_territories = manager.GetPlayerTerritories(this);
        dice_roll = random.Next(TOTAL_DECISION_CHANCE); // Re-roll prevents a failed claim missing piece always returning a random tile.

        if (our_territories.Count == 0 || dice_roll < MISTAKE_CHANCE)
            return ClaimRandom();

        if (dice_roll < MISTAKE_CHANCE + CONSOLIDATION_CHANCE)
            return ClaimConsolidation();
        
        ClaimDisruption();
    }

    private void ClaimOurMissingPiece(IReadOnlyCollection<Territory> our_missing_pieces) {
        manager.ClaimTerritory(this, our_missing_pieces.ElementAt(random.Next(our_missing_pieces.Count)));
    }

    private void ClaimTheirMissingPiece(IReadOnlyCollection<Territory> their_missing_pieces) {
        manager.ClaimTerritory(this, their_missing_pieces.ElementAt(random.Next(their_missing_pieces.Count)));
    }

    private void ClaimDisruption() {
        
        // Get a list of free tiles, pick a random one
        // Check if it has a neighbour that is an enemy, if so claim
        // If not increment pointer and try again
        // If the pointer exits the bound of the count of territories, loop round
        // If the original tile is reached, return ClaimRandom();

        IReadOnlyCollection null_territories = manager.GetPlayerTerritories(null);

        int start_pointer = random.Next(null_territories.Count);
        int current_pointer = start_pointer;
        IList<Territory> null_list = null_territories as IList<Territory>;

        do {
            
            Territory candidate = null_list[current_pointer];
            
            foreach (Territory neighbour in candidate.Neighours) {
                if (neighbour.Owner != null && neighbour.Owner != this){
                    manager.SpeakClaim(this, candidate);
                    return;
                }
            }

            pointer = (pointer + 1) % null_list.Count;
            
        } while (pointer != start)

        ClaimRandom();
    }

    private void ClaimConsolidation() {
        // Get a list of our owned tiles, pick a random one
        // Check if it has an unclaimed neighbour
        // If not, increment and loop round
        // If so, claim
        // If the original tile is reached, return ClaimRandom();

        IReadOnlyCollection owned_territories = manager.GetPlayerTerritories(this);

        if (owned_territories.Count == 0) {
            ClaimRandom();
            return;
        }

        int start_pointer = random.Next(owned_territories.Count);
        int current_pointer = start_pointer;
        IList<Territory> owned_list = owned_territories as IList<Territory>;

        do {
            
            Territory candidate = owned_list[current_pointer];
            
            foreach (Territory neighbour in candidate.Neighours) {
                if (neighbour.Owner == null){
                    manager.SpeakClaim(this, candidate);
                    return;
                }
            }

            pointer = (pointer + 1) % null_list.Count;
            
        } while (pointer != start)

        ClaimRandom();
    }

    private void ClaimRandom() {
        IReadOnlyCollection null_territories = manager.GetPlayerTerritories(null);
        manager.ClaimTerritory(this, null_territories[random.Next(null_territories.Count)]);
    }


    // ----- // PLACEMENTS // ----- //

    public override void RequestPlacement() {

    }

    // ----- // PLAYS // ----- //

    public override void RequestPlay() { 
        
    }
}
