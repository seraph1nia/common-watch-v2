using Game.Core;

namespace Game.Shared;

/// <summary>Bounded default-rule campaign replay for UI and adapter verification. No state injection.</summary>
public static class GameplayScenarios
{
    public static IReadOnlyList<string> Checkpoints { get; } = Array.AsReadOnly<string>(
        ["major-10", "boss-10", "boss-20", "economy", "army", "town-hall", "research", "forecast", "reward", "late", "research-full", "research-open", "victory", "defeat"]);
    public static GameplayIntent? NextPreparation(MatchSnapshot state, int player, string family = "frontline")
    {
        if (state.Phase is not (Phase.Building or Phase.Preparation)) return null;
        var action = GameplayCampaignPolicy.Next(state, player, family);
        if (action is null) return null;
        GameplayAction kind = Enum.GetValues<GameplayAction>().First(a => GameplayIntent.Name(a) == action.Action);
        return new(kind, action.Slot, action.Building, action.Unit, action.Technology, action.Resource, action.Bundles, action.Payment, action.UnitId);
    }
    public static string RewardChoice(RunState run) => GameplayCampaignPolicy.RewardChoice(run);
    public static SoloSession CreateAt(string checkpoint, bool enrich = true, Rules? rules = null, ulong seed = 1, string? matchId = null)
    {
        if (checkpoint == "boss-10") checkpoint = "major-10"; // Historical CLI alias; current catalog owns the encounter.
        if (!Checkpoints.Contains(checkpoint)) throw new ArgumentException("Unknown gameplay scenario: " + checkpoint);
        var solo = new SoloSession(seed: seed, matchId: matchId, rules: rules);
        try
        {
            Require(solo.Start());
            if (checkpoint == "defeat")
            {
                // No investment: actual enemies defeat the default Keep through ordinary battle progression.
                for (int stage = 0; stage < solo.Snapshot.Rules.PreparationStages; stage++) Require(solo.Send("ready"));
                FinishCombat(solo);
                if (solo.Snapshot.Phase != Phase.Defeat) throw new InvalidOperationException("Uninvested defense unexpectedly survived.");
                return solo;
            }
            int target = checkpoint switch { "major-10" => 10, "boss-20" => 20, "reward" => 2, "forecast" => 8, "economy" => 8, "army" => 8, "town-hall" => 8, "research" => 12, "research-full" => 20, "research-open" => 14, "late" => 16, _ => 21 };
            string family = checkpoint is "army" ? "mixed" : checkpoint == "town-hall" ? "reserve" : checkpoint is "research" or "research-full" or "research-open" ? "research" : "frontline";
            for (int wave = 1; wave <= 20; wave++)
            {
                if (solo.Snapshot.Phase == Phase.RewardDraft)
                {
                    if (checkpoint == "reward") return solo;
                    Require(solo.ChooseReward(GameplayCampaignPolicy.RewardChoice(solo.City.Run)));
                }
                if (solo.Snapshot.Phase is Phase.Defeat or Phase.Victory) break;
                for (int stage = 1; stage <= solo.Snapshot.Rules.PreparationStages; stage++)
                {
                    for (int decision = 0; decision < 100; decision++)
                    {
                        var action = GameplayCampaignPolicy.Next(solo.Snapshot, solo.PlayerId, family, purchaseResearch: checkpoint != "research-open");
                        if (action is null) break;
                        Require(solo.Send(action.Action, action.Slot, action.Building, action.Unit, action.Payment,
                            action.Technology, action.Resource, action.Bundles, action.UnitId));
                        if (decision == 99) throw new InvalidOperationException("Campaign policy exceeded decision bound.");
                    }
                    if (wave >= target && stage == solo.Snapshot.Rules.PreparationStages) { if (enrich) Enrich(solo, checkpoint); solo.ResetPlayback(); return solo; }
                    Require(solo.Send("ready"));
                }
                FinishCombat(solo);
            }
            if (checkpoint != "victory" || solo.Snapshot.Phase != Phase.Victory)
                throw new InvalidOperationException($"Gameplay scenario {checkpoint} failed: {solo.Snapshot.Phase}, wave {solo.Snapshot.Wave}.");
            solo.ResetPlayback(); return solo;
        }
        catch { solo.Dispose(); throw; }
    }
    public static void FinishCombat(SoloSession solo)
    {
        for (int step = 0; step < 18000 && (solo.Snapshot.Phase == Phase.Combat || solo.Snapshot.DyingBodies.Length > 0); step++) solo.Step();
        if (solo.Snapshot.Phase == Phase.Combat || solo.Snapshot.DyingBodies.Length > 0) throw new InvalidOperationException("Combat replay exceeded bounded duration.");
    }
    private static void Enrich(SoloSession solo, string checkpoint)
    {
        if (checkpoint == "forecast")
        {
            int market = Array.FindIndex(solo.City.Slots, s => s.Type == Building.Market);
            int foodBundle = solo.Snapshot.MarketRates.Single(r => r.Resource == Resource.Food).Units;
            int bundles = Math.Min(2, solo.City.Food / foodBundle);
            if (bundles > 0) Require(solo.Send("trade", market, resource: Resource.Food, bundles: bundles));
            // Immediate-only rewards alter the earned surplus. If a complete
            // Food bundle remains unavailable, remove future Farm income through
            // its ordinary refund instead of inventing stock or forcing payment.
            for (int slot = 0; slot < solo.City.Slots.Length && solo.City.FoodForecast is { Unfed.Length: 0 }; slot++)
                if (solo.City.Slots[slot].Type == Building.Farm) Require(solo.Send("sell", slot));
            Require(solo.Send("buy-plot", 7));
            Require(solo.Send("build", 7, Building.ArrowTower));
            if (solo.City.FoodForecast is not { Unfed.Length: > 0, Participating.Length: > 0 })
                throw new InvalidOperationException("Food shortage scenario must earn partial funding.");
        }
        if (checkpoint == "army")
        {
            // The reference policy can defer the ranged home while replacing melee.
            // Earn its last land and equipment by an explicit legal surplus sale.
            if (!solo.City.Soldiers.Any(u => u.Type == UnitType.Crossbowman))
            {
                int market = Array.FindIndex(solo.City.Slots, s => s.Type == Building.Market);
                Require(solo.Send("trade", market, resource: Resource.Cloth, bundles: 3));
                Require(solo.Send("buy-home"));
                int trainer = Array.FindIndex(solo.City.Slots, s => s.Type == Building.ArcheryRange);
                Require(solo.Send("recruit", trainer, soldierType: UnitType.Crossbowman, squadId: -1));
            }
        }
        if (checkpoint == "town-hall")
        {
            var hall = solo.City.Army!.Halls.FirstOrDefault();
            if (hall is null) throw new InvalidOperationException("Reserve policy did not earn a Town Hall.");
            if (hall.CapacityLevel < 2)
            {
                Require(solo.Send("upgrade-capacity", hall.Slot));
                hall = solo.City.Army!.Halls.Single(h => h.Slot == hall.Slot);
            }
            var member = solo.City.Soldiers.FirstOrDefault(u => u.RecoveryEligible && u.Assignment is { Stored: false });
            if (member is not null) Require(solo.Send("store", hall.Slot, unitId: member.Id));
        }
    }
    private static void Require(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException("Legal gameplay replay: " + result.Message); }
}
