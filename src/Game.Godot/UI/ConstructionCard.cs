using Godot;

namespace Game.GodotClient;

/// <summary>Native selectable card with independent typography and a content-driven minimum height.</summary>
public partial class ConstructionCard : Button
{
    private MarginContainer? _content;
    private Label _title = null!, _purpose = null!, _cost = null!, _reason = null!;
    public void Initialize()
    {
        ToggleMode = true; SizeFlagsHorizontal = SizeFlags.ExpandFill; CustomMinimumSize = new(240, 108);
        _content = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_content); _content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) _content.AddThemeConstantOverride("margin_" + side, 14);
        var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore }; body.AddThemeConstantOverride("separation", 3); _content.AddChild(body);
        _title = Line(18); _purpose = Line(15); _cost = Line(16); _reason = Line(15);
        _purpose.AddThemeColorOverride("font_color", new Color("b7ceba"));
        _cost.AddThemeColorOverride("font_color", new Color("efd478"));
        _reason.AddThemeColorOverride("font_color", new Color("f0bc9d"));
        foreach (var label in new[] { _title, _purpose, _cost, _reason }) body.AddChild(label);
        _content.MinimumSizeChanged += FitContent;
    }
    private static Label Line(int size)
    {
        var label = WrapLabel(); label.MouseFilter = MouseFilterEnum.Ignore;
        label.AddThemeFontSizeOverride("font_size", size); return label;
    }
    private void FitContent() => CustomMinimumSize = new(240, Mathf.Max(108, _content?.GetCombinedMinimumSize().Y ?? 0));
    internal bool ContentFits => _content is not null && GetGlobalRect().Encloses(_content.GetGlobalRect())
        && _content.GetCombinedMinimumSize().Y <= Size.Y;
    public void Present(string title, string purpose, string cost, string reason, string details, bool selected)
    {
        _title.Text = (selected ? "✓ " : "") + title;
        _purpose.Text = purpose; _cost.Text = "Cost: " + cost;
        _reason.Text = reason; _reason.Visible = reason.Length > 0;
        TooltipText = details + (reason.Length > 0 ? "\n" + reason : "");
        Disabled = reason.Length > 0; SetPressedNoSignal(selected);
    }
}
