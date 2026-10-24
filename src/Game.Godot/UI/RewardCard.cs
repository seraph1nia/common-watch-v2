using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class RewardCard : PanelContainer
{
    private Label _primary = null!, _qualifier = null!, _footer = null!;
    public Button ChooseButton { get; private set; } = null!;
    public string TargetsText => _primary.Text;
    public string EffectsText { get; private set; } = "";
    internal Rect2 CopyArea => _primary.GetGlobalRect();
    public void Initialize(HudSkin skin, RunCardView card, Action choose)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill; skin.Panel(this, "hud/card_frame");
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(Margin(content, 14));
        var title = WrapLabel(card.Title); title.ThemeTypeVariation = "WatchHeading"; content.AddChild(title);
        _primary = WrapLabel(); _primary.AddThemeFontSizeOverride("font_size", 20); content.AddChild(_primary);
        _qualifier = WrapLabel(); _qualifier.AddThemeFontSizeOverride("font_size", 14); content.AddChild(_qualifier);
        content.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        _footer = WrapLabel(); _footer.ThemeTypeVariation = "WatchDetail"; content.AddChild(_footer);
        ChooseButton = skin.Button("Choose reward", choose, "ForestPrimaryButton");
        ChooseButton.AutowrapMode = TextServer.AutowrapMode.WordSmart; content.AddChild(ChooseButton); Refresh(card);
    }
    public void Present(ChoiceImpact impact)
    {
        EffectsText = impact.Details; TooltipText = impact.Details;
    }
    public void Refresh(RunCardView card)
    {
        _primary.Text = card.Description; _qualifier.Text = card.Subtitle; _footer.Text = card.Footer;
        _qualifier.Visible = card.Subtitle.Length > 0; _footer.Visible = card.Footer.Length > 0;
        ChooseButton.Disabled = !card.Enabled;
        ChooseButton.Text = card.Selected ? "Accepted" : card.Enabled ? "Choose reward" : "Awaiting authority";
    }
}
