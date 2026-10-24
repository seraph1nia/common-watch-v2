// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;
namespace Game.Core.Tests;
public sealed class CampaignCompositionTests
{
    [Fact]
    public void MidpointIsAnOrdinaryEliteFormationAndTheFinalBossRemainsStrictlySingle()
    {
        var rules = new Rules(); var combat = new CombatConfiguration(rules);
        var campaign = new CampaignConfiguration(rules.Campaign, combat);
        var midpoint = campaign.Wave(10); var final = campaign.Wave(20);
        Assert.False(midpoint.IsBoss); Assert.Equal(6, midpoint.Allocation);
        Assert.Equal(new[] { new SpawnEntry(UnitType.Swordsman, 3, 2), new(UnitType.Berserker, 3, 2),
            new(UnitType.Crossbowman, 1), new(UnitType.Mage, 2) }, midpoint.Entries);
        Assert.Equal(new ResourceCost(4, 2, 10), midpoint.Reward);
        Assert.True(final.IsBoss); Assert.True(Assert.Single(final.Entries).IsBoss);
        Assert.Equal(1, final.Allocation); Assert.Equal(5, final.Entries[0].Level);
        var invalid = rules.Campaign with { Waves = rules.Campaign.Waves.Select(w => w.Number == 10 ? w with { IsBoss = true } : w).ToArray() };
        Assert.Throws<ArgumentException>(() => new CampaignConfiguration(invalid, combat));
    }
}
