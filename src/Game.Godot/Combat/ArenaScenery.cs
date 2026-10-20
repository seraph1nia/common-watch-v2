using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Sharp scenery with an alpha feather only outside the complete playable board.</summary>
public partial class ArenaScenery : Node2D
{
    private AssetLoader _assets = null!;
    private ArenaState? _arena;
    private HexProjection _projection = new(30);
    private readonly ShaderMaterial _edge = new() { Shader = GD.Load<Shader>("res://Combat/ArenaEdge.gdshader") };
    public bool AnimatedEnvironment { get; set; }
    public bool Available { get; private set; }

    public override void _Ready() { ShowBehindParent = true; ZIndex = -20; Material = _edge; }

    public void Present(AssetLoader assets, ArenaState? arena, HexProjection projection)
    {
        _assets = assets; _arena = arena; _projection = projection;
        Available = arena is not null && assets.Load("arena/ground_repeat_01") is not null;
        Visible = Available;
        if (!Available) return;
        var layout = new ArenaLayout(arena!, projection);
        var outer = ArenaPresentation.RenderBounds(arena!, projection, true);
        _edge.SetShaderParameter("inner_bounds", new Vector4(layout.Position.X, layout.Position.Y, -layout.Position.X, -layout.Position.Y));
        _edge.SetShaderParameter("outer_bounds", new Vector4(outer.Position.X, outer.Position.Y, outer.End.X, outer.End.Y));
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Available) ArenaPresentation.DrawModular(this, _assets, _arena!, _projection, AnimatedEnvironment);
    }

    public override void _ExitTree() => _edge.Dispose();
}
