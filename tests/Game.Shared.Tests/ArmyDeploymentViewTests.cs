using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class ArmyDeploymentViewTests
{
    [Fact]
    public void LegalHomeMoveKeepsTheRosterAndPreviewInTheSameAuthoritativeCell()
    {
        using var solo = ScenarioRunner.CreateAt("settlement");
        Accept(solo.Send("ready")); Accept(solo.Send("recruit", 1));
        var unit = Assert.Single(solo.City.Soldiers);
        long revision = solo.Snapshot.Revision;
        var homes = ArmyDeploymentView.Homes(solo.Snapshot, solo.PlayerId, unit.SquadId);
        Assert.Equal(6, homes.Length); Assert.Equal(revision, solo.Snapshot.Revision);
        Assert.Equal(3, homes.Count(h => h.Row == homes[0].Row));
        Assert.True(homes[0].CanDeploy); Assert.True(homes[1].CanDeploy);
        Assert.All(homes.Skip(2), h => { Assert.False(h.CanDeploy); Assert.Contains("Purchase", h.Restriction); Assert.Equal(new ResourceCost(5), h.Purchase); });
        Assert.False(solo.Send("deploy-squad", squadId: unit.SquadId, cell: homes[2].BattlefieldCell).Accepted);
        var stock = solo.City.Resources;
        Accept(solo.Send("deploy-squad", squadId: unit.SquadId, cell: homes[1].BattlefieldCell));
        Assert.Equal(stock, solo.City.Resources);
        Assert.All(solo.City.Soldiers, u => Assert.Equal(1, u.Assignment!.Tile));
        Assert.All(BattleDeployment.Preview(solo.Snapshot).Where(u => u.Owner == solo.PlayerId && u.Deployed),
            u => Assert.Equal(homes[1].BattlefieldCell, u.Hex!.Position.Cell));
        var army = new ArmyConfiguration(solo.Snapshot.Rules.Army, new HexBoard(solo.Snapshot.Arena!.Board));
        Assert.Contains("Home 2", ArmyDeploymentView.Destination(solo.City, army, unit.Type, unit.SquadId));
        Assert.Contains("2/3", ArmyDeploymentView.Destination(solo.City, army, unit.Type, unit.SquadId));
        Accept(solo.Send("recruit", 1, squadId: unit.SquadId));
        Accept(solo.Send("ready")); Accept(solo.Send("recruit", 1, squadId: unit.SquadId));
        Assert.True(ArmyDeploymentView.Homes(solo.Snapshot, solo.PlayerId, unit.SquadId)[1].Full);
        Assert.Contains("No compatible", ArmyDeploymentView.Destination(solo.City, army, unit.Type, unit.SquadId));
        Assert.False(solo.Send("recruit", 1, squadId: unit.SquadId).Accepted);
        Accept(solo.Send("ready"));
        Assert.Equal(Phase.Combat, solo.Snapshot.Phase);
        Assert.All(solo.City.Soldiers, u => Assert.Equal(homes[1].BattlefieldCell, u.Hex!.Position.Cell));
    }
    [Fact]
    public void TowerAndLockedHomesExplainDifferentRestrictionsWithoutSubmitting()
    {
        using var solo = ScenarioRunner.CreateAt("initial");
        Accept(solo.Send("build", 9, Building.ArrowTower));
        var homes = ArmyDeploymentView.Homes(solo.Snapshot, solo.PlayerId, 0);
        Assert.Contains("Tower occupies", homes[0].Restriction); Assert.Contains("ArrowTower", homes[0].Occupancy);
        Assert.Contains("Select", homes[1].Restriction); Assert.Contains("Purchase", homes[2].Restriction);
        Assert.Equal(ArmyPlotStatus.TowerOccupied, homes[0].Home.State);
        Assert.Equal(ArmyPlotStatus.Locked, homes[2].Home.State);
    }
    [Fact]
    public void FoodAndWoundCountsComeFromTheAcceptedRosterAndCoreEntryProjection()
    {
        using var solo = GameplayScenarios.CreateAt("forecast");
        var homes = ArmyDeploymentView.Homes(solo.Snapshot, solo.PlayerId, solo.City.Soldiers.First().SquadId);
        Assert.Equal(solo.City.FoodForecast!.Participating.Length, homes.Sum(h => h.Participating));
        Assert.Equal(solo.City.Soldiers.Count(u => u.Assignment is { Stored: false } && u.Health < u.Profile.Health), homes.Sum(h => h.Wounded));
        Assert.True(homes.Sum(h => h.Members.Length - h.Participating) > 0);
        var preview = BattleDeployment.Preview(solo.Snapshot).Where(u => u.Owner == solo.PlayerId && u.Deployed).ToArray();
        Assert.Equal(solo.City.FoodForecast.Participating.Order(), preview.Select(u => u.Id).Order());
        foreach (var home in homes)
            Assert.Equal(home.Participating, preview.Count(u => u.Hex!.Position.Cell == home.BattlefieldCell));
    }
    [Fact]
    public void ReturningAStoredMemberUsesTheSameVisibleDestinationAsCore()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall");
        var stored = solo.City.Soldiers.First(u => u.Assignment is { Stored: true });
        var army = new ArmyConfiguration(solo.Snapshot.Rules.Army, new HexBoard(solo.Snapshot.Arena!.Board));
        var destination = army.Find(solo.City.Soldiers.Where(u => u.Id != stored.Id), solo.City.Army!.PurchasedHomes,
            plots: solo.City.Army.Homes.Select(h => h.Plot).ToArray(), type: stored.Type);
        Assert.NotNull(destination);
        Assert.Contains($"Home {destination.Tile + 1}", ArmyDeploymentView.Destination(solo.City, army, stored.Type, 0, stored.Id));
        int health = stored.Health;
        Accept(solo.Send("send", stored.Assignment!.HallSlot, unitId: stored.Id));
        Assert.Equal(destination.Tile, solo.City.Soldiers.Single(u => u.Id == stored.Id).Assignment!.Tile);
        Accept(solo.Send("store", stored.Assignment.HallSlot, unitId: stored.Id));
        Assert.Equal(health, solo.City.Soldiers.Single(u => u.Id == stored.Id).Health);
    }
    private static void Accept(CommandResult result) => Assert.True(result.Accepted, result.Message);
    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void ForeignSectorHomesUseTheirOwnCellsAndQuotes(int players)
    {
        using var host = new AuthoritySession(AuthorityPolicy.PlayingHost);
        for (int peer = 2; peer <= players; peer++) Assert.True(host.Admit(peer, WireJson.ProtocolVersion, "", 0).Accepted);
        Accept(host.ExecuteLocal(Command.FromSnapshot(host.Snapshot(), 1, "start", 1)));
        var state = host.Snapshot();
        var foreign = ArmyDeploymentView.Homes(state, players, 0);
        Assert.Equal(state.Arena!.Sectors.Single(s => s.Owner == players).DeploymentCells, foreign.Select(h => h.BattlefieldCell));
        Assert.Empty(foreign.Select(h => h.BattlefieldCell).Intersect(ArmyDeploymentView.Homes(state, 1, 0).Select(h => h.BattlefieldCell)));
        Assert.Equal(new ResourceCost(5), foreign[2].Purchase);
    }
}
