using Game.Core;

namespace Game.Shared;

public enum RenderMode { Debug, Sprites }
public sealed record RenderOptions(RenderMode Mode, string AssetsRoot)
{
    public static RenderOptions Parse(string[] args)
    {
        RenderMode mode = RenderMode.Debug;
        string root = Path.Combine(AppContext.BaseDirectory, "assets", "sprites");
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--render-mode")
            {
                if (++i >= args.Length) throw new ArgumentException("--render-mode requires debug or sprites.");
                mode = args[i] switch { "debug" => RenderMode.Debug, "sprites" => RenderMode.Sprites,
                    _ => throw new ArgumentException("--render-mode requires debug or sprites.") };
            }
            else if (args[i] == "--assets-root")
            {
                if (++i >= args.Length) throw new ArgumentException("--assets-root requires a directory.");
                root = Path.GetFullPath(args[i]);
            }
        }
        return new(mode, root);
    }
}

public static class VisualAssets
{
    private static readonly HashSet<string> Known = Enum.GetValues<Building>().Select(BuildingId)
        .Concat(Enum.GetValues<UnitType>().Select(t => "unit/" + t.ToString().ToLowerInvariant()))
        .Concat(Enum.GetValues<UnitType>().Select(t => "unit/enemy_" + t.ToString().ToLowerInvariant()))
        .Append("terrain/hex").ToHashSet(StringComparer.Ordinal);
    public static string BuildingId(Building type) => "building/" + type.ToString().ToLowerInvariant();
    public static string UnitId(UnitState unit) => "unit/" + (unit.Faction == Faction.Skeletons ? "enemy_" : "") + unit.Type.ToString().ToLowerInvariant();
    public static string RelativePath(string identifier)
    {
        if (!Known.Contains(identifier)) throw new ArgumentException("Unknown logical visual identifier.", nameof(identifier));
        string[] parts = identifier.Split('/');
        string folder = parts[0] switch { "building" => "buildings", "unit" => "units", "terrain" => "terrain", _ => throw new ArgumentException(nameof(identifier)) };
        return Path.Combine(folder, parts[1] + ".png");
    }
    /// <summary>Only proposes a file. Framework decoders must fall back if it is invalid.</summary>
    public static string? Resolve(string root, string identifier, RenderMode mode, Func<string, bool>? fileExists = null)
    {
        if (mode == RenderMode.Debug) return null; // Never inspect sprites in permanent no-art mode.
        string path = Path.Combine(root, RelativePath(identifier));
        return (fileExists ?? File.Exists)(path) ? path : null;
    }
}
