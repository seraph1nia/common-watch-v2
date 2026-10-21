using Godot;

namespace Game.GodotClient;

/// <summary>Bounded batched cosmetics. No timers, nodes or allocations per particle.</summary>
public partial class AmbientParticles : Node2D
{
    public Rect2 Bounds { get; set; }
    public float Time { get; private set; }
    private readonly EnvironmentMotion _motion = new(), _snowMotion = new();
    public Vector2 MotionOffset => _motion.Offset;
    public float SnowOffset => _snowMotion.Offset.Y;
    public void UpdateMotion(float time, bool sampled = false)
    {
        Time = time;
        bool rain = Rain > .01f, snow = !rain && Snow > .01f;
        _motion.Update(time, new Vector2(Wind, rain ? 185 : snow ? 17 : -4), sampled);
        _snowMotion.Update(time, new Vector2(0, snow ? 20 : 0), sampled);
    }
    public float Amount { get; set; }
    public float Snow { get; set; }
    public float Rain { get; set; }
    public float Embers { get; set; }
    public float Wind { get; set; }
    public bool Splashes { get; set; }
    public int Seed { get; set; }
    public Color Tint { get; set; }
    public float Quality { get; set; }
    private (Rect2 Visible, Rect2 Bounds, float Time, Vector2 Offset, float SnowOffset, float Amount, float Snow, float Rain, float Embers, float Wind, bool Splashes, int Seed, Color Tint, float Quality)? _drawState;
    public void UpdateParticles(bool enabled = true)
    {
        var visible = Bounds.Intersection(WorldArt.VisibleArea(this));
        Visible = enabled && Quality > 0 && Math.Max(Amount, Math.Max(Snow, Math.Max(Rain, Embers))) > 0 && visible.HasArea();
        if (!IsVisibleInTree()) return;
        var state = (visible, Bounds, Time, MotionOffset, SnowOffset, Amount, Snow, Rain, Embers, Wind, Splashes, Seed, Tint, Quality);
        if (_drawState == state) return;
        _drawState = state;
        QueueRedraw();
    }
    private static float Random(int i, int seed) => Mathf.PosMod(Mathf.Sin(i * 127.1f + seed * 31.7f) * 43758.5453f, 1);
    public override void _Draw()
    {
        Rect2 visible = _drawState?.Visible ?? default;
        if (!visible.HasArea()) return;
        // A world grid keeps positions continuous while panning; coarsen at overview zoom.
        float cell = Math.Max(72, visible.Size.X / 18);
        int x0 = (int)MathF.Floor(visible.Position.X / cell), y0 = (int)MathF.Floor(visible.Position.Y / cell);
        int nx = Math.Min(21, (int)MathF.Ceiling(visible.Size.X / cell) + 2);
        int ny = Math.Min(16, (int)MathF.Ceiling(visible.Size.Y / cell) + 2);
        for (int y = y0; y < y0 + ny; y++) for (int x = x0; x < x0 + nx; x++)
        {
            int id = x * 977 + y * 131;
            float r = Random(id, Seed), phase = Random(id + 41, Seed);
            if (r > Quality * Math.Max(Amount, Math.Max(Snow, Math.Max(Rain, Embers)))) continue;
            Vector2 p = new((x + Mathf.PosMod(phase + MotionOffset.X / cell + MathF.Sin(Time * .27f + id) * .06f, 1)) * cell,
                (y + Mathf.PosMod(r + (MotionOffset.Y + r * SnowOffset) / cell, 1)) * cell);
            if (!visible.HasPoint(p)) continue;
            float alpha = .2f + .35f * MathF.Pow(MathF.Sin(Time * .8f + id), 2);
            if (Rain > .01f)
            {
                DrawLine(p, p + new Vector2(Wind * .08f, 10 + r * 9), new Color(.73f, .82f, .9f, .22f * Rain), 1, true);
                if (Splashes && r < .1f)
                {
                    float t = Mathf.PosMod(Time * .7f + phase, 1);
                    DrawArc(new Vector2(p.X, Bounds.Position.Y + Bounds.Size.Y * (.55f + phase * .32f)), 1 + t * 5, 0, Mathf.Pi, 10, new Color(.7f, .83f, .88f, (1 - t) * .18f * Rain), .7f, true);
                }
            }
            else
            {
                Color color = Snow > .01f ? new Color(.87f, .92f, 1, alpha * Snow) : Embers > .01f ? new Color(1, .46f + r * .2f, .16f, alpha * Embers) : Tint with { A = alpha * Amount };
                float size = Snow > .01f ? 1 + r * 1.8f : .7f + r;
                DrawCircle(p, size * 2.6f, color with { A = color.A * .1f }, true, -1, true);
                DrawCircle(p, size, color, true, -1, true);
            }
        }
    }
}
