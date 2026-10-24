// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.Shared;

/// <summary>Explicit development alternative and paid checkpoint; never changes the default campaign.</summary>
public static class MajorEncounterComparison
{
    public static Rules EliteRules(int major)
    {
        if (major is not (10 or 20)) throw new ArgumentOutOfRangeException(nameof(major));
        int front = major == 10 ? 3 : 5, support = front - 1;
        var waves = CampaignDefinition.Default().Waves.Select(w => w.Number != major ? w : w with
        {
            IsBoss = false,
            Entries = [new(UnitType.Swordsman, front, 3), new(UnitType.Berserker, support),
                new(UnitType.Crossbowman, support), new(UnitType.Mage, support)]
        }).ToArray();
        return new Rules { Campaign = new(waves) };
    }
    public static GameplayIntent? Next(MatchSnapshot state, int player)
        => state.Wave == 1 ? OpeningCampaign.Next(state, player) : GameplayScenarios.NextPreparation(state, player, "mixed");
    public static SoloSession CreateAt(int major, bool elite, ulong seed = 1)
    {
        var solo = new SoloSession(seed, $"major-review-{major}-{seed}", elite ? EliteRules(major) : null);
        try
        {
            Accept(solo.Send("doctrine:warden")); Accept(solo.Start());
            for (int boundary = 0; boundary < 65; boundary++)
            {
                if (solo.Snapshot.Phase is Phase.Defeat or Phase.Victory) throw new InvalidOperationException("Major comparison prefix ended before its checkpoint.");
                if (solo.Snapshot.Phase == Phase.RewardDraft) Accept(solo.ChooseReward(GameplayScenarios.RewardChoice(solo.City.Run)));
                for (int n = 0; n < 150; n++)
                {
                    var next = Next(solo.Snapshot, solo.PlayerId);
                    if (next is null) break;
                    Accept(solo.Submit(next));
                    if (n == 149) throw new InvalidOperationException("Major preparation command bound exceeded.");
                }
                if (solo.Snapshot.Wave == major && solo.Snapshot.Turn == solo.Snapshot.Rules.PreparationStages)
                { solo.ResetPlayback(); return solo; }
                Accept(solo.Send("ready"));
                if (solo.Snapshot.Phase == Phase.Combat) GameplayScenarios.FinishCombat(solo);
            }
            throw new InvalidOperationException("Major comparison boundary bound exceeded.");
        }
        catch { solo.Dispose(); throw; }
    }
    private static void Accept(CommandResult result)
    { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
}
