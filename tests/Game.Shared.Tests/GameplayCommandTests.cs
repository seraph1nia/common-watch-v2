using System.Text.Json;
using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class GameplayCommandTests
{
    [Fact]
    public void FullIntentCapturesAuthorityExpectationsAndKeepsLocalIdentity()
    {
        using var solo = ScenarioRunner.CreateAt("settlement");
        var intent = new GameplayIntent(GameplayAction.Trade, 1, Building.BombardTower, UnitType.Mage,
            TechnologyId.Frost, Resource.Food, 3, ConstructionPayment.Standard, 42, 23, 37);
        Command command = intent.Capture(solo.Snapshot, 99, solo.PlayerId);
        Assert.Equal(Command.FromSnapshot(solo.Snapshot, 99, "trade", solo.PlayerId, 1, Building.BombardTower,
            UnitType.Mage, TechnologyId.Frost, Resource.Food, 3, ConstructionPayment.Standard, 42, 23, 37), command);
        string payload = JsonSerializer.Serialize(command, WireJson.Options);
        solo.Send("ready");
        Assert.Equal(command, JsonSerializer.Deserialize<Command>(payload, WireJson.Options));
        Assert.NotEqual(solo.Snapshot.TurnSerial, command.TurnSerial);
    }

    [Fact]
    public void SoloForwardsEveryArgumentEvenWhenAuthorityRejectsIt()
    {
        using var solo = ScenarioRunner.CreateAt("settlement");
        Assert.False(solo.Send("trade", 1, resource: Resource.Food, bundles: 7, technology: TechnologyId.Fire,
            unitId: 123, squadId: 45, cell: 9).Accepted);
        Assert.Equal(Resource.Food, solo.LastCommand!.Resource);
        Assert.Equal(7, solo.LastCommand.Bundles);
        Assert.Equal(TechnologyId.Fire, solo.LastCommand.Technology);
        Assert.Equal(123, solo.LastCommand.UnitId);
        Assert.Equal(45, solo.LastCommand.SquadId);
        Assert.Equal(9, solo.LastCommand.Cell);
    }

    [Fact]
    public void ExactRetryDoesNotRepurchaseAndUsesOriginalSequence()
    {
        using var solo = ScenarioRunner.CreateAt("initial");
        Assert.True(solo.Submit(new(GameplayAction.Build, 0, Building.Lumbermill)).Accepted);
        Command original = solo.LastCommand!;
        ResourceCost stock = solo.City.Resources;
        Assert.True(solo.RetryLast()!.Accepted);
        Assert.Equal(original, solo.LastCommand);
        Assert.Equal(stock, solo.City.Resources);
        Assert.True(solo.Send("sell", 0).Accepted);
        Assert.True(solo.Send("build", 0, Building.Farm).Accepted);
        Assert.NotEqual(original.ExpectedGeneration, solo.City.Slots[0].Generation);
    }

    [Fact]
    public void PauseSpeedAndPreparationAreAuthoritative()
    {
        using var solo = ScenarioRunner.CreateAt("settlement");
        Assert.True(solo.Submit(new(GameplayAction.Pause)).Accepted);
        Assert.True(solo.Snapshot.Paused);
        Assert.False(solo.Submit(new(GameplayAction.Ready)).Accepted);
        Assert.True(solo.Send(CombatSpeedRates.Action(CombatSpeed.Two)).Accepted);
        Assert.Equal(CombatSpeed.Two, solo.Snapshot.CombatSpeed);
        Assert.True(solo.Submit(new(GameplayAction.Resume)).Accepted);
        Assert.True(solo.Submit(new(GameplayAction.Unready)).Accepted);
        Assert.True(solo.Submit(new(GameplayAction.Ready)).Accepted);
        Assert.Equal(2, solo.Snapshot.Turn);
    }

    [Fact]
    public void ArmyPurchaseTowerBranchAndSaleUsePersistentPlotExpectations()
    {
        using var solo = ScenarioRunner.CreateAt("initial");
        Assert.True(solo.Send("buy-home", 14).Accepted);
        Assert.True(solo.City.Army!.Homes.Single(h => h.PlotId == 14).Purchased);
        Assert.Equal(2, solo.LastCommand!.ExpectedHomeCount);
        Assert.False(solo.Send("buy-home", 14).Accepted);
        Assert.True(solo.Send("build", 14, Building.ArrowTower).Accepted);
        Assert.False(solo.Send("upgrade-tower", 14, Building.BombardTower).Accepted);
        Assert.Equal(1, solo.LastCommand!.ExpectedTrackLevel);
        Assert.True(solo.Send("sell", 14).Accepted);
        Assert.Equal(ArmyPlotStatus.OwnedEmpty, solo.City.Army.Homes.Single(h => h.PlotId == 14).State);
    }
}
