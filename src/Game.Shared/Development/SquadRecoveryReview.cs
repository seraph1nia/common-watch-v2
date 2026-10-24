// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
namespace Game.Shared;

public sealed record RecoveryReviewCase(string Name, MatchSnapshot Before, SquadRecoveryPlan Plan,
    VeteranOptions[] Comparisons, MatchSnapshot AfterMoves, MatchSnapshot AfterPayment, int Confirmed, string Status);
public sealed record RecoveryEquivalence(bool SameBefore, bool SameAfterMoves, bool SameAfterPayment, int IndividualCommands, int BatchCommands);
public sealed record ReplacementReviewCase(string Name, UnitState Before, RecruitmentQuote Quote, ResourceCost Stock,
    ResourceCost AfterRetirement, ResourceCost AfterRecruitment, UnitState Recruit, bool SpaceBefore);
public static class SquadRecoveryReview
{
    public static int WoundedSquad(CityState city) => city.Soldiers.Where(u => u.Assignment is { Stored: false } && u.Health < u.Profile.Health)
        .GroupBy(u => u.SquadId).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
    public static void Accept(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
    public static SoloSession Create(string name)
    {
        var solo = name is "early-healthy" or "early-wounded" or "no-hall" ? ScenarioRunner.CreateAt("post-draft", 1)
            : GameplayScenarios.CreateAt(name == "late-full-field" ? "late" : "town-hall");
        try
        {
            if (name == "early-healthy") { Accept(solo.Send("buy-home")); Accept(solo.Send("recruit", 1, squadId: -1)); }
            if (name == "early-wounded") Accept(solo.Send("build", 4, Building.TownHall));
            if (name is "recovered-return" or "return-blocked")
            {
                var healthy = solo.City.Soldiers.First(u => u.Assignment is { Stored: false } && u.Health == u.Profile.Health);
                Accept(solo.Send("store", solo.City.Army!.Halls[0].Slot, unitId: healthy.Id));
                if (name == "return-blocked")
                    while (solo.City.Soldiers.Count(u => u.Assignment is { Stored: false }) < solo.City.Army.PurchasedHomes * solo.City.Army.SquadCapacity)
                        Accept(solo.Send("recruit", 2));
            }
            if (name is "full-hall" or "partial-hall")
            {
                int squad = WoundedSquad(solo.City); var hall = solo.City.Army!.Halls[0];
                int used = solo.City.Soldiers.Count(u => u.Assignment is { Stored: true });
                int free = name == "partial-hall" ? 1 : 0;
                foreach (var u in solo.City.Soldiers.Where(u => u.Assignment is { Stored: false } && u.SquadId != squad).Take(hall.Capacity - used - free).ToArray())
                    Accept(solo.Send("store", hall.Slot, unitId: u.Id));
            }
            if (name == "food-shortage")
            {
                int market = Array.FindIndex(solo.City.Slots, s => s.Type == Building.Market);
                int units = solo.Snapshot.MarketRates.Single(r => r.Resource == Resource.Food).Units;
                int bundles = solo.City.Food / units;
                if (bundles > 0) Accept(solo.Send("trade", market, resource: Resource.Food, bundles: bundles));
                foreach (int slot in Enumerable.Range(0, 9).Where(i => solo.City.Slots[i].Type == Building.Farm).ToArray()) Accept(solo.Send("sell", slot));
                if (solo.City.FoodForecast!.Unfed.Length == 0)
                {
                    var field = solo.City.Soldiers.First(u => u.Assignment is { Stored: false });
                    Accept(solo.Send("store", solo.City.Army!.Halls[0].Slot, unitId: field.Id));
                    Accept(solo.Send("recruit", 2));
                }
                if (solo.City.FoodForecast!.Unfed.Length == 0) throw new InvalidOperationException("Legal shortage setup must expose an actual unfunded member.");
            }
            return solo;
        }
        catch { solo.Dispose(); throw; }
    }
    public static RecoveryReviewCase[] Collect()
    {
        return new[] { "early-healthy", "early-wounded", "no-hall", "obsolete-equipment", "late-full-field", "food-shortage", "full-hall", "partial-hall", "recovered-return", "return-blocked" }.Select(name =>
        {
            using var solo = Create(name); var before = solo.Snapshot;
            int squad = name == "early-healthy" ? solo.City.Soldiers.MaxBy(u => u.Id)!.SquadId : WoundedSquad(solo.City);
            var plan = SquadRecovery.Project(before, solo.PlayerId, squad, solo.City.Army!.Halls.FirstOrDefault()?.Slot ?? -1, name is "recovered-return" or "return-blocked" ? RecoveryMove.ReturnRecovered : RecoveryMove.StoreWounded);
            var comparisons = solo.City.Soldiers.Where(u => u.SquadId == squad && u.Assignment is { Stored: false }).Select(u => VeteranComparison.Project(before, solo.City, u.Id)).ToArray();
            var sequence = new RecoverySequence(plan); Execute(solo, sequence);
            var after = solo.Snapshot;
            Accept(solo.Send("ready"));
            return new RecoveryReviewCase(name, before, plan, comparisons, after, solo.Snapshot, sequence.Completed, sequence.Status);
        }).ToArray();
    }
    public static void Execute(SoloSession solo, RecoverySequence sequence)
    {
        for (int n = 0; n < 40 && !sequence.Finished && sequence.Stopped.Length == 0; n++)
        {
            var intent = sequence.Next(solo.Snapshot);
            if (intent is null) continue;
            Accept(solo.Submit(intent)); sequence.Submitted(solo.LastCommand!); sequence.Acknowledge(solo.LastCommand!, solo.LastResult!);
        }
        sequence.Next(solo.Snapshot);
        if (!sequence.Finished && sequence.Stopped.Length == 0) throw new InvalidOperationException("Recovery sequence exceeded its bound.");
    }
    public static ReplacementReviewCase[] Replacements() => new[] { "early-wounded", "obsolete-equipment", "late-full-field" }.Select(name =>
    {
        using var solo = Create(name); var unit = solo.City.Soldiers.Where(u => u.Assignment is { Stored: false } && u.Health < u.Profile.Health).OrderBy(u => u.Level).ThenBy(u => u.Id).First();
        var option = VeteranComparison.Project(solo.Snapshot, solo.City, unit.Id).Replacements.OrderByDescending(r => r.Quote.Level).First();
        var stock = solo.City.Resources; Accept(solo.Send("retire", unitId: unit.Id)); var retired = solo.City.Resources;
        int id = solo.City.Soldiers.Max(u => u.Id); Accept(solo.Send("recruit", option.TrainerSlot, soldierType: unit.Type));
        var recruit = solo.City.Soldiers.Single(u => u.Id > id);
        if (retired != stock || !stock.TryPay(option.Quote.Cost, out var expected) || solo.City.Resources != expected || recruit.Experience != 0)
            throw new InvalidOperationException("Replacement must charge its exact quote with no retirement refund or inherited service.");
        return new ReplacementReviewCase(name, unit, option.Quote, stock, retired, solo.City.Resources, recruit, option.SpaceNow);
    }).ToArray();
    public static object RewardThenProduction()
    {
        var attempts = new List<string>();
        for (int attempt = 0; attempt < 16; attempt++)
        {
            string match = "recovery-healing-" + attempt; attempts.Add(match);
            using var solo = GameplayScenarios.CreateAt("town-hall", matchId: match);
            var plan = SquadRecovery.Project(solo.Snapshot, solo.PlayerId, WoundedSquad(solo.City), solo.City.Army!.Halls[0].Slot, RecoveryMove.StoreWounded);
            Execute(solo, new(plan)); Accept(solo.Send("ready")); GameplayScenarios.FinishCombat(solo);
            if (solo.Snapshot.Phase != Phase.RewardDraft || !solo.City.Run.Offers.Contains("loot-recovery")) continue;
            var quote = solo.City.Run.Quotes.Single(q => q.Id == "loot-recovery");
            var before = solo.Snapshot; Accept(solo.ChooseReward("loot-recovery")); var afterReward = solo.Snapshot;
            string settled = JsonSerializer.Serialize(afterReward, WireJson.Options); Accept(solo.RetryLast()!);
            if (settled != JsonSerializer.Serialize(solo.Snapshot, WireJson.Options)) throw new InvalidOperationException("Recovery reward retry repeated a heal.");
            var stored = solo.City.Soldiers.Where(u => u.Assignment is { Stored: true }).ToArray();
            int hall = solo.City.Army!.Halls[0].HealingPercent;
            Accept(solo.Send("ready"));
            foreach (var u in stored)
                if (solo.City.Soldiers.Single(a => a.Id == u.Id).Health != (u.RecoveryEligible ? ArmyConfiguration.HealedHealth(u.Health, u.Profile.Health, hall) : u.Health))
                    throw new InvalidOperationException("Production must heal from the post-reward HP exactly once.");
            return new { attempts, before, quote, afterReward, afterProduction = solo.Snapshot };
        }
        throw new InvalidOperationException("Bounded legal search found no recovery offer after storing wounded members.");
    }
    public static object UnfundedStorage()
    {
        using var solo = Create("food-shortage");
        var plan = SquadRecovery.Project(solo.Snapshot, solo.PlayerId, WoundedSquad(solo.City), solo.City.Army!.Halls[0].Slot, RecoveryMove.StoreWounded);
        Execute(solo, new(plan)); Accept(solo.Send("ready")); var afterPayment = solo.Snapshot;
        int id = plan.Moving.Select(m => m.Before.Id).First(u => !solo.City.LastUpkeep!.Funded.Contains(u));
        GameplayScenarios.FinishCombat(solo);
        if (solo.Snapshot.Phase != Phase.RewardDraft) throw new InvalidOperationException("Shortage fixture needs a real surviving clear to verify future eligibility.");
        Accept(solo.ChooseReward(solo.City.Run.Offers.First(o => o != "loot-recovery")));
        var before = solo.City.Soldiers.Single(u => u.Id == id);
        if (before.RecoveryEligible) throw new InvalidOperationException("Unfunded storage cannot retain recovery eligibility.");
        Accept(solo.Send("ready")); var after = solo.City.Soldiers.Single(u => u.Id == id);
        if (after.Health != before.Health) throw new InvalidOperationException("Unfunded storage gained a free heal.");
        return new { afterPayment, member = id, before, after, nextState = solo.Snapshot };
    }
    public static RecoveryEquivalence Compare()
    {
        using var individual = Create("obsolete-equipment"); using var batch = Create("obsolete-equipment");
        string Snapshot(SoloSession session) => JsonSerializer.Serialize(session.Snapshot, WireJson.Options);
        bool before = Snapshot(individual) == Snapshot(batch);
        var plan = SquadRecovery.Project(batch.Snapshot, batch.PlayerId, WoundedSquad(batch.City), batch.City.Army!.Halls[0].Slot, RecoveryMove.StoreWounded);
        foreach (var m in plan.Moving) Accept(individual.Send("store", plan.Hall, unitId: m.Before.Id));
        var sequence = new RecoverySequence(plan); Execute(batch, sequence);
        bool moved = Snapshot(individual) == Snapshot(batch);
        Accept(individual.Send("ready")); Accept(batch.Send("ready"));
        return new(before, moved, Snapshot(individual) == Snapshot(batch), plan.Moving.Length, sequence.Completed);
    }
}
