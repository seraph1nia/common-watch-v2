using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Shared visual bounds and board furniture for playback and the read-only deployment view.</summary>
public static class ArenaPresentation
{
    public static Rect2 GridBounds(ArenaState arena, HexProjection projection)
    {
        var points = arena.Board.Cells.SelectMany(c => projection.Corners(c.Coordinate)).ToArray();
        var min = new Vector2(points.Min(p => p.X), points.Min(p => p.Y));
        return new(min, new Vector2(points.Max(p => p.X), points.Max(p => p.Y)) - min);
    }
    // Allows for upright sprites, health indicators, missiles and the ornamental rail.
    public static Rect2 RenderBounds(ArenaState arena, HexProjection projection, bool illustrated = false)
    {
        if (illustrated)
        {
            var layout = new ArenaLayout(arena, projection);
            return new Rect2(new Vector2(layout.Position.X - layout.Pitch * .34375f, layout.Position.Y - layout.Pitch * .30f),
                new Vector2(layout.Size.X + layout.Pitch * .6875f, layout.Size.Y + layout.Pitch * .6125f));
        }
        var bounds = GridBounds(arena, projection);
        return new Rect2(-bounds.Size / 2, bounds.Size).Grow(50);
    }
    public static bool Draw(Node2D canvas, AssetLoader assets, ArenaState arena, HexProjection projection)
    {
        if (assets.Options.Mode == RenderMode.Sprites && DrawModular(canvas, assets, arena, projection)) return true;
        Rect2 grid = GridBounds(arena, projection);
        grid.Position = -grid.Size / 2;
        Rect2 rail = grid.Grow(24);
        // Atmospheric surround uses the approved map, with proportional source cropping.
        Rect2 viewport = canvas.GetViewport().GetVisibleRect();
        var inverse = canvas.GetViewport().CanvasTransform.AffineInverse();
        Rect2 atmosphere = new(inverse * viewport.Position, viewport.Size / canvas.GetViewport().CanvasTransform.Scale);
        canvas.DrawRect(atmosphere, new Color("1c3029"));
        if (assets.SettlementMap() is { } map)
        {
            DrawCover(canvas, map.Texture, atmosphere, new Color(.48f, .55f, .43f));
            canvas.DrawRect(atmosphere, new Color(.05f, .10f, .08f, .38f));
        }
        using var shadow = new StyleBoxFlat { BgColor = new Color(0, 0, 0, .5f), CornerRadiusTopLeft = 20, CornerRadiusTopRight = 20,
            CornerRadiusBottomLeft = 20, CornerRadiusBottomRight = 20 };
        canvas.DrawStyleBox(shadow, new Rect2(rail.Position + new Vector2(6, 12), rail.Size).Grow(8));
        if (assets.Load("hud/card_frame", true) is { } texture)
        {
            var metadata = assets.Catalog.Metadata("hud/card_frame"); var slice = metadata.Slice!;
            using var frame = new StyleBoxTexture { Texture = texture, TextureMarginLeft = slice.Left, TextureMarginRight = slice.Right, TextureMarginTop = slice.Top, TextureMarginBottom = slice.Bottom };
            float scale = metadata.BorderScale;
            canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One * scale);
            canvas.DrawStyleBox(frame, new Rect2(rail.Position / scale, rail.Size / scale));
            canvas.DrawSetTransform(Vector2.Zero);
        }
        else
        {
            canvas.DrawRect(rail, new Color("423c2a"));
            canvas.DrawRect(rail.Grow(-4), new Color("92815a"), false, 3, true);
            canvas.DrawRect(rail.Grow(-10), new Color("253f32"));
        }
        canvas.DrawRect(grid.Grow(6), new Color("394a32"));
        canvas.DrawRect(grid.Grow(8), new Color("b4a06b"), false, 2, true);
        // Carved corner leaves stay outside the playable hexes and carry no input Controls.
        foreach (Vector2 corner in new[] { rail.Position, new Vector2(rail.End.X, rail.Position.Y), rail.End, new Vector2(rail.Position.X, rail.End.Y) })
        {
            canvas.DrawCircle(corner, 9, new Color("68794b"), true, -1, true);
            canvas.DrawArc(corner, 13, 0, Mathf.Tau, 24, new Color("c1ae72"), 2, true);
            canvas.DrawLine(corner - new Vector2(16, 0), corner + new Vector2(16, 0), new Color("b4a06b"), 2, true);
        }
        return false;
    }
    static bool DrawModular(Node2D canvas, AssetLoader assets, ArenaState arena, HexProjection projection)
    {
        // Probe one ground tile: absent/corrupt packs retain the complete geometric renderer.
        if (assets.Load("arena/ground_repeat_01") is null) return false;
        var layout = new ArenaLayout(arena, projection);
        var art = new WorldArt(assets);
        Vector2 start = new(layout.Position.X, layout.Position.Y);
        float pitch = layout.Pitch, height = layout.Size.Y;
        int count = layout.Sectors.Length;
        int[] variants = [1, 2, 3, 2, 1, 3];
        float capWidth = pitch * 352 / 1024;
        var paintedBounds = new Rect2(start.X - capWidth, start.Y - pitch * .30f, layout.Size.X + capWidth * 2, height + pitch * .6125f);
        canvas.DrawRect(WorldArt.VisibleArea(canvas), new Color("171b23"));
        // Continue a full tile beneath BOTH transparent caps and the final odd-row half-cell.
        // The repeat pitch excludes the half-column stagger in the whole board's bounds.
        for (int i = -1; i <= count; i++)
        {
            int variant = variants[(i + variants.Length) % variants.Length];
            float x = start.X + i * pitch;
            if (!WorldArt.VisibleArea(canvas).Intersects(new Rect2(x, paintedBounds.Position.Y, pitch, paintedBounds.Size.Y))) continue;
            var ground = new Rect2(x, start.Y - pitch * .08f, pitch, height + pitch * .20f);
            canvas.DrawRect(ground.Intersection(paintedBounds), new Color("343238"));
            art.Texture(canvas, $"arena/ground_repeat_{variant:00}", ground, clip: paintedBounds);
            // Upright bands use a uniform scale in both projections.
            art.Texture(canvas, $"arena/backdrop_repeat_{variant:00}", new Rect2(x, start.Y - pitch * .30f, pitch, pitch * .375f), clip: paintedBounds);
            art.Texture(canvas, "arena/foreground_repeat", new Rect2(x, start.Y + height, pitch, pitch * .3125f), clip: paintedBounds);
            art.Texture(canvas, "arena/rain_repeat", new Rect2(x, start.Y, pitch, height), new Color(1, 1, 1, .16f), paintedBounds);
        }
        // Caps are strictly outside the outermost playable corners, never at sector joins.
        art.Texture(canvas, "arena/arena_left_cap", new Rect2(start.X - capWidth, start.Y - pitch * .12f, capWidth, pitch * 1.25f));
        art.Texture(canvas, "arena/arena_right_cap", new Rect2(-start.X, start.Y - pitch * .12f, capWidth, pitch * 1.25f));
        return true;
    }
    private static void DrawCover(Node2D canvas, Texture2D texture, Rect2 target, Color tint)
    {
        Vector2 source = texture.GetSize();
        float scale = Mathf.Max(target.Size.X / source.X, target.Size.Y / source.Y);
        Vector2 crop = target.Size / scale;
        canvas.DrawTextureRectRegion(texture, target, new Rect2((source - crop) / 2, crop), tint);
    }
}
