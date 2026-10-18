using Godot;

namespace Game.GodotClient;

/// <summary>One native theme and the supplied nine-slices, shared by all screens.</summary>
public sealed class HudSkin : IDisposable
{
    private readonly AssetLoader _assets;
    private readonly Theme _parchment = new();
    public Theme Theme { get; } = GD.Load<Theme>("res://UI/WatchTheme.tres");
    public int NinePatches { get; private set; }
    public HudSkin(AssetLoader assets)
    {
        _assets = assets;
        _parchment.SetColor("font_color", "Label", new Color("30251c"));
    }
    public Button Button(string text, Action clicked)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 36), Theme = Theme };
        button.Pressed += clicked;
        SkinButton(button);
        return button;
    }
    private NinePatchRect? NinePatch(Control parent, string id)
    {
        if (_assets.Load(id, true) is not { } texture || _assets.Catalog.Metadata(id).Slice is not { } slice) return null;
        var meta = _assets.Catalog.Metadata(id);
        var patch = new NinePatchRect
        {
            Name = "IllustratedBackground",
            Texture = texture,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            PatchMarginLeft = slice.Left,
            PatchMarginTop = slice.Top,
            PatchMarginRight = slice.Right,
            PatchMarginBottom = slice.Bottom,
            Scale = Vector2.One * meta.BorderScale,
        };
        var background = new Node2D { Name = "ArtBackground", ShowBehindParent = true };
        parent.AddChild(background); background.AddChild(patch);
        void Layout() { patch.Position = Vector2.Zero; patch.Size = parent.Size / meta.BorderScale; }
        parent.Resized += Layout; Layout();
        NinePatches++;
        return patch;
    }
    public void Panel(PanelContainer panel, string id = "hud/panel")
    {
        bool illustrated = NinePatch(panel, id) is not null;
        panel.Theme = illustrated ? _parchment : Theme;
        if (illustrated) panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
    }
    private void SkinButton(Button button)
    {
        var patch = NinePatch(button, "hud/button");
        if (patch is null) return;
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled" }) button.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        button.MouseEntered += () => patch.Modulate = new Color(1.15f, 1.15f, 1.15f);
        button.MouseExited += () => patch.Modulate = Colors.White;
    }

    public void Dispose() { _parchment.Dispose(); Theme.Dispose(); }
}
