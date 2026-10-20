using Game.Core;
using Xunit;

namespace Game.Core.Tests;

public sealed class SixColumnArenaTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void CompleteSectorsRetainSixHomesAndNonoverlappingStructures(int players)
    {
        var arena = new SharedArena(Enumerable.Range(1, players));
        var state = arena.Snapshot();
        Assert.Equal(6, SharedArena.SectorWidth);
        Assert.Equal(players * 6 * 9, state.Board.Cells.Length);
        Assert.Equal(2, state.Board.Version);
        Assert.Equal(players * 11, state.Footprints.Length);
        Assert.Equal(state.Footprints.Length, state.Footprints.Select(f => f.Cell).Distinct().Count());
        foreach (var sector in state.Sectors)
        {
            Assert.Equal(6, sector.DeploymentCells.Length);
            Assert.Equal(6, sector.EnemyCells.Length);
            Assert.All(sector.DeploymentCells, cell => Assert.True(arena.CanOccupy(cell)));
            Assert.All(sector.DeploymentCells.Concat(sector.EnemyCells).Concat(state.Footprints.Where(f => f.Owner == sector.Owner).Select(f => f.Cell)), cell =>
                Assert.InRange(arena.Board.Cell(cell).Coordinate.Column, (sector.Owner - 1) * 6, sector.Owner * 6 - 1));
            // Each home and enemy entry can reach the shared floor despite narrower static rails.
            var reached = new HashSet<int>(); var queue = new Queue<int>(); queue.Enqueue(sector.EnemyCells[0]);
            while (queue.TryDequeue(out int cell))
            {
                if (!reached.Add(cell)) continue;
                foreach (int neighbor in arena.Board.Cell(cell).Neighbors.Where(arena.CanOccupy)) queue.Enqueue(neighbor);
            }
            Assert.All(sector.DeploymentCells.Concat(sector.EnemyCells), cell => Assert.Contains(cell, reached));
            for (int home = 0; home < 6; home++) Assert.Equal(home, arena.DeploymentHome(sector.Owner, arena.Deployment(sector.Owner, home)));
        }
        var armed = new SharedArena(Enumerable.Range(1, players), Enumerable.Range(1, players).SelectMany(owner => Enumerable.Range(9, 6).Select(slot => (owner, slot))));
        Assert.Equal(players * 17, armed.Footprints.Count);
        Assert.Equal(armed.Footprints.Count, armed.Footprints.Select(f => f.Cell).Distinct().Count());
    }
}
