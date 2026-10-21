using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Whole-card native selection, using canonical run presentation and reward frame styling.</summary>
public partial class DoctrineCard : PanelContainer
{
    public Button ChooseButton { get; private set; } = null!;
    private Label _footer = null!;
    private Viewport? _viewport;
    private Action? _density;
    private bool? _compact;
    public void Initialize(HudSkin skin, RunCardView card, Action choose)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill; CustomMinimumSize = new(0, 420);
        skin.Panel(this, "hud/card_frame");
        var body = new VBoxContainer(); AddChild(Margin(body, 14)); body.AddThemeConstantOverride("separation", 8);
        var title = WrapLabel(card.Title); title.AddThemeFontSizeOverride("font_size", 25); body.AddChild(title);
        var category = WrapLabel(card.Subtitle); category.Modulate = new Color("efd478"); body.AddChild(category);
        var scroll = new ScrollContainer { FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.All, MouseFilter = MouseFilterEnum.Pass }; body.AddChild(scroll);
        var description = WrapLabel(card.Description.Replace("\n\n", "\n")); description.AddThemeFontSizeOverride("font_size", 16);
        description.MouseFilter = MouseFilterEnum.Ignore;
        scroll.AddChild(description);
        _footer = WrapLabel(card.Footer); body.AddChild(_footer);
        ChooseButton = skin.Button("Select doctrine", choose, "ForestPrimaryButton"); ChooseButton.ToggleMode = true; body.AddChild(ChooseButton);
        // Labels and padding forward left clicks to the same selection action.
        GuiInput += input => { if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && !ChooseButton.Disabled) { choose(); AcceptEvent(); } };
        foreach (var label in body.GetChildren().OfType<Label>()) label.MouseFilter = MouseFilterEnum.Ignore;
        void Density()
        {
            bool compact = GetViewport().GetVisibleRect().Size.Y < 900;
            if (_compact == compact) return;
            _compact = compact;
            CustomMinimumSize = new(0, compact ? 420 : 480);
            foreach (var label in body.GetChildren().OfType<Label>()) label.AddThemeFontSizeOverride("font_size", label == title ? (compact ? 23 : 25) : 16);
        }
        _viewport = GetViewport(); _density = Density; _viewport.SizeChanged += Density; Density();
        Refresh(card, false);
    }
    public override void _ExitTree()
    {
        if (_viewport is not null && _density is not null) _viewport.SizeChanged -= _density;
    }
    public void Refresh(RunCardView card, bool selected)
    {
        ChooseButton.Disabled = !card.Enabled;
        ChooseButton.SetPressedNoSignal(selected);
        ChooseButton.Text = selected ? "✓ Selected" : "Select doctrine";
        Modulate = selected ? new Color(1, 1, .83f) : Colors.White;
        _footer.Text = card.Footer;
    }
}
