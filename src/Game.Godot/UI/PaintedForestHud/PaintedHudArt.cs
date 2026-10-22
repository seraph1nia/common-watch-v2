using Godot;

namespace Game.GodotClient;

/// <summary>Additive painted skin. Text, commands and focus remain native Controls.</summary>
public sealed class PaintedHudArt
{
    private const string Root = "res://UI/PaintedForestHud/textures/";
    private readonly Dictionary<string, Texture2D?> _textures = [];
    private readonly StyleBoxFlat _selected = new() { BgColor = Colors.Transparent, BorderColor = new Color("ffd27d"),
        BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14, CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14 };
    public void IndicateSelection(Button button) => button.Draw += () =>
    {
        if (button.ButtonPressed) button.DrawStyleBox(_selected, new Rect2(Vector2.One * 4, button.Size - Vector2.One * 8));
    };
    public Texture2D? Texture(string id)
    {
        if (_textures.TryGetValue(id, out var cached)) return cached;
        var path = Root + id + ".png";
        return _textures[id] = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }
    public bool Available => ResourceLoader.Exists(Root + "focus.svg") && new[] { "panel", "owl", "rabbit", "squirrel" }.All(id => Texture(id) is not null)
        && new[] { "ready", "pause", "plaque", "tab", "details", "menu" }
            .All(family => new[] { "normal", "hover", "pressed", "disabled" }.All(state => Texture(family + "_" + state) is not null));

    public StyleBoxTexture Frame(string id = "panel") => new()
    {
        Texture = Texture(id), TextureMarginLeft = id == "panel" ? 40 : 32,
        TextureMarginRight = id == "panel" ? 40 : 32,
        TextureMarginTop = id == "panel" ? 40 : 32,
        TextureMarginBottom = id == "panel" ? 40 : 32,
        ContentMarginLeft = 36, ContentMarginRight = 36, ContentMarginTop = 44, ContentMarginBottom = 32
    };

    public void Apply(Theme theme)
    {
        theme.SetStylebox("panel", "PanelContainer", Frame());
        theme.SetStylebox("panel", "ForestModal", Frame());
        foreach (var (type, family) in new[] { ("Button", "menu"), ("ForestPrimaryButton", "ready"),
                ("ForestArcaneButton", "pause"), ("ForestCard", "plaque"), ("PaintedTab", "tab"), ("PaintedDetails", "details") })
        {
            if (type is "PaintedTab" or "PaintedDetails") theme.SetTypeVariation(type, "Button");
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
                theme.SetStylebox(state, type, new StyleBoxTexture
                {
                    Texture = Texture(family + "_" + state),
                    TextureMarginLeft = family == "plaque" ? 28 : 22, TextureMarginRight = family == "plaque" ? 28 : 22,
                    TextureMarginTop = 16, TextureMarginBottom = 16,
                    ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 6
                });
            var focus = new StyleBoxTexture { Texture = GD.Load<Texture2D>(Root + "focus.svg"),
                TextureMarginLeft = 22, TextureMarginRight = 22, TextureMarginTop = 22, TextureMarginBottom = 22,
                ExpandMarginLeft = 3, ExpandMarginRight = 3, ExpandMarginTop = 3, ExpandMarginBottom = 3 };
            theme.SetStylebox("focus", type, focus);
            theme.SetColor("font_color", type, new Color("f6e8ca"));
            theme.SetColor("font_pressed_color", type, new Color("ffd27d"));
            theme.SetColor("font_disabled_color", type, new Color("bcb8ac"));
        }
        foreach (string color in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_disabled_color" })
            theme.SetColor(color, "ForestCard", new Color("302015"));
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            theme.SetStylebox(state, "OptionButton", theme.GetStylebox(state, "Button"));
    }

    /// <summary>Fixed-size, aspect-preserving ornament outside Container measurement and hit testing.</summary>
    public void Attach(Control parent, string id, Vector2 size, Vector2 attachment, Vector2 offset, Vector2? perch = null)
    {
        if (Texture(id) is not { } texture) return;
        var layer = new Node2D { Name = "Painted_" + id }; parent.AddChild(layer);
        var sprite = new Sprite2D { Texture = texture }; layer.AddChild(sprite);
        void Layout()
        {
            float scale = parent.GetViewportRect().Size.X >= 1600 ? 1.25f : 1;
            var box = size * scale;
            float ratio = Mathf.Min(box.X / texture.GetWidth(), box.Y / texture.GetHeight());
            var actual = texture.GetSize() * ratio;
            layer.Position = parent.Size * attachment + offset * scale;
            sprite.Scale = Vector2.One * ratio;
            sprite.Position = (Vector2.One * .5f - (perch ?? new Vector2(.5f, .9f))) * actual;
        }
        parent.Resized += Layout;
        layer.TreeExiting += () => parent.Resized -= Layout;
        Layout();
    }
}
