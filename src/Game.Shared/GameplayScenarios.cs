using Game.Core;

namespace Game.Shared;

/// <summary>Bounded default-rule campaign replay for UI and adapter verification. No state injection.</summary>
public static class GameplayScenarios
{
    public static IReadOnlyList<string> Checkpoints { get; } = Array.AsReadOnly<string>(
        ["economy", "army", "town-hall", "research", "forecast", "reward", "late", "research-full", "research-open", "victory", "defeat"]);
    public static GameplayIntent? NextPreparation(MatchSnapshot state, int player, string family = "frontline")
    {
        if (state.Phase is not (Phase.Building or Phase.Preparation)) return null;
        var action = GameplayCampaignPolicy.Next(state, player, family);
        if (action is null) return null;
        GameplayAction kind = Enum.GetValues<GameplayAction>().First(a => GameplayIntent.Name(a) == action.Action);
        return new(kind, action.Slot, action.Building, action.Unit, action.Technology, action.Resource, action.Bundles, action.Payment, action.UnitId);
    }
    public static string RewardChoice(RunState run) => GameplayCampaignPolicy.RewardChoice(run);
    public static SoloSession CreateAt(string checkpoint)
    {
        if (!Checkpoints.Contains(checkpoint)) throw new ArgumentException("Unknown gameplay scenario: " + checkpoint);
        var solo = new SoloSession(seed: 1);
        try
        {
            Require(solo.Start());
            if (checkpoint == "defeat")
            {
                // No investment: actual enemies defeat the default Keep through ordinary battle progression.
                for (int stage = 0; stage < 3; stage++) Require(solo.Send("ready"));
                FinishCombat(solo);
                if (solo.Snapshot.Phase != Phase.Defeat) throw new InvalidOperationException("Uninvested defense unexpectedly survived.");
                return solo;
            }
            int target = checkpoint switch { "reward" => 2, "forecast" => 8, "economy" => 8, "army" => 8, "town-hall" => 8, "research" => 12, "research-full" => 20, "research-open" => 14, "late" => 16, _ => 21 };
            string family = checkpoint is "army" ? "mixed" : checkpoint == "town-hall" ? "reserve" : checkpoint is "research" or "research-full" or "research-open" ? "research" : "frontline";
            for (int wave = 1; wave <= 20; wave++)
            {
                if (solo.Snapshot.Phase == Phase.RewardDraft)
                {
                    if (checkpoint == "reward") return solo;
                    Require(solo.ChooseReward(GameplayCampaignPolicy.RewardChoice(solo.City.Run)));
                }
                if (solo.Snapshot.Phase is Phase.Defeat or Phase.Victory) break;
                for (int stage = 1; stage <= 3; stage++)
                {
                    for (int decision = 0; decision < 100; decision++)
                    {
                        var action = GameplayCampaignPolicy.Next(solo.Snapshot, solo.PlayerId, family, purchaseResearch: checkpoint != "research-open");
                        if (action is null) break;
                        Require(solo.Send(action.Action, action.Slot, action.Building, action.Unit, action.Payment,
                            action.Technology, action.Resource, action.Bundles, action.UnitId));
                        if (decision == 99) throw new InvalidOperationException("Campaign policy exceeded decision bound.");
                    }
                    if (wave >= target && stage == 3) { Enrich(solo, checkpoint); solo.ResetPlayback(); return solo; }
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
            Require(solo.Send("trade", market, resource: Resource.Food, bundles: 2));
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
            var member = solo.City.Soldiers.FirstOrDefault(u => u.RecoveryEligible && u.Assignment is { Stored: false });
            if (member is not null) Require(solo.Send("store", hall.Slot, unitId: member.Id));
        }
    }
    private static void Require(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException("Legal gameplay replay: " + result.Message); }
}
