using System.Numerics;
using Game.Core;

namespace Game.Shared;

public static class ArenaAssets
{
    // Explicit allowlist. The pack's manifest is provenance, never the game catalog.
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[] {
        "ground_repeat_01", "ground_repeat_02", "ground_repeat_03",
        "backdrop_repeat_01", "backdrop_repeat_02", "backdrop_repeat_03",
        "foreground_repeat", "arena_left_cap", "arena_right_cap", "keep", "defense_tower",
        "rain_repeat", "embers_repeat", "smoke_repeat", "fire_glow", "crystal_glow"
    });
}

/// <summary>Presentation geometry derived from detached cells, with one global projection origin.</summary>
public sealed class ArenaLayout
{
    public Vector2 Center { get; }
    public Vector2 Size { get; }
    public float Pitch { get; }
    public int Columns { get; }
    public ArenaSector[] Sectors { get; }
    public Vector2 Position => -Size / 2;
    public ArenaLayout(ArenaState arena, HexProjection projection)
    {
        var points = arena.Board.Cells.SelectMany(c => projection.Corners(c.Coordinate)).ToArray();
        var min = new Vector2(points.Min(p => p.X), points.Min(p => p.Y));
        var max = new Vector2(points.Max(p => p.X), points.Max(p => p.Y));
        Center = (min + max) / 2; Size = max - min;
        var cells = arena.Board.Cells.ToDictionary(c => c.Id);
        Sectors = arena.Sectors.OrderBy(s => s.EnemyCells.Min(id => cells[id].Coordinate.Column)).ToArray();
        // Enemy entry cells span the sector's actual columns. No authored 6/7 assumption.
        Columns = Sectors[0].EnemyCells.Max(id => cells[id].Coordinate.Column) - Sectors[0].EnemyCells.Min(id => cells[id].Coordinate.Column) + 1;
        Pitch = (float)(Columns * Math.Sqrt(3) * projection.Radius);
    }
    public Vector2 SectorCenter(int owner)
    {
        int index = Array.FindIndex(Sectors, s => s.Owner == owner);
        return new(Position.X + (Math.Max(0, index) + .5f) * Pitch, 0);
    }
}
