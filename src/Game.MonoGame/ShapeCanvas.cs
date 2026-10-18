using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game.MonoGameClient;

// The single white pixel is a technical GPU primitive, never game artwork.
internal sealed class ShapeCanvas : IDisposable
{
    readonly SpriteBatch batch;
    readonly Texture2D pixel;
    public ShapeCanvas(GraphicsDevice device)
    {
        batch = new(device);
        pixel = new(device, 1, 1);
        pixel.SetData(new[] { Color.White });
    }
    public void Begin() => batch.Begin(samplerState: SamplerState.LinearClamp);
    public void End() => batch.End();
    public void Rect(float x, float y, float width, float height, Color color) => batch.Draw(pixel, new Rectangle((int)x, (int)y, Math.Max(1, (int)width), Math.Max(1, (int)height)), color);
    public void Line(Vector2 from, Vector2 to, Color color, float width = 2)
    {
        Vector2 delta = to - from;
        batch.Draw(pixel, from, null, color, MathF.Atan2(delta.Y, delta.X), new(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
    public void Polygon(Vector2[] vertices, Color fill, Color border, float borderWidth = 2)
    {
        // Scanline fill keeps all world geometry within SpriteBatch.
        int minY = (int)vertices.Min(p => p.Y), maxY = (int)vertices.Max(p => p.Y);
        for (int y = minY; y <= maxY; y++)
        {
            List<float> intersections = [];
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
    public void Sprite(Texture2D texture, Vector2 anchor, float width, float height) =>
        batch.Draw(texture, new Rectangle((int)(anchor.X - width / 2), (int)(anchor.Y - height), (int)width, (int)height), Color.White);
    public void Dispose() { batch.Dispose(); pixel.Dispose(); }
}
