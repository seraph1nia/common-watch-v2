using Godot;
using Game.Shared;

namespace Game.GodotClient;

/// <summary>Application-scoped optional PNG cache; native resources never depend on editor imports.</summary>
public sealed class AssetLoader : IDisposable
{
    public RenderOptions Options { get; private set; }
    public AssetCatalog Catalog { get; private set; }
    private readonly Dictionary<string, Texture2D?> WorldTextures = [];
    private readonly Dictionary<string, Texture2D?> HudTextures = [];
    public int WorldTextureCount => WorldTextures.Values.Count(t => t is not null);
    public int HudTextureCount => HudTextures.Values.Count(t => t is not null);
    public int DecodeAttempts { get; private set; }
    private bool _mapChecked;
    private SettlementMapLayout? _mapLayout;

    public (Texture2D Texture, SettlementMapLayout Layout)? SettlementMap()
    {
        if (Options.Mode != RenderMode.Sprites) return null;
        if (!_mapChecked)
        {
            _mapChecked = true;
            try
            {
                string path = System.IO.Path.Combine(Options.AssetsRoot, "terrain", "settlement_map.json");
                if (System.IO.File.Exists(path)) _mapLayout = SettlementMapLayout.Read(path);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            { GD.PushWarning($"Optional settlement map: {e.Message}. Using debug grid."); }
        }
        if (_mapLayout is not { } layout || Load("terrain/settlement_map") is not { } texture) return null;
        return texture.GetWidth() == layout.Width && texture.GetHeight() == layout.Height ? (texture, layout) : null;
    }

    public AssetLoader(RenderOptions options)
    {
        Options = options;
        Catalog = new(options.AssetsRoot, options.Mode == RenderMode.Sprites || options.Hud == HudArt.Illustrated);
    }
    public void Configure(RenderOptions options)
    {
        if (options == Options) return;
        if (options.AssetsRoot != Options.AssetsRoot)
            throw new InvalidOperationException("Replace the asset owner to change its root.");
        Options = options;
        Catalog = new(options.AssetsRoot, options.Mode == RenderMode.Sprites || options.Hud == HudArt.Illustrated);
    }
    public Texture2D? Load(string id, bool hud = false)
    {
        if (hud ? Options.Hud != HudArt.Illustrated : Options.Mode != RenderMode.Sprites) return null;
        var cache = hud ? HudTextures : WorldTextures;
        if (cache.TryGetValue(id, out var cached)) return cached;
        Texture2D? texture = null;
        if (Catalog.Resolve(id, true) is { } path)
        {
            try
            {
                if (!hud) DecodeAttempts++;
                using var source = System.IO.File.OpenRead(path);
                Span<byte> signature = stackalloc byte[8];
                if (source.Read(signature) != 8 || !signature.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                    throw new InvalidDataException("Invalid PNG header");
                using var image = new Image();
                if (image.Load(path) == Error.Ok && !image.IsEmpty())
                {
                    var metadata = Catalog.Metadata(id);
                    if (metadata.Size.Length == 2 && (metadata.Size[0] != image.GetWidth() || metadata.Size[1] != image.GetHeight()))
                        throw new InvalidDataException("PNG dimensions differ from catalog");
                    texture = ImageTexture.CreateFromImage(image);
                }
            }
            catch (Exception e) { GD.PushWarning($"Optional art {id}: {e.Message}. Using plain rendering."); }
        }
        cache[id] = texture;
        return texture;
    }


    public void Dispose()
    {
        foreach (var texture in WorldTextures.Values.Concat(HudTextures.Values)) texture?.Dispose();
        WorldTextures.Clear(); HudTextures.Clear();
    }
}
