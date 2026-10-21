using Godot;
using Game.Core;

namespace Game.GodotClient;

public partial class RewardCard : PanelContainer
{
    private Label _footer = null!;
    public Button ChooseButton { get; private set; } = null!;
    public void Initialize(HudSkin skin, RunCardView card, Action choose)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill; skin.Panel(this, "hud/card_frame");
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(Margin(content, 16));
        var title = WrapLabel(card.Title); title.AddThemeFontSizeOverride("font_size", 22); content.AddChild(title);
        var category = WrapLabel(card.Subtitle); category.AddThemeColorOverride("font_color", new Color("efd478")); content.AddChild(category);
        content.AddChild(WrapLabel(card.Description));
        content.AddChild(new Control { CustomMinimumSize = new(0, 12), SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        _footer = WrapLabel(); content.AddChild(_footer);
        ChooseButton = skin.Button("Choose reward", choose, "ForestPrimaryButton"); ChooseButton.AutowrapMode = TextServer.AutowrapMode.WordSmart; content.AddChild(ChooseButton); Refresh(card);
    }
    public void Refresh(RunCardView card)
    {
        _footer.Text = card.Footer; ChooseButton.Disabled = !card.Enabled;
        ChooseButton.Text = card.Selected ? "Selected — waiting for allies" : card.Enabled ? "Choose reward" : "Unavailable / awaiting authority";
    }
}
