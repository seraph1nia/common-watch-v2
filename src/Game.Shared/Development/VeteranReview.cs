// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public sealed record VeteranReviewCase(string Name, string MatchId, ulong Seed, int Wave, ResourceCost Stock, VeteranOptions Comparison);
public static class VeteranReview
{
    public static VeteranReviewCase[] Collect()
    {
        var cases = new List<VeteranReviewCase>();
        foreach (ulong seed in new[] { 1UL, SoloSession.DefaultSeed })
        {
            using var early = ScenarioRunner.CreateAt("post-draft", seed);
            Add("same-equipment", early, early.City.Soldiers.OrderByDescending(u => u.Health).ThenBy(u => u.Id).First(), seed);
        }
        using var recovering = GameplayScenarios.CreateAt("town-hall");
        Add("eligible-stored-recovery", recovering, recovering.City.Soldiers.First(u => u.Assignment is { Stored: true } && u.Health < u.Profile.Health), 1);
        var field = recovering.City.Soldiers.First(u => u.Assignment is { Stored: false });
        Accept(recovering.Send("retire", unitId: field.Id)); Accept(recovering.Send("recruit", 2));
        Add("not-yet-eligible", recovering, recovering.City.Soldiers.MaxBy(u => u.Id)!, 1);
        using var late = GameplayScenarios.CreateAt("late");
        Add("newer-equipment", late, late.City.Soldiers.First(u => u.Level < late.City.Slots[2].Level), 1);
        return cases.ToArray();
        void Add(string name, SoloSession session, UnitState unit, ulong seed) => cases.Add(new(name, session.Snapshot.MatchId, seed, session.Snapshot.Wave,
            session.City.Resources, VeteranComparison.Project(session.Snapshot, session.City, unit.Id)));
    }
    private static void Accept(CommandResult result) { if (!result.Accepted) throw new InvalidOperationException(result.Message); }
}
