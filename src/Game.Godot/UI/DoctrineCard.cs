using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Whole-card native selection, using canonical run presentation and reward frame styling.</summary>
public partial class DoctrineCard : PanelContainer
{
    public Button ChooseButton { get; private set; } = null!;
    private Label _footer = null!;
    private Label _numbers = null!;
    private Label _lead = null!;
    private ScrollContainer _scroll = null!;
    internal bool IntroductionFits => _scroll.GetGlobalRect().Encloses(_lead.GetGlobalRect());
    internal Button DetailsButton { get; private set; } = null!;
    internal bool DetailsVisible => _numbers.Visible;
    internal string FullDetails => _numbers.Text + "\n" + _footer.Text;
    internal string Introduction { get; private set; } = "";
    private Viewport? _viewport;
    private Action? _density;
    private bool? _compact;
    public void Initialize(HudSkin skin, RunCardView card, Action choose)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill; CustomMinimumSize = new(0, 420);
        skin.Panel(this, "hud/card_frame");
        var body = new VBoxContainer(); AddChild(Margin(body, 14)); body.AddThemeConstantOverride("separation", 8);
        var title = WrapLabel(card.Title); title.ThemeTypeVariation = "WatchHeading"; body.AddChild(title);
        var category = WrapLabel(card.Subtitle); category.ThemeTypeVariation = "WatchDetail"; category.Modulate = new Color("efd478"); body.AddChild(category);
        var scroll = new ScrollContainer { FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.All, MouseFilter = MouseFilterEnum.Pass }; body.AddChild(scroll);
        _scroll = scroll;
        var copy = DoctrineIntroduction.For(card.Id);
        Introduction = copy.Playstyle + "\n" + copy.Immediate;
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(text);
        var lead = WrapLabel(Introduction); lead.AddThemeFontSizeOverride("font_size", 16); lead.MouseFilter = MouseFilterEnum.Ignore; text.AddChild(lead); _lead = lead;
        _numbers = WrapLabel(card.Description.Replace("\n\n", "\n")); _numbers.AddThemeFontSizeOverride("font_size", 14);
        _numbers.MouseFilter = MouseFilterEnum.Ignore; text.AddChild(_numbers); _numbers.Hide();
        DetailsButton = skin.Button("Full effects & stock ▾", () => { _numbers.Visible = !_numbers.Visible; DetailsButton.Text = _numbers.Visible ? "Full effects & stock ▴" : "Full effects & stock ▾"; _footer.Visible = _numbers.Visible; }, "ForestQuietButton");
        body.AddChild(DetailsButton);
        _footer = WrapLabel(card.Footer); body.AddChild(_footer);
        _footer.Hide();
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
            foreach (var label in body.GetChildren().OfType<Label>()) label.AddThemeFontSizeOverride("font_size", label == title ? 22 : 16);
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
