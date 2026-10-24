// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.Shared;

public sealed record OpeningTaskStep(string Label, Command Command, CommandResult Result,
    Phase BeforePhase, Phase AfterPhase, int Wave, int Turn, long BeforeRevision, long AfterRevision,
    CityState Before, CityState After);
public sealed record OpeningTaskTrace(string Task, string Observation, OpeningTaskStep[] Steps, CityState[] Outcomes);

/// <summary>Scripted novice-style tasks, including rejected intentions. Never fabricates a result or state.</summary>
public static class OpeningMistakes
{
    public static OpeningTaskTrace Run(string task)
    {
        if (task is not ("overspend" or "late-food" or "missing-trainer" or "after-commit" or "no-food"))
            throw new ArgumentOutOfRangeException(nameof(task));
        using var solo = new SoloSession(1, "normal-opening-task-" + task);
        var steps = new List<OpeningTaskStep>(); var outcomes = new List<CityState>();
        void Send(string label, string action, int slot = -1, Building building = Building.Empty,
            ConstructionPayment payment = ConstructionPayment.Standard, bool accept = true)
        {
            var before = solo.Snapshot;
            var result = solo.Send(action, slot, building, payment: payment); var command = solo.LastCommand!; var after = solo.Snapshot;
            if (result.Accepted != accept) throw new InvalidOperationException($"{task}: {label}: {result.Message}");
            if (!accept && (after.Revision != before.Revision || solo.City.Resources != before.Players.Single().Resources))
                throw new InvalidOperationException("Rejected intention changed the authority.");
            steps.Add(new(label, command, result, before.Phase, after.Phase, before.Wave, before.Turn,
                before.Revision, after.Revision, before.Players.Single(), after.Players.Single()));
        }
        void Intent(GameplayIntent intent)
        {
            var before = solo.Snapshot;
            var result = solo.Submit(intent); var command = solo.LastCommand!; var after = solo.Snapshot;
            if (!result.Accepted) throw new InvalidOperationException(result.Message);
            steps.Add(new("Ordinary next-wave decision", command, result, before.Phase, after.Phase,
                before.Wave, before.Turn, before.Revision, after.Revision, before.Players.Single(), after.Players.Single()));
        }
        Send("Begin with ordinary Warden allocation", "start");
        if (task == "overspend")
        {
            Send("Spend starting Wood on Farm", "build", 0, Building.Farm);
            Send("Buy Metal income", "build", 1, Building.MetalMine);
            Send("Buy trainer; Wood reaches zero", "build", 2, Building.Barracks);
            Send("Normal Lumbermill is unaffordable", "build", 3, Building.Lumbermill, accept: false);
            Send("Explicit paid safety valve", "build", 3, Building.Lumbermill, ConstructionPayment.GoldRecovery);
        }
        else
        {
            Send("Establish ordinary Wood income", "build", 3, Building.Lumbermill);
            Send("Establish Metal income", "build", 1, Building.MetalMine);
            if (task is "late-food" or "no-food") Send("Build trainer before Food", "build", 2, Building.Barracks);
            else Send("Buy Food income", "build", 0, Building.Farm);
        }
        Send("First genuine payment", "ready");
        if (task is "missing-trainer" or "after-commit")
        {
            if (task == "missing-trainer") Send("Recruit without a trainer is rejected", "recruit", 2, accept: false);
            Send("Earned Wood now funds the missing trainer", "build", 2, Building.Barracks);
        }
        Send("Recruit before final commitment", "recruit", 2); Send("Recruit second member", "recruit", 2);
        Send("Middle payment leaves a spendable window", "ready");
        Send("Recruit third member", "recruit", 2); Send("Recruit fourth member", "recruit", 2);
        if (task == "late-food") Send("Late Farm: rely on actual final income for upkeep", "build", 0, Building.Farm);
        Send("Final production and Food enter combat directly", "ready");
        if (task == "after-commit") Send("Recruitment window has closed", "recruit", 2, accept: false);
        GameplayScenarios.FinishCombat(solo); outcomes.Add(solo.City);
        for (int wave = 2; wave <= 3 && solo.Snapshot.Phase == Phase.RewardDraft; wave++)
        {
            Send("Immediate reward after an actual clear", "reward:" + GameplayScenarios.RewardChoice(solo.City.Run));
            for (int stage = 0; stage < solo.Snapshot.Rules.PreparationStages; stage++)
            {
                for (int n = 0; n < 100 && OpeningCampaign.Next(solo.Snapshot, solo.PlayerId) is { } intent; n++)
                { Intent(intent); if (n == 99) throw new InvalidOperationException("Task trace exceeded decision bound."); }
                Send("Deliberate Ready", "ready");
            }
            GameplayScenarios.FinishCombat(solo); outcomes.Add(solo.City);
        }
        return new(task, "Scripted task trace; no human observation, comprehension or causal placement claim.", steps.ToArray(), outcomes.ToArray());
    }
}
