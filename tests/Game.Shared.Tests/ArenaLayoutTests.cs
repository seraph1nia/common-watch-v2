using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class ArenaLayoutTests
{
    [Theory]
    [InlineData(6)] [InlineData(7)]
    public void RepeatPitchDoesNotAccumulateRowStagger(int columns)
    {
        var projection = new HexProjection(30, .72);
        for (int row = 0; row < 9; row++)
        {
            var left = projection.Project(HexCoordinate.FromOffset(0, row));
            var next = projection.Project(HexCoordinate.FromOffset(columns, row));
            Assert.InRange(Math.Abs(next.X - left.X - columns * Math.Sqrt(3) * 30), 0, .001);
            Assert.Equal(left.Y, next.Y);
        }
    }
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void SectorPitchRetainsEveryCellAndGlobalParity(int players)
    {
        var arena = new SharedArena(Enumerable.Range(1, players)).Snapshot();
        foreach (double compression in new[] { 1, .72 })
        {
            var projection = new HexProjection(30, compression);
            var layout = new ArenaLayout(arena, projection);
            Assert.Equal(arena.Sectors[0].EnemyCells.Length, layout.Columns);
            Assert.InRange(Math.Abs(layout.Pitch - layout.Columns * Math.Sqrt(3) * 30), 0, .001);
            Assert.InRange(Math.Abs(layout.Size.X - (layout.Pitch * players + (float)(Math.Sqrt(3) * 15))), 0, .001);
            foreach (var cell in arena.Board.Cells)
            {
                var world = projection.Project(cell.Coordinate);
                Assert.Equal(cell.Coordinate, projection.HitTest(world, arena.Board.Cells.Select(c => c.Coordinate)));
                Assert.InRange(world.X - layout.Center.X, -layout.Size.X / 2, layout.Size.X / 2);
                Assert.InRange(world.Y - layout.Center.Y, -layout.Size.Y / 2, layout.Size.Y / 2);
            }
            foreach (var sector in layout.Sectors.Skip(1))
                Assert.InRange(layout.SectorCenter(sector.Owner).X - layout.SectorCenter(sector.Owner - 1).X, layout.Pitch - .001f, layout.Pitch + .001f);
        }
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public void ArenaCaptureIsEarnedByLegalCommands(int players)
    {
        var battle = new BattleShowcase("arena", players);
        Assert.Equal(players, battle.Snapshot.Arena!.Sectors.Length);
        Assert.Equal(players * SharedArena.SectorWidth * SharedArena.Depth, battle.Snapshot.Arena.Board.Cells.Length);
        Assert.Equal(Phase.Combat, battle.Snapshot.Phase);
        Assert.Contains(battle.Frames.SelectMany(f => f.CombatEvents), e => e.Type == CombatEventType.StructureHit);
        Assert.Contains(battle.Frames.SelectMany(f => f.CombatEvents), e => e.Type == CombatEventType.StructureDestroyed);
    }
}
