using System.Text.Json;
using Game.Core;
using Xunit;

namespace Game.Core.Tests;

public sealed class ResourceTests
{
    [Fact]
    public void WholeSixResourceOperationsRejectAnyInvalidComponentAtomically()
    {
        ResourceCost balance = new(10, 20, 30, 40, 50, 60);
        Assert.True(balance.TryPay(new(1, 2, 3, 4, 5, 6), out ResourceCost paid));
        Assert.Equal(new ResourceCost(9, 18, 27, 36, 45, 54), paid);
        foreach (Resource resource in Enum.GetValues<Resource>())
        {
            Assert.False(balance.TryPay(new ResourceCost().With(resource, balance.Amount(resource) + 1), out ResourceCost rejected));
            Assert.Equal(default, rejected);
            Assert.False(balance.TryPay(new ResourceCost().With(resource, -1), out _));
            ResourceCost full = balance.With(resource, int.MaxValue);
            Assert.False(full.TryAdd(new ResourceCost().With(resource, 1), out rejected));
            Assert.Equal(default, rejected);
            Assert.False(full.TryMultiply(2, out _));
            Assert.False(balance.With(resource, -1).TryPay(default, out _));
        }
        ResourceCost aliased = balance;
        Assert.True(aliased.TryAdd(new(1), out aliased)); Assert.Equal(11, aliased.Gold);
        Assert.True(aliased.TryPay(new(1), out aliased)); Assert.Equal(balance, aliased);
        Assert.True(aliased.TryMultiply(2, out aliased)); Assert.Equal(120, aliased.Cloth);
        Assert.False(balance.TryMultiply(-1, out _));
        Assert.True(balance.TryMultiply(0, out ResourceCost zero)); Assert.Equal(default, zero);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ResourceCost>("{\"Gold\":1.5}", WireJson.Options));
        string json = JsonSerializer.Serialize(balance, WireJson.Options);
        Assert.Equal(balance, JsonSerializer.Deserialize<ResourceCost>(json, WireJson.Options));
    }

    [Fact]
    public void InvestmentRefundRoundsAfterSummingEachResource()
    {
        Assert.True(new ResourceCost(25, 15, Stone: 5).TryAdd(new(30, 10, Stone: 10), out ResourceCost total));
        Assert.Equal(new ResourceCost(27, 12, Stone: 7), total.HalfRefund());
    }

    [Fact]
    public void ProductionOverflowRestoresReadinessAndAllCities()
    {
        using var match = new Match(combatSeed: 1); match.Join(); match.Join();
        Command Request(int city, string action) => new(1, match.Id, match.Phase, match.TurnSerial, action, city);
        Assert.True(match.Apply(1, Request(1, "start")).Accepted);
        match.Players[2].Gold = int.MaxValue;
        Assert.True(match.Apply(1, Request(1, "ready")).Accepted);
        string before = JsonSerializer.Serialize(match.Snapshot(), WireJson.Options);
        Assert.False(match.Apply(2, Request(2, "ready")).Accepted);
        Assert.Equal(before, JsonSerializer.Serialize(match.Snapshot(), WireJson.Options));
        Assert.Equal(0, match.ProductionCount);
        match.Players[2].Gold = 0;
        Assert.True(match.Apply(2, Request(2, "ready")).Accepted);
        Assert.Equal(1, match.ProductionCount);
        Assert.Equal(2, match.Players[2].Gold);
        Assert.All(match.Players.Values, c => Assert.Equal(new ResourceCost(c.Gold, c.Wood, Stone: 2), c.Resources));
    }

    [Fact]
    public void FrozenQuotesAndPublishedIdentityIncludeEconomyWithoutRerollingCombat()
    {
        using var match = new Match(combatSeed: 1);
        var changed = new Rules { StartingGold = 61 };
        Assert.Equal(match.Configuration.Fingerprint, new CombatConfiguration(changed).Fingerprint);
        Assert.NotEqual(match.ConfigurationFingerprint, RulesIdentity.Resolve(changed));
        Assert.NotEqual(match.ConfigurationFingerprint, RulesIdentity.Resolve(new Rules { FarmOutput = 6 }));
        Assert.NotEqual(match.ConfigurationFingerprint, RulesIdentity.Resolve(new Rules { SwordMetalCost = 15 }));
        BuildingDefinition barracks = match.Economy.Building(Building.Barracks);
        barracks.Recruits![0] = UnitType.Mage;
        Assert.Equal(UnitType.Swordsman, match.Economy.Building(Building.Barracks).Recruits![0]);
        Assert.Equal(match.ConfigurationFingerprint, RulesIdentity.Resolve(match.Snapshot().Rules));
        Assert.Throws<ArgumentException>(() => new EconomyConfiguration(new Rules { StartingWood = -1 }));
    }

    [Fact]
    public void MarketQuotesArePositiveWholeBoundedBundles()
    {
        var economy = new EconomyConfiguration(new());
        foreach (Resource resource in Enum.GetValues<Resource>().Where(r => r != Resource.Gold))
        {
            Assert.True(economy.TryMarketQuote(resource, 2, out ResourceCost stock, out ResourceCost proceeds));
            Assert.Equal(resource == Resource.Food ? 50 : 10, stock.Amount(resource));
            Assert.Equal(resource is Resource.Metal or Resource.Cloth ? 4 : 2, proceeds.Gold);
            Assert.False(economy.TryMarketQuote(resource, int.MaxValue, out _, out _));
            Assert.False(economy.TryMarketQuote(resource, 0, out _, out _));
            Assert.False(economy.TryMarketQuote(resource, -1, out _, out _));
        }
        Assert.False(economy.TryMarketQuote(Resource.Gold, 1, out _, out _));
        Assert.False(economy.TryMarketQuote((Resource)99, 1, out _, out _));
    }
}
