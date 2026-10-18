using Godot;
using Game.Core;

namespace Game.GodotClient;

public partial class DraftPanel : Control
{
    private HudSkin _skin = null!;
    private Action _submitted = null!;
    private HBoxContainer _cards = null!;
    private Label _summary = null!;
    private GodotSession _session = null!;
    private string _shape = "", _draftKey = "";
    private bool _dismissed;
    public void Dismiss() { _dismissed = true; Hide(); }
    public void Open() { _dismissed = false; }
    public void Reset() { _draftKey = ""; _dismissed = false; }
    private readonly Dictionary<string, RewardCard> _views = [];
    public IReadOnlyList<Button> RewardButtons => _views.Values.Select(v => v.ChooseButton).ToArray();
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _submitted = submitted; SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .7f) }; AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = new PanelContainer(); AddChild(panel); panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panel.OffsetLeft = 60; panel.OffsetRight = -60; panel.OffsetTop = 50; panel.OffsetBottom = -60; skin.Panel(panel);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; panel.AddChild(Margin(scroll, 20));
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(column);
        column.AddChild(skin.Button("Close rewards — inspect your city / run before choosing", Dismiss));
        _summary = WrapLabel(); column.AddChild(_summary);
        _cards = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; column.AddChild(_cards); Hide();
    }
    public void Refresh(GodotSession session, bool finishingCombat)
    {
        _session = session;
        string draftKey = session.Snapshot.MatchId + ":" + session.City.Run.DraftWave;
        if (_draftKey != draftKey) { _draftKey = draftKey; _dismissed = false; }
        Visible = !_dismissed && session.Snapshot.Phase == Phase.RewardDraft && !finishingCombat && !session.City.Eliminated;
        if (!Visible) return;
        var view = RunPresentation.Project(session.Snapshot, session.City, session.PlayerId, session.Connected && !session.Pending, false);
        _summary.Text = view.Title + "\n" + view.Status + "\n" + view.Forecast;
        string shape = session.Snapshot.MatchId + ":" + session.City.Run.DraftWave + ":" + string.Join(',', view.Cards.Select(c => c.Id));
        if (_shape != shape)
        {
            _shape = shape; foreach (Node child in _cards.GetChildren()) { _cards.RemoveChild(child); child.QueueFree(); } _views.Clear();
            foreach (var card in view.Cards)
            {
                string id = card.Id; var control = new RewardCard(); _cards.AddChild(control);
                control.Initialize(_skin, card, () => { if (_session.Connected && !_session.Pending && _session.City.Run.Choice is null) _session.ChooseReward(id); _submitted(); });
                _views.Add(id, control);
            }
        }
        foreach (var card in view.Cards) _views[card.Id].Refresh(card);
    }
}
