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
    private Texture2D? _drawTexture;
    private (Vector2 Offset, Vector2 Scale, float Opacity, Color Tint, Rect2 Bounds)? _parameters;
    internal int ShaderUpdates { get; private set; }
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
        var visible = Bounds.Intersection(WorldArt.VisibleArea(this));
        float opacity = Opacity * Strength;
        Visible = Enabled && opacity > .001f && visible.HasArea();
        if (!IsVisibleInTree()) return;
        if (_parameters is not { } previous || previous.Offset != MotionOffset) SetParameter("motion_offset", MotionOffset);
        if (_parameters is null || _parameters.Value.Scale != TextureScale) SetParameter("texture_scale", TextureScale);
        if (_parameters is null || _parameters.Value.Opacity != opacity) SetParameter("opacity", opacity);
        if (_parameters is null || _parameters.Value.Tint != Tint) SetParameter("tint", Tint);
        if (_parameters is null || _parameters.Value.Bounds != Bounds)
            SetParameter("region", new Vector4(Bounds.Position.X, Bounds.Position.Y, Bounds.Size.X, Bounds.Size.Y));
        _parameters = (MotionOffset, TextureScale, opacity, Tint, Bounds);
        if (_visible != visible || _drawTexture != Texture)
        {
            _visible = visible; _drawTexture = Texture;
            QueueRedraw();
        }
    }
    private void SetParameter(StringName name, Variant value)
    {
        EffectMaterial.SetShaderParameter(name, value);
        ShaderUpdates++;
    }
    public override void _Draw()
    {
        if (_drawTexture is { } texture) DrawTextureRect(texture, _visible, false);
        else DrawRect(_visible, Colors.White);
    }
    public override void _ExitTree() => EffectMaterial?.Dispose();
}
