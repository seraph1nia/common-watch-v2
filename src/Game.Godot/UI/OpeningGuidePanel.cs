using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Small optional first-watch objective. Collapsed preference lasts across sessions in this app.</summary>
public partial class OpeningGuidePanel : PanelContainer
{
    private Button _toggle = null!, _action = null!;
    private VBoxContainer _body = null!;
    private HBoxContainer _row = null!;
    private Label _objective = null!, _message = null!;
    private bool _expanded, _effective;
    private OpeningHint? _hint;
    public event Action<OpeningDestination>? Navigate;
    public event Action? LayoutRequested;
    internal Button ToggleButton => _toggle;
    internal Button ActionButton => _action;
    internal bool Expanded => _body.Visible;
    internal bool ExpandedPreference => _expanded;
    internal bool ContentFits => !_body.Visible || _message.GetLineCount() <= _message.MaxLinesVisible;
    internal string LayoutDetails { get; private set; } = "";
    internal string Text => _objective.Text + "\n" + _message.Text;
    public void Initialize(HudSkin skin)
    {
        skin.Panel(this);
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(.025f, .045f, .06f, .94f), BorderColor = new Color("809d89"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 });
        _row = new HBoxContainer(); _row.AddThemeConstantOverride("separation", 4); AddChild(_row);
        _toggle = skin.Button("Guide ▾", () => { _expanded = !_expanded; QueueSort(); LayoutRequested?.Invoke(); }); _toggle.AddThemeFontSizeOverride("font_size", 14);
        _toggle.CustomMinimumSize = new(104, 44); _toggle.SizeFlagsVertical = SizeFlags.ShrinkCenter; _row.AddChild(_toggle);
        _body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _body.AddThemeConstantOverride("separation", 4); _row.AddChild(_body);
        _objective = WrapLabel(); _objective.AddThemeFontSizeOverride("font_size", 13); _objective.MaxLinesVisible = 1; _objective.AddThemeColorOverride("font_color", new Color("efd478")); _body.AddChild(_objective);
        _message = WrapLabel(); _message.AddThemeFontSizeOverride("font_size", 13); _message.MaxLinesVisible = 2; _body.AddChild(_message);
        _action = skin.Button("", () => { if (_hint is { } hint) Navigate?.Invoke(hint.Destination); }); _action.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _action.AddThemeFontSizeOverride("font_size", 14); _action.CustomMinimumSize = new(132, 44); _action.SizeFlagsVertical = SizeFlags.ShrinkCenter; _row.AddChild(_action);
        Hide();
    }
    public void Refresh(MatchSnapshot state, CityState city)
    {
        _hint = OpeningGuidance.Project(state, city); Visible = _hint is not null;
        if (_hint is not { } hint) return;
        _objective.Text = hint.Objective; _message.Text = hint.Message; _message.TooltipText = hint.Message;
        _action.Text = hint.Action; _action.Disabled = city.Eliminated && hint.Destination != OpeningDestination.Outcome;
    }
    public void Layout(Vector2 origin, float width, float available)
    {
        float padding = GetThemeStylebox("panel").GetMinimumSize().X;
        Position = origin; Size = new(_expanded ? width : 104 + padding, 0);
        _body.Visible = _action.Visible = _expanded;
        // Establish the wrap width before deciding whether the optional body fits.
        // Hidden containers have not yet received a native sort at their new width.
        float contentWidth = Mathf.Max(1, width - padding - 104 - 132 - 8);
        _body.Size = new(contentWidth, _body.Size.Y);
        foreach (var child in _body.GetChildren().OfType<Control>()) child.Size = new(contentWidth, child.Size.Y);
        LayoutDetails = $"needed={GetCombinedMinimumSize().Y} available={available} body={_body.Size} lines={_message.GetLineCount()}";
        _effective = _expanded && GetCombinedMinimumSize().Y <= available;
        _body.Visible = _action.Visible = _effective;
        _toggle.Text = _effective ? "Guide ▾" : "Guide ▸";
        _toggle.TooltipText = "Optional guide; collapse to continue without instructions.\nA small window may show only this toggle.";
        Size = new(_effective ? width : 104 + padding, GetCombinedMinimumSize().Y);
        Visible &= Size.Y <= available;
    }
    public override void _ExitTree() { Navigate = null; LayoutRequested = null; }
}
