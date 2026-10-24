// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;
namespace Game.Shared.Tests;

public sealed class MajorEncounterComparisonTests
{
    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    public void BothExperimentalFormationsRespectBossSafetyAndTheDeclaredBudget(int major)
    {
        var baseline = MajorEncounterComparison.SingleBossRules();
        var combat = new CombatConfiguration(baseline);
        var original = baseline.Campaign.Waves[major - 1];
        Assert.True(original.IsBoss);
        Assert.Single(original.Entries);
        var hashes = new HashSet<CombatFingerprint> { new CampaignConfiguration(baseline.Campaign, combat).Fingerprint };
        foreach (bool heavy in new[] { false, true })
        {
            var rules = MajorEncounterComparison.FormationRules(major, heavy);
            var candidate = rules.Campaign.Waves[major - 1];
            Assert.False(candidate.IsBoss);
            Assert.DoesNotContain(candidate.Entries, e => e.IsBoss);
            Assert.Equal(original.Reward, candidate.Reward);
            decimal ratio = WavePower.Total(candidate, combat) / WavePower.Total(original, combat);
            Assert.InRange(ratio, .97m, 1.03m);
            Assert.True(hashes.Add(new CampaignConfiguration(rules.Campaign, combat).Fingerprint));
            var invalid = rules.Campaign with { Waves = rules.Campaign.Waves.Select(w => w.Number == major ? w with { IsBoss = true } : w).ToArray() };
            Assert.Throws<ArgumentException>(() => new CampaignConfiguration(invalid, combat));
        }
    }
}
