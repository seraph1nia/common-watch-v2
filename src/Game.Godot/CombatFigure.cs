using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

// One retained CanvasItem per visible actor; freed at lifecycle/reset boundaries.
public partial class CombatFigure : Node2D
{
    public const string HitFlashCode = "shader_type canvas_item; uniform float flash : hint_range(0.0,1.0); void fragment(){ vec4 c=COLOR; COLOR=vec4(mix(c.rgb,vec3(1.0),flash),c.a); }";
    readonly ShaderMaterial flashMaterial = new();
    Texture2D? texture;
    AssetMetadata meta = AssetMetadata.Default("unit/swordsman");
    UnitType type;
    Color color;
    public void Present(Texture2D? image, AssetMetadata metadata, UnitType unitType, bool friendly, FigurePose pose, Vector2 ground, float zoom, bool shader, Shader hitShader)
    {
        texture = image; meta = metadata; type = unitType;
        color = new Color(friendly ? "7baad0" : "bb7766");
        Position = ground + new Vector2(pose.Offset.X, pose.Offset.Y) * zoom;
        Rotation = pose.Rotation; Scale = Vector2.One * zoom * pose.Scale;
        Modulate = new Color(1, 1, 1, pose.Opacity);
        flashMaterial.Shader = hitShader;
        Material = shader ? flashMaterial : null;
        flashMaterial.SetShaderParameter("flash", pose.Flash * .6f);
        if (!shader) Modulate = Colors.White.Lerp(new Color(1.7f, 1.4f, 1.4f), pose.Flash) with { A = pose.Opacity };
        QueueRedraw();
    }
    public Rect2 Bounds
    {
        get
        {
            if (texture is null) return new Rect2(-12, -34, 26, 36);
            Vector2 size = new Vector2(texture.GetWidth(), texture.GetHeight()) * (meta.DisplaySize * .86f / Math.Max(texture.GetWidth(), texture.GetHeight()));
            return new Rect2(-size * new Vector2(meta.Anchor[0], meta.Anchor[1]) + new Vector2(meta.Offset[0], meta.Offset[1]), size);
        }
    }
    public override void _Draw()
    {
        if (texture is not null)
        {
            float factor = meta.DisplaySize * .86f / Math.Max(texture.GetWidth(), texture.GetHeight());
            Vector2 size = new Vector2(texture.GetWidth(), texture.GetHeight()) * factor;
            DrawTextureRect(texture, new Rect2(-size * new Vector2(meta.Anchor[0], meta.Anchor[1]) + new Vector2(meta.Offset[0], meta.Offset[1]), size), false);
            return;
        }
        // A readable tabletop token, entirely geometric. It is never catalog artwork.
        DrawLine(new(-4, -9), new(-5, -1), new Color("b9b1a0"), 4, true);
        DrawLine(new(4, -9), new(6, -1), new Color("b9b1a0"), 4, true);
        DrawColoredPolygon([new(-8, -10), new(-6, -24), new(5, -24), new(8, -10)], color);
        DrawCircle(new(0, -28), 5, new Color("d4c5a2"), true, -1, true);
        DrawLine(new(-4, -30), new(4, -30), new Color("e4e9e8"), 3, true);
        if (type == UnitType.Mage)
        { DrawLine(new(9, 0), new(9, -30), new Color("baad8a"), 2, true); DrawCircle(new(9, -31), 3, new Color("a192f7")); }
        else if (type == UnitType.Crossbowman)
        { DrawLine(new(-9, -19), new(10, -19), new Color("dcbe86"), 3, true); DrawLine(new(7, -25), new(7, -13), new Color("f3e3bd"), 2, true); }
        else
        { DrawLine(new(8, -13), new(12, -32), new Color("e1e5df"), 2, true); DrawColoredPolygon([new(-10, -22), new(-3, -22), new(-3, -13), new(-7, -10), new(-11, -15)], color.Darkened(.25f)); }
    }
    public override void _ExitTree() => flashMaterial.Dispose();
}
