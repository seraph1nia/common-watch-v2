// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public sealed record EconomyCommand(Command Command, CommandResult Result, ResourceCost Before, ResourceCost After,
    int ResearchPointsBefore, int ResearchPointsAfter, int KeepHealing, long SoldierHealing, int HealedSoldiers,
    PreparationPayment? Preparation = null);
public sealed record StoredPayment(int UnitId, bool Eligible, int Before, int After, int Maximum, int HealingPercent);
public sealed record PreparationPayment(int Wave, int Turn, int ResourceMultiplier, int ProductionsBefore, int ProductionsAfter,
    ResourceCost Quote, ResearchState ResearchBefore, ResearchState ResearchAfter, int FoodPaid, StoredPayment[] Stored);
public sealed record EconomyShortage(int Wave, int Turn, UnitType Type, int Level, ResourceCost Cost,
    string[] MissingResources, long FoodDemand, int FoodStock);
public sealed record RewardEconomyBattle(int Wave, string Result, long CombatTicks, long CleanupTicks,
    ResourceCost PreparedStock, ResourceCost EndStock, int FoodPaid, int Funded, int Participating, int Unfunded,
    int Casualties, int Wounded, int KeepDamage, ResourceCost ClearGrant, int ClearResearch,
    string[] Offers, string? Choice, string[] Technologies);
public sealed record RewardEconomyRun(string Doctrine, string Family, ulong Seed, string MatchId, ResourceCost Starting,
    RewardEconomyBattle[] Battles, EconomyCommand[] Commands, EconomyShortage[] Shortages);

/// <summary>Matched, bounded ordinary-command policies. No snapshot or simulation writes.</summary>
public static class RewardEconomyReview
{
    public static RewardEconomyRun Run(string doctrine, string family, ulong seed, int? lastWaveLimit = null, Rules? rules = null, string openingOrder = "canonical", bool recordPreparations = false, bool adaptOpening = false)
    {
        using var match = new Match(rules: rules, matchId: $"reward-economy-{doctrine}-{family}-{seed}", combatSeed: seed);
        int player = match.Join()!.Id; long sequence = 0;
        var commands = new List<EconomyCommand>(); var battles = new List<RewardEconomyBattle>();
        var shortages = new List<EconomyShortage>();
        CityState City(MatchSnapshot state) => state.Players.Single(p => p.Id == player);
        void Submit(Command command)
        {
            var beforeState = match.Snapshot(); var before = City(beforeState); var result = match.Apply(player, command);
            if (!result.Accepted) throw new InvalidOperationException($"{doctrine}/{family}/{seed}: {command.Action}: {result.Message}");
            var after = City(match.Snapshot());
            var healed = after.Soldiers.Select(u => before.Soldiers.FirstOrDefault(b => b.Id == u.Id) is { } old
                ? Math.Max(0, (long)u.Health - old.Health) : 0).ToArray();
            PreparationPayment? payment = null;
            if (recordPreparations && command.Action == "ready")
            {
                var stored = before.Soldiers.Where(u => u.Assignment is { Stored: true }).Select(u =>
                {
                    var hall = before.Army!.Halls.Single(h => h.Slot == u.Assignment!.HallSlot);
                    return new StoredPayment(u.Id, u.RecoveryEligible, u.Health, after.Soldiers.Single(a => a.Id == u.Id).Health,
                        u.Profile.Health, hall.HealingPercent);
                }).ToArray();
                payment = new(beforeState.Wave, beforeState.Turn, PreparationSchedule.ResourceMultiplier(match.Rules, beforeState.Turn),
                    beforeState.ProductionCount, match.Snapshot().ProductionCount, before.ProductionIncome!.Value,
                    before.Research, after.Research, match.Phase == Phase.Combat ? after.LastUpkeep!.Paid : 0, stored);
            }
            commands.Add(new(command, result, before.Resources, after.Resources, before.Research.Points, after.Research.Points,
                Math.Max(0, after.Health - before.Health), healed.Sum(), healed.Count(h => h > 0), payment));
        }
        void Send(string action) => Submit(Command.FromSnapshot(match.Snapshot(), ++sequence, action, player));
        Send("doctrine:" + doctrine); Send("start"); var starting = City(match.Snapshot()).Resources;
        int lastWave = lastWaveLimit ?? (family == "opening" ? 3 : 20);
        if (lastWave is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(lastWaveLimit));
        for (int wave = 1; wave <= lastWave && match.Phase is not (Phase.Defeat or Phase.Victory); wave++)
        {
            for (int stage = 0; stage < match.Rules.PreparationStages; stage++)
            {
                for (int decision = 0; decision < 120; decision++)
                {
                    var state = match.Snapshot();
                    var intent = (family == "opening" || adaptOpening && wave == 1) ? OpeningCampaign.Next(state, player, openingOrder)
                        : GameplayScenarios.NextPreparation(state, player, family);
                    if (intent is null) break;
                    Submit(intent.Capture(state, ++sequence, player));
                    if (decision == 119) throw new InvalidOperationException("Reward economy decision bound exceeded.");
                }
                var stopped = match.Snapshot(); var city = City(stopped);
                foreach (var quote in city.RecruitmentQuotes.Where(q => city.Slots.Any(s => s.Level == q.Level
                    && stopped.BuildingCatalog.SingleOrDefault(b => b.Type == s.Type)?.Recruits?.Contains(q.Type) == true)))
                {
                    string[] missing = Enum.GetValues<Resource>().Where(r => city.Resources.Amount(r) < quote.Cost.Amount(r)).Select(r => r.ToString()).ToArray();
                    if (missing.Length > 0 || city.Food < (city.FoodForecast?.Demand ?? 0) + quote.Upkeep)
                        shortages.Add(new(wave, stopped.Turn, quote.Type, quote.Level, quote.Cost, missing,
                            (city.FoodForecast?.Demand ?? 0) + quote.Upkeep, city.Food));
                }
                Send("ready");
            }
            if (match.Phase != Phase.Combat || match.Wave != wave) throw new InvalidOperationException("Review did not enter its numbered battle.");
            var prepared = City(match.Snapshot()); long combat = 0, cleanup = 0;
            for (int step = 0; step < 18000 && (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0); step++)
            {
                if (match.Phase == Phase.Combat) combat++; else cleanup++;
                match.Step();
            }
            var ended = match.Snapshot(); var after = City(ended);
            if (match.Phase == Phase.Combat || ended.DyingBodies.Length > 0) throw new InvalidOperationException("Reward economy combat bound exceeded.");
            string[] offers = match.Phase == Phase.RewardDraft ? after.Run.Offers.ToArray() : [];
            // Identical ordinal policy before/after. It deliberately does not optimize new rewards.
            string? choice = offers.Order(StringComparer.Ordinal).FirstOrDefault();
            if (choice is not null) Send("reward:" + choice);
            var upkeep = prepared.LastUpkeep!;
            battles.Add(new(wave, ended.Phase.ToString(), combat, cleanup, prepared.Resources, after.Resources,
                upkeep.Paid, upkeep.Funded.Length, upkeep.Participating.Length, upkeep.Unfed.Length,
                upkeep.Participating.Count(id => !after.Soldiers.Any(u => u.Id == id)),
                after.Soldiers.Count(u => u.Health > 0 && u.Health < u.Profile.Health), prepared.Health - after.Health,
                after.LastReward?.Wave == wave ? after.LastReward.Amount : default,
                after.LastReward?.Wave == wave ? after.LastReward.Research : 0,
                offers, choice, after.Research.Technologies.Select(t => t.ToString()).ToArray()));
        }
        return new(doctrine, family, seed, match.Id, starting, battles.ToArray(), commands.ToArray(), shortages.ToArray());
    }
}
