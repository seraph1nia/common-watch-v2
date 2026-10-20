using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>One upright structure, sharing ground depth with the sampled combat figures.</summary>
public partial class CombatStructure : Node2D
{
    WorldArt _art = null!;
    StructureState _state = null!;
    float _flash;
    public void Present(AssetLoader assets, StructureState state, Vector2 ground, int depth, float opacity, float flash)
    {
        _art ??= new(assets); _state = state; _flash = flash;
        Position = ground; Modulate = Colors.White with { A = opacity };
        ZIndex = depth;
        Visible = true; QueueRedraw();
    }
    public override void _Draw()
    {
        _art.Structure(this, _state, Vector2.Zero, Colors.White.Lerp(new Color(1.7f, 1.4f, 1.4f), _flash));
    }
}
