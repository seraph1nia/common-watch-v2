using Game.Shared;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGameClient;

internal sealed class OptionalSprites : IDisposable
{
    readonly GraphicsDevice graphics;
    RenderOptions options;
    public OptionalSprites(GraphicsDevice graphics, RenderOptions options) { this.graphics = graphics; this.options = options; Catalog = new(options.AssetsRoot, options.Mode == RenderMode.Sprites || options.Hud == HudArt.Illustrated); }
    public void Configure(RenderOptions next) { options = next; Catalog = new(options.AssetsRoot, options.Mode == RenderMode.Sprites || options.Hud == HudArt.Illustrated); }
    readonly Dictionary<string, Texture2D?> cache = [];
    public AssetCatalog Catalog { get; private set; }
    public int LoadAttempts { get; private set; }
    public Texture2D? Get(string id)
    {
        if (id.StartsWith("hud/") ? options.Hud != HudArt.Illustrated : options.Mode == RenderMode.Debug) return null;
        if (cache.TryGetValue(id, out Texture2D? existing)) return existing;
        Texture2D? texture = null;
        string? path = Catalog.Resolve(id, true);
        if (path is not null)
        {
            if (!id.StartsWith("hud/")) LoadAttempts++;
            try
            {
                using var input = File.OpenRead(path);
                texture = Texture2D.FromStream(graphics, input);
                var metadata = Catalog.Metadata(id);
                if (metadata.Size.Length == 2 && (metadata.Size[0] != texture.Width || metadata.Size[1] != texture.Height))
                    throw new InvalidOperationException("PNG dimensions differ from catalog");
                // Native SpriteBatch and Gum use premultiplied alpha; PNG stores straight alpha.
                var pixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
                texture.GetData(pixels);
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Microsoft.Xna.Framework.Color.FromNonPremultiplied(pixels[i].ToVector4());
                texture.SetData(pixels);
            }
            catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or NotSupportedException)
            { texture?.Dispose(); texture = null; Console.Error.WriteLine($"Sprite fallback for {id}: {exception.Message}"); }
        }
        cache[id] = texture;
        return texture;
    }
    public void Dispose() { foreach (Texture2D? texture in cache.Values) texture?.Dispose(); }
}
