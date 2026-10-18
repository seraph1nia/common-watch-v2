using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class GameplayParityTests
{
    [Fact]
    public void PaidEconomyExposesEveryCatalogEntryAndRecordsActualRefunds()
    {
        using var solo = GameplayScenarios.CreateAt("late");
        Assert.True(solo.Send("buy-plot", 7).Accepted);
        foreach (var definition in solo.Snapshot.BuildingCatalog)
        {
            var before = solo.City.Resources;
            Accept(solo.Send("build", 7, definition.Type));
            Assert.True(before.TryPay(definition.Construction, out var expected)); Assert.Equal(expected, solo.City.Resources);
            if (solo.City.Slots[7].UpgradeQuote is { } upgrade)
            {
                before = solo.City.Resources; Accept(solo.Send("upgrade", 7));
                Assert.True(before.TryPay(upgrade, out expected)); Assert.Equal(expected, solo.City.Resources);
            }
            var refund = solo.City.Slots[7].Refund; before = solo.City.Resources;
            Accept(solo.Send("sell", 7)); Assert.True(before.TryAdd(refund, out expected)); Assert.Equal(expected, solo.City.Resources);
        }
        int market = Array.FindIndex(solo.City.Slots, s => s.Type == Building.Market);
        foreach (var rate in solo.Snapshot.MarketRates)
        {
            if (solo.City.Resources.Amount(rate.Resource) >= rate.Units)
            {
                var before = solo.City.Resources; int bonus = solo.City.MarketSaleBonus;
                Accept(solo.Send("trade", market, resource: rate.Resource, bundles: 1));
                Assert.Equal(before.Amount(rate.Resource) - rate.Units, solo.City.Resources.Amount(rate.Resource));
                Assert.Equal(before.Gold + rate.Gold + bonus, solo.City.Gold);
            }
            Assert.False(solo.Send("trade", market, resource: rate.Resource, bundles: 0).Accepted);
        }
    }

    [Fact]
    public void FourClassesKeepIdentityAndCanRegroupDeployStoreReturnAndRetire()
    {
        using var solo = GameplayScenarios.CreateAt("army");
        Assert.All(Enum.GetValues<UnitType>(), type => Assert.Contains(solo.City.Soldiers, u => u.Type == type));
        int member = solo.City.Soldiers.First(u => u.Type == UnitType.Swordsman).Id;
        int squad = solo.City.Soldiers.First(u => u.Id == member).SquadId;
        var sector = solo.Snapshot.Arena!.Sectors.Single(s => s.Owner == solo.PlayerId);
        // Free the other members of a home, then move the chosen squad there.
        var sourceHome = solo.City.Soldiers.Single(u => u.Id == member).Assignment!.Tile;
        var destination = solo.City.Army!.Homes.First(h => h.Cell != sourceHome && h.Plot.Type == Building.Empty);
        foreach (var unit in solo.City.Soldiers.Where(u => u.Assignment?.Tile == destination.Cell).ToArray()) Accept(solo.Send("retire", unitId: unit.Id));
        Accept(solo.Send("deploy-squad", squadId: squad, cell: sector.DeploymentCells[destination.Cell]));
        Assert.All(solo.City.Soldiers.Where(u => u.SquadId == squad), u => Assert.Equal(destination.Cell, u.Assignment!.Tile));
        Assert.False(solo.Send("build", destination.PlotId, Building.ArrowTower).Accepted);
        Accept(solo.Send("regroup-member", unitId: member, squadId: -1));
        Assert.NotEqual(squad, solo.City.Soldiers.Single(u => u.Id == member).SquadId);
        // Sell a utility building, use the real refund and quote to store a veteran.
        Accept(solo.Send("trade", 8, resource: Resource.Metal, bundles: 2));
        Accept(solo.Send("sell", 8)); Accept(solo.Send("build", 8, Building.TownHall));
        var before = solo.City.Soldiers.Single(u => u.Id == member);
        Accept(solo.Send("store", 8, unitId: member));
        var stored = solo.City.Soldiers.Single(u => u.Id == member);
        Assert.True(stored.Assignment!.Stored); Assert.Equal(before.Health, stored.Health); Assert.Equal(before.Experience, stored.Experience);
        Assert.False(solo.Send("sell", 8).Accepted);
        Accept(solo.Send("send", 8, unitId: member)); Assert.False(solo.City.Soldiers.Single(u => u.Id == member).Assignment!.Stored);
        Accept(solo.Send("retire", unitId: member)); Assert.DoesNotContain(solo.City.Soldiers, u => u.Id == member);
    }

    [Fact]
    public void PaidTownHallTracksAndRecoveryEligibilityRemainIndependent()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall");
        var hall = Assert.Single(solo.City.Army!.Halls);
        var stored = solo.City.Soldiers.Where(u => u.Assignment is { Stored: true }).ToArray();
        Assert.NotEmpty(stored); Assert.Contains(stored, u => u.RecoveryEligible);
        Assert.All(stored, u => Assert.DoesNotContain(u.Id, solo.City.FoodForecast!.Participating));
        int market = Array.FindIndex(solo.City.Slots, s => s.Type == Building.Market);
        Accept(solo.Send("trade", market, resource: Resource.Metal, bundles: 4));
        Accept(solo.Send("trade", market, resource: Resource.Wood, bundles: 2));
        Accept(solo.Send("trade", market, resource: Resource.Food, bundles: 2));
        Accept(solo.Send("upgrade-capacity", hall.Slot));
        Assert.Equal(3, solo.City.Army!.Halls.Single().CapacityLevel);
        Assert.Equal(hall.HealingLevel, solo.City.Army.Halls.Single().HealingLevel);
        var stocks = solo.City.Resources;
        Assert.False(solo.Send("upgrade-healing", hall.Slot).Accepted); // Paid policy already completed this independent track.
        Assert.Equal(stocks, solo.City.Resources);
        Accept(solo.Send("ready")); // Third real production and payment; no manually applied healing.
        Assert.Equal(Phase.Combat, solo.Snapshot.Phase);
        Assert.All(stored, old => Assert.True(solo.City.Soldiers.Single(u => u.Id == old.Id).Health >= old.Health));
        Assert.All(stored, u => Assert.DoesNotContain(u.Id, solo.City.LastUpkeep!.Participating));
    }

    [Fact]
    public void ResearchUsesCanonicalPrerequisitesExclusivePathsAndProduction()
    {
        using var solo = GameplayScenarios.CreateAt("research-full");
        Assert.True(solo.City.Research.Has(TechnologyId.GuardianMastery));
        Assert.False(solo.Send("research-tech", technology: TechnologyId.Assault).Accepted);
        Assert.False(solo.Send("research-tech", technology: TechnologyId.Guardian).Accepted);
        int stone = solo.City.ProductionIncome!.Value.Stone;
        Accept(solo.Send("research-tech", technology: TechnologyId.Stonecraft));
        Assert.Equal(stone + solo.City.Slots.Count(s => s.Type == Building.Stonecutter), solo.City.ProductionIncome!.Value.Stone);
        Assert.False(solo.Send("research-tech", technology: TechnologyId.FireMastery).Accepted);
        Accept(solo.Send("research-tech", technology: TechnologyId.RangedFoundation));
        Accept(solo.Send("research-tech", technology: TechnologyId.Precision));
        Assert.False(solo.Send("research-tech", technology: TechnologyId.Venom).Accepted);
        Accept(solo.Send("research-tech", technology: TechnologyId.ManagedForestry));
    }

    [Theory]
    [InlineData(TechnologyId.MeleeFoundation, TechnologyId.Guardian, TechnologyId.GuardianMastery, TechnologyId.Stonecraft)]
    [InlineData(TechnologyId.MeleeFoundation, TechnologyId.Assault, TechnologyId.AssaultMastery, TechnologyId.None)]
    [InlineData(TechnologyId.RangedFoundation, TechnologyId.Venom, TechnologyId.VenomMastery, TechnologyId.None)]
    [InlineData(TechnologyId.RangedFoundation, TechnologyId.Precision, TechnologyId.PrecisionMastery, TechnologyId.ManagedForestry)]
    [InlineData(TechnologyId.MagicFoundation, TechnologyId.Fire, TechnologyId.FireMastery, TechnologyId.None)]
    [InlineData(TechnologyId.MagicFoundation, TechnologyId.Frost, TechnologyId.FrostMastery, TechnologyId.Irrigation)]
    public void EveryTechnologyCanBePurchasedThroughTheAdapter(TechnologyId foundation, TechnologyId branch, TechnologyId mastery, TechnologyId production)
    {
        using var solo = GameplayScenarios.CreateAt("research-open");
        foreach (var id in new[] { foundation, branch, mastery, production }.Where(id => id != TechnologyId.None))
        {
            int points = solo.City.Research.Points;
            int price = solo.Snapshot.TechnologyCatalog.Single(t => t.Id == id).Cost;
            Accept(solo.Submit(new(GameplayAction.Research, Technology: id)));
            Assert.Equal(points - price, solo.City.Research.Points);
            Assert.True(solo.City.Research.Has(id));
            Assert.False(solo.Submit(new(GameplayAction.Research, Technology: id)).Accepted);
        }
    }

    [Theory]
    [InlineData(Building.RapidArrowTower)]
    [InlineData(Building.BombardTower)]
    public void TowerBranchesExcludeSquadsAndSellingReleasesClaim(Building branch)
    {
        using var solo = GameplayScenarios.CreateAt("late");
        var home = solo.City.Army!.Homes[0];
        foreach (var unit in solo.City.Soldiers.Where(u => u.Assignment?.Tile == home.Cell).ToArray()) Accept(solo.Send("retire", unitId: unit.Id));
        Accept(solo.Send("build", home.PlotId, Building.ArrowTower));
        Accept(solo.Send("upgrade-tower", home.PlotId, branch));
        var tower = solo.City.Army!.Homes.Single(h => h.PlotId == home.PlotId).Plot;
        Assert.Equal(branch, tower.Type); Assert.Equal(2, tower.Level);
        Assert.False(solo.Send("upgrade-tower", home.PlotId, branch).Accepted);
        Assert.False(solo.Send("recruit", 2, squadId: -1).Accepted);
        Accept(solo.Send("sell", home.PlotId));
        Accept(solo.Send("recruit", 2, squadId: -1));
        Assert.Contains(solo.City.Soldiers, u => u.Assignment is { Stored: false } a && a.Tile == home.Cell);
    }

    [Fact]
    public void ForecastAndTwentyWaveTerminalUseActualReceiptsAndNoFinalDraft()
    {
        using var forecast = GameplayScenarios.CreateAt("forecast");
        var food = forecast.City.FoodForecast!;
        int stock = forecast.City.Food;
        Assert.Equal(stock + forecast.City.ProductionIncome!.Value.Food, food.Available);
        Accept(forecast.Send("ready"));
        Assert.Equal(food.Paid, forecast.City.LastUpkeep!.Paid); Assert.Equal(food.Participating, forecast.City.LastUpkeep.Participating);
        Assert.Equal(food.Available - food.Paid, forecast.City.Food);
        using var victory = GameplayScenarios.CreateAt("victory");
        Assert.Equal(Phase.Victory, victory.Snapshot.Phase); Assert.Equal(20, victory.Snapshot.Wave);
        Assert.Equal(20, victory.Snapshot.LastRewardedWave); Assert.Equal(60, victory.Snapshot.ProductionCount);
        Assert.Equal(19, victory.City.Run.DraftWave); Assert.NotNull(victory.City.Run.Choice);
        Assert.False(victory.ChooseReward(victory.City.Run.Offers[0]).Accepted);
        using var defeat = GameplayScenarios.CreateAt("defeat");
        Assert.Equal(Phase.Defeat, defeat.Snapshot.Phase); Assert.Empty(defeat.City.Run.Offers);
    }
    private static void Accept(CommandResult result) => Assert.True(result.Accepted, result.Message);
}
