using Godot;
using Game.Core;

namespace Game.GodotClient;

public partial class DraftPanel : PanelContainer
{
    private HudSkin _skin = null!;
    private Action _submitted = null!;
    private HBoxContainer _cards = null!;
    private readonly List<Button> _buttons = [];
    public IReadOnlyList<Button> RewardButtons => _buttons;
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _submitted = submitted;
        SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        OffsetLeft = -560; OffsetRight = 560; OffsetTop = -210; OffsetBottom = 210;
        _skin.Panel(this);
        var column = new VBoxContainer(); AddChild(Margin(column, 20));
        column.AddChild(new Label { Text = "WAVE COMPLETE — CHOOSE ONE REWARD" });
        _cards = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; column.AddChild(_cards);
        Hide();
    }
    public void Refresh(GodotSession session, bool finishingCombat)
    {
        Visible = session.Snapshot.Phase == Phase.RewardDraft && !finishingCombat;
        foreach (Node child in _cards.GetChildren()) { _cards.RemoveChild(child); child.QueueFree(); }
        _buttons.Clear();
        if (!Visible) return;
        foreach (string id in session.City.Run.Offers)
        {
            var card = new RewardCard();
            _cards.AddChild(card);
            card.Initialize(_skin, session.City, id, session.Connected, () => { session.ChooseReward(id); _submitted(); });
            _buttons.Add(card.ChooseButton);
        }
    }
}
