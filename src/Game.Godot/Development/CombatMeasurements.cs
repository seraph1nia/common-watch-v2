// SPDX-License-Identifier: GPL-3.0-only
using Godot;

namespace Game.GodotClient;

// CLI-only observations of actual retained figures and text, never authority.
public partial class CombatOverlay
{
    internal (string Text, Rect2 Bounds)[] ScreenTextBounds => labels.Select(label =>
    {
        var size = ThemeDB.FallbackFont.GetStringSize(label.Text, fontSize: label.Size);
        var rectangle = new Rect2(label.Position - new Vector2(size.X / 2, label.Size), new(size.X, label.Size));
        return (label.Text, CombatMeasurements.ScreenBounds(this, rectangle));
    }).ToArray();
}

internal static class CombatMeasurements
{
    internal static float[] Box(Rect2 rectangle) => [rectangle.Position.X, rectangle.Position.Y, rectangle.Size.X, rectangle.Size.Y];
    internal static Rect2 ScreenBounds(Node2D node, Rect2 bounds)
    {
        var transform = node.GetGlobalTransformWithCanvas();
        Vector2[] corners = [bounds.Position, new(bounds.End.X, bounds.Position.Y), bounds.End, new(bounds.Position.X, bounds.End.Y)];
        var result = new Rect2(transform * corners[0], Vector2.Zero);
        foreach (var corner in corners.Skip(1)) result = result.Expand(transform * corner);
        return result;
    }
}

public partial class BattlefieldView
{
    internal object MeasureCombat()
    {
        var viewport = GetViewportRect();
        var actors = _figures.Where(p => p.Value.IsVisibleInTree())
            .Select(p => (Id: p.Key, Bounds: CombatMeasurements.ScreenBounds(p.Value, p.Value.Bounds)))
            .Where(p => p.Bounds.Intersects(viewport)).ToArray();
        var text = _combatOverlay?.ScreenTextBounds ?? [];
        var occupied = actors.Length > 0 ? actors.Select(a => a.Bounds).Aggregate((a, b) => a.Merge(b)) : new Rect2();
        return new
        {
            presentedTick = AnimationTick, zoom = GetGlobalTransformWithCanvas().Scale.X,
            renderBounds = CombatMeasurements.Box(CombatMeasurements.ScreenBounds(this, RenderBounds)),
            occupiedBounds = CombatMeasurements.Box(occupied),
            actorCount = actors.Length, aliveCount = Playback.Units().Count(u => u.Health > 0),
            medianActorHeight = actors.Length == 0 ? 0 : actors.Select(a => a.Bounds.Size.Y).Order().ElementAt(actors.Length / 2),
            actors = actors.Select(a => new { id = a.Id, bounds = CombatMeasurements.Box(a.Bounds) }).ToArray(),
            labels = text.Select(t => new { text = t.Text, bounds = CombatMeasurements.Box(t.Bounds) }).ToArray(),
            labelActorOverlaps = text.Count(t => actors.Any(a => a.Bounds.Intersects(t.Bounds))),
            labelLabelOverlaps = text.SelectMany((t, i) => text.Skip(i + 1).Where(other => t.Bounds.Intersects(other.Bounds))).Count(),
            selected = SelectedUnit, highlightedTarget = HighlightedTarget, squadCues = SquadCueCount
        };
    }
}
