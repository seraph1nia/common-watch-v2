using Godot;
using Game.Core;

namespace Game.GodotClient;

/// <summary>Native drawing with catalog ground anchors and optional geometric fallback.</summary>
public sealed class WorldArt(AssetLoader _assets)
{
    public void Structure(Node2D canvas, StructureState state, Vector2 ground, Color tint)
    {
        string id = state.Kind switch
        {
            ArenaStructureKind.Keep => "arena/keep",
            ArenaStructureKind.ArrowTower => "arena/defense_tower",
            ArenaStructureKind.CatapultTower => "building/catapulttower",
            _ => ""
        };
        float size = state.Kind == ArenaStructureKind.Keep ? 111 : 96;
        if (id.Length > 0 && Sprite(canvas, id, ground, Vector2.One * size, tint)) return;
        canvas.DrawRect(new Rect2(ground + new Vector2(-12, -24), new Vector2(24, 29)), new Color("a8a084") * tint);
        canvas.DrawColoredPolygon([ground + new Vector2(-16, -24), ground + new Vector2(0, -36), ground + new Vector2(16, -24)], new Color("586b78") * tint);
    }
    public static Rect2 VisibleArea(Node2D canvas)
    {
        var inverse = canvas.GetGlobalTransformWithCanvas().AffineInverse();
        var viewport = canvas.GetViewportRect();
        return new Rect2(inverse * viewport.Position, viewport.Size * inverse.Scale);
    }
    public bool Texture(Node2D canvas, string id, Rect2 target, Color? tint = null, Rect2? clip = null)
    {
        var destination = clip is { } bounds ? target.Intersection(bounds) : target;
        // Cull before decoding. Every visible sector shares the application texture cache.
        if (!destination.HasArea() || !VisibleArea(canvas).Intersects(destination)) return true;
        if (_assets.Load(id) is not { } texture) return false;
        var source = new Rect2((destination.Position - target.Position) / target.Size * texture.GetSize(), destination.Size / target.Size * texture.GetSize());
        canvas.DrawTextureRectRegion(texture, destination, source, tint ?? Colors.White);
        return true;
    }
    public static void Hex(Node2D canvas, Vector2[] corners, Color fill, Color border, float width)
    {
        canvas.DrawColoredPolygon(corners, fill);
        canvas.DrawPolyline(corners.Append(corners[0]).ToArray(), border, width, true);
    }
    public static void Text(Node2D canvas, string text, Vector2 position, int size, Color? color = null)
    {
        float width = ThemeDB.FallbackFont.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        canvas.DrawString(ThemeDB.FallbackFont, position - new Vector2(width / 2, 0), text, HorizontalAlignment.Left, -1, size, color ?? new Color("e7edf2"));
    }
    public bool Sprite(Node2D canvas, string id, Vector2 anchor, Vector2 size, Color? tint = null)
    {
        if (_assets.Load(id) is not { } texture) { Overlay(canvas, anchor, new Rect2(anchor - new Vector2(24, 32), new Vector2(48, 40))); return false; }
        float factor = Math.Min(size.X / texture.GetWidth(), size.Y / texture.GetHeight());
        size = new Vector2(texture.GetWidth(), texture.GetHeight()) * factor;
        var meta = _assets.Catalog.Metadata(id);
        var rect = new Rect2(anchor - new Vector2(meta.Anchor[0], meta.Anchor[1]) * size + new Vector2(meta.Offset[0], meta.Offset[1]), size);
        canvas.DrawTextureRect(texture, rect, false, tint ?? Colors.White);
        Overlay(canvas, anchor, rect); return true;
    }

    public bool Ground(Node2D canvas, string id, Vector2[] corners, Color? tint = null)
    {
        if (_assets.Load(id) is not { } texture) return false;
        // Only the authored top surface maps to the ground. Do not squash an entire terrain
        // cutout (which can include rocks, side faces or transparent padding) as a rectangle.
        var uv = _assets.Catalog.Metadata(id).GroundUv;
        if (uv is null) return false; // Unqualified terrain keeps the geometric hex fallback.
        canvas.DrawPolygon(corners, new Color[] { tint ?? Colors.White }, uv.Select(p => new Vector2(p.X, p.Y)).ToArray(), texture);
        return true;
    }

    private void Overlay(Node2D canvas, Vector2 ground, Rect2 bounds)
    {
        if (!_assets.Options.DebugOverlay) return;
        canvas.DrawRect(bounds, new Color("a9caed"), false, 1);
        canvas.DrawLine(ground - new Vector2(5, 0), ground + new Vector2(5, 0), new Color("ff82ce"), 1);
        canvas.DrawLine(ground - new Vector2(0, 5), ground + new Vector2(0, 5), new Color("ff82ce"), 1);
    }

}
