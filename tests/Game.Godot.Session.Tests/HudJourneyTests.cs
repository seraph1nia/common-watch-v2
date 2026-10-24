// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Game.GodotClient;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class HudJourneyTests
{
    private static JourneyChange Observe(HudJourney journal, SoloSession solo, bool finishing = false, int generation = 1, object? identity = null)
        => journal.Observe(identity ?? solo, solo.Snapshot, solo.PlayerId, generation,
            HudPresentation.Project(solo.Snapshot, solo.PlayerId, finishing, false));

    [Fact]
    public void BaselineSynchronizesOnceWithoutInventingHistoricalReceipts()
    {
        using var solo = ScenarioRunner.CreateAt("combat");
        var journal = new HudJourney();
        var first = Observe(journal, solo);
        Assert.True(first.Baseline); Assert.True(first.EnterCombat); Assert.True(first.SuspendManagement);
        var repeat = Observe(journal, solo);
        Assert.False(repeat.EnterCombat); Assert.False(repeat.SuspendManagement);
        Assert.Null(journal.Receipt); Assert.Equal(0, journal.ReceiptCount);
        Assert.True(Observe(journal, solo, generation: 2).Baseline);
        Assert.False(Observe(journal, solo, generation: 2).SuspendManagement);
    }

    [Fact]
    public void FinishingGateDelaysTheOutcomeTransitionWithoutChangingAuthority()
    {
        using var solo = ScenarioRunner.CreateAt("combat");
        var journal = new HudJourney(); Observe(journal, solo);
        for (int n = 0; n < 18000 && (solo.Snapshot.Phase == Phase.Combat || solo.Snapshot.DyingBodies.Length > 0); n++) solo.Step();
        Assert.Equal(Phase.RewardDraft, solo.Snapshot.Phase);
        Assert.False(Observe(journal, solo, finishing: true).EnterOutcome);
        Assert.Equal(solo.City.Run.DraftWave, HudPresentation.DisplayedWave(solo.Snapshot, true));
        Assert.True(Observe(journal, solo).EnterOutcome);
        Assert.False(Observe(journal, solo).SuspendManagement);
    }

    [Fact]
    public void SoloReceiptSurvivesSameUpdateAdvancementAndExactRetry()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var journal = new HudJourney(); Observe(journal, solo);
        string id = solo.City.Run.Offers[0];
        Assert.True(solo.ChooseReward(id).Accepted);
        Assert.Equal(Phase.Building, solo.Snapshot.Phase);
        var change = Observe(journal, solo);
        Assert.True(change.EnterPlanning); Assert.Equal(id, change.Reward!.Id);
        Assert.Equal(RunCatalog.Card(id).Name, journal.Receipt!.Name);
        Assert.Equal(1, journal.ReceiptCount);
        var receipt = journal.Receipt;
        Assert.True(solo.RetryLast()!.Accepted);
        Assert.Null(Observe(journal, solo).Reward);
        Assert.Same(receipt, journal.Receipt); Assert.Equal(1, journal.ReceiptCount);
    }

    [Fact]
    public void RejectionKeepsTheUnchosenStateAndNoReceipt()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var journal = new HudJourney(); Observe(journal, solo);
        Assert.False(solo.ChooseReward("invalid").Accepted);
        Assert.Null(Observe(journal, solo).Reward); Assert.Null(journal.Receipt);
        Assert.True(HudPresentation.Project(solo.Snapshot, solo.PlayerId, false, false).CanChooseReward);
        Assert.Equal(0, journal.ReceiptCount);
    }

    [Fact]
    public void RejoinShowsCurrentAcceptedChoiceWithoutReplayingAcceptance()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        Assert.True(solo.ChooseReward(solo.City.Run.Offers[0]).Accepted);
        var journal = new HudJourney();
        var baseline = Observe(journal, solo);
        Assert.True(baseline.Baseline); Assert.Null(baseline.Reward);
        Assert.NotNull(journal.Receipt); Assert.Equal(0, journal.ReceiptCount);
        Assert.Equal(RunPresentation.Receipt(solo.City.Run.Rewards.Single()), journal.Receipt.Details);
        Assert.Null(Observe(journal, solo).Reward);
    }

    [Fact]
    public void SessionIdentityAndResetDiscardOldReceiptAndIntent()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var journal = new HudJourney(); Observe(journal, solo);
        Assert.True(solo.ChooseReward(solo.City.Run.Offers[0]).Accepted); Observe(journal, solo);
        Assert.Equal(1, journal.ReceiptCount);
        Assert.True(Observe(journal, solo, identity: new object()).Baseline);
        Assert.Equal(0, journal.ReceiptCount);
        journal.Reset(); Assert.Null(journal.Receipt); Assert.Equal(0, journal.ReceiptCount);
    }

    [Fact]
    public void RewardInspectionNeverCreatesANextWaveDeploymentPreview()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var view = HudPresentation.Project(solo.Snapshot, solo.PlayerId, false, true);
        Assert.False(view.Arena); Assert.False(view.Management); Assert.True(view.CanChooseReward);
    }

    [Fact]
    public void AcknowledgmentBeforeSnapshotLocksTheOldOffersWithoutInventingAReceipt()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var journal = new HudJourney(); Observe(journal, solo);
        var command = Command.FromSnapshot(solo.Snapshot, 500, "reward:" + solo.City.Run.Offers[0], solo.PlayerId);
        journal.Acknowledge(command, new(500, true, "Accepted"));
        Assert.True(journal.AwaitingChoiceSnapshot(solo.Snapshot, solo.PlayerId));
        var waiting = HudPresentation.Project(solo.Snapshot, solo.PlayerId, false, true, journal.AwaitingChoiceSnapshot(solo.Snapshot, solo.PlayerId));
        Assert.Equal(HudMoment.RewardWaiting, waiting.Moment);
        Assert.False(waiting.CanChooseReward); Assert.False(waiting.Arena); Assert.False(waiting.Management);
        Assert.Null(journal.Receipt); Assert.Null(Observe(journal, solo).Reward);
        journal.Acknowledge(command with { Sequence = 501, Action = "pause" }, new(501, true, "Paused"));
        Assert.True(journal.AwaitingChoiceSnapshot(solo.Snapshot, solo.PlayerId));
        Assert.True(solo.ChooseReward(solo.City.Run.Offers[0]).Accepted);
        Assert.NotNull(Observe(journal, solo).Reward);
        Assert.False(journal.AwaitingChoiceSnapshot(solo.Snapshot, solo.PlayerId));
        Assert.Equal(1, journal.ReceiptCount);
    }

    [Fact]
    public void RejectedOrForeignAcknowledgmentsCannotLockCurrentOffers()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var journal = new HudJourney(); Observe(journal, solo);
        var command = Command.FromSnapshot(solo.Snapshot, 500, "reward:" + solo.City.Run.Offers[0], solo.PlayerId);
        journal.Acknowledge(command, new(500, false, "Rejected"));
        journal.Acknowledge(command with { MatchId = "old-session", Sequence = 501 }, new(501, true, "Accepted"));
        Assert.False(journal.AwaitingChoiceSnapshot(solo.Snapshot, solo.PlayerId));
    }

    [Fact]
    public void AcceptedHealingReceiptNeverRewritesTheFrozenBattleLoss()
    {
        using var planning = ScenarioRunner.CreateAt("settlement");
        using var draft = ScenarioRunner.CreateAt("draft");
        // Detached unit fixture for heal-delivery ordering, not a claimed campaign
        // checkpoint. Native journey captures use only actual legal commands.
        var end = draft.Snapshot with { Players = draft.Snapshot.Players.Select(c => c with { Health = HealthPoints.FromWhole(90), LastBattleLoss = c.LastBattleLoss! with { KeepDamage = HealthPoints.FromWhole(10) } }).ToArray() };
        var outcomes = new BattleOutcomeJournal(); outcomes.Observe(planning.Snapshot); outcomes.Observe(end);
        var frozen = outcomes.For(draft.PlayerId)!;
        Assert.Equal(HealthPoints.FromWhole(10), frozen.KeepDamage);
        var journal = new HudJourney();
        journal.Observe(draft, end, draft.PlayerId, 1, HudPresentation.Project(end, draft.PlayerId, false, false));
        var healed = end with { Players = end.Players.Select(c => c with { Health = HealthPoints.FromWhole(100), Run = c.Run with { Choice = "loot-recovery", Rewards = [new(1, "loot-recovery", default, HealthPoints.FromWhole(10), 0, 0, false)] } }).ToArray() };
        outcomes.Observe(healed);
        var change = journal.Observe(draft, healed, draft.PlayerId, 1, HudPresentation.Project(healed, draft.PlayerId, false, false));
        Assert.Same(frozen, outcomes.For(draft.PlayerId));
        Assert.Contains("Restore 10 Keep HP", change.Reward!.Details); Assert.Equal(1, journal.ReceiptCount);
    }
}
