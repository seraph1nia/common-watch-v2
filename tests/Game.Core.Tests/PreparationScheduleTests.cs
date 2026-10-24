// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
using Xunit;
namespace Game.Core.Tests;

public sealed class PreparationScheduleTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ActualBoundariesDeliverQuotedResourcesAndOnlyOneResearchEventWithExactRetries(int stages)
    {
        var rules = new Rules { PreparationStages = stages };
        using var host = new AuthoritySession(AuthorityPolicy.Solo, rules: rules, matchId: "preparation-boundaries");
        long sequence = 0;
        void Send(string action, int slot = -1, Building building = Building.Empty)
        {
            var result = host.ExecuteLocal(Command.FromSnapshot(host.Snapshot(), ++sequence, action, 1, slot, building));
            Assert.True(result.Accepted, result.Message);
        }
        Send("start"); Send("build", 0, Building.ResearchTower);
        for (int stage = 1; stage <= stages; stage++)
        {
            var before = host.Snapshot(); var city = before.Players.Single();
            Assert.Equal(stage, before.Turn);
            Assert.Equal(stage == stages ? Phase.Preparation : Phase.Building, before.Phase);
            var income = city.ProductionIncome!.Value;
            Assert.Equal(rules.BaseGold * (stages == 2 && stage == 1 ? 2 : 1), income.Gold);
            var ready = Command.FromSnapshot(before, ++sequence, "ready", 1);
            var accepted = host.ExecuteLocal(ready); Assert.True(accepted.Accepted, accepted.Message);
            var after = host.Snapshot(); var paid = after.Players.Single();
            Assert.Equal(stage, after.ProductionCount);
            Assert.True(city.Resources.TryAdd(income, out var expected)); Assert.Equal(expected, paid.Resources);
            Assert.Equal(city.Research.Income(progress: 1), paid.Research);
            string settled = JsonSerializer.Serialize(after, WireJson.Options);
            Assert.Equal(accepted, host.ExecuteLocal(ready));
            Assert.Equal(settled, JsonSerializer.Serialize(host.Snapshot(), WireJson.Options));
            SnapshotCompatibility.RequireSharedArena(after);
        }
        Assert.Equal(Phase.Combat, host.Phase);
        Assert.Equal(stages == 2 ? new ResearchState(0, 2) : new ResearchState(1, 0), host.Snapshot().Players.Single().Research);
    }
    [Fact]
    public void UnsupportedSchedulesAndForeignTimingFingerprintsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new Match(new Rules { PreparationStages = 1 }));
        Assert.Throws<ArgumentException>(() => new Match(new Rules { PreparationStages = 4 }));
        using var two = new Match(new Rules { PreparationStages = 2 }); two.Join();
        using var three = new Match(new Rules { PreparationStages = 3 }); three.Join();
        Assert.NotEqual(two.Snapshot().ConfigurationFingerprint, three.Snapshot().ConfigurationFingerprint);
        var mismatched = two.Snapshot() with { ConfigurationFingerprint = three.Snapshot().ConfigurationFingerprint };
        Assert.Throws<ArgumentException>(() => SnapshotCompatibility.RequireSharedArena(mismatched));
    }
}
