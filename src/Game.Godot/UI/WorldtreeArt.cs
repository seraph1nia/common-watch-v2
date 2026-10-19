using System.Text.Json;
using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Image-space presentation metadata; no prices, prerequisites or eligibility.</summary>
public sealed record WorldtreeSocket(TechnologyId Id, Vector2 Center, float Diameter, string Icon);
public sealed record WorldtreeArt(Texture2D Background, Texture2D Details, IReadOnlyDictionary<string, Texture2D> Rings,
    Vector2 ImageSize, Rect2 DetailsRect, WorldtreeSocket[] Sockets)
{
    public static WorldtreeArt? Load(AssetLoader assets)
    {
        if (assets.Options.Hud != Game.Shared.HudArt.Illustrated) return null;
        try
        {
            string directory = System.IO.Path.Combine(assets.Options.AssetsRoot, "..", "hud", "worldtree");
            using var json = JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(directory, "layout.json")));
            var root = json.RootElement;
            Vector2 size = new(root.GetProperty("width").GetSingle(), root.GetProperty("height").GetSingle());
            float[] panel = root.GetProperty("details").EnumerateArray().Select(v => v.GetSingle()).ToArray();
            var sockets = root.GetProperty("nodes").EnumerateArray().Select(n => new WorldtreeSocket(
                Enum.Parse<TechnologyId>(n.GetProperty("id").GetString()!),
                new(n.GetProperty("x").GetSingle(), n.GetProperty("y").GetSingle()),
                n.GetProperty("diameter").GetSingle(), n.GetProperty("icon").GetString()!)).ToArray();
            if (size != new Vector2(1672, 941) || panel.Length != 4 || sockets.Length != 18
                || sockets.Select(s => s.Id).Distinct().Count() != 18
                || sockets.Any(s => s.Id == TechnologyId.None || !Enum.IsDefined(s.Id) || !s.Center.IsFinite()
                    || !float.IsFinite(s.Diameter) || s.Diameter is < 80 or > 180
                    || !new Rect2(Vector2.Zero, size).Encloses(new(s.Center - Vector2.One * s.Diameter / 2, Vector2.One * s.Diameter))))
                throw new InvalidDataException("Invalid Worldtree socket layout.");
            var detailsRect = new Rect2(panel[0], panel[1], panel[2], panel[3]);
            if (!detailsRect.Position.IsFinite() || !detailsRect.Size.IsFinite() || detailsRect.Size.X <= 0 || detailsRect.Size.Y <= 0
                || !new Rect2(Vector2.Zero, size).Encloses(detailsRect)
                || sockets.Any(s => detailsRect.Intersects(new(s.Center - Vector2.One * s.Diameter / 2, Vector2.One * s.Diameter))))
                throw new InvalidDataException("Worldtree details overlap sockets.");
            Texture2D? Texture(string name) => assets.LoadWorldtreePng(name);
            if (Texture("background") is not { } background || background.GetSize() != size
                || Texture("details_panel") is not { } frame) return null;
            var rings = new Dictionary<string, Texture2D>();
            foreach (string state in new[] { "idle", "hover", "selected", "owned", "locked" })
            {
                if (Texture("node_" + state) is not { } texture) return null;
                rings.Add(state, texture);
            }
            return new(background, frame, rings, size, detailsRect, sockets);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or ArgumentException
            or InvalidOperationException or KeyNotFoundException or OverflowException or FormatException)
        { GD.PushWarning("Optional Worldtree art: " + e.Message + ". Using plain research panel."); return null; }
    }
}
