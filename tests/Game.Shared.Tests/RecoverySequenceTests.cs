// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;
namespace Game.Shared.Tests;

public sealed class RecoverySequenceTests
{
    private static int WoundedSquad(SoloSession solo) => solo.City.Soldiers.Where(u => u.Assignment is { Stored: false } && u.Health < u.Profile.Health)
        .GroupBy(u => u.SquadId).OrderByDescending(g => g.Count()).First().Key;
    private static SquadRecoveryPlan Plan(SoloSession solo) => SquadRecovery.Project(solo.Snapshot, solo.PlayerId, WoundedSquad(solo), solo.City.Army!.Halls[0].Slot, RecoveryMove.StoreWounded);
    private static void Accept(CommandResult r) => Assert.True(r.Accepted, r.Message);
    private static void Execute(SoloSession solo, RecoverySequence sequence)
    {
        for (int i = 0; i < 24 && !sequence.Finished && sequence.Stopped.Length == 0; i++)
        {
            var intent = sequence.Next(solo.Snapshot); if (intent is null) continue;
            Accept(solo.Submit(intent)); sequence.Submitted(solo.LastCommand!); sequence.Acknowledge(solo.LastCommand!, solo.LastResult!);
        }
        sequence.Next(solo.Snapshot);
    }
    [Fact]
    public void FiniteSquadMovePreservesHealthIdentityAndEquipmentThenActualPaymentMatchesPreview()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var plan = Plan(solo);
        Assert.Equal(3, plan.Moving.Length); var stock = solo.City.Resources;
        var sequence = new RecoverySequence(plan); Execute(solo, sequence);
        Assert.True(sequence.Finished, sequence.Status); Assert.Equal(3, sequence.Completed); Assert.Equal(stock, solo.City.Resources);
        foreach (var entry in plan.Moving)
        {
            var actual = solo.City.Soldiers.Single(u => u.Id == entry.Before.Id);
            Assert.Equal(entry.Before.Health, actual.Health); Assert.Equal(entry.Before.Profile, actual.Profile);
            Assert.Equal(entry.Before.Level, actual.Level); Assert.Equal(entry.Before.Experience, actual.Experience);
            Assert.Equal(entry.Destination, actual.Assignment);
        }
        var food = plan.Food!; Accept(solo.Send("ready"));
        Assert.Equal(food.Paid, solo.City.LastUpkeep!.Paid); Assert.Equal(food.Participating, solo.City.LastUpkeep.Participating);
        foreach (var entry in plan.Moving) Assert.Equal(entry.NextHealth, solo.City.Soldiers.Single(u => u.Id == entry.Before.Id).Health);
        long revision = solo.Snapshot.Revision; Accept(solo.RetryLast()!); Assert.Equal(revision, solo.Snapshot.Revision);
    }
    [Fact]
    public void AckBeforeSnapshotAndDuplicateAckCannotSendOrAdvanceAnotherMember()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var sequence = new RecoverySequence(Plan(solo));
        var before = solo.Snapshot; var first = sequence.Next(before)!; Accept(solo.Submit(first));
        var command = solo.LastCommand!; var result = solo.LastResult!; sequence.Acknowledge(command, result); sequence.Submitted(command);
        sequence.Acknowledge(command, result); Assert.Null(sequence.Next(before)); Assert.Equal(0, sequence.Completed);
        var next = sequence.Next(solo.Snapshot); Assert.NotNull(next); Assert.NotEqual(first.UnitId, next!.UnitId); Assert.Equal(1, sequence.Completed);
        sequence.Acknowledge(command, result); Assert.Null(sequence.Next(solo.Snapshot)); Assert.Equal(1, sequence.Completed);
        long revision = solo.Snapshot.Revision; Accept(solo.RetryLast()!); Assert.Equal(revision, solo.Snapshot.Revision);
    }
    [Fact]
    public void SnapshotBeforeAckDoesNotAdvanceAndCancelRetainsOnlyTheAcceptedPrefix()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var sequence = new RecoverySequence(Plan(solo));
        var first = sequence.Next(solo.Snapshot)!; Accept(solo.Submit(first)); sequence.Submitted(solo.LastCommand!); Assert.Null(sequence.Next(solo.Snapshot));
        sequence.Cancel("Cancelled by player."); sequence.Submitted(solo.LastCommand!); sequence.Acknowledge(solo.LastCommand!, solo.LastResult!);
        Assert.Null(sequence.Next(solo.Snapshot)); Assert.Equal(1, sequence.Completed); Assert.False(sequence.Finished);
        var refreshed = Plan(solo); Assert.DoesNotContain(refreshed.Moving, m => m.Before.Id == first.UnitId);
    }
    [Fact]
    public void PartialHallCapacityIsQuotedAndMovesOnlyTheReviewedMember()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var initial = Plan(solo); var hall = solo.City.Army!.Halls[0];
        int used = solo.City.Soldiers.Count(u => u.Assignment is { Stored: true });
        foreach (var member in solo.City.Soldiers.Where(u => u.Assignment is { Stored: false } && u.SquadId != initial.Squad).Take(hall.Capacity - used - 1).ToArray())
            Accept(solo.Send("store", hall.Slot, unitId: member.Id));
        var plan = SquadRecovery.Project(solo.Snapshot, solo.PlayerId, initial.Squad, hall.Slot, RecoveryMove.StoreWounded);
        Assert.Equal(3, plan.Members.Length); Assert.Single(plan.Moving); Assert.Equal(2, plan.Members.Count(m => !m.Moves));
        var sequence = new RecoverySequence(plan); Execute(solo, sequence); Assert.True(sequence.Finished, sequence.Status);
        Assert.Equal(hall.Capacity, solo.City.Soldiers.Count(u => u.Assignment is { Stored: true }));
    }
    [Fact]
    public void ActualReadyOrUnrelatedPaidChangeInvalidatesAReviewBeforeSubmission()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var plan = Plan(solo);
        Accept(solo.Send("ready")); Assert.False(SquadRecovery.IsCurrent(plan, solo.Snapshot));
        var sequence = new RecoverySequence(plan); Assert.Null(sequence.Next(solo.Snapshot)); Assert.NotEmpty(sequence.Stopped);
    }
    [Fact]
    public void HealthyAndMissingHallHaveNoExecutableRecoveryPlan()
    {
        using var solo = ScenarioRunner.CreateAt("post-draft");
        var plan = SquadRecovery.Project(solo.Snapshot, solo.PlayerId, solo.City.Soldiers[0].SquadId, -1, RecoveryMove.StoreWounded);
        Assert.False(plan.CanCommit); Assert.Contains("Town Hall", plan.Unavailable);
        Assert.Null(new RecoverySequence(plan).Next(solo.Snapshot));
    }
    [Fact]
    public void AnotherAcknowledgmentForTheSameMemberCannotConfirmTheIssuedCommand()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var sequence = new RecoverySequence(Plan(solo));
        var intent = sequence.Next(solo.Snapshot)!; Accept(solo.Submit(intent)); var issued = solo.LastCommand!;
        sequence.Submitted(issued);
        var old = issued with { Sequence = issued.Sequence - 1 };
        sequence.Acknowledge(old, solo.LastResult! with { Sequence = old.Sequence });
        Assert.Null(sequence.Next(solo.Snapshot)); Assert.Equal(0, sequence.Completed);
        sequence.Acknowledge(issued, solo.LastResult!); Assert.NotNull(sequence.Next(solo.Snapshot)); Assert.Equal(1, sequence.Completed);
    }
    [Fact]
    public void PaidResearchChangesMaximumHealthAndInvalidatesTheOldRecoveryQuote()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var plan = Plan(solo);
        Assert.True(solo.City.Technologies.Single(t => t.Id == TechnologyId.MeleeFoundation).Available);
        Accept(solo.Send("research-tech", technology: TechnologyId.MeleeFoundation));
        Assert.False(SquadRecovery.IsCurrent(plan, solo.Snapshot));
        var before = plan.Moving[0].Before;
        Assert.True(solo.City.Soldiers.Single(u => u.Id == before.Id).Profile.Health > before.Profile.Health);
        Assert.Null(new RecoverySequence(plan).Next(solo.Snapshot));
    }
    [Fact]
    public void RejectedInFlightMoveStopsWithoutSubstitutingAnotherMember()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var plan = Plan(solo); var sequence = new RecoverySequence(plan);
        var intent = sequence.Next(solo.Snapshot)!; var hall = solo.City.Army!.Halls[0];
        int used = solo.City.Soldiers.Count(u => u.Assignment is { Stored: true });
        foreach (var u in solo.City.Soldiers.Where(u => u.Assignment is { Stored: false } && u.SquadId != plan.Squad).Take(hall.Capacity-used).ToArray())
            Accept(solo.Send("store", hall.Slot, unitId: u.Id));
        var stock = solo.City.Resources;
        Assert.False(solo.Submit(intent).Accepted); sequence.Submitted(solo.LastCommand!); sequence.Acknowledge(solo.LastCommand!, solo.LastResult!);
        Assert.Null(sequence.Next(solo.Snapshot)); Assert.Equal(0, sequence.Completed); Assert.Contains("rejected", sequence.Stopped);
        Assert.Equal(stock, solo.City.Resources); Assert.False(solo.City.Soldiers.Single(u => u.Id == intent.UnitId).Assignment!.Stored);
    }
    [Fact]
    public void RecoveredReturnUsesCurrentSpaceAndCannotDuplicateOrReequipMembers()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall"); var hall = solo.City.Army!.Halls[0];
        var healthy = solo.City.Soldiers.First(u => u.Assignment is { Stored: false } && u.Health == u.Profile.Health);
        Accept(solo.Send("store", hall.Slot, unitId: healthy.Id));
        var plan = SquadRecovery.Project(solo.Snapshot, solo.PlayerId, 0, hall.Slot, RecoveryMove.ReturnRecovered);
        Assert.NotEmpty(plan.Moving); var stock = solo.City.Resources;
        var sequence = new RecoverySequence(plan); Execute(solo, sequence); Assert.True(sequence.Finished, sequence.Status);
        foreach (var entry in plan.Moving)
        {
            var actual = solo.City.Soldiers.Single(u => u.Id == entry.Before.Id);
            Assert.Equal(entry.Before.Health, actual.Health); Assert.Equal(entry.Before.Level, actual.Level); Assert.Equal(entry.Before.Experience, actual.Experience);
            Assert.Equal(entry.Destination, actual.Assignment);
        }
        Assert.Equal(stock, solo.City.Resources); long revision = solo.Snapshot.Revision;
        Accept(solo.RetryLast()!); Assert.Equal(revision, solo.Snapshot.Revision);
    }

}
