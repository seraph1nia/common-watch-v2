// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public sealed record EncounterResult(int Wave, string Purpose, string Result, long Ticks, int Participants, int Lost, int KeepDamage,
    int FoodPaid, ResourceCost? Reward, string[] Composition, string[] Adaptations, string[] Decisions);
public sealed record CampaignRun(string Family, ulong Seed, bool Adaptive, string MatchId, EncounterResult[] Waves);

/// <summary>Legal paid policy comparisons. Baseline and forecast responses share Core commands, prices and timing.</summary>
public static class CampaignReview
{
    public static CampaignRun Run(string family, ulong seed, bool adaptive, Rules? rules = null)
    {
        using var match = new Match(rules: rules, matchId: $"encounter-review-{family}-{seed}-{adaptive}", combatSeed: seed);
        int player = match.Join()!.Id; long sequence = 0;
        void Submit(GameplayIntent intent) => Accept(match.Apply(player, intent.Capture(match.Snapshot(), ++sequence, player)));
        void Send(string action) => Accept(match.Apply(player, Command.FromSnapshot(match.Snapshot(), ++sequence, action, player)));
        Send("start"); var results = new List<EncounterResult>();
        for (int wave = 1; wave <= 20 && match.Phase is not (Phase.Defeat or Phase.Victory); wave++)
        {
            if (match.Phase == Phase.RewardDraft) Send("reward:" + GameplayScenarios.RewardChoice(match.Players[player].Run));
            var adaptations = new List<string>(); var decisions = new List<string>();
            UnitState[] expected = []; BattleFoodForecast? food = null;
            for (int stage = 0; stage < match.Rules.PreparationStages; stage++)
            {
                for (int n = 0; n < 120; n++)
                {
                    var state = match.Snapshot();
                    var response = adaptive ? Response(state, player, family) : null;
                    GameplayIntent? intent = response?.Intent;
                    if (intent is null)
                    {
                        var action = GameplayCampaignPolicy.Next(state, player, family, purchaseResearch: !adaptive || family != "research");
                        if (action is null) break;
                        var kind = Enum.GetValues<GameplayAction>().First(a => GameplayIntent.Name(a) == action.Action);
                        intent = new(kind, action.Slot, action.Building, action.Unit, action.Technology, action.Resource, action.Bundles, action.Payment, action.UnitId);
                    }
                    Submit(intent); decisions.Add($"P{stage + 1}: {intent.Action} {intent.Building} {intent.SoldierType} {intent.Technology} plot {intent.Slot + 1}");
                    if (response is not null) adaptations.Add(response.Reason + " · " + intent);
                    if (n == 119) throw new InvalidOperationException("Campaign response bound exceeded.");
                }
                if (stage == match.Rules.PreparationStages - 1) { var state = match.Snapshot(); expected = state.NextWave; food = state.Players.Single(p => p.Id == player).FoodForecast; }
                Send("ready");
            }
            if (match.Phase != Phase.Combat || match.Wave != wave) throw new InvalidOperationException("Campaign did not reach the numbered battle.");
            var prepared = match.Snapshot(); var before = prepared.Players.Single(p => p.Id == player);
            if (!Manifest(expected).SequenceEqual(Manifest(prepared.Enemies)) || food is null || !food.Participating.SequenceEqual(before.LastUpkeep!.Participating) || food.Paid != before.LastUpkeep.Paid)
                throw new InvalidOperationException("Accepted forecast/upkeep was not consumed exactly.");
            for (int tick = 0; tick < 18000 && (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0); tick++) match.Step();
            var after = match.Snapshot(); var city = after.Players.Single(p => p.Id == player);
            if (match.Phase == Phase.Combat || after.DyingBodies.Length > 0) throw new InvalidOperationException("Campaign combat exceeded its bound.");
            var catalog = prepared.WaveCatalog.Single(w => w.Number == wave);
            results.Add(new(wave, EncounterBriefing.Purpose(catalog, prepared.WaveCatalog.FirstOrDefault(w => w.Number == wave - 1), wave == prepared.TotalWaves),
                after.Phase.ToString(), after.Tick - prepared.Tick, before.LastUpkeep!.Participating.Length,
                before.LastUpkeep.Participating.Count(id => !city.Soldiers.Any(u => u.Id == id)), before.Health - city.Health, before.LastUpkeep.Paid,
                city.LastReward?.Wave == wave ? city.LastReward.Amount : null,
                expected.GroupBy(u => (u.Type, u.Level, u.IsBoss)).Select(g => $"{g.Count()} {g.Key.Type} L{g.Key.Level}{(g.Key.IsBoss ? " BOSS" : "")}").ToArray(), adaptations.ToArray(), decisions.ToArray()));
        }
        return new(family, seed, adaptive, match.Id, results.ToArray());
    }
    private static string[] Manifest(IEnumerable<UnitState> units) => units.Select(u => $"{u.Type}/{u.Level}/{u.Rank}/{u.IsBoss}/{u.Origin}/{u.Destination}/{u.Profile}").Order(StringComparer.Ordinal).ToArray();
    private sealed record Adaptation(GameplayIntent Intent, string Reason);
    private static Adaptation? Response(MatchSnapshot state, int player, string family)
    {
        var city = state.Players.Single(p => p.Id == player);
        if (city.Ready || city.Eliminated || state.Phase is not (Phase.Building or Phase.Preparation)) return null;
        var threat = state.NextWave.Where(u => u.Destination == player).ToArray();
        bool Available(TechnologyId id) => city.Technologies.Any(t => t.Id == id && t.Available);
        if (family == "research")
        {
            TechnologyId id = Available(TechnologyId.MeleeFoundation) ? TechnologyId.MeleeFoundation
                : threat.Any(u => u.IsBoss) && Available(TechnologyId.Assault) ? TechnologyId.Assault
                : threat.Any(u => u.Level >= 4) && Available(TechnologyId.AssaultMastery) ? TechnologyId.AssaultMastery : TechnologyId.None;
            if (id != TechnologyId.None) return new(new(GameplayAction.Research, Technology: id), "Forecast response: reserve research for primary damage against a boss / upgraded frontline");
        }
        if (family == "mixed" && city.Soldiers.Any(u => u.Type == UnitType.Mage))
        {
            TechnologyId id = Available(TechnologyId.MagicFoundation) ? TechnologyId.MagicFoundation
                : threat.Any(u => u.Type == UnitType.Berserker) && Available(TechnologyId.Frost) ? TechnologyId.Frost
                : threat.Count(u => u.Type == UnitType.Berserker) >= 2 && Available(TechnologyId.FrostMastery) ? TechnologyId.FrostMastery : TechnologyId.None;
            if (id != TechnologyId.None) return new(new(GameplayAction.Research, Technology: id), "Forecast response: slowing investment against heavy melee pressure");
        }
        if (threat.Any(u => u.Type is UnitType.Crossbowman or UnitType.Mage) && city.Soldiers.Any(u => u.Class == UnitClass.Melee && u.Assignment is { Stored: false }))
        {
            var bow = city.Soldiers.FirstOrDefault(u => u.Class is UnitClass.Ranged or UnitClass.Magic && u.Assignment is { Stored: false, Tile: < 3 });
            var rear = city.Army!.Homes.FirstOrDefault(h => h.Cell >= 3 && h.Purchased && h.Plot.Type == Building.Empty && h.Used == 0);
            if (bow is not null && rear is not null)
            {
                int cell = state.Arena!.Sectors.Single(s => s.Owner == player).DeploymentCells[rear.Cell];
                return new(new(GameplayAction.Deploy, SquadId: bow.SquadId, Cell: cell), "Forecast response: move exposed support behind melee homes");
            }
        }
        return null;
    }
    private static void Accept(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
}
