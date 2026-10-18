using System.Text.Json;

namespace Game.Shared;

public sealed record SliceMargins(int Left, int Top, int Right, int Bottom)
{
    public bool Valid(int width, int height) => Left > 0 && Top > 0 && Right > 0 && Bottom > 0 && Left + Right < width && Top + Bottom < height;
}

public sealed record AssetMetadata(string Id, string Path, int[] Size, float[] Anchor, float DisplaySize,
    float[] Offset, SliceMargins? Slice, float BorderScale)
{
    public static AssetMetadata Default(string id) => new(id, "", [], [.5f, id.StartsWith("terrain/") || id.StartsWith("resource/") ? .5f : .92f],
        id.StartsWith("building/") ? 142 : id.StartsWith("terrain/") ? 116 : id.StartsWith("resource/") ? 28 : 46, [0, 0], null, 1);
}

/// <summary>Local presentation metadata only. Invalid catalogs fail closed to native geometric rendering.</summary>
public sealed class AssetCatalog
{
    readonly string root, spriteRoot;
    readonly Dictionary<string, AssetMetadata> entries = new(StringComparer.Ordinal);
    public IReadOnlyCollection<AssetMetadata> Entries => entries.Values;
    public string? Error { get; private set; }
    public AssetCatalog(string spritesDirectory, bool enabled = true)
    {
        spriteRoot = System.IO.Path.GetFullPath(spritesDirectory);
        root = System.IO.Path.GetDirectoryName(spriteRoot)!;
        if (!enabled) return;
        string file = System.IO.Path.Combine(root, "catalog.json");
        if (!File.Exists(file)) return;
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
            if (document.RootElement.GetProperty("version").GetInt32() != 1) throw new FormatException("Unsupported catalog version");
            foreach (JsonElement e in document.RootElement.GetProperty("assets").EnumerateArray())
            {
                string id = e.GetProperty("id").GetString()!;
                _ = VisualAssets.RelativePath(id); // Stable presentation identifiers only.
                string path = e.GetProperty("path").GetString()!;
                string full = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, path));
                if (System.IO.Path.IsPathRooted(path) || !full.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new FormatException("Asset path escapes the catalog directory");
                string folder = id.StartsWith("hud/") ? "hud/" : "sprites/";
                if (!path.StartsWith(folder, StringComparison.Ordinal) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new FormatException("Invalid asset folder or extension");
                int[] size = e.GetProperty("size").EnumerateArray().Select(v => v.GetInt32()).ToArray();
                float[] anchor = e.TryGetProperty("anchor", out var a) ? a.EnumerateArray().Select(v => v.GetSingle()).ToArray() : [.5f, .5f];
                float[] offset = e.TryGetProperty("offset", out var o) ? o.EnumerateArray().Select(v => v.GetSingle()).ToArray() : [0, 0];
                float display = e.TryGetProperty("display_size", out var d) ? d.GetSingle() : 1;
                float border = e.TryGetProperty("border_scale", out var b) ? b.GetSingle() : 1;
                if (size.Length != 2 || size.Any(v => v <= 0 || v > 8192) || anchor.Length != 2 || anchor.Any(v => !float.IsFinite(v) || v < 0 || v > 1)
                    || offset.Length != 2 || offset.Any(v => !float.IsFinite(v) || Math.Abs(v) > 4096) || !float.IsFinite(display) || display <= 0 || display > 4096
                    || !float.IsFinite(border) || border <= 0 || border > 4) throw new FormatException("Invalid presentation dimensions");
                SliceMargins? slice = null;
                if (e.TryGetProperty("slice", out var s))
                {
                    slice = new(s.GetProperty("left").GetInt32(), s.GetProperty("top").GetInt32(), s.GetProperty("right").GetInt32(), s.GetProperty("bottom").GetInt32());
                    if (!slice.Valid(size[0], size[1])) throw new FormatException("Invalid nine-slice margins");
                }
                if (id.StartsWith("hud/") && slice is null) throw new FormatException("HUD requires slice metadata");
                if (!entries.TryAdd(id, new(id, path, size, anchor, display, offset, slice, border))) throw new FormatException("Duplicate asset ID");
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException or ArgumentException or KeyNotFoundException or OverflowException)
        { entries.Clear(); Error = e.Message; }
    }
    public AssetMetadata Metadata(string id) => entries.GetValueOrDefault(id) ?? AssetMetadata.Default(id);
    public string? Resolve(string id, bool enabled)
    {
        if (!enabled) return null;
        if (entries.TryGetValue(id, out var entry))
        {
            string path = System.IO.Path.Combine(root, entry.Path);
            return File.Exists(path) ? path : null;
        }
        if (id.StartsWith("hud/")) return null; // No valid margins, no decorative skin.
        return VisualAssets.Resolve(spriteRoot, id, RenderMode.Sprites);
    }
}
