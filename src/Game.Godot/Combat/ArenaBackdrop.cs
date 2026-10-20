using Godot;

namespace Game.GodotClient;

/// <summary>Viewport atmosphere, independent of battlefield bounds and camera transforms.</summary>
public partial class ArenaBackdrop : CanvasLayer
{
    private readonly TextureRect _image = new()
    {
        MouseFilter = Control.MouseFilterEnum.Ignore,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered
    };

    public override void _Ready()
    {
        Layer = -1;
        AddChild(_image);
        _image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Hide();
    }

    public void Present(AssetLoader assets, bool arenaVisible)
    {
        _image.Texture = arenaVisible ? assets.Load("arena/atmosphere") : null;
        Visible = _image.Texture is not null;
    }
}
