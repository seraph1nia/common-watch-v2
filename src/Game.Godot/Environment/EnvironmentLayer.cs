using Godot;

namespace Game.GodotClient;

/// <summary>Retained, input-free world layer; clipped geometry samples continuous world coordinates.</summary>
public partial class EnvironmentLayer : Node2D
{
    public bool Enabled { get; set; } = true;
    public float Strength { get; set; } = 1;
    public float SpeedScale { get; set; } = 1;
    public float Opacity { get; set; } = 1;
    public Color Tint { get; set; } = Colors.White;
    public Vector2 Velocity { get; set; }
    public Vector2 TextureScale { get; set; } = new(512, 512);
    public Texture2D? Texture { get; set; }
    public Rect2 Bounds { get; set; }
    public float Time { get; private set; }
    private Rect2 _visible;
    private readonly EnvironmentMotion _motion = new();
    public Vector2 MotionOffset => _motion.Offset;
    public ShaderMaterial EffectMaterial { get; private set; } = null!;
    public void Initialize(Shader shader)
    {
        EffectMaterial = new ShaderMaterial { Shader = shader };
        Material = EffectMaterial;
    }
    public void UpdateLayer(float time, bool sampled = false)
    {
        Time = time;
        _motion.Update(time, Velocity * SpeedScale, sampled);
        _visible = Bounds.Intersection(WorldArt.VisibleArea(this));
        Visible = Enabled && Opacity > .001f && _visible.HasArea();
        if (!Visible) return;
        EffectMaterial.SetShaderParameter("visual_time", time);
        EffectMaterial.SetShaderParameter("motion_offset", MotionOffset);
        EffectMaterial.SetShaderParameter("texture_scale", TextureScale);
        EffectMaterial.SetShaderParameter("opacity", Opacity * Strength);
        EffectMaterial.SetShaderParameter("tint", Tint);
        EffectMaterial.SetShaderParameter("region", new Vector4(Bounds.Position.X, Bounds.Position.Y, Bounds.Size.X, Bounds.Size.Y));
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (Texture is { } texture) DrawTextureRect(texture, _visible, false);
        else DrawRect(_visible, Colors.White);
    }
    public override void _ExitTree() => EffectMaterial?.Dispose();
}
