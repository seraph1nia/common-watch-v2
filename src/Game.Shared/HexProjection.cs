using System.Numerics;
using Game.Core;

namespace Game.Shared;

/// <summary>Pointy-top axial hexes in world space; camera transforms belong to each client.</summary>
public sealed class HexProjection
{
    public double Radius { get; }
    public HexProjection(double radius)
    {
        if (!double.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Radius = radius;
    }
    public Vector2 Project(HexCoordinate coordinate) => new(
        (float)(Radius * Math.Sqrt(3) * (coordinate.Q + coordinate.R / 2.0)), (float)(Radius * 1.5 * coordinate.R));
    public HexCoordinate Unproject(Vector2 point)
    {
        double q = (Math.Sqrt(3) / 3 * point.X - point.Y / 3.0) / Radius;
        double r = point.Y * 2 / 3.0 / Radius;
        double x = Math.Round(q), z = Math.Round(r), y = Math.Round(-q - r);
        double dx = Math.Abs(x - q), dy = Math.Abs(y + q + r), dz = Math.Abs(z - r);
        if (dx > dy && dx > dz) x = -y - z;
        else if (dz > dy) z = -x - y;
        return new(checked((int)x), checked((int)z));
    }
    public Vector2[] Corners(HexCoordinate coordinate)
    {
        Vector2 center = Project(coordinate);
        return Enumerable.Range(0, 6).Select(i => center + new Vector2(
            (float)(Radius * Math.Cos((60 * i - 30) * Math.PI / 180)),
            (float)(Radius * Math.Sin((60 * i - 30) * Math.PI / 180)))).ToArray();
    }
    public HexCoordinate? HitTest(Vector2 point, IEnumerable<HexCoordinate> cells)
    {
        HexCoordinate cell = Unproject(point);
        return cells.Contains(cell) ? cell : null;
    }
}

public static class SettlementLayout
{
    // Same slot-to-offset arrangement as upstream VillageLayout.Slot: 3 columns, rows 2..4.
    public static IReadOnlyList<HexCoordinate> Coordinates { get; } = Array.AsReadOnly(
        Enumerable.Range(0, 9).Select(i => HexCoordinate.FromOffset(i % 3 - 1, i / 3 + 2)).ToArray());
}

public static class CombatVisuals
{
    public static Vector2 Position(UnitState unit, MatchSnapshot snapshot, double tick, double radius = 30)
    {
        if (unit.Hex is not { } hex || snapshot.Arena is not { } arena) return Vector2.Zero;
        var projection = new HexProjection(radius);
        Vector2 At(int cell) => arena.Board.Cells.FirstOrDefault(c => c.Id == cell) is { } found
            ? projection.Project(found.Coordinate) : Vector2.Zero;
        Vector2 position = At(hex.Position.Cell);
        if (hex.HoldsTransit)
        {
            // Core keeps the transit pose on dying actors; freeze it at death.
            double elapsed = hex.Lifecycle == UnitLifecycle.Dying
                ? hex.FrozenMoveTicks : (hex.FrozenTick ?? tick) - hex.StartTick;
            float amount = (float)Math.Clamp(elapsed / Math.Max(1, hex.EndTick - hex.StartTick), 0, 1);
            position = Vector2.Lerp(position, At(hex.Destination.Cell), amount);
        }
        // Two columns keep individual circle/health markers readable in a squad.
        // These cosmetic offsets never change the authoritative cell or claims.
        if (unit.IsBoss) return position;
        float spacing = (float)radius * .8f;
        return position + new Vector2((unit.FormationSlot % 2 - .5f) * spacing,
            (unit.FormationSlot / 2 - .5f) * spacing);
    }
}
