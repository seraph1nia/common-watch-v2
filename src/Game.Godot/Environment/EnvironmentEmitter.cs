using Godot;

namespace Game.GodotClient;

public enum EnvironmentEmitterKind { Fire, Crystal, Smoke }

/// <summary>A localized overlay attached to a measured art anchor, never the whole building.</summary>
public partial class EnvironmentEmitter : Node2D
{
    public EnvironmentEmitterKind Kind { get; set; }
    public float Radius { get; set; } = 26;
    public float Intensity { get; set; }
    public Vector2 Wind { get; set; }
    public float Time { get; set; }
    private Texture2D? _fire, _crystal;
    private ShaderMaterial _glow = null!;
    private float? _glowTime;
    private (EnvironmentEmitterKind Kind, float Radius, float Intensity, float Time, float Wind, float Phase)? _drawState;
    internal int ShaderUpdates { get; private set; }
    public void Initialize(EnvironmentController environment, AssetLoader assets)
    {
        _glow = new ShaderMaterial { Shader = environment.Shader("SoftGlow") }; Material = _glow;
        _fire = assets.Load("arena/fire_glow"); _crystal = assets.Load("arena/crystal_glow");
    }
    public void UpdateEmitter(float time, float intensity)
    {
        Time = time; Intensity = intensity;
        Visible = intensity > .001f && WorldArt.VisibleArea(this).Grow(Radius * 4).HasPoint(Vector2.Zero);
        if (!IsVisibleInTree()) return;
        if (_glowTime != time)
        {
            _glow.SetShaderParameter("visual_time", time);
            _glowTime = time; ShaderUpdates++;
        }
        bool smoke = Kind == EnvironmentEmitterKind.Smoke;
        var state = (Kind, Radius, Intensity, smoke ? time : 0, smoke ? Wind.X : 0, smoke ? Position.X : 0);
        if (_drawState == state) return;
        _drawState = state;
        QueueRedraw();
    }
    public override void _Draw()
    {
        float phase = Position.X * .019f;
        if (Kind == EnvironmentEmitterKind.Smoke)
        {
            // Analytic plumes fade/expand; the smoke origin is a foundation-relative roof anchor.
            for (int i = 0; i < 7; i++)
            {
                float age = Mathf.PosMod(Time * .12f + i / 7f + phase, 1);
                Vector2 p = new(Mathf.Sin(age * 4 + phase) * age * Radius * .5f + age * (Radius * .35f + Wind.X * .2f), -age * Radius * 3);
                DrawCircle(p, Radius * (.12f + age * .45f), new Color(.55f, .6f, .63f, MathF.Sin(age * Mathf.Pi) * .035f * Intensity), true, -1, true);
            }
            return;
        }
        Texture2D? texture = Kind == EnvironmentEmitterKind.Crystal ? _crystal : _fire;
        Color color = Kind == EnvironmentEmitterKind.Crystal ? new(.45f, .83f, 1, Intensity * .7f) : new(1, .65f, .32f, Intensity * .55f);
        var rect = new Rect2(-Vector2.One * Radius, Vector2.One * Radius * 2);
        if (texture is not null) DrawTextureRect(texture, rect, false, color);
        else for (int i = 4; i > 0; i--) DrawCircle(Vector2.Zero, Radius * i / 4, color with { A = color.A * .018f }, true, -1, true);
    }
    public override void _ExitTree() => _glow?.Dispose();
}
