// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public sealed record CooperationBattle(int Wave, long Ticks, int[] Fallen, int[] Keep, int[] Participating, int[] Rewards, int CrossSectorSamples);
public sealed record CooperationRun(int Players, string Policy, ulong Seed, string Result, int FallWave, long SpectatorTicks, CooperationBattle[] Battles);

/// <summary>Uneven groups use paid ordinary commands; no injected health, resources or armies.</summary>
public static class CooperationReview
{
    public static GameplayIntent? Next(MatchSnapshot state, int player, string policy)
    {
        if (policy == "idle") return null;
        var intent = GameplayScenarios.NextPreparation(state, player, "frontline");
        var city = state.Players.Single(p => p.Id == player);
        // A deliberate small-army decision. Stop at the blocked purchase rather
        // than bypassing the policy to invent a more successful army.
        if (policy == "limited" && intent?.Action == GameplayAction.Recruit && city.Soldiers.Length >= 3) return null;
        return intent;
    }
    public static CooperationRun Run(int players, string policy, ulong seed, Action<MatchSnapshot>? observe = null, int maximumWaves = 20)
    {
        using var match = new Match(matchId: $"coop-review-{players}-{policy}-{seed}", combatSeed: seed);
        int[] ids = Enumerable.Range(0, players).Select(_ => match.Join()!.Id).ToArray();
        var sequences = ids.ToDictionary(id => id, _ => 0L);
        void Send(int id, string action) => Accept(match.Apply(id, Command.FromSnapshot(match.Snapshot(), ++sequences[id], action, id)));
        Send(ids[0], "start"); int fallWave = 0; long fellAt = 0; var battles = new List<CooperationBattle>();
        for (int n = 0; n < maximumWaves && match.Phase is not (Phase.Defeat or Phase.Victory); n++)
        {
            if (match.Phase == Phase.RewardDraft)
                foreach (int id in ids)
                {
                    var city = match.Snapshot().Players.Single(p => p.Id == id);
                    if (!city.Eliminated && city.Run.Choice is null) Send(id, "reward:" + GameplayScenarios.RewardChoice(city.Run));
                }
            for (int stage = 0; stage < 3; stage++)
                foreach (int id in ids)
                {
                    if (match.Snapshot().Players.Single(p => p.Id == id).Eliminated) continue;
                    for (int action = 0; action < 120; action++)
                    {
                        var state = match.Snapshot();
                        var intent = Next(state, id, id == ids[^1] ? "idle" : policy);
                        if (intent is null) break;
                        Accept(match.Apply(id, intent.Capture(state, ++sequences[id], id)));
                        if (action == 119) throw new InvalidOperationException("Cooperative policy bound exceeded.");
                    }
                    observe?.Invoke(match.Snapshot()); Send(id, "ready");
                }
            var before = match.Snapshot();
            if (before.Phase != Phase.Combat) throw new InvalidOperationException("All living cities must reach combat.");
            int allocated = before.WaveCatalog.Single(w => w.Number == before.Wave).Entries.Sum(e => e.Count) * players;
            if (before.Enemies.Length != allocated || before.Players.Where(p => p.Eliminated).SelectMany(p => p.Soldiers).Any(u => u.Health > 0 && u.Hex?.Lifecycle == UnitLifecycle.Alive))
                throw new InvalidOperationException("Original roster allocation or fallen-city participation changed.");
            int crossSector = 0;
            for (int tick = 0; tick < 18000 && (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0); tick++)
            {
                match.Step();
                if (fallWave == 0 && match.Players.Values.Any(p => p.Eliminated)) { fallWave = match.Wave; fellAt = match.Tick; }
                if (tick % 20 == 0)
                {
                    var state = match.Snapshot();
                    crossSector += state.Players.SelectMany(p => p.Soldiers).Count(u => u.Deployed && u.Health > 0 && TeamAwareness.Sector(state, u) is var sector && sector > 0 && sector != u.Owner);
                    observe?.Invoke(state);
                }
            }
            var after = match.Snapshot(); observe?.Invoke(after);
            if (after.Phase == Phase.Combat) throw new InvalidOperationException("Cooperative battle exceeded its bound.");
            if (after.Players.Any(p => p.Eliminated && p.LastReward?.Wave == before.Wave))
                throw new InvalidOperationException("Fallen city received a new clear reward.");
            battles.Add(new(before.Wave, after.Tick - before.Tick, after.Players.Where(p => p.Eliminated).Select(p => p.Id).ToArray(),
                after.Players.Select(p => p.Health).ToArray(), before.Players.Select(p => p.LastUpkeep is { } receipt && receipt.Wave == before.Wave ? receipt.Participating.Length : 0).ToArray(),
                after.Players.Select(p => p.LastReward?.Wave == before.Wave ? p.LastReward.Amount.Gold : 0).ToArray(), crossSector));
        }
        return new(players, policy, seed, match.Phase.ToString(), fallWave, fallWave == 0 ? 0 : match.Tick - fellAt, battles.ToArray());
    }
    private static void Accept(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
}
