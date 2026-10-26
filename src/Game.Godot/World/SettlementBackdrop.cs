using Godot;

namespace Game.GodotClient;

/// <summary>Retained painted-map layer. Only its material animates; plot geometry never moves.</summary>
public partial class SettlementBackdrop : Node2D
{
    private EnvironmentController _environment = null!;
    private ShaderMaterial _material = null!;
    private Texture2D? _texture;
    private Vector2 _size;
    private (int Frame, float Strength)? _sample;

    public void Initialize(EnvironmentController environment)
    {
        _environment = environment;
        _material = new ShaderMaterial { Shader = environment.Shader("SettlementLivingMap") };
        Material = _material;
        ZIndex = -1;
    }

    public void Present(Texture2D? texture, Vector2 size)
    {
        _texture = texture; _size = size;
        Visible = texture is not null;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree()) return;
        float strength = _environment.Settings.Quality switch
        {
            EnvironmentQuality.Off => 0,
            EnvironmentQuality.Subtle => .45f,
            _ => 1
        };
        int frame = (int)(Math.Floor(_environment.VisualTime * 8) % 64);
        if (_sample == (frame, strength)) return;
        _sample = (frame, strength);
        _material.SetShaderParameter("visual_time", frame / 8f);
        _material.SetShaderParameter("strength", strength);
    }

    public override void _Draw()
    {
        if (_texture is null) return;
        var imageRect = new Rect2(-_size / 2, _size);
        var viewport = GetViewport().GetVisibleRect();
        var inverse = GetViewport().CanvasTransform.AffineInverse();
        var worldRect = new Rect2(inverse * viewport.Position, viewport.Size / GetViewport().CanvasTransform.Scale);
        if (!imageRect.Encloses(worldRect))
        {
            float cover = Mathf.Max(worldRect.Size.X / _size.X, worldRect.Size.Y / _size.Y);
            Vector2 crop = worldRect.Size / cover;
            DrawTextureRectRegion(_texture, worldRect, new Rect2((_size - crop) / 2, crop), new Color(.55f, .62f, .52f));
        }
        DrawTextureRect(_texture, imageRect, false);
    }

    public override void _ExitTree() => _material.Dispose();
}
