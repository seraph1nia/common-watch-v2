using Godot;

namespace Game.GodotClient;

/// <summary>Native selectable card with independent typography and a content-driven minimum height.</summary>
public partial class ConstructionCard : Button
{
    private MarginContainer? _content;
    private Label _title = null!, _purpose = null!, _cost = null!, _reason = null!;
    private TextureRect _thumbnail = null!;
    private TextureRect _portraitFrame = null!;
    private Control _portrait = null!;
    public void Initialize(bool painted = false)
    {
        ToggleMode = true; SizeFlagsHorizontal = SizeFlags.ExpandFill; CustomMinimumSize = new(176, 104);
        _content = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_content); _content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) _content.AddThemeConstantOverride("margin_" + side, 10);
        _content.AddThemeConstantOverride("margin_top", 20); _content.AddThemeConstantOverride("margin_bottom", 18);
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }; _content.AddChild(row);
        _portrait = new Control { CustomMinimumSize = new(40, 52), MouseFilter = MouseFilterEnum.Ignore }; row.AddChild(_portrait);
        _portraitFrame = new TextureRect { Position = new(0, 6), Size = new(40, 40), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore }; _portrait.AddChild(_portraitFrame);
        _thumbnail = new TextureRect { Position = new(4, 10), Size = new(32, 32), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore }; _portrait.AddChild(_thumbnail);
        var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 2); row.AddChild(body);
        _title = Line(16); _purpose = Line(14); _cost = Line(16); _reason = Line(14);
        _purpose.AddThemeColorOverride("font_color", new Color("b7ceba"));
        _cost.AddThemeColorOverride("font_color", new Color("efd478"));
        _reason.AddThemeColorOverride("font_color", new Color("f0bc9d"));
        foreach (var label in new[] { _title, _purpose, _cost, _reason }) body.AddChild(label);
        if (painted)
        {
            _title.AddThemeColorOverride("font_color", new Color("302015"));
            _purpose.AddThemeColorOverride("font_color", new Color("50362a"));
            _cost.AddThemeColorOverride("font_color", new Color("392716"));
            _reason.AddThemeColorOverride("font_color", new Color("67291e"));
        }
        _content.MinimumSizeChanged += FitContent;
    }
    private static Label Line(int size)
    {
        var label = WrapLabel(); label.MouseFilter = MouseFilterEnum.Ignore;
        label.AddThemeFontSizeOverride("font_size", size); return label;
    }
    private void FitContent() => CustomMinimumSize = new(176, Mathf.Max(104, _content?.GetCombinedMinimumSize().Y ?? 0));
    internal bool ContentFits => _content is not null && GetGlobalRect().Encloses(_content.GetGlobalRect())
        && _content.GetCombinedMinimumSize().Y <= Size.Y;
    public void Present(string title, string purpose, string cost, string reason, string details, bool selected, bool showReason = true)
    {
        _title.Text = (selected ? "✓ " : "") + title;
        _purpose.Text = purpose; _cost.Text = cost;
        _reason.Text = reason; _reason.Visible = showReason && reason.Length > 0;
        _purpose.Visible = !showReason || reason.Length == 0;
        TooltipText = details + (reason.Length > 0 ? "\n" + reason : "");
        Disabled = reason.Length > 0; SetPressedNoSignal(selected);
    }
    public void SetThumbnail(Texture2D? texture, Texture2D? frame)
    {
        if (_thumbnail.Texture != texture) _thumbnail.Texture = texture;
        if (_portraitFrame.Texture != frame) _portraitFrame.Texture = frame;
        _portrait.Visible = texture is not null; _portraitFrame.Visible = frame is not null;
    }
}
