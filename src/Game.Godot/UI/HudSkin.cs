using Godot;
using Game.Shared;

namespace Game.GodotClient;

/// <summary>Optional forest resources adapt the native theme; the static menu backdrop is shared by all modes.</summary>
public sealed class HudSkin : IDisposable
{
    public Theme Theme { get; }
    public bool Illustrated { get; }
    public WorldtreeArt? Worldtree { get; }
    public Texture2D? MenuBackground { get; }
    public int NinePatches { get; private set; }
    public HudSkin(AssetLoader assets)
    {
        var plain = GD.Load<Theme>("res://UI/WatchTheme.tres");
        Theme = (Theme)plain.Duplicate();
        Worldtree = WorldtreeArt.Load(assets);
        MenuBackground = assets.MenuBackground();
        if (assets.Options.Hud != HudArt.Illustrated) return;
        const string path = "res://UI/ForestHud/ForestTheme.tres";
        if (!ResourceLoader.Exists(path)) return;
        // A partially installed optional pack falls back as a whole, before Godot loads it.
        if (ResourceLoader.GetDependencies(path).Any(dependency => !ResourceLoader.Exists(dependency.Split("::")[^1]))) return;
        var forest = GD.Load<Theme>(path);
        if (forest is null) return;
        Theme.MergeWith(forest); Illustrated = true;
        // OptionButton is a separate native type. Give it every state, including focus.
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            Theme.SetStylebox(state, "OptionButton", Theme.GetStylebox(state, "Button"));
        Theme.SetFontSize("font_size", "Label", 16);
        Theme.SetFontSize("font_size", "Button", 16);
        Theme.SetFontSize("font_size", "OptionButton", 16);
        foreach (string type in new[] { "Button", "ForestPrimaryButton", "ForestArcaneButton", "ForestDangerButton" })
            Theme.SetColor("font_disabled_color", type, new Color("b1bfb0"));
    }
    public Button Button(string text, Action clicked, string variation = "")
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 44), Theme = Theme,
            ThemeTypeVariation = Illustrated ? variation : "", MouseFilter = Control.MouseFilterEnum.Stop };
        button.Pressed += clicked; return button;
    }
    public void Panel(PanelContainer panel, string id = "hud/panel")
    {
        panel.Theme = Theme; panel.MouseFilter = Control.MouseFilterEnum.Stop;
        if (!Illustrated) return;
        panel.ThemeTypeVariation = id switch { "ribbon" => "ForestRibbon", "modal" => "ForestModal", _ => "" };
        var style = (StyleBoxTexture)Theme.GetStylebox("panel", !panel.ThemeTypeVariation.IsEmpty ? panel.ThemeTypeVariation : "PanelContainer").Duplicate();
        // Padding is independent of source-pixel slice margins. Never scale the frame.
        style.ContentMarginLeft = style.ContentMarginRight = 12;
        style.ContentMarginTop = style.ContentMarginBottom = id == "ribbon" ? 16 : 10;
        panel.AddThemeStyleboxOverride("panel", style); NinePatches++;
    }
    private Texture2D? Texture(string path) => Illustrated && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    public Texture2D? Icon(string name) => Texture($"res://UI/ForestHud/icons/{name}.png");
    public void Ornament(Control parent, string name, Vector2 position, Vector2 size)
    {
        if (Texture($"res://UI/ForestHud/ornaments/{name}.png") is not { } texture) return;
        var decoration = new Node2D { Name = "ForestOrnament" }; parent.AddChild(decoration);
        decoration.AddChild(new TextureRect { Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Position = position, Size = size,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore });
    }
    public void Dispose() => Theme.Dispose();
}
