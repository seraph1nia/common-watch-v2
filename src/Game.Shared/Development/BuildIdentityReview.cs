// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

/// <summary>Bounded legal-command receipts plus a read-only, one-slot relevance experiment.</summary>
public static class BuildIdentityReview
{
    public static BuildRun Review(string doctrine, string family, ulong seed, int lastWave = 20, bool opening = false, bool sparseAtEnd = false)
    {
        using var match = new Match(matchId: $"build-review-{doctrine}-{family}-{seed}-{(sparseAtEnd ? "sparse" : "campaign")}-{lastWave}", combatSeed: seed);
        var player = match.Join()!; long sequence = 0;
        var actions = new List<string>(); var waves = new List<BuildWave>();
        void Send(string action) => Accept(match.Apply(player.Id, Command.FromSnapshot(match.Snapshot(), ++sequence, action, player.Id)));
        Send("doctrine:" + doctrine); Send("start");
        for (int wave = 1; wave <= lastWave && match.Phase is not (Phase.Defeat or Phase.Victory); wave++)
        {
            if (match.Phase == Phase.RewardDraft)
            {
                // Prefer offered relics to exercise the real two-relic cap, otherwise retain the reference choice.
                string choice = player.Run.Offers.FirstOrDefault(id => RunCatalog.Card(id).Rarity == CardRarity.Relic) ?? GameplayScenarios.RewardChoice(player.Run);
                Send("reward:" + choice); actions.Add($"W{wave}: reward {choice}");
            }
            if (sparseAtEnd && wave == lastWave)
            {
                foreach (var member in match.Snapshot().Players.Single(p => p.Id == player.Id).Soldiers)
                {
                    Accept(match.Apply(player.Id, Command.FromSnapshot(match.Snapshot(), ++sequence, "retire", player.Id, unitId: member.Id)));
                    actions.Add($"W{wave}: sparse sensitivity retire member {member.Id}");
                }
                var samples = Enumerable.Range(0, 128).Select(n => Sample(match, player.Id, wave - 1, (ulong)n)).ToArray();
                return new(doctrine, family, seed, waves.ToArray(), actions.ToArray(), samples, match.Id);
            }
            for (int stage = 0; stage < 3; stage++)
            {
                for (int n = 0; n < 120; n++)
                {
                    var state = match.Snapshot();
                    var intent = opening ? OpeningCampaign.Next(state, player.Id) : GameplayScenarios.NextPreparation(state, player.Id, family);
                    if (intent is null) break;
                    Accept(match.Apply(player.Id, intent.Capture(state, ++sequence, player.Id)));
                    actions.Add($"W{wave}/P{stage + 1}: {intent.Action} {intent.Building} {intent.SoldierType} {intent.Technology} plot {intent.Slot + 1}");
                    if (n == 119) throw new InvalidOperationException("Build review decision bound exceeded.");
                }
                Send("ready");
            }
            if (match.Phase != Phase.Combat || match.Wave != wave) throw new InvalidOperationException("Review must reach its numbered real battle.");
            var prepared = match.Snapshot().Players.Single(p => p.Id == player.Id);
            for (int step = 0; step < 18000 && (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0); step++) match.Step();
            if (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0) throw new InvalidOperationException("Build review combat bound exceeded.");
            var stateAfter = match.Snapshot(); var city = stateAfter.Players.Single(p => p.Id == player.Id);
            DraftSample? sample = match.Phase == Phase.RewardDraft ? Sample(match, player.Id, wave, city.Run.OfferSeed) : null;
            if (sample is not null && !sample.Offers.SequenceEqual(city.Run.Offers)) throw new InvalidOperationException("Sample must match retained authoritative offers.");
            waves.Add(new(wave, stateAfter.Phase.ToString(), BuildDirection.Summary(prepared), prepared.Research.Technologies.Select(TechnologyIds.Name).ToArray(),
                prepared.Army!.Homes.Select((h, i) => $"H{i + 1}: {h.Plot.Type}, members {h.Used}").ToArray(), city.Run.Cards, sample));
        }
        return new(doctrine, family, seed, waves.ToArray(), actions.ToArray(), null, match.Id);
    }
    public static DraftSample Sample(Match match, int player, int completedWave, ulong seed)
    {
        var state = match.Snapshot(); var city = state.Players.Single(p => p.Id == player);
        var eligible = RunCatalog.Cards.Where(c => RunEffects.Eligible(match.Players[player], c, completedWave, HealthPoints.FromWhole(match.Rules.CityHealth), match.Campaign, match.Players.Count)).ToArray();
        string[] first = RunEffects.Offers(match.Players[player], completedWave, seed, HealthPoints.FromWhole(match.Rules.CityHealth), match.Campaign, match.Players.Count);
        string[] second = RunEffects.Offers(match.Players[player], completedWave, seed, HealthPoints.FromWhole(match.Rules.CityHealth), match.Campaign, match.Players.Count);
        if (!first.SequenceEqual(second) || first.Distinct().Count() != 3 || first.Any(id => !eligible.Any(c => c.Id == id))) throw new InvalidOperationException("Draft determinism/eligibility invariant failed.");
        var relevant = eligible.Where(c => BuildImpact.Reward(state, city, c.Id).HasCurrentTarget).Select(c => c.Id).ToArray();
        string[] experiment = first.ToArray();
        if (!experiment.Intersect(relevant).Any())
            for (int slot = 2; slot >= 0; slot--)
            {
                var pool = eligible.Where(c => relevant.Contains(c.Id) && c.Rarity == RunCatalog.Card(experiment[slot]).Rarity).Select(c => c.Id).ToArray();
                if (pool.Length == 0) continue;
                experiment[slot] = pool[(int)(SeededDecision.Mix(seed) % (ulong)pool.Length)];
                break;
            }
        foreach (string[] offers in new[] { first, experiment })
            if (offers.Distinct().Count() != 3 || offers.Any(id => !eligible.Any(c => c.Id == id) || city.Run.Stacks(id) >= RunCatalog.Card(id).MaxStacks)
                || offers.Any(id => RunCatalog.Card(id).Rarity == CardRarity.Relic && (completedWave < 7 || city.Run.Cards.Count(c => RunCatalog.Card(c.Id).Rarity == CardRarity.Relic) >= 2)))
                throw new InvalidOperationException("Draft cap/distinctness invariant failed.");
        if (!first.Select(id => RunCatalog.Card(id).Rarity).SequenceEqual(experiment.Select(id => RunCatalog.Card(id).Rarity)))
            throw new InvalidOperationException("Relevance experiment must retain slot rarity.");
        return new(completedWave, city.Soldiers.Length, first, first.Count(id => relevant.Contains(id)), eligible.Length, relevant.Length,
            experiment, experiment.Count(id => relevant.Contains(id)), city.Run.Cards.Count(c => RunCatalog.Card(c.Id).Rarity == CardRarity.Relic),
            RunCatalog.Cards.Where(c => city.Run.Stacks(c.Id) >= c.MaxStacks).Select(c => c.Id).ToArray());
    }
    private static void Accept(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
}
public sealed record DraftSample(int CompletedWave, int Soldiers, string[] Offers, int CurrentTargets, int Eligible, int EligibleCurrentTargets,
    string[] OneCurrentSlotExperiment, int ExperimentCurrentTargets, int Relics, string[] Capped);
public sealed record BuildWave(int Wave, string Result, string Direction, string[] Technologies, string[] Homes, AcquiredCard[] Cards, DraftSample? Draft);
public sealed record BuildRun(string Doctrine, string Family, ulong Seed, BuildWave[] Waves, string[] Actions, DraftSample[]? SparseSensitivity = null, string MatchId = "");
