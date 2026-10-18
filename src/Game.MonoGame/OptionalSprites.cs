using Game.Shared;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGameClient;

internal sealed class OptionalSprites(GraphicsDevice graphics, RenderOptions options) : IDisposable
{
    readonly Dictionary<string, Texture2D?> cache = [];
    public int LoadAttempts { get; private set; }
    public Texture2D? Get(string id)
    {
        if (options.Mode == RenderMode.Debug) return null;
        if (cache.TryGetValue(id, out Texture2D? existing)) return existing;
        Texture2D? texture = null;
        string? path = VisualAssets.Resolve(options.AssetsRoot, id, options.Mode);
        if (path is not null)
        {
            LoadAttempts++;
            try
            {
                using var input = File.OpenRead(path);
                texture = Texture2D.FromStream(graphics, input);
            }
            catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or NotSupportedException)
            { Console.Error.WriteLine($"Sprite fallback for {id}: {exception.Message}"); }
        }
        cache[id] = texture;
        return texture;
    }
    public void Dispose() { foreach (Texture2D? texture in cache.Values) texture?.Dispose(); }
}
