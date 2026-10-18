using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGameClient;

// The single white pixel is a technical GPU primitive, never game artwork.
internal sealed class ShapeCanvas : IDisposable
{
    readonly SpriteBatch batch;
    readonly Texture2D pixel;
    readonly BasicEffect flashEffect;
    Matrix local = Matrix.Identity;
    readonly List<float> intersections = new(8);
    public int ShaderUses { get; private set; }
    public ShapeCanvas(GraphicsDevice device)
    {
        batch = new(device);
        flashEffect = new(device) { TextureEnabled = true, VertexColorEnabled = true, FogEnabled = true, FogColor = Vector3.One };
        pixel = new(device, 1, 1);
        pixel.SetData(new[] { Color.White });
    }
    public void Begin() => batch.Begin(samplerState: SamplerState.LinearClamp);
    public void End() => batch.End();
    public void Transform(Vector2 position, float rotation, float scale) => local = Matrix.CreateScale(scale) * Matrix.CreateRotationZ(rotation) * Matrix.CreateTranslation(position.X, position.Y, 0);
    public void ResetTransform() => local = Matrix.Identity;
    public void HitShader(float flash)
    {
        End();
        flashEffect.Projection = Matrix.CreateOrthographicOffCenter(0, pixel.GraphicsDevice.Viewport.Width, pixel.GraphicsDevice.Viewport.Height, 0, 0, 1);
        flashEffect.World = Matrix.Identity; flashEffect.View = Matrix.Identity;
        flashEffect.FogStart = -flash; flashEffect.FogEnd = 1 - flash;
        batch.Begin(samplerState: SamplerState.LinearClamp, effect: flashEffect);
        ShaderUses++;
    }
    public void EndHitShader() { End(); Begin(); }

    public void Rect(float x, float y, float width, float height, Color color)
    {
        if (width <= 0 || height <= 0) return;
        Vector2 position = Vector2.Transform(new Vector2(x, y), local);
        float scale = new Vector2(local.M11, local.M12).Length();
        batch.Draw(pixel, position, null, color, MathF.Atan2(local.M12, local.M11), Vector2.Zero, new Vector2(width, height) * scale, SpriteEffects.None, 0);
    }
    public void Line(Vector2 from, Vector2 to, Color color, float width = 2)
    {
        from = Vector2.Transform(from, local); to = Vector2.Transform(to, local);
        width *= new Vector2(local.M11, local.M12).Length();
        Vector2 delta = to - from;
        batch.Draw(pixel, from, null, color, MathF.Atan2(delta.Y, delta.X), new(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
    public void Polygon(Vector2[] vertices, Color fill, Color border, float borderWidth = 2)
    {
        // Scanline fill keeps all world geometry within SpriteBatch.
        int minY = (int)vertices.Min(p => p.Y), maxY = (int)vertices.Max(p => p.Y);
        for (int y = minY; y <= maxY; y++)
        {
            intersections.Clear();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 a = vertices[i], b = vertices[(i + 1) % vertices.Length];
                if ((a.Y <= y && b.Y > y) || (b.Y <= y && a.Y > y))
                    intersections.Add(a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y));
            }
            intersections.Sort();
            for (int i = 0; i + 1 < intersections.Count; i += 2)
                Rect(intersections[i], y, intersections[i + 1] - intersections[i], 1, fill);
        }
        for (int i = 0; i < vertices.Length; i++) Line(vertices[i], vertices[(i + 1) % vertices.Length], border, borderWidth);
    }
    public void Circle(Vector2 center, float radius, Color color)
    {
        for (int y = -(int)radius; y <= radius; y++)
        {
            float extent = MathF.Sqrt(Math.Max(0, radius * radius - y * y));
            Rect(center.X - extent, center.Y + y, extent * 2, 1, color);
        }
    }
    public Rectangle Sprite(Texture2D texture, Vector2 ground, float extent, Game.Shared.AssetMetadata metadata, Color? tint = null)
    {
        float factor = extent / Math.Max(texture.Width, texture.Height);
        Vector2 size = new Vector2(texture.Width, texture.Height) * factor;
        var rect = new Rectangle((int)(ground.X - size.X * metadata.Anchor[0] + metadata.Offset[0]),
            (int)(ground.Y - size.Y * metadata.Anchor[1] + metadata.Offset[1]), Math.Max(1, (int)size.X), Math.Max(1, (int)size.Y));
        batch.Draw(texture, rect, tint ?? Color.White);
        return rect;
    }
    public void Figure(Texture2D texture, Game.Shared.AssetMetadata meta, Color tint, float extentScale = 1)
    {
        float scale = extentScale * new Vector2(local.M11, local.M12).Length() * meta.DisplaySize / Math.Max(texture.Width, texture.Height);
        Vector2 position = Vector2.Transform(new Vector2(meta.Offset[0], meta.Offset[1]), local);
        batch.Draw(texture, position, null, tint, MathF.Atan2(local.M12, local.M11), new Vector2(texture.Width * meta.Anchor[0], texture.Height * meta.Anchor[1]), scale, SpriteEffects.None, 0);
    }
    public void Ring(Vector2 p, float radius, Color color, float start = 0, float end = MathF.Tau, float width = 1)
    {
        for (int i = 0; i < 24; i++)
        {
            float a = start + (end - start) * i / 24, b = start + (end - start) * (i + 1) / 24;
            Line(p + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius, p + new Vector2(MathF.Cos(b), MathF.Sin(b)) * radius, color, width);
        }
    }
    public void Dispose() { flashEffect.Dispose(); batch.Dispose(); pixel.Dispose(); }
}
