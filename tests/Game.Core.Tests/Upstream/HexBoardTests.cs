using System.Text.Json;
using Xunit;

namespace Game.Core.Tests;

public sealed class HexBoardTests
{
    [Fact]
    public void DefaultBoardMatchesTheOddRowTerrainLatticeAndKeepsNinePlotsSeparate()
    {
        var board = new HexBoard(HexBoardDefinition.Default());
        Assert.Equal(63, board.Cells.Count); Assert.Equal(1, board.Capacity);
        Assert.All(board.Cells, c =>
        {
            Assert.InRange(c.Coordinate.Column, 0, 6); Assert.InRange(c.Coordinate.R, 0, 8);
            Assert.Equal(c.Coordinate, HexCoordinate.FromOffset(c.Coordinate.Column, c.Coordinate.R));
            Assert.Equal(c.Neighbors.Order(), c.Neighbors);
            foreach (HexCell other in board.Cells) Assert.Equal(c.Coordinate.Distance(other.Coordinate), board.Distance(c.Id, other.Id));
        });
        int Center(int row) => board.Cells.Single(c => c.Coordinate.Column == 3 && c.Coordinate.R == row).Id;
        Assert.Equal(8, board.Distance(Center(0), Center(8)));
        Assert.Equal(Enumerable.Range(0, 7), board.Rear(Faction.Adventurers).Select(id => board.Cell(id).Coordinate.Column));
        Assert.Equal(Enumerable.Range(0, 7), board.Rear(Faction.Skeletons).Select(id => board.Cell(id).Coordinate.Column));
        using var match = new Match(combatSeed: 1);
        Assert.Equal(9, match.Join()!.Slots.Length);
        Assert.All(board.Cells, c => Assert.Single(board.Anchors));
    }
    [Fact]
    public void VirtualAnchorDoesNotShortenCellDistancesAndSiegeNeedsNoProtectedEntry()
    {
        var board = new HexBoard(HexBoardDefinition.Default());
        foreach (int id in board.SiegeCells)
        {
            Assert.Equal(1, board.CityDistance(id)); Assert.True(board.Allows(Faction.Skeletons, id));
            Assert.Equal(DeploymentAffinity.Neutral, board.Cell(id).Affinity);
        }
        HexCell[] siege = board.SiegeCells.Select(board.Cell).OrderBy(c => c.Coordinate.Column).ToArray();
        Assert.Equal(6, board.Distance(siege[0].Id, siege[^1].Id));
        Assert.DoesNotContain(board.Cells, c => c.Id == 0);
        foreach (Faction faction in Enum.GetValues<Faction>())
            foreach (int rear in board.Rear(faction))
            {
                Assert.True(board.Allows(faction, rear)); Assert.True(board.Allows(faction == Faction.Adventurers ? Faction.Skeletons : Faction.Adventurers, rear));
                Assert.Contains(board.Cell(rear).Neighbors, board.Front(faction).Contains);
                Assert.Single(board.Anchors);
            }
    }
    [Fact]
    public void AnchorsAreDistinctPresentationPointsWithoutSizeOrShape()
    {
        var board = new HexBoard(HexBoardDefinition.Default());
        HexAnchor anchor = Assert.Single(board.Anchors);
        Assert.Equal(new HexAnchor(1, 0, 0), anchor); // one logical squad pose; member offsets are cosmetic
    }
    [Fact]
    public void AdjacentTransitionsHaveStableRouteIdsAndOppositeDirectionsShareEdgeTokens()
    {
        var board = new HexBoard(HexBoardDefinition.Default());
        var rebuilt = new HexBoard(board.Definition() with { Cells = board.Definition().Cells.Reverse().ToArray() });
        Assert.Equal(board.Transitions, rebuilt.Transitions);
        Assert.All(board.Transitions, t =>
        {
            Assert.Equal(1, board.Distance(t.Source.Cell, t.Destination.Cell));
            Assert.Contains(board.Anchors, a => a.Id == t.Source.Anchor);
            Assert.Contains(board.Anchors, a => a.Id == t.Destination.Anchor);
            Assert.Equal(t.EdgeToken, board.Transition(t.Destination, t.Source).EdgeToken);
        });
        Assert.Throws<KeyNotFoundException>(() => board.Transition(new(1, 1), new(21, 1)));
        Assert.Throws<KeyNotFoundException>(() => board.Transition(new(1, 1), new(2, 999)));
    }
    [Fact]
    public void AuthoringAndExportedArraysCannotChangeTheFrozenBoard()
    {
        HexBoardDefinition definition = HexBoardDefinition.Default(); var board = new HexBoard(definition);
        string before = JsonSerializer.Serialize(board.Definition());
        definition.Cells[0].Neighbors[0] = 0; definition.Cells[0] = definition.Cells[0] with { Affinity = DeploymentAffinity.Neutral };
        definition.Entries[0].RearCells[0] = 1; definition.SiegeCells[0] = 1;
        HexBoardDefinition exported = board.Definition(); exported.Cells[0].Neighbors[0] = 0; exported.Anchors[0] = exported.Anchors[0] with { AnchorX = 123 };
        Assert.Equal(before, JsonSerializer.Serialize(board.Definition()));
    }
    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("asymmetric")]
    [InlineData("disconnected")]
    [InlineData("not-adjacent")]
    [InlineData("anchor")]
    [InlineData("entry")]
    [InlineData("siege")]
    public void MalformedTopologyAnchorsAndDeploymentAreRejected(string defect)
    {
        HexBoardDefinition d = HexBoardDefinition.Default();
        switch (defect)
        {
            case "duplicate": d.Cells[1] = d.Cells[0]; break;
            case "unknown": d.Cells[0] = d.Cells[0] with { Neighbors = [999] }; break;
            case "asymmetric": d.Cells[0] = d.Cells[0] with { Neighbors = [] }; break;
            case "disconnected": d = d with { Cells = d.Cells.Select(c => c with { Neighbors = [] }).ToArray() }; break;
            case "not-adjacent": d.Cells[0] = d.Cells[0] with { Neighbors = [21] }; d.Cells[20] = d.Cells[20] with { Neighbors = [1] }; break;
            case "anchor": d.Anchors[0] = d.Anchors[0] with { AnchorX = 10001 }; break;
            case "entry": d.Entries[0].RearCells[0] = d.Entries[1].RearCells[0]; break;
            case "siege": d.SiegeCells[0] = d.Entries[0].RearCells[0]; break;
        }
        Assert.Throws<ArgumentException>(() => new HexBoard(d));
    }
}
