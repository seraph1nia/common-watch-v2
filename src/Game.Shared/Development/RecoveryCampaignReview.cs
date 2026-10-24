// SPDX-License-Identifier: GPL-3.0-only
using System.Security.Cryptography;
using System.Text.Json;
using Game.Core;
namespace Game.Shared;

public sealed record RecoveryCampaignBattle(int Wave, Phase Result, int Participants, int Stored, int StoredFunded, int Food, int Lost, int KeepDamage);
public sealed record RecoveryCampaignRun(string Doctrine, ulong Seed, bool Batch, string Digest, RecoveryCampaignBattle[] Battles,
    int MemberMoves, int GroupReviews, long OrdinaryHealing, long RewardHealing);
/// <summary>Same explicit recovery choices, individual versus finite sequence. No player automation or balance claim.</summary>
public static class RecoveryCampaignReview
{
    public static RecoveryCampaignRun Run(string doctrine, ulong seed, bool batch)
    {
        using var solo = new SoloSession(seed, $"recovery-campaign-{doctrine}-{seed}");
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var battles = new List<RecoveryCampaignBattle>(); int moves = 0, reviews = 0; long ordinary = 0, bonus = 0;
        void Observe() => digest.AppendData(JsonSerializer.SerializeToUtf8Bytes(solo.Snapshot, WireJson.Options));
        void Submit(GameplayIntent intent)
        {
            var before = solo.City; SquadRecoveryReview.Accept(solo.Submit(intent));
            long healed = solo.City.Soldiers.Sum(u => Math.Max(0L, (long)u.Health - (before.Soldiers.FirstOrDefault(b => b.Id == u.Id)?.Health ?? u.Health)));
            if (intent.Action == GameplayAction.Ready) ordinary += healed;
            if (intent.Action is GameplayAction.Store or GameplayAction.Send) moves++;
            Observe();
        }
        void Send(string action)
        {
            var before = solo.City; SquadRecoveryReview.Accept(solo.Send(action));
            if (action.StartsWith("reward:", StringComparison.Ordinal)) bonus += solo.City.Soldiers.Sum(u => Math.Max(0L, (long)u.Health - (before.Soldiers.FirstOrDefault(b => b.Id == u.Id)?.Health ?? u.Health)));
            Observe();
        }
        void Execute(SquadRecoveryPlan plan)
        {
            reviews++;
            if (!batch)
                foreach (var entry in plan.Moving) Submit(new(plan.Move == RecoveryMove.StoreWounded ? GameplayAction.Store : GameplayAction.Send, plan.Hall, UnitId: entry.Before.Id));
            else
            {
                var sequence = new RecoverySequence(plan);
                for (int n = 0; n < 40 && !sequence.Finished && sequence.Stopped.Length == 0; n++)
                {
                    var intent = sequence.Next(solo.Snapshot); if (intent is null) continue;
                    Submit(intent); sequence.Submitted(solo.LastCommand!); sequence.Acknowledge(solo.LastCommand!, solo.LastResult!);
                }
                sequence.Next(solo.Snapshot);
                if (!sequence.Finished) throw new InvalidOperationException(sequence.Status);
            }
        }
        Send("doctrine:" + doctrine); Send("start");
        for (int wave = 1; wave <= 20 && solo.Snapshot.Phase is not (Phase.Defeat or Phase.Victory); wave++)
        {
            if (solo.Snapshot.Phase == Phase.RewardDraft) Send("reward:" + GameplayScenarios.RewardChoice(solo.City.Run));
            for (int stage = 0; stage < solo.Snapshot.Rules.PreparationStages; stage++)
            {
                for (int decision = 0; decision < 150; decision++)
                {
                    var state = solo.Snapshot; var city = solo.City;
                    if (wave == 1)
                    {
                        if (OpeningCampaign.Next(state, solo.PlayerId) is { } opening) { Submit(opening); continue; }
                        break;
                    }
                    if (OpeningCampaign.Transition(state, solo.PlayerId, "normal-wood") is { } transition) { Submit(transition); continue; }
                    // Explicit benchmark preference: full-health return; a materially wounded,
                    // near-current-tier member justifies reviewing its whole wounded squad.
                    bool moved = false;
                    foreach (var hall in city.Army!.Halls)
                    {
                        var returned = SquadRecovery.Project(state, solo.PlayerId, 0, hall.Slot, RecoveryMove.ReturnRecovered);
                        if (returned.CanCommit) { Execute(returned); moved = true; break; }
                        bool woundedStored = city.Soldiers.Any(u => u.Assignment is { Stored: true } a && a.HallSlot == hall.Slot && u.RecoveryEligible && u.Health < u.Profile.Health);
                        if (woundedStored && hall.HealingQuote is { } price && city.Resources.TryPay(price, out _))
                        { Submit(new(GameplayAction.Healing, hall.Slot)); moved = true; break; }
                        var wounded = city.Soldiers.Where(u => u.Assignment is { Stored: false } && u.RecoveryEligible && u.Level >= city.Slots[2].Level - 1
                            && (long)u.Health * 100 < (long)u.Profile.Health * 65).OrderBy(u => u.Health / (decimal)u.Profile.Health).ThenBy(u => u.Id).FirstOrDefault();
                        if (wounded is null) continue;
                        var recover = SquadRecovery.Project(state, solo.PlayerId, wounded.SquadId, hall.Slot, RecoveryMove.StoreWounded);
                        if (recover.CanCommit) { Execute(recover); moved = true; break; }
                        if (hall.CapacityQuote is { } expansion && city.Resources.TryPay(expansion, out _))
                        { Submit(new(GameplayAction.Capacity, hall.Slot)); moved = true; break; }
                    }
                    if (moved) continue;
                    var action = GameplayCampaignPolicy.Next(state, solo.PlayerId, "reserve", rotateRecovery: false);
                    if (action is null) break;
                    var kind = Enum.GetValues<GameplayAction>().First(a => GameplayIntent.Name(a) == action.Action);
                    Submit(new(kind, action.Slot, action.Building, action.Unit, action.Technology, action.Resource, action.Bundles, action.Payment, action.UnitId));
                    if (decision == 149) throw new InvalidOperationException("Recovery benchmark exceeded its decision bound.");
                }
                Submit(new(GameplayAction.Ready));
            }
            var prepared = solo.City; var paid = prepared.LastUpkeep!;
            GameplayScenarios.FinishCombat(solo); Observe();
            battles.Add(new(wave, solo.Snapshot.Phase, paid.Participating.Length, prepared.Soldiers.Count(u => u.Assignment is { Stored: true }),
                prepared.Soldiers.Count(u => u.Assignment is { Stored: true } && paid.Funded.Contains(u.Id)), paid.Paid,
                paid.Participating.Count(id => !solo.City.Soldiers.Any(u => u.Id == id)), prepared.Health - solo.City.Health));
        }
        return new(doctrine, seed, batch, Convert.ToHexStringLower(digest.GetHashAndReset()), battles.ToArray(), moves, reviews, ordinary, bonus);
    }
}
