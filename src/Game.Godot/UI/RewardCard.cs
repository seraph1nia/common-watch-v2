using Godot;
using Game.Core;

namespace Game.GodotClient;

/// <summary>Dynamic native card text over the supplied optional nine-slice artwork.</summary>
public partial class RewardCard : PanelContainer
{
    public Button ChooseButton { get; private set; } = null!;
    public void Initialize(HudSkin skin, CityState city, string id, bool connected, Action choose)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        skin.Panel(this, "hud/card_frame");
        var card = RunCatalog.Card(id);
        var content = new VBoxContainer(); AddChild(Margin(content, 36));
        content.AddChild(WrapLabel(card.Name)); content.AddChild(WrapLabel(card.Rarity.ToString()));
        content.AddChild(WrapLabel(card.Effect + (card.Multiplied ? $"\nResources multiplied by {RunEffects.Multiplier(city.Run.DraftWave)}." : "")));
        ChooseButton = skin.Button("Choose reward", choose); ChooseButton.Disabled = !connected;
        content.AddChild(ChooseButton);
    }
}
