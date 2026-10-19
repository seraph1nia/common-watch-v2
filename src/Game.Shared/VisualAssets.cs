using Game.Core;

namespace Game.Shared;

public enum RenderMode { Debug, Sprites }
public enum ViewPreset { Topdown, Angled }
public enum HudArt { Plain, Illustrated }
public sealed record RenderOptions(RenderMode Mode, string AssetsRoot)
{
    public ViewPreset View { get; init; } = ViewPreset.Topdown;
    public HudArt Hud { get; init; } = HudArt.Plain;
    public bool DebugOverlay { get; init; }
    public bool Showroom { get; init; }
    public float GroundCompression => View == ViewPreset.Angled ? .72f : 1;
    public static RenderOptions Parse(string[] args)
    {
        RenderMode mode = RenderMode.Debug;
        string root = Path.Combine(AppContext.BaseDirectory, "assets", "sprites");
        ViewPreset view = ViewPreset.Topdown;
        HudArt hud = HudArt.Plain;
        bool overlay = false;
        string Value(int index) => index < args.Length ? args[index] : throw new ArgumentException("Missing presentation option value.");
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--render-mode" or "--world-art")
            {
                if (++i >= args.Length) throw new ArgumentException("--render-mode requires debug or sprites.");
                mode = args[i] switch
                {
                    "debug" => RenderMode.Debug,
                    "sprites" => RenderMode.Sprites,
                    _ => throw new ArgumentException("--render-mode requires debug or sprites.")
                };
            }
            else if (args[i] == "--view") view = Value(++i) switch { "topdown" => ViewPreset.Topdown, "angled" => ViewPreset.Angled, _ => throw new ArgumentException("--view requires topdown or angled") };
            else if (args[i] == "--hud-art") hud = Value(++i) switch { "plain" => HudArt.Plain, "illustrated" => HudArt.Illustrated, _ => throw new ArgumentException("--hud-art requires plain or illustrated") };
            else if (args[i] == "--debug-overlay") overlay = Value(++i) switch { "on" => true, "off" => false, _ => throw new ArgumentException("--debug-overlay requires on or off") };
            else if (args[i] == "--assets-root")
            {
                if (++i >= args.Length) throw new ArgumentException("--assets-root requires a directory.");
                root = Path.GetFullPath(args[i]);
            }
        }
        return new(mode, root) { View = view, Hud = hud, DebugOverlay = overlay, Showroom = args.Contains("--asset-showroom") };
    }
}

public static class VisualAssets
{
    private static readonly HashSet<string> Known = Enum.GetValues<Building>().Select(BuildingId)
        .Concat(Enum.GetValues<UnitType>().Select(t => "unit/" + t.ToString().ToLowerInvariant()))
        .Concat(Enum.GetValues<UnitType>().Select(t => "unit/enemy_" + t.ToString().ToLowerInvariant()))
        .Concat(new[] { "terrain/hex", "terrain/woodland_hex", "terrain/settlement_map", "resource/gold", "resource/wood", "resource/stone", "hud/panel", "hud/card_frame", "hud/button", "hud/tooltip" }).ToHashSet(StringComparer.Ordinal);
    public static string BuildingId(Building type) => "building/" + type.ToString().ToLowerInvariant();
    public static string UnitId(UnitState unit) => "unit/" + (unit.Faction == Faction.Skeletons ? "enemy_" : "") + unit.Type.ToString().ToLowerInvariant();
    public static string RelativePath(string identifier)
    {
        if (!Known.Contains(identifier)) throw new ArgumentException("Unknown logical visual identifier.", nameof(identifier));
        string[] parts = identifier.Split('/');
        string folder = parts[0] switch { "building" => "buildings", "unit" => "units", "terrain" => "terrain", "resource" => "resources", "hud" => "hud", _ => throw new ArgumentException(nameof(identifier)) };
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
