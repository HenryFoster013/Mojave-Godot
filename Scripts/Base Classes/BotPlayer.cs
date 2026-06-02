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

    public Territory GetConsolidationTarget(bool claimancy_mode) {
        List<Territory> targets = GetAllConsolidationTargets(claimancy_mode);
        if (targets.Count > 0)
            return targets[0];
        return null;
    }

    public List<Territory> GetAllConsolidationTargets(bool claimancy_mode) {

        /*
            Gets all owned territories, groups them by region and shuffles the groups internally.
            Concats each group (largest first) into a single list.
            Loops through each territory and checks if they have any unclaimed neighbours inside the same region
            Loops through again and adds any unclaimed neighbours outside of the same region
            Returns the list of potential targets.
        */

        List<Territory> priority_list = manager.GetPlayerTerritories(this)
            .GroupBy(t => t.region)
            .OrderByDescending(g => g.Count())
            .SelectMany(g => manager.Shuffle(g.ToList()))
            .ToList();

        List<Territory> potential_targets = new();
        HashSet<Territory> touched_territories = new();

        void TryAdd(Territory territory) {
            if (touched_territories.Add(territory)) {
                if((claimancy_mode && territory.Owner == null) || (!claimancy_mode && territory.Owner != this))
                    potential_targets.Add(territory);
            }
        }

        foreach (Territory territory in priority_list) {
            foreach (Territory neighbour in manager.Shuffle(territory.neighbours.ToList())) {
                if (neighbour.region == territory.region)
                    TryAdd(neighbour);
            }
        }

        foreach (Territory territory in priority_list) {
            foreach (Territory neighbour in territory.neighbours) {
                TryAdd(neighbour);
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

        Territory target = GetConsolidationTarget(true);
        if(target != null) {
            manager.SpeakClaim(this, GetConsolidationTarget(true));
            return;
        }

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