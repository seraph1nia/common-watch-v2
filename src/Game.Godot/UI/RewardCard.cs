using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class RewardCard : PanelContainer
{
    private Label _footer = null!, _targets = null!, _effects = null!, _description = null!;
    private ScrollContainer _scroll = null!;
    private string _rarity = "";
    public Button ChooseButton { get; private set; } = null!;
    public Button EffectsButton { get; private set; } = null!;
    public string TargetsText => _targets.Text;
    public string EffectsText => _effects.Text;
    public bool EffectsVisible => _effects.Visible;
    internal Rect2 CopyArea => _scroll.GetGlobalRect();
    public void Initialize(HudSkin skin, RunCardView card, Action choose)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill; skin.Panel(this, "hud/card_frame");
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(Margin(content, 8));
        var title = WrapLabel(card.Title); title.AddThemeFontSizeOverride("font_size", 21); content.AddChild(title);
        _rarity = card.Subtitle;
        _targets = WrapLabel(); _targets.AddThemeFontSizeOverride("font_size", 14); _targets.MaxLinesVisible = 2; content.AddChild(_targets);
        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new(0, 64), SizeFlagsVertical = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.All, FollowFocus = true };
        content.AddChild(_scroll);
        var copy = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _scroll.AddChild(copy);
        _description = WrapLabel(BuildImpact.Description(card.Id)); copy.AddChild(_description);
        _effects = WrapLabel(); _effects.AddThemeFontSizeOverride("font_size", 14); _effects.Hide(); copy.AddChild(_effects);
        EffectsButton = skin.Button("Current effects", () =>
        {
            _effects.Visible = !_effects.Visible; _description.Visible = !_effects.Visible; _scroll.ScrollVertical = 0;
            EffectsButton.Text = _effects.Visible ? "Reward description" : "Current effects";
        }); content.AddChild(EffectsButton);
        _footer = WrapLabel(); _footer.AddThemeFontSizeOverride("font_size", 14); content.AddChild(_footer);
        ChooseButton = skin.Button("Choose reward", choose, "ForestPrimaryButton"); ChooseButton.AutowrapMode = TextServer.AutowrapMode.WordSmart; content.AddChild(ChooseButton); Refresh(card);
    }
    public void Present(ChoiceImpact impact)
    {
        _targets.Text = impact.Targets; _effects.Text = impact.Details; _targets.TooltipText = impact.Targets + "\n" + impact.Details;
    }
    public void Refresh(RunCardView card)
    {
        _footer.Text = _rarity + " · " + card.Footer; ChooseButton.Disabled = !card.Enabled;
        ChooseButton.Text = card.Selected ? "Selected — waiting for allies" : card.Enabled ? "Choose reward" : "Unavailable / awaiting authority";
    }
}
