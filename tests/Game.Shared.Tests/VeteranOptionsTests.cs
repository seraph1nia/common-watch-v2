// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class VeteranOptionsTests
{
    private static void Accept(CommandResult result) => Assert.True(result.Accepted, result.Message);
    [Fact]
    public void RecoveryPreviewMatchesRealPaidProductionWhileIdentityEquipmentAndHistoryPersist()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall");
        var member = solo.City.Soldiers.First(u => u.Assignment is { Stored: true } && u.Health < u.Profile.Health);
        var comparison = VeteranComparison.Project(solo.Snapshot, solo.City, member.Id);
        var recovery = Assert.Single(comparison.Recovery, r => r.StoredHere);
        Assert.Empty(recovery.Reason); Assert.InRange(recovery.ProductionsToFull, 1, 20);
        Assert.Contains("no stats or equipment", comparison.History);
        Assert.DoesNotContain("Service history", comparison.Summary);
        var resources = solo.City.Resources; int productions = solo.Snapshot.ProductionCount;
        var income = solo.City.ProductionIncome!.Value;
        Accept(solo.Send("ready"));
        var healed = solo.City.Soldiers.Single(u => u.Id == member.Id);
        Assert.Equal(recovery.HealthAfterProduction, healed.Health);
        Assert.Equal(member.Level, healed.Level); Assert.Equal(member.Experience, healed.Experience);
        Assert.Equal(member.Assignment, healed.Assignment); Assert.Equal(member.SquadId, healed.SquadId); Assert.Equal(member.Profile, healed.Profile);
        Assert.DoesNotContain(member.Id, solo.City.LastUpkeep!.Participating);
        Assert.Contains(member.Id, solo.City.LastUpkeep.Funded);
        Assert.Equal(productions + 1, solo.Snapshot.ProductionCount);
        Assert.Equal(resources.Metal + income.Metal, solo.City.Metal);
        int afterProduction = solo.Snapshot.ProductionCount; int food = solo.City.Food;
        Accept(solo.RetryLast()!);
        Assert.Equal(afterProduction, solo.Snapshot.ProductionCount); Assert.Equal(food, solo.City.Food);
        var retried = solo.City.Soldiers.Single(u => u.Id == member.Id);
        Assert.Equal(healed.Health, retried.Health); Assert.Equal(healed.Experience, retried.Experience); Assert.Equal(healed.Assignment, retried.Assignment);
        var decoded = SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(solo.Snapshot));
        var roundTrip = decoded.Players.Single(p => p.Id == solo.PlayerId).Soldiers.Single(u => u.Id == member.Id);
        Assert.Equal(healed.Health, roundTrip.Health); Assert.Equal(healed.Profile, roundTrip.Profile);
        Assert.Equal(healed.Assignment, roundTrip.Assignment); Assert.Equal(healed.Experience, roundTrip.Experience);
    }
    [Fact]
    public void ServiceHistoryDoesNotAdvanceEquipmentAndNewTierCostsRemainExplicit()
    {
        using var solo = GameplayScenarios.CreateAt("late");
        var member = solo.City.Soldiers.First(u => u.Level < solo.City.Slots[2].Level);
        var comparison = VeteranComparison.Project(solo.Snapshot, solo.City, member.Id);
        var replacement = Assert.Single(comparison.Replacements);
        Assert.True(member.Experience > 0); Assert.Equal(5, replacement.Quote.Level);
        Assert.True(replacement.Quote.Profile.Health > member.Profile.Health);
        Assert.True(replacement.Quote.Profile.Damage > member.Profile.Damage);
        Assert.Contains("no refund", comparison.Summary); Assert.Contains("separate paid recruit", comparison.Summary);
        Assert.Empty(comparison.Recovery); Assert.False(replacement.SpaceNow);
        int previousId = solo.City.Soldiers.Max(u => u.Id);
        var before = solo.City.Resources;
        Accept(solo.Send("retire", unitId: member.Id)); Assert.Equal(before, solo.City.Resources);
        Accept(solo.Send("recruit", replacement.TrainerSlot, soldierType: member.Type));
        var recruit = solo.City.Soldiers.Single(u => u.Id > previousId);
        Assert.NotEqual(member.Id, recruit.Id); Assert.Equal(replacement.Quote.Profile, recruit.Profile);
        Assert.Equal(0, recruit.Experience); Assert.Equal(replacement.Quote.Level, recruit.Level);
        Assert.True(before.TryPay(replacement.Quote.Cost, out var expected)); Assert.Equal(expected, solo.City.Resources);
        Assert.DoesNotContain(solo.City.Soldiers, u => u.Id == member.Id);
        long revision = solo.Snapshot.Revision; int count = solo.City.Soldiers.Length;
        Accept(solo.RetryLast()!); Assert.Equal(revision, solo.Snapshot.Revision); Assert.Equal(count, solo.City.Soldiers.Length); Assert.Equal(expected, solo.City.Resources);
    }
    [Fact]
    public void EarlySameTierVeteransSaveRecruitmentCostWithoutAnUnstatedServiceBonus()
    {
        foreach (ulong seed in new[] { 1UL, SoloSession.DefaultSeed })
        {
            using var solo = ScenarioRunner.CreateAt("post-draft", seed);
            var member = solo.City.Soldiers.First();
            var view = VeteranComparison.Project(solo.Snapshot, solo.City, member.Id);
            var recruit = Assert.Single(view.Replacements);
            Assert.Equal(member.Level, recruit.Quote.Level); Assert.Equal(member.Profile, recruit.Quote.Profile);
            Assert.True(member.Experience > 0); Assert.NotEqual(default, recruit.Quote.Cost);
            Assert.Equal(0, view.Upkeep - recruit.Quote.Upkeep);
        }
    }
    [Fact]
    public void UnfundedNewRecruitsCanBeStoredButGainNoRecoveryOrServiceFromStorage()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall");
        var veteran = solo.City.Soldiers.First(u => u.Assignment is { Stored: false });
        Accept(solo.Send("retire", unitId: veteran.Id)); Accept(solo.Send("recruit", 2));
        var fresh = solo.City.Soldiers.OrderByDescending(u => u.Id).First();
        Assert.False(fresh.RecoveryEligible);
        var comparison = VeteranComparison.Project(solo.Snapshot, solo.City, fresh.Id);
        Assert.Contains("Not eligible", comparison.Recovery[0].Reason);
        Assert.Equal(fresh.Health, comparison.Recovery[0].HealthAfterProduction);
        int hall = solo.City.Army!.Halls[0].Slot;
        Accept(solo.Send("store", hall, unitId: fresh.Id));
        Accept(solo.Send("ready"));
        var stored = solo.City.Soldiers.Single(u => u.Id == fresh.Id);
        Assert.Equal(fresh.Health, stored.Health); Assert.Equal(0, stored.Experience); Assert.False(stored.RecoveryEligible);
        Assert.Contains(fresh.Id, solo.City.LastUpkeep!.Funded); Assert.DoesNotContain(fresh.Id, solo.City.LastUpkeep.Participating);
        GameplayScenarios.FinishCombat(solo);
        var afterClear = solo.City.Soldiers.Single(u => u.Id == fresh.Id);
        Assert.True(afterClear.RecoveryEligible); Assert.Equal(0, afterClear.Experience); Assert.Equal(fresh.Level, afterClear.Level);
    }
    [Fact]
    public void FullIndividualStorageBlocksARecoveryMoveWithoutTouchingTheSoldier()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall");
        var hall = solo.City.Army!.Halls.Single();
        int used = solo.City.Soldiers.Count(u => u.Assignment is { Stored: true });
        foreach (var field in solo.City.Soldiers.Where(u => u.Assignment is { Stored: false }).Take(hall.Capacity - used).ToArray())
            Accept(solo.Send("store", hall.Slot, unitId: field.Id));
        var member = solo.City.Soldiers.First(u => u.Assignment is { Stored: false });
        var view = VeteranComparison.Project(solo.Snapshot, solo.City, member.Id);
        var recovery = Assert.Single(view.Recovery);
        Assert.False(recovery.CanStore); Assert.Contains("Hall full", recovery.Reason); Assert.Equal(member.Health, recovery.HealthAfterProduction);
        var stock = solo.City.Resources; long revision = solo.Snapshot.Revision;
        Assert.False(solo.Send("store", hall.Slot, unitId: member.Id).Accepted);
        Assert.Equal(stock, solo.City.Resources); Assert.Equal(revision, solo.Snapshot.Revision);
        var unchanged = solo.City.Soldiers.Single(u => u.Id == member.Id);
        Assert.Equal(member.Assignment, unchanged.Assignment); Assert.Equal(member.SquadId, unchanged.SquadId); Assert.Equal(member.Health, unchanged.Health);
    }
    [Fact]
    public void ReturningAHealedSoldierDoesNotChargeRecruitmentChangeTierOrDuplicateIt()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall");
        var member = solo.City.Soldiers.First(u => u.Assignment is { Stored: true });
        var quote = VeteranComparison.Project(solo.Snapshot, solo.City, member.Id);
        int hall = member.Assignment!.HallSlot;
        var field = solo.City.Soldiers.First(u => u.Type == member.Type && u.Assignment is { Stored: false });
        Accept(solo.Send("retire", unitId: field.Id));
        var before = solo.City.Resources;
        Accept(solo.Send("send", hall, unitId: member.Id));
        var returned = solo.City.Soldiers.Single(u => u.Id == member.Id);
        Assert.Equal(before, solo.City.Resources); Assert.False(returned.Assignment!.Stored);
        Assert.Equal(member.Health, returned.Health); Assert.Equal(member.Level, returned.Level); Assert.Equal(member.Experience, returned.Experience);
        long revision = solo.Snapshot.Revision;
        Accept(solo.RetryLast()!); Assert.Equal(revision, solo.Snapshot.Revision);
        Assert.False(solo.Send("send", hall, unitId: member.Id).Accepted);
        Assert.Single(solo.City.Soldiers, u => u.Id == member.Id);
    }
}
