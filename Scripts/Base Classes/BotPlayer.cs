using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RiskUtils;

public class BotPlayer : Player {

    public BotPlayer(GameManager _manager, string _name, string _colour)
        : base(_manager, _name, _colour) { type = PlayerType.BOT; }

    Random random = new Random();

    // ----- // GENERAL // ----- // 

    public Territory GetConsolidationTarget(bool claimancy_mode) => GetAllConsolidationTargets(claimancy_mode)[0];
    public List<Territory> GetAllConsolidationTargets(bool claimancy_mode) {

        // Gets the full set of owned territories
        // Organises them by region, split into seperate groups
        // Shuffles each group internally then combines them into a single list where in descending group size order
        // Returns their neighbours in the same order as the previous list (though shuffled within the neighbours)
        // Can be used to bias consolidation to larger groups of owned land within a single region

        Dictionary<Region, List<Territory>> region_groups = new();
        foreach (Territory t in manager.GetPlayerTerritories(this)) {
            Region key = t.region;
            if (!region_groups.ContainsKey(key))
                region_groups[key] = new List<Territory>();
            region_groups[key].Add(t);
        }

        List<Territory> priority_list = region_groups.Values.Select(group => manager.Shuffle(group)).OrderByDescending(group => group.Count).SelectMany(group => group).ToList();
        List<Territory> potential_targets = new();

        // Add only if it is within the same region (biases region control)

        foreach (Territory territory in priority_list) {
            foreach (Territory neighbour in manager.Shuffle(territory.neighbours.ToList())) {
                if (neighbour.region == territory.region) {
                    if ((claimancy_mode && neighbour.Owner == null) || (!claimancy_mode && neighbour.Owner != this)) {
                        if (!potential_targets.Contains(neighbour))
                                potential_targets.Add(neighbour);
                    }
                }
            }
        }

        // Add all ignoring region

        foreach (Territory territory in priority_list) {
            foreach (Territory neighbour in territory.neighbours) {
                if ((claimancy_mode && neighbour.Owner == null) || (!claimancy_mode && neighbour.Owner != this)) {
                    if (!potential_targets.Contains(neighbour))
                            potential_targets.Add(neighbour);
                }
            }
        }

        return potential_targets;
    }

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
        if (manager.GetPlayerTerritories(this).Count == 0) {
            ClaimRandom();
            return;
        }

       manager.SpeakClaim(this, GetConsolidationTarget(true));
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