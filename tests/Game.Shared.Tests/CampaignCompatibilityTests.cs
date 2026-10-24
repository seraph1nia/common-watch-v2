// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;
namespace Game.Shared.Tests;
public sealed class CampaignCompatibilityTests
{
    [Fact]
    public void CurrentSchemaAcceptsDeclaredBossOrFormationCatalogAndRejectsTamperingOrOldRules()
    {
        foreach (var rules in new[] { new Rules(), MajorEncounterComparison.SingleBossRules(), MajorEncounterComparison.FormationRules(20, true) })
        {
            using var solo = new SoloSession(rules: rules); Assert.True(solo.Start().Accepted);
            var state = solo.Snapshot;
            var received = SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(state));
            Assert.Equal(state.ConfigurationFingerprint, received.ConfigurationFingerprint);
            Assert.Equal(state.WaveCatalog[9].IsBoss, received.WaveCatalog[9].IsBoss);
            var tampered = state with { WaveCatalog = state.WaveCatalog.Select(w => w.Number == 10 ? w with { Reward = new(999) } : w).ToArray() };
            Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(tampered)));
            Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(state with { CombatRulesVersion = 7 })));
        }
    }
}
