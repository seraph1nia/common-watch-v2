// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;
namespace Game.Core.Tests;

public sealed class PreparationInvestmentTests
{
    // Normal paid Warden stock. Independent expected marginal outputs expose the lost reinvestment window.
    [Theory]
    [InlineData(Building.Farm, Resource.Food, 5, 8)]
    [InlineData(Building.Mine, Resource.Gold, 1, 2)]
    [InlineData(Building.Lumbermill, Resource.Wood, 1, 2)]
    [InlineData(Building.Stonecutter, Resource.Stone, 1, 2)]
    [InlineData(Building.MetalMine, Resource.Metal, 4, 8)]
    [InlineData(Building.Weaver, Resource.Cloth, 4, 8)]
    public void ProducerBuiltOrUpgradedAfterFirstPaymentLosesOneSpendableOutput(Building building, Resource resource, int one, int two)
    {
        foreach (int stages in new[] { 3, 2 })
        foreach (bool upgrade in new[] { false, true })
        {
            using var match = new Match(new Rules { PreparationStages = stages }, matchId: "investment-timing");
            int id = match.Join()!.Id; long sequence = 0;
            CityState City() => match.Snapshot().Players.Single();
            void Send(string action, int slot = -1, Building type = Building.Empty)
                => Assert.True(match.Apply(id, Command.FromSnapshot(match.Snapshot(), ++sequence, action, id, slot, type)).Accepted);
            Send("start");
            // Lumber earns Wood for upgrades. Warden's two Stone fund the ordinary upgrade cost.
            Send("build", 1, Building.Lumbermill);
            if (upgrade) Send("build", 0, building);
            Send("ready");
            var before = City();
            Send(upgrade ? "upgrade" : "build", 0, building);
            int additional = upgrade ? two - one : one;
            Assert.Equal((before.ProductionIncome!.Value.Amount(resource) + additional), City().ProductionIncome!.Value.Amount(resource));
            if (stages == 3)
            {
                var stock = City().Resources; var quote = City().ProductionIncome!.Value;
                Send("ready");
                Assert.Equal(stock.Amount(resource) + quote.Amount(resource), City().Resources.Amount(resource));
                Assert.Equal(Phase.Preparation, match.Phase); // Earned output remains spendable.
            }
            Assert.Equal(stages, match.Snapshot().Turn);
            Send("ready");
            Assert.Equal(Phase.Combat, match.Phase); // Candidate's new output first arrives here.
            Assert.Equal(0, City().LastUpkeep!.Paid);
        }
    }
}
