using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RiskUtils;

public class BotPlayer : Player {

    public BotPlayer(GameManager _manager, string _name, string _colour)
        : base(_manager, _name, _colour) { type = PlayerType.BOT; }

    Random random = new Random();

    // ----- // CLAIMS // ----- //

    public override async void RequestClaim() {

        await Task.Delay(TimeSpan.FromSeconds(CLAIM_DELAY));

        int dice_roll = random.Next(TOTAL_DECISION_CHANCE);

        IReadOnlyCollection<Territory> our_missing_pieces = manager.GetMissingRegionPieces(this).Where(t => t.Owner == null).ToList();
        IReadOnlyCollection<Territory> their_missing_pieces = manager.GetOtherMissingRegionPieces(this).Where(t => t.Owner == null).ToList();
    

        if (our_missing_pieces.Count > 0) {
            ClaimOurMissingPiece(our_missing_pieces);
            return;
        }

        if (their_missing_pieces.Count > 0 && dice_roll < IMMENENT_DISRUPTION_CHANCE) {
            ClaimTheirMissingPiece(their_missing_pieces);
            return;
        }

        IReadOnlyCollection<Territory> our_territories = manager.GetPlayerTerritories(this);
        dice_roll = random.Next(TOTAL_DECISION_CHANCE); // Re-roll prevents a failed claim missing piece always returning a random tile.

        if (our_territories.Count == 0 || dice_roll < MISTAKE_CHANCE) {
            ClaimRandom();
            return;
        }

        if (dice_roll < MISTAKE_CHANCE + CONSOLIDATION_CHANCE) {
            ClaimConsolidation();
            return;
        }
        
        ClaimDisruption();
    }

    private void ClaimOurMissingPiece(IReadOnlyCollection<Territory> our_missing_pieces) {
        manager.SpeakClaim(this, our_missing_pieces.ElementAt(random.Next(our_missing_pieces.Count)));
    }

    private void ClaimTheirMissingPiece(IReadOnlyCollection<Territory> their_missing_pieces) {
        manager.SpeakClaim(this, their_missing_pieces.ElementAt(random.Next(their_missing_pieces.Count)));
    }

    private void ClaimDisruption() {
        
        // Get a list of free tiles, pick a random one
        // Check if it has a neighbour that is an enemy, if so claim
        // If not increment pointer and try again
        // If the pointer exits the bound of the count of territories, loop round
        // If the original tile is reached, return ClaimRandom();

        IReadOnlyCollection<Territory> null_territories = manager.GetFreeTerritories();

        int start_pointer = random.Next(null_territories.Count);
        int current_pointer = start_pointer;
        IList<Territory> null_list = null_territories.ToList();

        do {
            
            Territory candidate = null_list[current_pointer];
            
            foreach (Territory neighbour in candidate.neighbours) {
                if (neighbour.Owner != null && neighbour.Owner != this){
                    manager.SpeakClaim(this, candidate);
                    return;
                }
            }

            current_pointer = (current_pointer + 1) % null_list.Count;
            
        } while (current_pointer != start_pointer);

        ClaimRandom();
    }

    private void ClaimConsolidation() {
        // Get a list of our owned tiles, pick a random one
        // Check if it has an unclaimed neighbour
        // If not, increment and loop round
        // If so, claim
        // If the original tile is reached, return ClaimRandom();

        // CHANGE : Needs to bias tiles within the same region.

        IReadOnlyCollection<Territory> owned_territories = manager.GetPlayerTerritories(this);

        if (owned_territories.Count == 0) {
            ClaimRandom();
            return;
        }

        int start_pointer = random.Next(owned_territories.Count);
        int current_pointer = start_pointer;
        IList<Territory> owned_list = owned_territories.ToList();

        do {
        
            Territory candidate = owned_list[current_pointer];
            
            foreach (Territory neighbour in candidate.neighbours) {
                if (neighbour.Owner == null){
                    manager.SpeakClaim(this, neighbour);
                    return;
                }
            }

            current_pointer = (current_pointer + 1) % owned_list.Count;
            
        } while (current_pointer != start_pointer);

        ClaimRandom();
    }

    private void ClaimRandom() {
        IReadOnlyCollection<Territory> null_territories = manager.GetFreeTerritories();
        manager.SpeakClaim(this, null_territories.ElementAt(random.Next(null_territories.Count)));
    }


    // ----- // PLACEMENTS // ----- //

    public override void RequestPlacement() {

    }

    // ----- // PLAYS // ----- //

    public override void RequestPlay() { 
        
    }
}