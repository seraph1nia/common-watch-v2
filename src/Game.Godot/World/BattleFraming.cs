// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Stable sector geometry and available native canvas space; no actor chasing.</summary>
public static class BattleFraming
{
    public static Rect2 Engagement(MatchSnapshot state, HexProjection projection, int owner)
    {
        var arena = state.Arena!;
        var layout = new ArenaLayout(arena, projection);
        var sector = arena.Sectors.FirstOrDefault(s => s.Owner == owner) ?? arena.Sectors[0];
        var footprints = arena.Footprints.Where(f => f.Owner == sector.Owner).Select(f => f.Cell).ToHashSet();
        var points = arena.Board.Cells.Where(c => footprints.Contains(c.Id)).SelectMany(c => projection.Corners(c.Coordinate))
            .Select(p => new Vector2(p.X - layout.Center.X, p.Y - layout.Center.Y)).ToList();
        var anticipated = state.Players.Where(p => p.Id == sector.Owner).SelectMany(p => p.Soldiers).Where(u => u.Deployed)
            .Concat(state.Enemies.Where(u => u.Destination == sector.Owner))
            .Concat(state.DyingBodies.Where(u => u.Owner == sector.Owner || u.Destination == sector.Owner)).ToArray();
        foreach (var unit in anticipated.Where(u => u.Hex is not null))
        {
            var at = CombatVisuals.Position(unit, state, state.Tick);
            points.Add(new(at.X - layout.Center.X, at.Y * (float)projection.Compression - layout.Center.Y));
        }
        if (anticipated.Length == 0 || anticipated.Any(u => u.Hex is null))
        {
            var ids = sector.DeploymentCells.Concat(sector.EnemyCells).ToHashSet();
            points.AddRange(arena.Board.Cells.Where(c => ids.Contains(c.Id)).SelectMany(c => projection.Corners(c.Coordinate))
                .Select(p => new Vector2(p.X - layout.Center.X, p.Y - layout.Center.Y)));
        }
        var frame = new Rect2(points[0], Vector2.Zero);
        foreach (var point in points.Skip(1)) frame = frame.Expand(point);
        float lateral = 28;
        float heads = anticipated.Any(u => u.IsBoss) ? 84 : 58;
        return new(frame.Position - new Vector2(lateral, heads), frame.Size + new Vector2(lateral * 2, heads + 16));
    }

    public static Rect2 SafeArea(Rect2 viewport, Rect2 content, IEnumerable<Rect2> occluders)
    {
        var outer = viewport.Grow(-12);
        var blocks = occluders.Where(b => b.Intersects(outer)).Select(b => b.Grow(8).Intersection(outer)).ToArray();
        var xs = blocks.SelectMany(b => new[] { b.Position.X, b.End.X }).Append(outer.Position.X).Append(outer.End.X).Distinct().Order().ToArray();
        var ys = blocks.SelectMany(b => new[] { b.Position.Y, b.End.Y }).Append(outer.Position.Y).Append(outer.End.Y).Distinct().Order().ToArray();
        Rect2 best = outer; float score = -1;
        for (int x = 0; x < xs.Length - 1; x++)
        for (int right = x + 1; right < xs.Length; right++)
        for (int y = 0; y < ys.Length - 1; y++)
        for (int bottom = y + 1; bottom < ys.Length; bottom++)
        {
            var candidate = new Rect2(xs[x], ys[y], xs[right] - xs[x], ys[bottom] - ys[y]);
            if (blocks.Any(b => b.Intersects(candidate))) continue;
            float fit = Mathf.Min(candidate.Size.X / content.Size.X, candidate.Size.Y / content.Size.Y);
            if (fit > score + .001f || Mathf.IsEqualApprox(fit, score) && candidate.Area > best.Area)
            { score = fit; best = candidate; }
        }
        return best;
    }
}
